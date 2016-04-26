using NUnit.Framework;
using Rex.Utilities;
using System.Linq;

namespace Rex.Utilities.Test
{
	interface InterfaceA
	{
		int One { get; }
	}
	interface InterfaceB : InterfaceA
	{
		int Two { get; }
	}

	class InterfaceTestClass : InterfaceB
	{
		public int One { get; set; }
		public int Two { get; set; }
		public int Three { get; set; }
	}


	[TestFixture]
	class InterfaceTest
	{
		public RexParser Parser { get; private set; }

		[SetUp]
		public void Setup()
		{
			RexUtils.LoadNamespaceInfos(true);
			Parser = new RexParser();
		}

		[Test]
		public void UpdateTest()
		{
			SetVar<InterfaceTestClass, InterfaceA>("ia");
			SetVar<InterfaceTestClass, InterfaceB>("ib");
			SetVar<InterfaceTestClass, InterfaceTestClass>("i");

			var helpInfo = Parser.Intellisence("ia.").Select(i => i.Details.ToString()).ToList();
			CollectionAssert.Contains(helpInfo, "int One { get; }");
			Assert.True(helpInfo.Count() == 1);

			helpInfo = Parser.Intellisence("ib.").Select(i => i.Details.ToString()).ToList();
			CollectionAssert.Contains(helpInfo, "int One { get; }");
			CollectionAssert.Contains(helpInfo, "int Two { get; }");
			Assert.True(helpInfo.Count() == 2);

			helpInfo = Parser.Intellisence("i.").Select(i => i.Details.ToString()).ToList();
			CollectionAssert.Contains(helpInfo, "int One { get; set; }");
			CollectionAssert.Contains(helpInfo, "int Two { get; set; }");
			CollectionAssert.Contains(helpInfo, "int Three { get; set; }");
			Assert.True(helpInfo.Count() >= 3);
		}

		private static TA SetVar<TA, TB>(string name) where TA : TB, new()
		{
			return (TA)(RexHelper.Variables[name] = new RexHelper.Varible { VarValue = new TA(), VarType = typeof(TB) }).VarValue;
		}
	}
}