using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rex.Utilities.Helpers
{
	static class ExtentionMethods
	{
		/// <summary>
		/// Checks if the IEnumerable is empty
		/// </summary>
		/// <param name="source">Enumerable source</param>
		public static bool IsEmpty<T>(this IEnumerable<T> source) => source != null && !source.Any();
		/// <summary>
		/// Checks if the IEnumerable is empty
		/// </summary>
		/// <param name="source">Enumerable source</param>
		/// <param name="lambda">Filter to run on the source</param>
		public static bool IsEmpty<T>(this IEnumerable<T> source, Func<T, bool> lambda) => source != null && !source.Any(lambda);
	}
}
