using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rex.Utilities.Helpers
{
    public static class UsingsHandler
    {
        public static HashSet<string> Usings { get; private set; }
        #region Macros

        /// <summary>
        /// Loads saved namespaces into the <see cref="Usings"/> <see cref="HashSet{string}"/>
        /// </summary>
        public static void LoadUsings()
        {
            if (Usings == null)
                Usings = new HashSet<string>();
            else
                Usings.Clear();

            if (File.Exists(RexUtils.UsingsFileName))
            {
                foreach (var aUsing in File.ReadAllLines(RexUtils.UsingsFileName))
                {
                    Usings.Add(aUsing);
                }
            }
            else
            {
                using (File.Create(RexUtils.UsingsFileName)) { }
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
                if (!File.Exists(RexUtils.UsingsFileName))
                    File.Create(RexUtils.UsingsFileName);

                using (var writer = File.AppendText(RexUtils.UsingsFileName)) writer.WriteLine(nameSpace);
                Usings.Add(nameSpace);
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
                Usings.Remove(nameSpace);
                File.WriteAllLines(RexUtils.UsingsFileName, Usings.ToArray());
            }
        }
        #endregion
    }
}
