using Rex.Utilities.Helpers;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;

namespace Rex.Utilities
{
	public static class RexUtils
	{
		public const BindingFlags InstanceBindings = BindingFlags.Public | BindingFlags.Instance;
		public const BindingFlags StaticBindings = BindingFlags.Public | BindingFlags.Static;

		private static string[] ignoreUsings;
		private static string[] _defaultUsings;
		private static List<MethodInfo> ExtentionMethods;
		private static List<Type> allVisibleTypes;

		/// <summary>
		/// The default using statments for REX. 
		/// </summary>
		public static IEnumerable<string> DefaultUsings
		{
			get
			{
				if (_defaultUsings == null)
				{
					_defaultUsings = new[] {
						"System",
						"System.Collections.Generic",
						"System.Collections",
						"System.Linq",
						"System.Text",
						"UnityEngine",
					};
				}
				return _defaultUsings;
			}
		}
		/// <summary>
		/// These using statments should be ignored, (compiler generated/Unity internals).
		/// </summary>
		public static IEnumerable<string> IgnoreUsings
		{
			get
			{
				if (ignoreUsings == null)
				{
					ignoreUsings = new[] {
						"NUnit",
						"antlr",
						"CompilerGenerated",
						"TreeEditor",
						"UnityEditorInternal",
						"UnityEngineInternal",
						"I18N",
						"AOT",
						"ICSharpCode",
					};
				}
				return ignoreUsings;
			}
		}

		/// <summary>
		/// Takes in a primitive type and returns it's name "System.Boolean" -> "bool"
		/// </summary>
		/// <param name="type">primitive type</param>
		public static string MapToPrimitive(Type type)
		{
			if (type == null) return null;
			if (type.Namespace != "System") return null;
			switch (type.FullName.TrimEnd('&'))
			{
				case "System.Boolean": return "bool";
				case "System.Byte": return "byte";
				case "System.SByte": return "sbyte";
				case "System.Char": return "char";
				case "System.Decimal": return "decimal";
				case "System.Double": return "double";
				case "System.Single": return "float";
				case "System.Int32": return "int";
				case "System.UInt32": return "uint";
				case "System.Int64": return "long";
				case "System.UInt64": return "ulong";
				case "System.Object": return "object";
				case "System.Int16": return "short";
				case "System.UInt16": return "ushort";
				case "System.String": return "string";
				case "System.Void": return "void";
				default: return null;
			}
		}

		/// <summary>
		/// Normalizes the form of the name to the correct primitve name: "Boolean" -> "bool"
		/// </summary>
		/// <param name="typeName">primitive name</param>
		public static string MapToPrimitive(string typeName)
		{
			switch (typeName)
			{
				case "Single": case "float": return "float";
				case "Double": case "double": return "double";
				case "Decimal": case "decimal": return "decimal";
				case "Byte": case "byte": return "byte";
				case "SByte": case "sbyte": return "sbyte";
				case "Int16": case "short": return "short";
				case "UInt16": case "ushort": return "ushort";
				case "Int32": case "int": return "int";
				case "UInt32": case "uint": return "uint";
				case "Int64": case "long": return "long";
				case "UInt64": case "ulong": return "ulong";
				case "Boolean": case "bool": return "bool";
				case "String": case "string": return "string";
				case "Char": case "char": return "char";
				case "Object": case "object": return "object";
				case "Void": case "void": return "void";
				default: return null;
			}
		}

		/// <summary>
		/// Takes in a primitive name and returns it's type.
		/// </summary>
		/// <param name="typeName">primitive name</param>
		public static Type PrimitiveToType(string typeName)
		{
			switch (typeName)
			{
				case "float": return typeof(float);
				case "double": return typeof(double);
				case "decimal": return typeof(decimal);
				case "byte": return typeof(byte);
				case "sbyte": return typeof(sbyte);
				case "short": return typeof(short);
				case "ushort": return typeof(ushort);
				case "int": return typeof(int);
				case "uint": return typeof(uint);
				case "long": return typeof(long);
				case "ulong": return typeof(ulong);
				case "bool": return typeof(bool);
				case "string": return typeof(string);
				case "char": return typeof(char);
				case "object": return typeof(object);
				case "void": return typeof(void);

				default: return null;
			}
		}

