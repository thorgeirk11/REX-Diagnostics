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
		[Test]
		public void UsingsTest()
		{
			RexUsingsHandler.Save("Lol");
			Assert.IsTrue(RexUsingsHandler.Usings.Contains("Lol"));

			RexUsingsHandler.Remove("Lol");
			CollectionAssert.DoesNotContain(RexUsingsHandler.Usings, "Lol");
		}

		[Test]
		public void MacroTest()
		{
			var before = RexMacroHandler.LoadMacros();
			foreach (var item in before)
			{
				RexMacroHandler.Remove(item);
			}

			Assert.IsEmpty(RexMacroHandler.LoadMacros());
			Assert.AreEqual(new[] { "Lol" }, RexMacroHandler.Save("Lol"));
			Assert.AreEqual(new[] { "Lol" }, RexMacroHandler.LoadMacros());
			Assert.IsEmpty(RexMacroHandler.Remove("Lol"));
			Assert.IsEmpty(RexMacroHandler.LoadMacros());
			Assert.AreEqual(new[] { "Lol1" }, RexMacroHandler.Save("Lol1"));
			Assert.AreEqual(new[] { "Lol1", "Lol2" }, RexMacroHandler.Save("Lol2"));
			Assert.AreEqual(new[] { "Lol1", "Lol2", "Lol3" }, RexMacroHandler.Save("Lol3"));
			Assert.AreEqual(new[] { "Lol1", "Lol2", "Lol3" }, RexMacroHandler.LoadMacros());

			foreach (var item in before)
			{
				RexMacroHandler.Save(item);
			}
		}
	}
}
