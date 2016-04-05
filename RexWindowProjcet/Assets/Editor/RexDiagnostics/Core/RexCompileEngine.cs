using Microsoft.CSharp;
using Rex.Utilities;
using Rex.Utilities.Helpers;
using Rex.Utilities.Input;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

[Serializable]
public class RexCompileEngine : ScriptableObject, IDisposable
{
	public const float TIME_OUT_FOR_COMPILE_SEC = 2;

	[SerializeField]
	private volatile int _compileThreadID = -1;

	/// <summary>
	/// The continues compiled result by the <see cref="_compileThread"/>.
	/// </summary>
	private static volatile CompiledExpression _currentCompiledExpression;
	public static readonly object CompilerLockObject = new object();

	private static Dictionary<string, RexHelper.Varible> _currentWrapperVaribles = new Dictionary<string, RexHelper.Varible>();

	private static string _wrapperVariables = string.Empty;
	private static IRexParser parser = new RexParser();

	public void OnEnable()
	{
		hideFlags = HideFlags.HideAndDontSave;

		var _compileThread = new Thread(CompilerMainThread);
		_compileThread.Start();
		_compileThread.Name = "REX Compiler thread";
		_compileThreadID = _compileThread.ManagedThreadId;
	}

	public void Dispose()
	{
		_compileThreadID = -1;
		DestroyImmediate(this);
	}

	private void CompilerMainThread()
	{
		var lastCode = "";
		CompileJob lastJob = null;
		Thread lastThread = null;
		var activeThreads = new List<Thread>();
		while (_compileThreadID == Thread.CurrentThread.ManagedThreadId)
		{
			activeThreads.RemoveAll(i => (i.ThreadState & ThreadState.Stopped) != 0);
			Thread.Sleep(1);
			if (!string.IsNullOrEmpty(RexISM.Code) &&
				lastCode != RexISM.Code)
			{
				lastCode = RexISM.Code;
				if (lastJob != null)
				{
					lastJob.RequestStop();
					lastThread.Abort();
				}
				lastJob = new CompileJob();
				lastThread = new Thread(lastJob.CompileCode);
				lastThread.Start(lastCode);
				activeThreads.Add(lastThread);
			}
		}
		if (activeThreads.Count > 0)
		{
			foreach (var thread in activeThreads)
			{
				thread.Join();
			}
		}
	}

	public CompiledExpression GetCompile(string code)
	{
		var startedWaiting = DateTime.Now;
		while (true)
		{
			lock (CompilerLockObject)
			{
				if (_currentCompiledExpression != null &&
					_currentCompiledExpression.Parse.WholeCode == code)
				{
					if (_currentCompiledExpression.Errors.Count > 0)
					{
						RexHelper.Messages[MsgType.Error].AddRange(_currentCompiledExpression.Errors);
						return null;
					}

					return _currentCompiledExpression;
				}
			}
			Thread.Sleep(10);
			if (DateTime.Now - startedWaiting > TimeSpan.FromSeconds(TIME_OUT_FOR_COMPILE_SEC))
			{
				RexHelper.Messages[MsgType.Error].Add("Time out on compiling expression, " + code);
				return null;
			}
		}
	}

	private class CompileJob
	{
		public void CompileCode(object code)
		{
			var parseResult = parser.ParseAssigment((string)code);
			var result = Compile(parseResult);
			if (!_shouldStop)
			{
				lock (CompilerLockObject)
				{
					_currentCompiledExpression = result;
				}
			}
		}

		public void RequestStop()
		{
			_shouldStop = true;
		}

		private volatile bool _shouldStop;
	}

	#region Compile
	public static CSharpCodeProvider Compiler
	{
		get
		{
			if (compiler == null)
			{
				var providerOptions = new Dictionary<string, string>();
				providerOptions.Add("CompilerVersion", "v4.0");
				compiler = new CSharpCodeProvider(providerOptions);
			}
			return compiler;
		}
	}

	private static CSharpCodeProvider compiler;
	private static string[] currentAssemblies;

	public static CompilerResults CompileCode(string code)
	{
		var compilerOptions = new CompilerParameters(GetCurrentAssemblies())
		{
			GenerateExecutable = false,
			GenerateInMemory = false
		};

		return Compiler.CompileAssemblyFromSource(compilerOptions, code);
	}