		public static readonly Dictionary<SyntaxType, string> SyntaxHighlightColors = new Dictionary<SyntaxType, string>
		{
			{ SyntaxType.Type,                  "#008000ff" },
			{ SyntaxType.Keyword,               "#008080ff" },
			{ SyntaxType.SingleQuotationMark,   "brown" },
			{ SyntaxType.QuotationMark,         "brown" },
			{ SyntaxType.ConstVal,              "brown" },
		};
		public static readonly Dictionary<SyntaxType, string> SyntaxHighlightProColors = new Dictionary<SyntaxType, string>
		{
			{ SyntaxType.Type,                  "#6f00ff" },
			{ SyntaxType.Keyword,               "blue" },
			{ SyntaxType.SingleQuotationMark,   "brown" },
			{ SyntaxType.QuotationMark,         "brown" },
			{ SyntaxType.ConstVal,              "brown" },
		};

		public static string TopLevelNameSpace(string nameSpace)
		{
			var parts = nameSpace.Split('.');

			var acc = string.Empty;
			foreach (var p in parts)
			{
				if (acc != string.Empty)
					acc += '.';

				acc += p;
				var WithNamespace = from type in AllVisibleTypes
									where type.Namespace == acc
									select type;
				if (WithNamespace.Any())
					return acc;
			}
			return string.Empty;
		}
		/// <summary>
		/// Refreshes the <see cref="NameSpaceInfo"/> list.
		/// </summary>
		/// <param name="includeIngoredUsings">Include namespaces form the ignore usings list.</param>
		public static List<NameSpaceInfo> LoadNamespaceInfos(bool includeIngoredUsings)
		{
			var namespaceInfos = new List<NameSpaceInfo>();

			var namespaces = new Dictionary<string, Assembly>();
			foreach (var ns in from type in AllVisibleTypes
							   where !string.IsNullOrEmpty(type.Namespace)
							   select new { Namespace = type.Namespace, Assembly = type.Assembly })
			{
				if (!includeIngoredUsings && IgnoreUsings.Any(ignore => ns.Namespace.StartsWith(ignore)))
				{
					continue;
				}

				if (!namespaces.ContainsKey(ns.Namespace))
				{
					namespaces.Add(ns.Namespace, ns.Assembly);
				}
			}

			foreach (var name in namespaces)
			{
				var topName = TopLevelNameSpace(name.Key);

				var indent = name.Key.Count(i => i == '.') - topName.Count(i => i == '.');

				if (topName.Contains('.'))
				{
					indent++;
					var rootName = topName.Split('.')[0];
					if (!namespaceInfos.Any(i => i.Name == rootName))
					{
						namespaceInfos.Add(new NameSpaceInfo
						{
							Folded = false,
							IndetLevel = 0,
							Name = rootName,
							Selected = RexUsingsHandler.Usings.Contains(rootName),
							AtMaxIndent = false,
						});
					}
				}
				namespaceInfos.Add(new NameSpaceInfo
				{
					Folded = false,
					IndetLevel = indent,
					Name = name.Key,
					Selected = DefaultUsings.Contains(name.Key) || RexUsingsHandler.Usings.Contains(name.Key),
					AtMaxIndent = namespaces.Count(j => j.Key.StartsWith(name.Key)) == 1
				});
			}
			namespaceInfos.Sort();
			return namespaceInfos;
		}

		/// <summary>
		/// Has <see cref="object.ToString()"/> been overwitten by this type or any above it.
		/// </summary>
		/// <param name="type">Type in question</param>
		internal static bool IsToStringOverride(Type type)
		{
			var method = type.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { }, null);
			return method.DeclaringType != typeof(object);
		}

