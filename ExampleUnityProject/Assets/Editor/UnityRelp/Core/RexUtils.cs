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

namespace Rex.Utilities
{
    public static class RexUtils
    {
        #region Constants
        private static string _macroDicrectory;
        public static string MacroDirectory
        {
            get
            {
                if (_macroDicrectory == null)
                {
                    _macroDicrectory = Application.persistentDataPath + @"_macros.txt";
                }
                return _macroDicrectory;
            }
            set { _macroDicrectory = value; }
        }
        
        private static string _usingsFileName;
        public static string UsingsFileName
        {
            get
            {
                if (_usingsFileName == null)
                {
                    _usingsFileName = Application.persistentDataPath + @"_macros.txt";
                }
                return _usingsFileName;
            }
            set { _usingsFileName = value; }
        }

        public static readonly Regex Assignment = new Regex(@"^(?<type>\S*\s+)?(?<var>[^ .,=]+)\s*=(?<expr>[^=].*)$", RegexOptions.Compiled | RegexOptions.Singleline);
        public static readonly Regex DotExpressionSearch = new Regex(@"^(?<fullType>(?<firstType>\w+\.)?(\w+\.)*)(?<search>\w*)$", RegexOptions.Compiled);
        public static readonly Regex ParameterRegex = new Regex(@"(?<fullType>(?<firstType>\w+\.)?(\w+\.)*)(?<search>\w*)(\((?<para>[^)]*))$", RegexOptions.Compiled);
        public static readonly Regex DotAfterMethodRegex = new Regex(@"(?<fullType>(?<firstType>\w+\.)?(\w+\.)*)(?<method>\w*)[(](?<params>[^)]*)[)]\.", RegexOptions.Compiled);

        public const BindingFlags InstanceBindings = BindingFlags.Public | BindingFlags.Instance;
        public const BindingFlags StaticBindings = BindingFlags.Public | BindingFlags.Static;

