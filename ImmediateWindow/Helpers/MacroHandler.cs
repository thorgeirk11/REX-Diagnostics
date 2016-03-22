using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rex.Utilities.Helpers
{
    public static class MacroHandler
    {
        private static Dictionary<string, string> MacroDic;
        public static IEnumerable<string> Macros { get { return MacroDic.Values; } }

        static MacroHandler()
        { Loaded = false; }
        public static bool Loaded { get; private set; }
        #region Macros
        public static void LoadMacros()
        {
            MacroDic = new Dictionary<string, string>();
            if (Directory.Exists(Utils.MacroDirectory))
            {
                try
                {
                    foreach (var macroFile in Directory.GetFiles(Utils.MacroDirectory))
                    {
                        try
                        {
                            MacroDic.Add(macroFile, File.ReadAllText(macroFile));
                        }
                        catch (Exception)
                        { throw; }
                    }
                }
                catch (Exception)
                { throw; }
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(Utils.MacroDirectory);
                }
                catch (Exception)
                { throw; }
            }
            Loaded = true;
        }
        public static void Save(string mactro)
        {
            if (!Macros.Contains(mactro))
            {
                try
                {
                    if (Directory.Exists(Utils.MacroDirectory))
                    {
                        var filePath = Utils.MacroDirectory + Path.DirectorySeparatorChar + Guid.NewGuid();

                        using (var file = File.Create(filePath))
                        using (var stream = new StreamWriter(file))
                        {
                            stream.Write(mactro);
                        }
                        MacroDic.Add(filePath, mactro);
                    }
                }
                catch
                { throw; }
            }
        }
        public static void Remove(string mactro)
        {
            if (MacroDic.ContainsValue(mactro))
            {
                var file = MacroDic.First(i => i.Value == mactro).Key;
                MacroDic.Remove(file);
                File.Delete(file);
            }
        }
        #endregion
    }
}