		public static IEnumerable<Type> AllVisibleTypes
		{
			get
			{
				if (allVisibleTypes == null)
				{
					allVisibleTypes = new List<Type>();
					foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
					{
						try
						{
							allVisibleTypes.AddRange(ass.GetExportedTypes());
						}
						catch (Exception)
						{
							continue;
						}
					}
				}
				return allVisibleTypes;
			}
		}

		public static MemberDetails GetCSharpRepresentation(Type t)
		{
			return GetCSharpRepresentation(t, false);
		}
		public static MemberDetails GetCSharpRepresentation(Type t, bool showFullName)
		{
			if (t.IsGenericType)
			{
				var genericArgs = t.GetGenericArguments().ToList();

				return GetCSharpRepresentation(t, showFullName, genericArgs);
			}

			return DealWithRefParameters(t, showFullName);
		}

		private static MemberDetails DealWithRefParameters(Type t, bool showFullName)
		{
			var nested = NestedType(t, showFullName);

			if (t.IsArray)
			{
				return new MemberDetails(nested);
			}

			if (!showFullName)
			{
				var keyWord = MapToPrimitive(t);
				if (keyWord != null)
				{
					return new MemberDetails(new[] { Syntax.Keyword(keyWord) });
				}
			}

			if (nested.IsEmpty())
			{
				if (showFullName && t.FullName != null)
				{
					return new MemberDetails(new[] {
						Syntax.NameSpaceForType(t.FullName.Substring(0, t.FullName.Length- t.Name.Length)),
						Syntax.NewType(t.Name)
					});
				}
				return new MemberDetails(new[] { Syntax.NewType(t.Name) });
			}
			else
				return new MemberDetails(nested.Concat(new[] { Syntax.NewType(t.Name) }));

		}

		internal static MemberDetails GetCSharpRepresentation(Type t, bool showFullName, List<Type> availableArguments)
		{
			if (t.IsGenericType)
			{
				var nested = NestedType(t, false);

				bool isGeneric = false;
				var name = t.Name;
				if (name.IndexOf("`") > -1)
				{
					name = name.Substring(0, name.IndexOf("`"));
					isGeneric = true;
				}

				var details = new List<Syntax>();
				details.AddRange(nested);

				if (showFullName)
				{
					string declaringNamespace;
					if (isGeneric)
					{
						declaringNamespace = t.FullName.Substring(0, t.FullName.IndexOf("`") - name.Length);
						declaringNamespace = declaringNamespace.Replace('+', '.');
					}
					else
					{
						declaringNamespace = t.FullName.Substring(0, t.FullName.Length - t.Name.Length);
					}
					declaringNamespace = declaringNamespace.Substring(0, declaringNamespace.Length - nested.Sum(i => i.String.Length));
					details.Insert(0, Syntax.NameSpaceForType(declaringNamespace));
					details.Add(Syntax.NewType(name));
				}
				else
				{
					details.Add(Syntax.NewType(name));
				}
				var genericArguments = GenericArgumentsToSyntax(availableArguments, showFullName);

				// If there are type arguments, add them with < >
				if (genericArguments.Count > 0)
				{
					details.Add(Syntax.GenericParaOpen);
					details.AddRange(genericArguments);
					details.Add(Syntax.GenericParaClose);
				}

				return new MemberDetails(details);
			}

			return DealWithRefParameters(t, showFullName);
		}

		private static IEnumerable<Syntax> NestedType(Type t, bool showFullName)
		{
			if (t.IsArray)
			{
				return GetCSharpRepresentation(t.GetElementType(), showFullName).Concat(new[] { Syntax.BracketOpen, Syntax.BracketClose });
			}
			if (!t.IsGenericParameter && t.DeclaringType != null)
			{
				// This is a nested type, build the nesting type first
				return GetCSharpRepresentation(t.DeclaringType, showFullName).Concat(new[] { Syntax.Dot });
			}
			return Enumerable.Empty<Syntax>();
		}

