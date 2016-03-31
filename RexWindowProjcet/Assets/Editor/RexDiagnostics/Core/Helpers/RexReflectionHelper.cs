using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Rex.Utilities.Helpers
{
    public static class RexReflectionHelper
    {
        /// <summary>
        /// Uses reflection to extract info from object.
        /// <para>Use: <see cref="RexReflectionHelper"/>.ExtractDetails(new { Data1 = "stuff", MoreData = 42 })</para>
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
                    var info = RexHelper.GetMemberDetails(prop);
                    try
                    {
                        var value = prop.GetValue(details, null);
                        detailList.Add(new MemberDetails(value,
                            info.
                            Concat(new[] { Syntax.Space, Syntax.EqualsOp, Syntax.Space }).
                            Concat(RexHelper.GetSyntaxForValue(value))
                        ));
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
                foreach (var field in type.GetFields())
                {
                    var info = RexHelper.GetMemberDetails(field);
                    try
                    {
                        var value = field.GetValue(details);
                        detailList.Add(new MemberDetails(value,
                            info.
                            Concat(new[] { Syntax.Space, Syntax.EqualsOp, Syntax.Space }).
                            Concat(RexHelper.GetSyntaxForValue(value))
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
