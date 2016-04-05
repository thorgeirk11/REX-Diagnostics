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
			UsingsHandler.Save("Lol");
			Assert.IsTrue(UsingsHandler.Usings.Contains("Lol"));

			UsingsHandler.Remove("Lol");
			Assert.IsEmpty(UsingsHandler.Usings);
		}

		[Test]
		public void MacroTest()
		{
			RexMacroHandler.LoadMacros();
			Assert.IsEmpty(RexMacroHandler.Macros);

			RexMacroHandler.Save("Lol");
			Assert.IsTrue(RexMacroHandler.Macros.Contains("Lol"));
			RexMacroHandler.Remove("Lol");
			Assert.IsEmpty(RexMacroHandler.Macros);

			RexMacroHandler.Save("Lol1");
			RexMacroHandler.Save("Lol2");
			RexMacroHandler.Save("Lol3");
			Assert.AreEqual(new[] { "Lol1", "Lol2", "Lol3" }, RexMacroHandler.Macros);
		}
	}
}