		public static List<Syntax> GenericArgumentsToSyntax(List<Type> availableArguments, bool showFullName)
		{
			var genericArguments = new List<Syntax>();
			// Build the type arguments (if any)
			var argsCount = availableArguments.Count;
			for (int i = 0; i < argsCount && availableArguments.Count > 0; i++)
			{
				if (i != 0) genericArguments.AddRange(new[] { Syntax.Comma, Syntax.Space });

				var genericType = GetCSharpRepresentation(availableArguments[0], showFullName, availableArguments[0].GetGenericArguments().ToList());
				genericArguments.AddRange(genericType);
				availableArguments.RemoveAt(0);
			}

			return genericArguments;
		}

		/// <summary>
		/// Takes in a member detail and builds a formated rich text string.
		/// </summary>
		/// <param name="intellisenseHelp">Syntax to highlight</param>
		/// <param name="colors">Color of the SyntaxHighlingting</param>
		/// <param name="search">Bold the part that user is searching for</param>
		/// <returns>Syntax highlighted rich text string</returns>
		public static string SyntaxHighlingting(MemberDetails intellisenseHelp, Dictionary<SyntaxType, string> colors, string search)
		{
			var str = new StringBuilder();
			var name = intellisenseHelp.Name;
			foreach (var syntax in intellisenseHelp)
			{
				var syntaxStr = syntax.String;
				if (name.IsEquivelent(syntax) && !string.IsNullOrEmpty(search))
				{
					var lowerStr = syntaxStr.ToLower();
					var searchLower = search.ToLower();
					if (lowerStr.Contains(searchLower))
					{
						var index = lowerStr.IndexOf(searchLower);
						syntaxStr = syntaxStr.Insert(index + search.Length, "</b>").Insert(index, "<b>");
					}
				}

				if (colors.ContainsKey(syntax.Type))
					syntaxStr = ColoredString(syntaxStr, colors[syntax.Type]);

				str.Append(syntaxStr);
			}
			return str.ToString();
		}
		public static string SyntaxHighlingting(IEnumerable<Syntax> intellisenseHelp, Dictionary<SyntaxType, string> colors)
		{
			var str = new StringBuilder();
			foreach (var syntax in intellisenseHelp)
			{
				var syntaxStr = syntax.String;
				if (colors.ContainsKey(syntax.Type))
					syntaxStr = ColoredString(syntax.String, colors[syntax.Type]);

				str.Append(syntaxStr);
			}
			return str.ToString();
		}

		/// <summary>
		/// Gets all the extention Methods of the type.
		/// </summary>
		/// <param name="extendedType"></param>
		/// <returns></returns>
		internal static IEnumerable<MethodInfo> GetExtensionMethods(Type extendedType)
		{
			if (ExtentionMethods == null)
			{
				ExtentionMethods = (from type in AllVisibleTypes
									where type.IsSealed && !type.IsGenericType && !type.IsNested
									from method in type.GetMethods(BindingFlags.Static
										 | BindingFlags.Public | BindingFlags.NonPublic)
									where method.IsDefined(typeof(ExtensionAttribute), false)
									select method).ToList();
			}

			return from method in ExtentionMethods
				   where method.GetParameters()[0].ParameterType == extendedType
				   select method;
		}

		private static string ColoredString(string syntax, string color)
		{
			return string.Format(@"<color=""{0}"">{1}</color>", color, syntax);
		}

		public static void Add(this Dictionary<MessageType, List<string>> messages, MessageType key, params string[] value)
		{
			if (messages.ContainsKey(key))
			{
				messages[key].AddRange(value);
			}
			else
			{
				messages[key] = value.ToList();
			}
		}
	}

}
