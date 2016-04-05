using System;
using System.Collections.Generic;
using System.Linq;
using System.CodeDom.Compiler;
using System.Collections;
using Rex.Utilities.Helpers;
using System.Diagnostics;

namespace Rex.Utilities
{
	public static class RexHelper
	{
		public class Varible
		{
			public object VarValue { get; set; }
			public Type VarType { get; set; }
		}
		public static readonly Dictionary<string, Varible> Variables = new Dictionary<string, Varible>();
		public static IEnumerable<AConsoleOutput> Output { get { return outputList; } }

		private static readonly LinkedList<AConsoleOutput> outputList = new LinkedList<AConsoleOutput>();
		const int OUTPUT_LENGHT = 20;

		public static readonly Dictionary<MsgType, List<string>> Messages = new Dictionary<MsgType, List<string>>
		{
			{ MsgType.None, new List<string>()},
			{ MsgType.Info, new List<string>()},
			{ MsgType.Warning, new List<string>()},
			{ MsgType.Error, new List<string>()}
		};

		#region Execute
		public static T Execute<T>(CompiledExpression compileResult, bool showMessages = true)
			where T : AConsoleOutput, new()
		{
			try
			{
				var val = Invoke(compileResult);

				// If this is a variable declaration
				if (compileResult.Parse.IsDeclaring)
				{
					DeclaringVariable(compileResult.Parse.Variable, val, showMessages);
				}

				// If expression is void
				if (compileResult.FuncType == FuncType._void)
				{
					var outp = new T();
					outp.LoadInDetails(null, "Expression successfully executed.", Enumerable.Empty<MemberDetails>());
					return outp;
				}

				// If expression is null
				if (val == null)
				{
					var outp = new T();
					outp.LoadInDetails(null, "null", Enumerable.Empty<MemberDetails>());
					return outp;
				}

				// Get the type of the variable
				var type = val.GetType();
				var message = val.ToString();
				if (!RexUtils.IsToStringOverride(type))
				{
					message = RexUtils.GetCSharpRepresentation(type, true).ToString();
				}

				var output = new T();
				output.LoadInDetails(val, message, RexReflectionUtils.ExtractDetails(val));
				if (!(val is string || val is Enum) && val is IEnumerable)
				{
					foreach (object o in (val as IEnumerable))
					{
						var member = new T();
						var msg = o == null ? "null" : o.ToString();
						member.LoadInDetails(o, msg, RexReflectionUtils.ExtractDetails(o));
						output.Members.Add(member);
					}
				}
				return output;
			}
			catch (Exception ex)
			{
				var exception = ex.InnerException == null ? ex : ex.InnerException;

				if (exception is AccessingDeletedVariableException)
				{
					var deletedVar = exception as AccessingDeletedVariableException;
					var msg = "Variable " + deletedVar.VarName + " has been deleted, but is still being accessed";
					if (showMessages) Messages[MsgType.Warning].Add(msg);
					return new T
					{
						Message = msg,
						Exception = deletedVar
					};
				}

				if (showMessages) Messages[MsgType.Error].Add(exception.ToString());
				return new T { Exception = exception };
			}
		}

		private static object Invoke(CompiledExpression compileResult)
		{
			object val = null;
			if (compileResult.HasInitialized)
			{
				if (compileResult.FuncType == FuncType._object)
				{
					val = compileResult.InitializedFunction();
				}
				else
				{
					compileResult.InitializedAction();
				}
			}
			else
			{
				if (compileResult.FuncType == FuncType._object)
				{
					compileResult.InitializedFunction = RexUtils.ExecuteAssembly<Func<object>>(compileResult.Assembly);
					val = compileResult.InitializedFunction();
				}
				else
				{
					compileResult.InitializedAction = RexUtils.ExecuteAssembly<Action>(compileResult.Assembly);
					compileResult.InitializedAction();
				}
			}

			return val;
		}

		/// <summary>
		/// Handles a variable declaration.
		/// </summary>
		/// <param name="varName">Name of the variable</param>
		/// <param name="val">Value of the variable</param>
		/// <param name="showMessages">Should show an warning message or not</param>
		private static void DeclaringVariable(string varName, object val, bool showMessages)
		{
			var warning = string.Empty;
			if (val != null)
			{
				var valType = val.GetType();

				if (RexReflectionUtils.ContainsAnonymousType(valType))
				{
					warning = string.Format("Cannot declare a variable '{0}' with anonymous type", varName);
				}
				else
				{
					if (!valType.IsVisible)
					{
						var interfaces = valType.GetInterfaces();
						var iEnumerable = interfaces.FirstOrDefault(t => t.IsGenericType && t.GetInterface("IEnumerable") != null);
						if (iEnumerable != null)
						{
							Variables[varName] = new Varible { VarValue = val, VarType = iEnumerable };
							return;
						}
						warning = string.Format("Expression returned a compiler generated class. Could not declare variable '{0}'", varName);
					}
					else
					{
						Variables[varName] = new Varible { VarValue = val, VarType = valType };
						return;
					}
				}
			}
			else
			{
				warning = string.Format("Expression returned null. Could not declare variable '{0}'", varName);
			}
			if (showMessages) Messages[MsgType.Warning].Add(warning);

		}

		/// <summary>
		/// Outputs errors if there are any. returns true if there are none.
		/// </summary>
		/// <param name="result">todo: describe result parameter on DealWithErrors</param>
		public static List<string> DealWithErrors(CompilerResults result)
		{
			var errorList = new List<string>();
			foreach (CompilerError error in result.Errors)
			{
				if (!error.IsWarning)
				{
					if (error.ErrorText.StartsWith("Cannot implicitly convert type"))
						continue;

					if (error.ErrorText.StartsWith("Only assignment, call, increment, decrement, and new object expressions") &&
						Messages[MsgType.Error].Count > 0)
						continue;

					if (error.ErrorText.Trim().EndsWith("(Location of the symbol related to previous error)") &&
						Messages[MsgType.Error].Count > 0)
						continue;

					if (!Messages[MsgType.Error].Contains(error.ErrorText))
						errorList.Add(error.ErrorText);
				}
			}
			return errorList;
		}
		#endregion

		public static void RemoveOutput(AConsoleOutput deleted)
		{
			outputList.Remove(deleted);
		}

		public static void AddOutput(AConsoleOutput output)
		{
			if (output == null)
				return;
			foreach (var item in outputList)
			{
				item.ShowDetails = false;
				item.ShowMembers = false;
			}
			output.ShowDetails = true;
			output.ShowMembers = true;
			if (outputList.Count >= OUTPUT_LENGHT)
			{
				outputList.RemoveLast();
			}
			outputList.AddFirst(output);
		}

		public static void ClearOutput()
		{
			outputList.Clear();
		}

		private class DummyOutput : AConsoleOutput
		{
			public DummyOutput() : base() { }

			public override void Display()
			{
				throw new NotImplementedException();
			}

			public override void LoadInDetails(object value, string message, IEnumerable<MemberDetails> details)
			{ }
		}
	}
}
