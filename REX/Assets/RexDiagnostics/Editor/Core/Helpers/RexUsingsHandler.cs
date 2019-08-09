using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace Rex.Utilities.Helpers
{
	public static class RexUsingsHandler
	{
		private const string REX_USINGS = "Rex_Usings";

		public static IEnumerable<string> Usings
		{
			get
			{
				var usingString = EditorPrefs.GetString(REX_USINGS, string.Empty);
				if (string.IsNullOrEmpty(usingString))
				{
					return Enumerable.Empty<string>();
				}
				else
				{
					return usingString.Split('|');
				}
			}
		}

		/// <summary>
		/// Save a namespace to usings.
		/// </summary>
		/// <param name="nameSpace">namespace to save</param>
		public static void Save(string nameSpace)
		{
			if (!Usings.Contains(nameSpace))
			{
				var prevUsing = EditorPrefs.GetString(REX_USINGS, "");
				EditorPrefs.SetString(REX_USINGS, prevUsing + "|" + nameSpace);
			}
		}

		/// <summary>
		/// Remove a namespace from the usings.
		/// </summary>
		/// <param name="nameSpace">namespace to remove</param>
		public static void Remove(string nameSpace)
		{
			if (Usings.Contains(nameSpace))
			{
				var prevUsing = EditorPrefs.GetString(REX_USINGS, "");
				EditorPrefs.SetString(REX_USINGS, prevUsing.Replace("|" + nameSpace, ""));
			}
		}
	}
}
