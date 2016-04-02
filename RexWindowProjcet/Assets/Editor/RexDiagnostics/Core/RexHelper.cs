using System;
using System.Collections.Generic;
using System.Linq;
using System.CodeDom.Compiler;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Rex.Utilities.Helpers;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Rex.Utilities.Input;

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

		private static IEnumerable<string> _currentWrapperVaribles = new string[0];
		private static string _wrapperVariables = string.Empty;

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
				output.LoadInDetails(val, message, RexReflectionHelper.ExtractDetails(val));
				if (!(val is string || val is Enum) && val is IEnumerable)
				{
					foreach (object o in (val as IEnumerable))
					{
						var member = new T();
						var msg = o == null ? "null" : o.ToString();
						member.LoadInDetails(o, msg, RexReflectionHelper.ExtractDetails(o));
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

				if (ContainsAnonymousType(valType))
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

		public static bool ContainsAnonymousType(Type valType)
		{
			if (RexReflectionHelper.IsAnonymousType(valType))
				return true;

			if (valType.IsGenericType)
			{
				foreach (var genericType in valType.GetGenericArguments())
				{
					if (ContainsAnonymousType(genericType)) return true;
				}
			}
			if (valType.IsArray)
			{
				if (ContainsAnonymousType(valType.GetElementType()))
				{
					return true;
				}
			}
			return false;
		}
		#endregion
		#region Compile
		public static CompiledExpression Compile(ParseResult parseResult)
		{
			var Errors = new List<string>();
			var returnTypes = new[] { FuncType._object, FuncType._void };
			foreach (var returnType in returnTypes)
			{
				var wrapper = MakeWrapper(RexUtils.NamespaceInfos.Where(ns => ns.Selected), parseResult, returnType);
				var result = RexUtils.CompileCode(wrapper);
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
								Errors = Errors
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
							result = RexUtils.CompileCode(wrapper);
						}
					}
				}

				Errors = DealWithErrors(result);
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

		/// <summary>
		/// Outputs errors if there are any. returns true if there are none.
		/// </summary>
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
			if (Variables.Keys.SequenceEqual(_currentWrapperVaribles))
				return _wrapperVariables;

			_wrapperVariables = Variables.Aggregate("", (codeString, var) =>
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
			_currentWrapperVaribles = Variables.Keys.ToArray();
			return _wrapperVariables;
		}
		#endregion

		#region Parse
		public static ParseResult ParseAssigment(string parsedCode)
		{
			var match = RexUtils.Assignment.Match(parsedCode);
			if (match.Success)
			{
				var type = match.Groups["type"].Value.Trim();
				var variable = match.Groups["var"].Value.Trim();
				var expr = match.Groups["expr"].Value.Trim();

				if (type == "var")
				{
					type = string.Empty;
				}
				bool validIdentifier;
				try
				{
					validIdentifier = RexUtils.Compiler.IsValidIdentifier(variable);
				}
				catch (Exception)
				{
					validIdentifier = false;
				}
				if (validIdentifier)
				{
					return new ParseResult
					{
						Variable = variable,
						ExpressionString = expr,
						TypeString = type,
						IsDeclaring = true,
						WholeCode = parsedCode
					};
				}
			}

			return new ParseResult
			{
				ExpressionString = parsedCode,
				IsDeclaring = false,
				WholeCode = parsedCode
			};
		}

		///// <summary>
		///// Figures out which namepaces the expressions uses.
		///// </summary>
		///// <param name="parse">Parse of the expression</param>
		///// <returns></returns>
		////public static IEnumerable<NameSpaceInfo> FigureOutNamespaces(ParseResult parse)
		////{

		////((\w+\.\w+)|(\<[^>]*\>)|new ((\w+\.)*\w+)) Some Ideas...

		////}

		public static IEnumerable<CodeCompletion> Intellisence(string code)
		{
			var parse = ParseAssigment(code);
			var offset = parse.WholeCode.IndexOf(parse.ExpressionString);
			if (RexUtils.DotExpressionSearch.IsMatch(parse.ExpressionString))
				return DotExpression(RexUtils.DotExpressionSearch.Match(parse.ExpressionString), offset);

			Type endType = null;
			while (RexUtils.DotAfterMethodRegex.IsMatch(code))
			{
				var match = RexUtils.DotAfterMethodRegex.Match(code);
				var possibleMethods = PossibleMethods(match);
				if (possibleMethods.Count() == 1)
				{
					var method = possibleMethods.First();
					endType = method.ReturnType;
					code = code.Substring(match.Length);
					offset += match.Length;
				}
				else
				{
					return Enumerable.Empty<CodeCompletion>();
				}
			}


			//Math.PI.ToString().Trim(',', String.Empty.Length.To)

			var paramatch = RexUtils.ParameterRegex.Match(parse.ExpressionString);
			if (paramatch.Success)
			{
				var methodInfo = MethodsOverload(paramatch, endType);

				var para = paramatch.Groups["para"];
				offset += para.Index;

				var cutIndex = Math.Max(para.Value.LastIndexOf(','), para.Value.LastIndexOf('('));
				var paraVal = para.Value;
				if (cutIndex > 0)
				{
					offset += cutIndex + 1;
					paraVal = paraVal.Substring(cutIndex + 1);
				}

				var prevLength = paraVal.Length;
				paraVal = paraVal.TrimStart();
				offset += prevLength - paraVal.Length;

				var match = RexUtils.DotExpressionSearch.Match(paraVal);
				if (match.Success)
					return methodInfo.Concat(DotExpression(match, offset));
				else
					return methodInfo;
			}

			if (endType != null)
			{
				var methodSearch = RexUtils.DotExpressionSearch.Match(code);
				if (methodSearch.Success)
				{
					var full = methodSearch.Groups["fullType"];
					var search = methodSearch.Groups["search"];
					if (string.IsNullOrEmpty(full.Value))
					{
						return ExtractMemberInfo(search, endType, RexUtils.InstanceBindings, offset);
					}
					else
					{
						endType = GetLastIndexType(full, endType, 0);
						if (endType != null)
							return ExtractMemberInfo(search, endType, RexUtils.InstanceBindings, offset);
					}
				}
			}
			return Enumerable.Empty<CodeCompletion>();
		}

		private static IEnumerable<CodeCompletion> MethodsOverload(Match paramatch, Type endType)
		{
			var methodInfo = Enumerable.Empty<CodeCompletion>();
			if (endType == null)
			{
				methodInfo = DotExpression(paramatch, 0);
			}
			else
			{
				var type = GetLastIndexType(paramatch, endType, 0);
				var methodName = paramatch.Groups["search"];
				if (type != null)
					methodInfo = ExtractMemberInfo(methodName, type, RexUtils.InstanceBindings, 0);
			}
			foreach (var info in methodInfo)
			{
				info.IsMethodOverload = true;
				yield return info;
			}
		}

		public static IEnumerable<MethodInfo> PossibleMethods(Match match)
		{
			var fullType = match.Groups["fullType"];

			string name;
			Type type;
			if (!TypeOfFirst(fullType, out type, out name))
			{
				return Enumerable.Empty<MethodInfo>();
			}

			GetLastIndexType(fullType, type);

			if (type == null)
			{
				return Enumerable.Empty<MethodInfo>();
			}

			var methodParams = match.Groups["params"].Value;
			var paraCount = 0;
			if (!string.IsNullOrEmpty(methodParams.Trim()))
			{
				while (methodParams.Contains("(") && methodParams.Contains(")"))
				{
					var index = methodParams.LastIndexOf('(');
					var length = index - methodParams.Substring(index).IndexOf(')');
					methodParams = methodParams.Substring(index, length);
				}
				paraCount = methodParams.Count(i => i == ',') + 1;
			}

			var methodName = match.Groups["method"].Value;

			//UTILS ExTENTIONS METHODS
			//var extensionMethods = Utils.GetExtensionMethods(type);
			var possibleMethods = from meth in type.GetMethods()/*.Concat(extensionMethods)*/
								  where meth.Name == methodName &&
								  meth.GetParameters().Length == paraCount
								  select meth;
			return possibleMethods;
		}

		private static IEnumerable<CodeCompletion> DotExpression(Match match, int offset)
		{
			var full = match.Groups["fullType"];
			var first = match.Groups["firstType"];
			var search = match.Groups["search"];

			//Only search..
			if (string.IsNullOrEmpty(first.Value) &&
				!string.IsNullOrEmpty(search.Value) &&
				search.Length > 2)
			{
				return SearchWithoutType(search, offset);
			}

			string name;
			Type type;
			TypeOfFirst(full, out type, out name);

			type = GetLastIndexType(full, type);
			if (type == null)
			{
				return Enumerable.Empty<CodeCompletion>();
			}
			//static info
			if (full.Length == first.Length && !Variables.ContainsKey(name))
			{
				return ExtractMemberInfo(search, type, RexUtils.StaticBindings, offset);
			}
			else // instance information
			{
				return ExtractMemberInfo(search, type, RexUtils.InstanceBindings, offset);
			}
		}

		private static bool TypeOfFirst(Group full, out Type theType, out string name)
		{
			name = full.Value.Split('.').First();
			var theName = name;
			//Map to primative if needed
			if (RexUtils.MapToKeyWords.Values.Contains(name))
			{
				name = RexUtils.MapToKeyWords.First(i => i.Value == theName).Key.Name;
			}

			if (Variables.ContainsKey(name))
			{
				if (Variables[name].VarValue != null)
				{
					theType = Variables[name].VarType;
					return true;
				}
				else
				{
					theType = null;
					return false;
				}
			}
			theName = name;
			theType = (from t in RexUtils.AllVisibleTypes
					   where t.Name == theName
					   select t).FirstOrDefault();
			return theType != null;
		}

		private static IEnumerable<CodeCompletion> SearchWithoutType(Group search, int offset)
		{
			var lowerSearch = search.Value.ToLower();
			var variables = from i in Variables
							let lowerItem = i.Key.ToLower()
							where lowerItem.Contains(lowerSearch)
							select new
							{
								SearchName = lowerItem,
								IsNested = false,
								ReplacementString = i.Key,
								Details = new MemberDetails(i.Value.VarValue == null ? new[] {
									Syntax.Name(i.Key),
									Syntax.Space, Syntax.EqualsOp,
									Syntax.Space, Syntax.ConstVal("null")
								} :
								RexUtils.GetCSharpRepresentation(i.Value.VarType, false)
								.Concat(new[] {
									Syntax.Space, Syntax.Name(i.Key),
									Syntax.Space, Syntax.EqualsOp,
									Syntax.Space
								}).Concat(GetSyntaxForValue(i.Value.VarValue))),
								isInScope = true
							};

			var types = from t in RexUtils.AllVisibleTypes
						let lowerItem = t.Name.ToLower()
						where lowerItem.Contains(lowerSearch) && !RexReflectionHelper.IsCompilerGenerated(t)
						let isInScope = RexUtils.NamespaceInfos.Any(i => i.Name == t.Namespace && i.Selected)
						select new
						{
							SearchName = lowerItem,
							t.IsNested,
							ReplacementString = GetNestedName(t),
							Details = RexUtils.GetCSharpRepresentation(t, !isInScope),
							isInScope
						};

			return from i in types.Concat(variables)
				   orderby i.SearchName.IndexOf(lowerSearch), i.SearchName.Length, i.isInScope, i.IsNested
				   select new CodeCompletion
				   {
					   Details = i.Details,
					   Start = offset,
					   End = offset + search.Length - 1,
					   ReplaceString = i.ReplacementString,//.Details.Name.String,
					   Search = search.Value
				   };
		}

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

		public static string GetNestedName(Type type)
		{
			var name = type.Name;
			if (name.IndexOf("`") > -1)
				name = name.Substring(0, name.IndexOf("`"));

			if (type.IsNested)
			{
				return GetNestedName(type.DeclaringType) + "." + name;
			}
			else
			{
				return name;
			}
		}

		/// <summary>
		/// Finds the last index of a qurey.
		/// <para>Example: x.MyProp.AnotherProp   This will navigate down to the AnotherProp</para> 
		/// </summary>
		private static Type GetLastIndexType(Group full, Type type, int skipCount = 1)
		{
			var fullPath = full.Value.Split('.').Skip(skipCount).ToList();
			while (fullPath.Any() && type != null)
			{
				var curPath = fullPath.First();
				if (string.IsNullOrEmpty(curPath))
					break;

				var found = false;
				var bindings = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

				try
				{
					foreach (var member in type.GetProperties(bindings))
					{
						if (member.Name.Equals(curPath))
						{
							type = member.PropertyType;
							fullPath.RemoveAt(0);
							found = true;
							break;
						}
					}
					foreach (var member in type.GetFields(bindings))
					{
						if (member.Name.Equals(curPath))
						{
							type = member.FieldType;
							fullPath.RemoveAt(0);
							found = true;
							break;
						}
					}
					if (!found)
						return null;
				}
				catch (Exception)
				{
					return null;
				}
			}

			return type;
		}

		private static readonly Regex propsRegex = new Regex("(.et|add|remove)_(?<Name>.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static IEnumerable<CodeCompletion> ExtractMemberInfo(Group search, Type varType, BindingFlags bindings, int offset)
		{
			var helpList = new Dictionary<string, List<MemberDetails>>();

			// properties
			foreach (var prop in varType.GetProperties(bindings))
			{
				helpList.Add(prop.Name, new List<MemberDetails> { GetMemberDetails(prop) });
			}

			// fields
			foreach (var field in varType.GetFields(bindings))
			{
				helpList.Add(field.Name, new List<MemberDetails> { GetMemberDetails(field) });
			}

			// methods
			foreach (var metod in from met in varType.GetMethods(bindings)
								  where !propsRegex.IsMatch(met.Name) && met.Name.Contains(search.Value)
								  select met)
			{
				var infoStr = GetMemberDetails(metod);
				if (!helpList.ContainsKey(metod.Name))
				{
					helpList.Add(metod.Name, new List<MemberDetails> { infoStr });
				}
				else
					helpList[metod.Name].Add(infoStr);
			}

			// extension methods
			//foreach (var metod in from met in Utils.GetExtensionMethods(varType)
			//                      where met.Name.Contains(search.Value)
			//                      select met)
			//{
			//    var infoStr = GetMemberDetails(metod);
			//    if (!helpList.ContainsKey(metod.Name))
			//    {
			//        helpList.Add(metod.Name, new List<MemberDetails> { infoStr });
			//    }
			//    else
			//        helpList[metod.Name].Add(infoStr);
			//}

			var lowerSearch = search.Value.ToLower();
			return (from item in helpList
					from val in item.Value
					let lowerItem = item.Key.ToLower()
					where lowerItem.Contains(lowerSearch)
					orderby lowerItem.IndexOf(lowerSearch), lowerItem
					select new CodeCompletion
					{
						Details = val,
						ReplaceString = val.Name.String,
						Start = offset + search.Index,
						End = offset + search.Index + search.Length - 1,
						Search = search.Value
					});
		}

		/// <summary>
		/// Uses the property info to build an member details.
		/// </summary>
		/// <param name="prop">property to inspect</param>
		internal static MemberDetails GetMemberDetails(PropertyInfo prop)
		{
			var syntax = new List<Syntax>();

			var get = prop.GetGetMethod();
			var set = prop.GetSetMethod();

			if ((get != null && get.IsStatic) ||
				(set != null && set.IsStatic))
				syntax.AddRange(new[] { Syntax.StaticKeyword, Syntax.Space });

			syntax.AddRange(RexUtils.GetCSharpRepresentation(prop.PropertyType));
			syntax.AddRange(new[] {
				Syntax.Space,
				Syntax.Name(prop.Name),
				Syntax.Space,
				Syntax.CurlyOpen
			});

			if (get != null)
				syntax.AddRange(new[] { Syntax.Space, Syntax.GetKeyword, Syntax.Semicolon });

			if (set != null)
				syntax.AddRange(new[] { Syntax.Space, Syntax.SetKeyword, Syntax.Semicolon });

			syntax.Add(Syntax.Space);
			syntax.Add(Syntax.CurlyClose);

			return new MemberDetails(syntax) { Type = MemberType.Property };
		}

		/// <summary>
		/// Uses the field info to build an member details.
		/// </summary>
		/// <param name="field">field to inspect</param>
		internal static MemberDetails GetMemberDetails(FieldInfo field)
		{
			var syntax = new List<Syntax>();
			if (field.IsStatic && !field.IsLiteral)
				syntax.AddRange(new[] { Syntax.StaticKeyword, Syntax.Space });

			if (field.IsInitOnly)
				syntax.AddRange(new[] { Syntax.ReadonlyKeyword, Syntax.Space });

			if (field.IsLiteral)
				syntax.AddRange(new[] { Syntax.ConstKeyword, Syntax.Space });


			syntax.AddRange(RexUtils.GetCSharpRepresentation(field.FieldType));
			syntax.AddRange(new[] { Syntax.Space, Syntax.Name(field.Name) });

			var showValue = false;
			object value = null;
			if (field.IsLiteral)
			{
				showValue = true;
				value = field.GetRawConstantValue();
			}
			else if (field.IsStatic && field.IsInitOnly)
			{
				showValue = true;
				value = field.GetValue(null);
			}

			if (showValue)
			{
				syntax.AddRange(new[] {
					Syntax.Space,
					Syntax.EqualsOp,
					Syntax.Space,
				});
				syntax.AddRange(GetSyntaxForValue(value));
			}

			return new MemberDetails(syntax) { Type = MemberType.Field };
		}


		/// <summary>
		/// Uses the method info to build an member details.
		/// </summary>
		/// <param name="meth">Method to inspect</param>
		private static MemberDetails GetMemberDetails(MethodInfo meth)
		{
			var syntax = new List<Syntax>();

			if (meth.IsStatic)
				syntax.AddRange(new[] { Syntax.StaticKeyword, Syntax.Space });

			syntax.AddRange(RexUtils.GetCSharpRepresentation(meth.ReturnType));
			syntax.AddRange(new[] { Syntax.Space, Syntax.Name(meth.Name) });
			if (meth.IsGenericMethod)
			{
				syntax.Add(Syntax.GenericParaOpen);
				syntax.AddRange(RexUtils.GenericArgumentsToSyntax(meth.GetGenericArguments().ToList(), false));
				syntax.Add(Syntax.GenericParaClose);
			}

			syntax.Add(Syntax.ParaOpen);

			var paras = meth.GetParameters();
			for (int i = 0; i < paras.Length; i++)
			{
				if (paras[i].IsOut)
					syntax.AddRange(new[] { Syntax.OutKeyword, Syntax.Space });
				if (!paras[i].IsOut && !paras[i].IsIn && paras[i].ParameterType.IsByRef)
					syntax.AddRange(new[] { Syntax.RefKeyword, Syntax.Space });

				syntax.AddRange(RexUtils.GetCSharpRepresentation(paras[i].ParameterType));
				syntax.AddRange(new[] { Syntax.Space, Syntax.ParaName(paras[i].Name) });
				if (i + 1 != paras.Length)
					syntax.AddRange(new[] { Syntax.Comma, Syntax.Space });
			}
			syntax.Add(Syntax.ParaClose);
			return new MemberDetails(syntax) { Type = MemberType.Method };
		}

		/// <summary>
		/// Generates the equals and value part of the syntax e.g '= 3.52' 
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static IEnumerable<Syntax> GetSyntaxForValue(object value)
		{
			if (value == null)
			{
				return new[] { Syntax.ConstVal("null") };
			}

			var type = value.GetType();
			if (type == typeof(string))
			{
				return new[] {
					Syntax.QuotationMark,
					Syntax.ConstVal(value.ToString()),
					Syntax.QuotationMark,
				};
			}

			if (type == typeof(char))
			{
				return new[] {
					Syntax.SingleQuotationMark,
					Syntax.ConstVal(value.ToString()),
					Syntax.SingleQuotationMark,
				};
			}

			return new[] { Syntax.ConstVal(value.ToString()) };
		}
		#endregion

		public static void SetupHelper()
		{
			RexUtils.LoadNamespaceInfos(includeIngoredUsings: false);
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
