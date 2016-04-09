using Rex.Utilities.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Rex.Utilities
{
	public class RexParser : IRexIntellisenceProvider, IRexParser
	{
		public const string AssignmentRegex = @"^(?<type>\S*\s+)?(?<var>[^ .,=]+)\s*=(?<expr>[^=].*)$";
		public const string DotExpressionSearch = @"^(?<fullType>(?<firstType>\w+\.)?(\w+\.)*)(?<search>\w*)$";
		public const string ParameterRegex = @"(?<fullType>(?<firstType>\w+\.)?(\w+\.)*)(?<search>\w*)(\((?<para>[^)]*))$";
		public const string DotAfterMethodRegex = @"(?<fullType>(?<firstType>\w+\.)?(\w+\.)*)(?<method>\w*)[(](?<params>[^)]*)[)]\.";

		/// <summary>
		/// Regex to reconize properties.
		/// </summary>
		private const string propsRegex = "(.et|add|remove)_(?<Name>.*)";

		public const BindingFlags InstanceBindings = BindingFlags.Public | BindingFlags.Instance;
		public const BindingFlags StaticBindings = BindingFlags.Public | BindingFlags.Static;

		public ParseResult ParseAssigment(string parsedCode)
		{
			var match = Regex.Match(parsedCode, AssignmentRegex, RegexOptions.Singleline);
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
					validIdentifier = RexCompileEngine.Compiler.IsValidIdentifier(variable);
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

		public IEnumerable<CodeCompletion> Intellisence(string exprssion)
		{
			var parse = ParseAssigment(exprssion);
			var offset = parse.WholeCode.IndexOf(parse.ExpressionString);

			var dotExpressionMatch = Regex.Match(parse.ExpressionString, DotExpressionSearch);
			if (dotExpressionMatch.Success)
				return DotExpression(dotExpressionMatch, offset);

			Type endType = null;
			var afterMethodMatch = Regex.Match(exprssion, DotAfterMethodRegex);
			while (afterMethodMatch.Success)
			{
				var possibleMethods = PossibleMethods(afterMethodMatch);
				if (possibleMethods.Count() == 1)
				{
					var method = possibleMethods.First();
					endType = method.ReturnType;
					exprssion = exprssion.Substring(afterMethodMatch.Length);
					offset += afterMethodMatch.Length;
				}
				else
				{
					return Enumerable.Empty<CodeCompletion>();
				}
				afterMethodMatch = Regex.Match(exprssion, DotAfterMethodRegex);
			}

			//Math.PI.ToString().Trim(',', String.Empty.Length.To)

			var parameterMatch = Regex.Match(parse.ExpressionString, ParameterRegex);
			if (parameterMatch.Success)
			{
				var methodInfo = MethodsOverload(parameterMatch, endType);

				var para = parameterMatch.Groups["para"];
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

				dotExpressionMatch = Regex.Match(paraVal, DotExpressionSearch);
				if (dotExpressionMatch.Success)
					return methodInfo.Concat(DotExpression(dotExpressionMatch, offset));
				else
					return methodInfo;
			}

			if (endType != null)
			{
				var methodSearch = Regex.Match(exprssion, DotExpressionSearch);
				if (methodSearch.Success)
				{
					var full = methodSearch.Groups["fullType"];
					var search = methodSearch.Groups["search"];
					if (string.IsNullOrEmpty(full.Value))
					{
						return ExtractMemberInfo(search, endType, InstanceBindings, offset);
					}
					else
					{
						endType = GetLastIndexType(full, endType, 0);
						if (endType != null)
							return ExtractMemberInfo(search, endType, InstanceBindings, offset);
					}
				}
			}
			return Enumerable.Empty<CodeCompletion>();
		}

		private IEnumerable<CodeCompletion> MethodsOverload(Match paramatch, Type endType)
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
					methodInfo = ExtractMemberInfo(methodName, type, InstanceBindings, 0);
			}
			foreach (var info in methodInfo)
			{
				info.IsMethodOverload = true;
				yield return info;
			}
		}

		public IEnumerable<MethodInfo> PossibleMethods(Match match)
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

		private IEnumerable<CodeCompletion> DotExpression(Match match, int offset)
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
			if (full.Length == first.Length && !RexHelper.Variables.ContainsKey(name))
			{
				return ExtractMemberInfo(search, type, StaticBindings, offset);
			}
			else // instance information
			{
				return ExtractMemberInfo(search, type, InstanceBindings, offset);
			}
		}

		private bool TypeOfFirst(Group full, out Type theType, out string name)
		{
			name = full.Value.Split('.').First();

			//Map to primative if needed
			var primitive = RexUtils.MapToPrimitive(name);
			if (primitive != null)
			{
				theType = RexUtils.PrimitiveToType(primitive);
				return true;
			}

			if (RexHelper.Variables.ContainsKey(name))
			{
				if (RexHelper.Variables[name].VarValue != null)
				{
					theType = RexHelper.Variables[name].VarType;
					return true;
				}
				else
				{
					theType = null;
					return false;
				}
			}

			var tmpName = name;
			theType = RexUtils.AllVisibleTypes.FirstOrDefault(t => t.Name == tmpName);
			return theType != null;
		}

		private IEnumerable<CodeCompletion> SearchWithoutType(Group search, int offset)
		{
			var lowerSearch = search.Value.ToLower();
			var variables = from i in RexHelper.Variables
							let lowerItem = i.Key.ToLower()
							where lowerItem.Contains(lowerSearch)
							select new
							{
								SearchName = lowerItem,
								IsNested = false,
								ReplacementString = i.Key,
								Details = new MemberDetails(i.Value.VarValue,
									RexUtils.GetCSharpRepresentation(i.Value.VarType, false).Concat(new[] {
									Syntax.Space, Syntax.Name(i.Key),
									Syntax.Space, Syntax.EqualsOp,
									Syntax.Space}).Concat(RexReflectionUtils.GetSyntaxForValue(i.Value.VarValue))),
								isInScope = true
							};

			var types = from type in RexUtils.AllVisibleTypes
						let lowerItem = type.Name.ToLower()
						where lowerItem.Contains(lowerSearch) && !RexReflectionUtils.IsCompilerGenerated(type)
						let isInScope = RexCompileEngine.Instance.NamespaceInfos.Any(i => i.Name == type.Namespace && i.Selected)
						select new
						{
							SearchName = lowerItem,
							type.IsNested,
							ReplacementString = GetNestedName(type),
							Details = RexUtils.GetCSharpRepresentation(type),
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
					   Search = search.Value,
					   IsInScope = i.isInScope
				   };
		}

		public string GetNestedName(Type type)
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
		private Type GetLastIndexType(Group full, Type type, int skipCount = 1)
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

		private IEnumerable<CodeCompletion> ExtractMemberInfo(Group search, Type varType, BindingFlags bindings, int offset)
		{
			var lowerSearch = search.Value.ToLower();
			return from help in GetAllMemberInfos(varType, bindings)
				   let key = help.Key.ToLower()
				   let searchIndex = key.IndexOf(lowerSearch)
				   where searchIndex >= 0 || string.IsNullOrEmpty(lowerSearch)
				   from val in help.Value
				   orderby searchIndex, key
				   select new CodeCompletion
				   {
					   Details = val,
					   ReplaceString = val.Name.String,
					   Start = offset + search.Index,
					   End = offset + search.Index + search.Length - 1,
					   Search = search.Value
				   };
		}

		private static Dictionary<string, List<MemberDetails>> GetAllMemberInfos(Type varType, BindingFlags bindings)
		{
			var helpList = new Dictionary<string, List<MemberDetails>>();

			// properties
			foreach (var prop in varType.GetProperties(bindings))
			{
				helpList.Add(prop.Name, new List<MemberDetails> { RexReflectionUtils.GetMemberDetails(prop) });
			}

			// fields
			foreach (var field in varType.GetFields(bindings))
			{
				helpList.Add(field.Name, new List<MemberDetails> { RexReflectionUtils.GetMemberDetails(field) });
			}

			// methods
			foreach (var metod in from met in varType.GetMethods(bindings)
								  where !Regex.IsMatch(met.Name, propsRegex)
								  select met)
			{
				var infoStr = RexReflectionUtils.GetMemberDetails(metod);
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

			return helpList;
		}
	}
}
