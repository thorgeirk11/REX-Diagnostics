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
using UnityEditor;
using UnityEngine;

[Serializable]
public class RexCompileEngine : ScriptableObject, IDisposable
{
	public const string REX_CLASS_NAME = "__TempRexClass";
	public const string REX_FUNC_NAME = "__REX_Func";

	public const float TIME_OUT_FOR_COMPILE_SEC = 2;

	private static RexCompileEngine _instance = null;

	public static RexCompileEngine Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = CreateInstance<RexCompileEngine>();
			}
			return _instance;
		}
	}

	[SerializeField]
	private long _compileThreadID = -1;

	public List<NameSpaceInfo> NamespaceInfos;

	/// <summary>
	/// The continues compiled result by the <see cref="_compileThread"/>.
	/// </summary>
	private static CompiledExpression _currentCompiledExpression;
	public static string CurrentCodeToCompile = string.Empty;

	private static Dictionary<string, RexHelper.Variable> _currentWrapperVaribles = new Dictionary<string, RexHelper.Variable>();

	private static string _wrapperVariables = string.Empty;
	private static IRexParser parser = new RexParser();

	public void OnEnable()
	{
		hideFlags = HideFlags.HideAndDontSave;

		if (_instance == null)
		{
			_instance = this;
		}
		else if (_instance != this)
		{
			Destroy(this);
			return;
		}

		if (NamespaceInfos == null)
			NamespaceInfos = RexUtils.LoadNamespaceInfos(false);

		var _compileThread = new Thread(CompilerMainThread)
		{
			IsBackground = true,
			Name = "REX Compiler thread"
		};
		_compileThread.Start();
		_compileThreadID = _compileThread.ManagedThreadId;
	}

	public void Dispose()
	{
		Interlocked.Exchange(ref _compileThreadID, -1);
		DestroyImmediate(this);
	}

	private void CompilerMainThread()
	{
		var activeThreads = new List<Thread>();
		Thread lastThread = null;
		while (Interlocked.Read(ref _compileThreadID) == Thread.CurrentThread.ManagedThreadId)
		{
			Thread.Sleep(500);
			var code = RexISM.Code;
			if (!string.IsNullOrEmpty(code) &&
				CurrentCodeToCompile != code)
			{
				Interlocked.Exchange(ref CurrentCodeToCompile, code);
				lastThread = new Thread(CompileCodeInThread)
				{
					IsBackground = true
				};
				lastThread.Start(code);

                foreach (var thread in activeThreads)
                {
                    thread.Abort();
                }
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
	private static void CompileCodeInThread(object code)
	{
		var c = (string)code;
		var parseResult = parser.ParseAssignment(c);
		if (CurrentCodeToCompile != c) return;

		var result = Compile(parseResult);
		if (CurrentCodeToCompile != c) return;

		Interlocked.Exchange(ref _currentCompiledExpression, result);
	}

	public CompiledExpression GetCompileAsync(string code, out Dictionary<MessageType, List<string>> messages)
	{
		messages = new Dictionary<MessageType, List<string>>();
		var startedWaiting = DateTime.Now;
		while (true)
		{
			var expression = _currentCompiledExpression;
			if (expression != null &&
				expression.Parse.WholeCode == code)
			{
				if (expression.Errors.Any())
				{
					messages.Add(MessageType.Error, expression.Errors.ToArray());
					return null;
				}

				return expression;
			}
			Thread.Sleep(10);
			if (DateTime.Now - startedWaiting > TimeSpan.FromSeconds(TIME_OUT_FOR_COMPILE_SEC))
			{
				messages.Add(MessageType.Error, "Time out on compiling expression, " + code);
				return null;
			}
		}
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

	private static CompilerResults CompileCode(string code)
	{
		var compilerOptions = new CompilerParameters(GetCurrentAssemblies())
		{
			GenerateExecutable = false,
			GenerateInMemory = false
		};

		return Compiler.CompileAssemblyFromSource(compilerOptions, code);
	}

	private static string[] GetCurrentAssemblies()
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
		var errors = Enumerable.Empty<string>();
		var returnTypes = new[] { FuncType._object, FuncType._void };
		foreach (var returnType in returnTypes)
		{
			var wrapper = MakeWrapper(Instance.NamespaceInfos.Where(ns => ns.Selected), parseResult, returnType);
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
					var info = Instance.NamespaceInfos.First(i => i.Name == name);
					if (!info.Selected)
					{
						var usings = Instance.NamespaceInfos.Where(ns => ns.Selected || ns.Name == name);
						wrapper = MakeWrapper(usings, parseResult, returnType);
						result = CompileCode(wrapper);
					}
				}
			}

			errors = RexHelper.DealWithErrors(result);
			if (!errors.Any())
			{
				return new CompiledExpression
				{
					Assembly = result.CompiledAssembly,
					Parse = parseResult,
					FuncType = returnType,
					Errors = errors
				};
			}
		}
		return new CompiledExpression
		{
			Parse = parseResult,
			Errors = errors
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
		wrapper.AppendLine(REX_CLASS_NAME);
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
		}}", REX_FUNC_NAME, returnstring);
	}

	private static string FuncBodyWrapper(string returnstring)
	{
		return string.Format(@"
		public Func<object> {0}() 
		{{ 
			return new Func<object>(() => {1});
		}}", REX_FUNC_NAME, returnstring);
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
