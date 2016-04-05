using System.Collections.Generic;
using System.Diagnostics;

namespace Rex.Utilities.Helpers
{
	public class RexMacroHandler
	{
		private const string REX_MACRO_NAME = "rex_macro_";

		public static List<string> LoadMacros()
		{
			var sw = Stopwatch.StartNew();
			var macros = new List<string>();
			var i = 1;
			while (UnityEditor.EditorPrefs.HasKey(REX_MACRO_NAME + i))
			{
				var macro = UnityEditor.EditorPrefs.GetString(REX_MACRO_NAME + i, null);
				if (macro == null) break;
				macros.Add(macro);
				i++;
			}
			UnityEngine.Debug.Log("LoadMacros: " + sw.ElapsedMilliseconds);
			return macros;
		}
		public static List<string> Save(string macro)
		{
			var macros = LoadMacros();
			if (!macros.Contains(macro))
			{
				macros.Add(macro);
				SaveMacros(macros);
			}
			return macros;
		}
		public static List<string> Remove(string macro)
		{
			var macros = LoadMacros();
			if (macros.Contains(macro))
			{
				macros.Remove(macro);
				SaveMacros(macros);
			}
			return macros;
		}

		private static void SaveMacros(IEnumerable<string> macros)
		{
			int i = 1;
			foreach (var macro in macros)
			{
				UnityEditor.EditorPrefs.SetString(REX_MACRO_NAME + i, macro);
				i++;
			}
			while (UnityEditor.EditorPrefs.HasKey(REX_MACRO_NAME + i))
			{
				UnityEditor.EditorPrefs.DeleteKey(REX_MACRO_NAME + i);
				i++;
			}
		}
	}
}