        public static readonly string[] defaultUsings = new[] {
            "System",
            "System.Collections.Generic",
            "System.Collections",
            "System.Linq",
            "System.Text",
            "UnityEngine",
        };
        public static readonly string[] IgnoreUsings = new[] {
            //"Rex",
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

        public const string className = "__TempRexClass";
        public const string FuncName = "Func";
        #endregion

        #region NameSpace related
        internal static string Usings
        {
            get
            {
                return NamespaceInfos.Where(i => i.Selected).Aggregate("", (i, j) => i + string.Format(Environment.NewLine + "using {0};", j.Name));
            }
        }
        public static IEnumerable<NameSpaceInfo> NamespaceInfos
        {
            get
            {
                if (namespaceInfos == null)
                {
                    LoadNamespaceInfos(false);
                }
                return namespaceInfos;
            }
        }
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
        private static List<NameSpaceInfo> namespaceInfos;
        /// <summary>
        /// Refreshes the <see cref="NameSpaceInfo"/> list.
        /// </summary>
        /// <param name="includeIngoredUsings">Include namespaces form the ignore usings list.</param>
        public static List<NameSpaceInfo> LoadNamespaceInfos(bool includeIngoredUsings)
        {
            namespaceInfos = new List<NameSpaceInfo>();

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

            UsingsHandler.LoadUsings();

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
                            Selected = UsingsHandler.Usings.Contains(rootName),
                            Assembly = name.Value,
                            AtMaxIndent = false,
                        });
                    }
                }
                namespaceInfos.Add(new NameSpaceInfo
                {
                    Folded = false,
                    IndetLevel = indent,
                    Assembly = name.Value,
                    Name = name.Key,
                    Selected = defaultUsings.Contains(name.Key) || UsingsHandler.Usings.Contains(name.Key),
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

        private static List<Type> allVisibleTypes;
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
        #endregion

        public static T ExecuteAssembly<T>(Assembly assembly) where T : class
        {
            var Class = Activator.CreateInstance(assembly.GetType(className));
            var method = Class.GetType().GetMethod(FuncName);
            return method.Invoke(Class, null) as T;
        }

        public static MemberDetails GetCSharpRepresentation(Type t, bool showFullName = false)
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
            if (!showFullName)
            {
                var typeName = t.Name.TrimEnd('&');
                if (MapToKeyWords.ContainsKey(t))
                {
                    return new MemberDetails(new[] { Syntax.Keyword(MapToKeyWords[t]) });
                }
                else if (MapToKeyWords.Any(i => i.Key.Name == typeName))
                {
                    return new MemberDetails(new[] { Syntax.Keyword(MapToKeyWords.First(i => i.Key.Name == typeName).Value) });
                }
            }

            if (nested.IsEmpty())
                return new MemberDetails(new[] { Syntax.NewType(showFullName ? t.FullName : t.Name) });
            else
                return new MemberDetails(nested.Concat(new[] { Syntax.NewType(t.Name) }));

        }

        internal static MemberDetails GetCSharpRepresentation(Type t, bool showFullName, List<Type> availableArguments)
        {
            if (t.IsGenericType)
            {
                var nested = NestedType(t, showFullName);

                var value = showFullName && nested.IsEmpty() ? t.FullName : t.Name;
                if (value.IndexOf("`") > -1)
                {
                    value = value.Substring(0, value.IndexOf("`"));
                }

                var details = new List<Syntax>();
                details.AddRange(nested);
                details.Add(Syntax.NewType(value));

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
                return NestedType(t.GetElementType(), showFullName);
            }
            if (!t.IsGenericParameter && t.DeclaringType != null)
            {
                // This is a nested type, build the nesting type first
                return GetCSharpRepresentation(t.DeclaringType, showFullName).Concat(new[] { Syntax.Dot });
            }
            return Enumerable.Empty<Syntax>();
        }

        /// <summary>
        /// Map between types that have special names and there names.
        /// </summary>
        internal readonly static Dictionary<Type, string> MapToKeyWords = new Dictionary<Type, string>
        {
            { typeof(float),    "float" },
            { typeof(double),   "double" },
            { typeof(decimal),  "decimal" },
            { typeof(byte),     "byte" },
            { typeof(sbyte),    "sbyte" },
            { typeof(short),    "short" },
            { typeof(ushort),   "ushort" },
            { typeof(int),      "int" },
            { typeof(uint),     "uint" },
            { typeof(long),     "long" },
            { typeof(ulong),    "ulong" },
            { typeof(bool),     "bool" },
            { typeof(string),   "string" },
            { typeof(object),   "object" },
            { typeof(void),     "void" },
        };

        internal static List<Syntax> GenericArgumentsToSyntax(List<Type> availableArguments, bool showFullName)
        {
            var genericArguments = new List<Syntax>();
            // Build the type arguments (if any)
            var argsCount = availableArguments.Count;
            for (int i = 0; i < argsCount && availableArguments.Count > 0; i++)
            {
                if (i != 0) genericArguments.Add(Syntax.Comma);

                genericArguments.AddRange(GetCSharpRepresentation(availableArguments[0], showFullName, availableArguments[0].GetGenericArguments().ToList()));
                availableArguments.RemoveAt(0);
            }

            return genericArguments;
        }

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


        public static readonly Dictionary<SyntaxType, string> SyntaxHighlightColors = new Dictionary<SyntaxType, string>
        {
            { SyntaxType.Type,       "#008000ff" },
            { SyntaxType.Keyword,    "#008080ff" },
        };
        public static readonly Dictionary<SyntaxType, string> SyntaxHighlightProColors = new Dictionary<SyntaxType, string>
        {
            { SyntaxType.Type,       "#6f00ff" },
            { SyntaxType.Keyword,    "blue" },
        };

        /// <summary>
        /// Takes in a member detail and builds a formated rich text string.
        /// </summary>
        /// <param name="intellisenseHelp">Syntax to highlight</param>
        /// <returns>Syntax highlighted rich text string</returns>
        public static string SyntaxHighlingting(MemberDetails intellisenseHelp, Dictionary<SyntaxType, string> colors, string search)
        {
            var str = new StringBuilder();
            Syntax last = null;
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

                BuildHighlight(ref str, ref last, syntax, syntaxStr);
                last = syntax;
            }
            return str.Replace("  ", " ").ToString();
        }
        public static string SyntaxHighlingting(IEnumerable<Syntax> intellisenseHelp, Dictionary<SyntaxType, string> colors)
        {
            var str = new StringBuilder();
            Syntax last = null;
            foreach (var syntax in intellisenseHelp)
            {
                var syntaxStr = syntax.String;
                if (colors.ContainsKey(syntax.Type))
                    syntaxStr = ColoredString(syntax.String, colors[syntax.Type]);

                BuildHighlight(ref str, ref last, syntax, syntaxStr);
            }
            return str.Replace("  ", " ").ToString();
        }

        private static void BuildHighlight(ref StringBuilder str, ref Syntax last, Syntax syntax, string nameStr)
        {
            switch (syntax.Type)
            {
                case SyntaxType.Keyword:
                    if (last != null && last.Type == SyntaxType.Keyword)
                        str.Append(" ");// style.padding.left = paddAmount;
                    break;

                case SyntaxType.ParaName:
                case SyntaxType.Name:
                    str.Append(" ");
                    //style.padding.left = paddAmount;
                    break;

                case SyntaxType.CurlyOpen:
                case SyntaxType.EqualsOp:
                    str.Append(" ");
                    //style.padding.left = style.padding.right = paddAmount;
                    break;

                case SyntaxType.GenericParaClose:
                case SyntaxType.GenericParaOpen:
                    str = new StringBuilder(str.ToString().Trim());
                    break;

            }
            str.Append(nameStr);
            switch (syntax.Type)
            {
                case SyntaxType.Keyword:
                    str.Append(" ");// style.padding.left = paddAmount;
                    break;

                case SyntaxType.CurlyOpen:
                case SyntaxType.EqualsOp:
                    str.Append(" ");
                    //style.padding.left = style.padding.right = paddAmount;
                    break;

                case SyntaxType.Comma:
                case SyntaxType.Semicolon:
                    str.Append(" ");
                    //style.padding.right = paddAmount;
                    break;

            }
            last = syntax;
        }

        /// <summary>
        /// Gets all the extention Methods of the type.
        /// </summary>
        /// <param name="extendedType"></param>
        /// <returns></returns>
        internal static IEnumerable<MethodInfo> GetExtensionMethods(Type extendedType)
        {
            return from method in ExtentionMethods
                   where method.GetParameters()[0].ParameterType == extendedType
                   select method;
        }

        static List<MethodInfo> ExtentionMethods = (from type in AllVisibleTypes
                                                    where type.IsSealed && !type.IsGenericType && !type.IsNested
                                                    from method in type.GetMethods(BindingFlags.Static
                                                         | BindingFlags.Public | BindingFlags.NonPublic)
                                                    where method.IsDefined(typeof(ExtensionAttribute), false)
                                                    select method).ToList();

        private static string ColoredString(string syntax, string color)
        {
            return string.Format(@"<color=""{0}"">{1}</color>", color, syntax);
        }
    }

}
