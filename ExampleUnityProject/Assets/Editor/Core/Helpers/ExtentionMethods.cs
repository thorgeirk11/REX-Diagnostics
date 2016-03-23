using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rex.Utilities.Helpers
{
    static class ExtentionMethods
    {
        /// <summary>
        /// Check to see if a flags enumeration has a specific flag set.
        /// </summary>
        /// <param name="variable">Flags enumeration to check</param>
        /// <param name="value">Flag to check for</param>
        /// <returns></returns>
        public static bool HasFlag<T>(this T variable, T value) where T : struct, IComparable, IFormattable, IConvertible
        {
            //if (typeof(T).IsEnum)
            //{
            var num = Convert.ToUInt64(value);
            var checking = Convert.ToUInt64(variable);
            return (checking & num) == num;
            //}
            //return false;
        }
        /// <summary>
        /// Checks if the IEnumerable is empty
        /// </summary>
        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null)
                return true; // or throw an exception
            return !source.Any();
        }
        /// <summary>
        /// Checks if the IEnumerable is empty
        /// </summary>
        public static bool IsEmpty<T>(this IEnumerable<T> source, Func<T, bool> lambda)
        {
            if (source == null)
                return true; // or throw an exception
            return !source.Any(lambda);
        }

        /// <summary>
        /// Takes a Flag enum and returns a list of all set flags.
        /// </summary>
        /// <typeparam name="T">Flagged enum</typeparam>
        /// <param name="mask">Flag enum to check</param>
        public static IEnumerable<T> GetFlags<T>(this T mask) where T : struct, IComparable, IFormattable, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException();

            return from m in Enum.GetValues(typeof(T)).Cast<T>()
                   where mask.HasFlag(m)
                   select m;
        }
    }
}
