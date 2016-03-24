using NUnit.Framework;
using Rex.Utilities;
using Rex.Utilities.Helpers;
using System;
using System.IO;
using System.Linq;

namespace Rex.Utilities.Test
{
    [TestFixture]
    class UsingsAndMacroTests
    {
        [SetUp]
        public void ClassSetup()
        {
            RexUtils.UsingsFileName = "testUsings.txt";
            File.Delete(RexUtils.UsingsFileName);

            RexUtils.MacroDirectory = "TestMacros";
            try
            {
                Directory.Delete(RexUtils.MacroDirectory, true);
            }
            catch (Exception)
            { }
        }

        [Test]
        public void UsingsTest()
        {
            UsingsHandler.LoadUsings();

            UsingsHandler.Save("Lol");
            Assert.IsTrue(UsingsHandler.Usings.Contains("Lol"));

            UsingsHandler.Usings.Remove("Lol");

            UsingsHandler.LoadUsings();
            Assert.IsTrue(UsingsHandler.Usings.Contains("Lol"));

            UsingsHandler.Remove("Lol");
            Assert.IsEmpty(UsingsHandler.Usings);

            UsingsHandler.LoadUsings();
            Assert.IsEmpty(UsingsHandler.Usings);
        }

        [Test]
        public void UsingsNoFileTest()
        {
            File.Delete(RexUtils.UsingsFileName);

            UsingsHandler.LoadUsings();

            UsingsHandler.Save("Lol");
            Assert.IsTrue(UsingsHandler.Usings.Contains("Lol"));

            UsingsHandler.Usings.Remove("Lol");

            UsingsHandler.LoadUsings();
            Assert.IsTrue(UsingsHandler.Usings.Contains("Lol"));

            UsingsHandler.Remove("Lol");
            Assert.IsEmpty(UsingsHandler.Usings);

            UsingsHandler.LoadUsings();
            Assert.IsEmpty(UsingsHandler.Usings);
        }

        [Test]
        public void MacroTest()
        {
            MacroHandler.LoadMacros();
            Assert.IsEmpty(MacroHandler.Macros);

            MacroHandler.Save("Lol");
            Assert.IsTrue(MacroHandler.Macros.Contains("Lol"));
            MacroHandler.Remove("Lol");
            Assert.IsEmpty(MacroHandler.Macros);

            MacroHandler.Save("Lol1");
            MacroHandler.Save("Lol2");
            MacroHandler.Save("Lol3");
            Assert.AreEqual(new[] { "Lol1", "Lol2", "Lol3" }, MacroHandler.Macros);
        }

        [Test]
        public void MacroNoFileTest()
        {
            //File.Delete(Utils.UsingsFileName);

            UsingsHandler.LoadUsings();

            UsingsHandler.Save("Lol");
            Assert.IsTrue(UsingsHandler.Usings.Contains("Lol"));

            UsingsHandler.Usings.Remove("Lol");

            UsingsHandler.LoadUsings();
            Assert.IsTrue(UsingsHandler.Usings.Contains("Lol"));

            UsingsHandler.Remove("Lol");
            Assert.IsEmpty(UsingsHandler.Usings);

            UsingsHandler.LoadUsings();
            Assert.IsEmpty(UsingsHandler.Usings);
        }
    }
}
