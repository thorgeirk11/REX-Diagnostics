using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Rex.Utilities.Helpers
{
	public static class RexReflectionUtils
	{
		/// <summary>
		/// Uses reflection to extract info from object.
		/// <para>Use: <see cref="RexReflectionUtils"/>.ExtractDetails(new { Data1 = "stuff", MoreData = 42 })</para>
		/// </summary>
		/// <param name="details"></param>
		/// <returns></returns>
		public static IEnumerable<MemberDetails> ExtractDetails(object details)
		{
			if (details == null)
				return Enumerable.Empty<MemberDetails>();

			var detailList = new List<MemberDetails>();
			var type = details.GetType();
			string val;

			if (!ExtractValue(details, out val))
			{
				foreach (var prop in type.GetProperties())
				{
					var info = GetMemberDetails(prop);
					try
					{
						var value = prop.GetValue(details, null);
						detailList.Add(new MemberDetails(value,
							info.
							Concat(new[] { Syntax.Space, Syntax.EqualsOp, Syntax.Space }).
							Concat(GetSyntaxForValue(value))
						));
					}
					catch (Exception)
					{
						continue;
					}
				}
				foreach (var field in type.GetFields())
				{
					var info = GetMemberDetails(field);
					try
					{
						var value = field.GetValue(details);
						detailList.Add(new MemberDetails(value,
							info.
							Concat(new[] { Syntax.Space, Syntax.EqualsOp, Syntax.Space }).
							Concat(GetSyntaxForValue(value))
						));
					}
					catch (Exception)
					{
						continue;
					}
				}
				return detailList;
			}
			else
			{
				return Enumerable.Empty<MemberDetails>();
			}
		}

		public static bool IsCompilerGenerated(Type type)
		{
			return type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0;
		}

		/// <summary>
		/// Check if the type overrides "ToString()"
		/// </summary>
		/// <param name="type"></param>
		public static bool IsOverridingToStirng(Type type)
		{
			return type.GetMethod("ToString", Type.EmptyTypes).DeclaringType != typeof(object);
		}

		/// <summary>
		/// Uses the property info to build an member details.
		/// </summary>
		/// <param name="prop">property to inspect</param>
		public static MemberDetails GetMemberDetails(PropertyInfo prop)
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
		public static MemberDetails GetMemberDetails(FieldInfo field)
		{
			var syntax = new List<Syntax>();

			//if (field.IsStatic && !field.IsLiteral)
			//	syntax.AddRange(new[] { Syntax.StaticKeyword, Syntax.Space });

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
		public static MemberDetails GetMemberDetails(MethodInfo meth)
		{
			var syntax = new List<Syntax>();

			//if (meth.IsStatic)
			//	syntax.AddRange(new[] { Syntax.StaticKeyword, Syntax.Space });

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
			for (var i = 0; i < paras.Length; i++)
			{
				if (paras[i].IsOut)
					syntax.AddRange(new[] { Syntax.OutKeyword, Syntax.Space });
				if (!paras[i].IsOut && !paras[i].IsIn && paras[i].ParameterType.IsByRef)
					syntax.AddRange(new[] { Syntax.RefKeyword, Syntax.Space });

				syntax.AddRange(RexUtils.GetCSharpRepresentation(paras[i].ParameterType));
				syntax.AddRange(new[] { Syntax.Space, Syntax.ParaName(paras[i].Name) });

				if (paras[i].IsOptional)
				{
					syntax.AddRange(new[] { Syntax.Space, Syntax.EqualsOp, Syntax.Space });
					syntax.AddRange(GetSyntaxForValue(paras[i].RawDefaultValue));
				}

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

			var enumerableItems = "";
			if (!IsOverridingToStirng(type) &&
				value is IEnumerable &&
				ExtractValue(value, out enumerableItems))
			{
				return RexUtils.GetCSharpRepresentation(type).Concat(new[]
				{
					Syntax.Space,
					Syntax.CurlyOpen,
					Syntax.Space,
					Syntax.ConstVal(enumerableItems.Replace("\n","\\n")),
					Syntax.Space,
					Syntax.CurlyClose
				});
			}
			return new[] { Syntax.ConstVal(value.ToString()) };
		}

		/// <summary>
		/// Is the type anonymous or a generic type with anonymous arguments.
		/// </summary>
		/// <param name="type">Type in question</param>
		public static bool ContainsAnonymousType(Type type)
		{
			if (IsAnonymousType(type))
				return true;

			if (type.IsGenericType)
			{
				foreach (var genericType in type.GetGenericArguments())
				{
					if (ContainsAnonymousType(genericType)) return true;
				}
			}
			if (type.IsArray)
			{
				return ContainsAnonymousType(type.GetElementType());
			}
			return false;
		}

		public static bool IsAnonymousType(Type type)
		{
			var nameContainsAnonymousType = type.FullName.Contains("AnonymousType") || type.FullName.Contains("<>__AnonType");
			var isAnonymousType = IsCompilerGenerated(type) && nameContainsAnonymousType;
			return isAnonymousType;
			//return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false) && type.IsGenericType
			//       && (type.Name.Contains("AnonymousType") || type.FullName.Contains("<>__AnonType"))
			//       && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
			//       && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
		}

		public static bool ExtractValue(object value, out string parsed)
		{
			parsed = string.Empty;
			if (ReferenceEquals(value, null))
				return false;

			double myNum = 0;
			if (double.TryParse(value.ToString(), out myNum))
			{
				parsed = myNum.ToString(); //Its a number.
				return true;
			}
			if (value is string ||
				value is Enum ||
				value is bool ||
				value is ValueType)
			{
				parsed = value.ToString();
				return true;
			}
			var list = value as IEnumerable;
			if (list != null)
			{
				parsed = ExtractList(list);
				return true;
			}

			return false;
		}

		private static string ExtractList(IEnumerable list)
		{
			if (list != null)
			{
				var sb = new StringBuilder();
				foreach (var item in list)
				{
					sb.Append(item + ", ");
				}
				return sb.ToString().Trim().TrimEnd(',');
			}
			return string.Empty;
		}
	}
}