	static string[] GetCurrentAssemblies()
	{
		if (currentAssemblies != null)
			return currentAssemblies;

		var assemblies = new List<string>();
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			try
			{
				var location = assembly.Location;
				if (!string.IsNullOrEmpty(location))
				{
					assemblies.Add(location);
				}
			}
			catch (NotSupportedException)
			{
				// this happens for dynamic assemblies, so just ignore it. 
			}
		}
		currentAssemblies = assemblies.ToArray();
		return currentAssemblies;
	}

	public static CompiledExpression Compile(ParseResult parseResult)
	{
		var Errors = new List<string>();
		var returnTypes = new[] { FuncType._object, FuncType._void };
		foreach (var returnType in returnTypes)
		{
			var wrapper = MakeWrapper(RexUtils.NamespaceInfos.Where(ns => ns.Selected), parseResult, returnType);
			var result = CompileCode(wrapper);
			//Path.
			if (result.Errors.Count == 1)
			{
				var error = result.Errors[0];
				if (error.ErrorNumber == "CS0103")
				{
					var errorRegex = Regex.Match(error.ErrorText, "The name (?<type>.*) does not exist in the current context");
					var typeNotFound = errorRegex.Groups["type"].Value.Substring(1).Trim('\'');

					var canditateTypes = (from t in RexUtils.AllVisibleTypes
										  where t.Name == typeNotFound
										  select t).ToArray();
					if (canditateTypes.Length == 0)
					{
						return new CompiledExpression
						{
							Parse = parseResult,
							Errors = new List<string> { result.Errors.Cast<CompilerError>().First().ErrorText }
						};
					}
					if (canditateTypes.Length > 1)
					{
						var types = from n in canditateTypes
									select RexUtils.GetCSharpRepresentation(n, true).ToString();
						var allTypeNames = string.Join(Environment.NewLine, types.ToArray());
						return new CompiledExpression
						{
							Parse = parseResult,
							Errors = new List<string> {
									string.Format("Ambiguous type name '{1}': {0} {2} {0} {3}", Environment.NewLine,
										typeNotFound,
										allTypeNames,
										"Use the Scope settings to select which namespace to use.")
								}
						};
					}
					var name = canditateTypes.First().Namespace;
					var info = RexUtils.NamespaceInfos.First(i => i.Name == name);
					if (!info.Selected)
					{
						var usings = RexUtils.NamespaceInfos.Where(ns => ns.Selected || ns.Name == name);
						wrapper = MakeWrapper(usings, parseResult, returnType);
						result = CompileCode(wrapper);
					}
				}
			}

			Errors = RexHelper.DealWithErrors(result);
			if (Errors.Count == 0)
			{
				return new CompiledExpression
				{
					Assembly = result.CompiledAssembly,
					Parse = parseResult,
					FuncType = returnType,
					Errors = Errors
				};
			}
		}
		return new CompiledExpression
		{
			Parse = parseResult,
			Errors = Errors
		};
	}


	public static string MakeWrapper(IEnumerable<NameSpaceInfo> usedNameSpace, ParseResult parseResult, FuncType returnType)
	{
		var variables = GetVaribleWrapper();

		var returnstring = parseResult.ExpressionString;
		if (!string.IsNullOrEmpty(parseResult.TypeString))
			returnstring = string.Format("({0})({1})", parseResult.TypeString, parseResult.ExpressionString);

		var wrapper = new StringBuilder();
		var usings = usedNameSpace.Select(ns => string.Format("using {0};", ns.Name));
		wrapper.AppendLine(string.Join(Environment.NewLine, usings.ToArray()));
		wrapper.Append("class ");
		wrapper.AppendLine(RexUtils.className);
		wrapper.AppendLine("{");
		wrapper.AppendLine(variables);
		if (parseResult.IsDeclaring || returnType == FuncType._object)
		{
			wrapper.AppendLine(FuncBodyWrapper(returnstring));
		}
		else
		{
			wrapper.AppendLine(ActionBodyWrapper(returnstring));
		}
		wrapper.AppendLine("}");
		return wrapper.ToString();
	}

	private static string ActionBodyWrapper(string returnstring)
	{
		return string.Format(@"
		public Action {0}() 
		{{ 
			return new Action(() => {1});
		}}", RexUtils.FuncName, returnstring);
	}

	private static string FuncBodyWrapper(string returnstring)
	{
		return string.Format(@"
		public Func<object> {0}() 
		{{ 
			return new Func<object>(() => {1});
		}}", RexUtils.FuncName, returnstring);
	}

	private static string GetVaribleWrapper()
	{
		if (RexHelper.Variables.SequenceEqual(_currentWrapperVaribles))
			return _wrapperVariables;

		_wrapperVariables = RexHelper.Variables.Aggregate("", (codeString, var) =>
			 codeString + Environment.NewLine +
			 string.Format(@"    {0} {1} 
		{{
			get
			{{
				if (!Rex.Utilities.RexHelper.Variables.ContainsKey(""{1}""))
					throw new Rex.Utilities.Helpers.AccessingDeletedVariableException() {{ VarName = ""{1}"" }};
				return ({0})Rex.Utilities.RexHelper.Variables[""{1}""].VarValue;
			}}
			set {{ Rex.Utilities.RexHelper.Variables[""{1}""].VarValue = value; }}
		}}",
		  RexUtils.GetCSharpRepresentation(var.Value.VarType, true).ToString(), var.Key));
		_currentWrapperVaribles = RexHelper.Variables.ToDictionary(i => i.Key, i => i.Value);
		return _wrapperVariables;
	}
	#endregion

}
