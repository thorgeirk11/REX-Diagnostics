using Rex.Utilities;
using Rex.Utilities.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rex.Utilities.Test
{
	[TestFixture]
	class IntelliSenseTests
	{
		[SetUp]
		public void Setup()
		{
			RexUtils.LoadNamespaceInfos(true);
			Parser = new RexParser();
		}

		RexParser Parser { get; set; }

		[Test]
		public void SimpleIntellisensTest()
		{
			SetVar("x", new DummyOutput());

			var helpInfo = Parser.Intellisense("x.").Select(i => i.Details.ToString());
			CollectionAssert.Contains(helpInfo, "object Value { get; set; }");
			CollectionAssert.Contains(helpInfo, "string ToString()");
			CollectionAssert.Contains(helpInfo, "Func<string> toString");

			helpInfo = Parser.Intellisense("x.ToStrin").Select(i => i.Details.ToString());
			Assert.AreEqual(2, helpInfo.Count());
			CollectionAssert.Contains(helpInfo, "string ToString()");
			CollectionAssert.Contains(helpInfo, "Func<string> toString"); ;
		}

		[Test]
		public void IntellisenseCaseInsensitveTest()
		{
			//Start of expression
			EqualIntellisense("Mat", "mat");
			EqualIntellisense("Direc", "direc");
			EqualIntellisense("Stri", "stri");

			//Inside static class:
			EqualIntellisense("Math.A", "Math.a");
			EqualIntellisense("Math.Ab", "Math.ab");
			EqualIntellisense("Math.ABS", "Math.abs");
			EqualIntellisense("Math.Abs", "Math.abs");

			// Fields:
			EqualIntellisense("Math.P", "Math.p");
			EqualIntellisense("Math.PI", "Math.pi");

			// Variables:
			SetVar("myVar", new DummyOutput());
			EqualIntellisense("myVar.V", "myVar.v");
			EqualIntellisense("myVar.Va", "myVar.va");
		}

		struct ArgsTester
		{
			public void WithDefaultArg(int myInt = 42) { }
			public void WithoutDefaultArg(int myInt) { }
		}

		[Test]
		public void DefaultArgs()
		{
			// Variables:
			SetVar("myVar", new ArgsTester());
			var results = Parser.Intellisense("myVar.With").ToArray();
			var with = results[0].ToString();
			var without = results[1].ToString();

			Assert.AreEqual("void WithDefaultArg(int myInt = 42)", with);
			Assert.AreEqual("void WithoutDefaultArg(int myInt)", without);
		}

		private void EqualIntellisense(string search1, string search2)
		{
			var help1 = Parser.Intellisense(search1).Select(i => i.Details.ToString());
			var help2 = Parser.Intellisense(search2).Select(i => i.Details.ToString());
			CollectionAssert.AreEqual(help1, help2);
			CollectionAssert.IsNotEmpty(help1);
		}

		[Test]
		public void SimpleStaticIntellisensTest()
		{
			var helpInfo = Parser.Intellisense("Math.Ab").Select(i => i.Details.ToString());
			Assert.AreEqual(7, helpInfo.Count());
			CollectionAssert.Contains(helpInfo, "sbyte Abs(sbyte value)");

			helpInfo = Parser.Intellisense("Math.Abs.").Select(i => i.Details.ToString());
			Assert.IsEmpty(helpInfo);

			helpInfo = Parser.Intellisense("Math.Abs(").Select(i => i.Details.ToString());
			Assert.AreEqual(7, helpInfo.Count());
			CollectionAssert.Contains(helpInfo, "sbyte Abs(sbyte value)");
		}

		[Test]
		public void TypeNotFoundTest()
		{
			var helpInfo = Parser.Intellisense("ASDASDASDASDASDASD");
			Assert.IsEmpty(helpInfo);
			helpInfo = Parser.Intellisense("Math.PI.ToString().g");
			Assert.IsNotEmpty(helpInfo);
		}

		[Test]
		public void GetNestedNameTest()
		{
			var name = Parser.GetNestedName(typeof(NestedTest.MyNestedClass.NestTest2));
			Assert.AreEqual("NestedTest.MyNestedClass.NestTest2", name);

			name = Parser.GetNestedName(typeof(NestedTest.MyNestedClass));
			Assert.AreEqual("NestedTest.MyNestedClass", name);

			name = Parser.GetNestedName(typeof(NestedTest));
			Assert.AreEqual("NestedTest", name);
		}

		[Test]
		public void SimpleMethodTest()
		{
			var helpInfo = Parser.Intellisense("Math.PI.ToString()").Select(i => i.Details.ToString()); ;
			Assert.IsEmpty(helpInfo);

			helpInfo = Parser.Intellisense("a = new Action(").Select(i => i.Details.ToString());
			CollectionAssert.Contains(helpInfo, "Action");
			CollectionAssert.Contains(helpInfo, "Action<T>");
			CollectionAssert.Contains(helpInfo, "Action<T1, T2>");

			helpInfo = Parser.Intellisense("Math.PI.GetHashCode().ToString(").Select(i => i.Details.ToString());
			Assert.IsEmpty(helpInfo);
		}

		[Test]
		public void SimpleAfterMethodTest()
		{
			var helpInfo = Parser.Intellisense("Math.Abs(-2).MaxValue");
			Assert.IsEmpty(helpInfo);

			var match = Regex.Match("Math.Abs(-2).MaxValue", RexParser.DotAfterMethodRegex);
			var possibleMethods = Parser.PossibleMethods(match);
			Assert.AreEqual(7, possibleMethods.Count());


			match = Regex.Match("Math.ToString().Lenght", RexParser.DotAfterMethodRegex);
			possibleMethods = Parser.PossibleMethods(match);
			Assert.AreEqual(1, possibleMethods.Count());

			var afterMethod = Parser.Intellisense("Math.ToString().").ToList();
			var SameType = Parser.Intellisense("Math.PI.ToString().").ToList();

			Assert.AreEqual(SameType.Count, afterMethod.Count);
			for (int i = 0; i < afterMethod.Count; i++)
			{
				afterMethod[i].Details.IsEquivelent(SameType[i].Details);
			}
		}

		[Test]
		public void DeepIntellisensTest()
		{
			var x = new RecursiveTest(null);
			SetVar("x", new RecursiveTest(x));

			var helpInfo = Parser.Intellisense("x.Recursion").Select(i => i.Details.ToString());
			Assert.AreEqual(2, helpInfo.Count());
			CollectionAssert.Contains(helpInfo, "RecursiveTest RecursionProp { get; set; }");
			CollectionAssert.Contains(helpInfo, "readonly RecursiveTest RecursionField");


			helpInfo = Parser.Intellisense("x.RecursionProp.Recursion").Select(i => i.Details.ToString());
			Assert.AreEqual(2, helpInfo.Count());
			CollectionAssert.Contains(helpInfo, "RecursiveTest RecursionProp { get; set; }");
			CollectionAssert.Contains(helpInfo, "readonly RecursiveTest RecursionField");

			helpInfo = Parser.Intellisense("x.RecursionField.Recursion").Select(i => i.Details.ToString());
			Assert.AreEqual(2, helpInfo.Count());
			CollectionAssert.Contains(helpInfo, "RecursiveTest RecursionProp { get; set; }");
			CollectionAssert.Contains(helpInfo, "readonly RecursiveTest RecursionField");
		}

		[Test]
		public void VariableInterfaceTest()
		{
			SetVar("TheA", new VariableA
			{
				TheInterface = new B
				{
					InnerB = new B()
				}
			});

			var helpInfo = Parser.Intellisense("TheA.TheIn").Select(i => i.Details.ToString());
			Assert.AreEqual(1, helpInfo.Count());
			CollectionAssert.Contains(helpInfo, "I_B TheInterface { get; set; }");

			helpInfo = Parser.Intellisense("TheA.TheInterface.").Select(i => i.Details.ToString());
			CollectionAssert.Contains(helpInfo, "double DoYouSeeMe { get; }");

			helpInfo = Parser.Intellisense("TheA.TheInterface.InnerB.").Select(i => i.Details.ToString());
			CollectionAssert.Contains(helpInfo, "double DoYouSeeMe { get; }");
		}

		[Test]
		public void KeywordToTypeTest()
		{
			var helpInfo1 = Parser.Intellisense("Double.").Select(i => i.Details.ToString());
			var helpInfo2 = Parser.Intellisense("double.").Select(i => i.Details.ToString());
			Assert.AreEqual(helpInfo1, helpInfo2);
			Assert.IsNotEmpty(helpInfo1);

			helpInfo1 = Parser.Intellisense("Int32.").Select(i => i.Details.ToString());
			helpInfo2 = Parser.Intellisense("int.").Select(i => i.Details.ToString());
			Assert.AreEqual(helpInfo1, helpInfo2);
			Assert.IsNotEmpty(helpInfo1);

			helpInfo1 = Parser.Intellisense("Boolean.").Select(i => i.Details.ToString());
			helpInfo2 = Parser.Intellisense("bool.").Select(i => i.Details.ToString());
			Assert.AreEqual(helpInfo1, helpInfo2);
			Assert.IsNotEmpty(helpInfo1);

			helpInfo1 = Parser.Intellisense("String.").Select(i => i.Details.ToString());
			helpInfo2 = Parser.Intellisense("string.").Select(i => i.Details.ToString());
			Assert.AreEqual(helpInfo1, helpInfo2);
			Assert.IsNotEmpty(helpInfo1);
		}

		[Test]
		public void DoNotShowStaticOnInstanceTest()
		{
			var helpInfo = Parser.Intellisense("Math.PI.");
			Assert.IsNotEmpty(helpInfo);
			Assert.IsEmpty(from h in helpInfo
						   where h.Details.Contains(Syntax.ConstKeyword) ||
								 h.Details.Contains(Syntax.StaticKeyword)
						   select h);

			helpInfo = Parser.Intellisense("Math.PI.GetType().");
			Assert.IsEmpty(from h in helpInfo
						   where h.Details.Contains(Syntax.ConstKeyword) ||
								 h.Details.Contains(Syntax.StaticKeyword)
						   select h);
			Assert.IsNotEmpty(helpInfo);
		}

		[Test]
		public void ShowVariblesTest()
		{
			RexHelper.Variables.Clear();
			SetVar("DoubleX", 1.0);
			SetVar("DoubleY", 1.0);
			SetVar("DoubleZ", 1.0);
			var helpInfo = Parser.Intellisense("Double");
			Assert.IsNotEmpty(helpInfo);
			var names = helpInfo.Select(i => i.Details.Name.String);
			foreach (var v in RexHelper.Variables)
			{
				CollectionAssert.Contains(names, v.Key);
			}
		}

		[Test]
		public void InvokeTest()
		{
			IntellisenseInvokeTest invoker;
			invoker = SetVar("invoker", new IntellisenseInvokeTest());

			var helpInfo = Parser.Intellisense("invoker.Counter").Select(i => i.Details.ToString()).First();
			Assert.AreEqual("int Counter { get; }", helpInfo);
			Assert.AreEqual(0, invoker.counter);
		}

		[Test]
		public void StaticInterfaceTest()
		{
			var helpInfo = Parser.Intellisense("StaticA.TheIn").Select(i => i.Details.ToString());
			Assert.AreEqual(1, helpInfo.Count());
			CollectionAssert.Contains(helpInfo, "static I_B TheInterface { get; set; }");

			helpInfo = Parser.Intellisense("StaticA.TheInterface.").Select(i => i.Details.ToString());
			CollectionAssert.Contains(helpInfo, "double DoYouSeeMe { get; }");

			helpInfo = Parser.Intellisense("StaticA.TheInterface.InnerB.").Select(i => i.Details.ToString());
			CollectionAssert.Contains(helpInfo, "double DoYouSeeMe { get; }");
		}

		[Test]
		public void OutRefParaTest()
		{
			var helpInfo = Parser.Intellisense("Double.TryParse").First();
			Assert.True(helpInfo.Details.Any(i => i.Type == SyntaxType.Keyword && i.String == "out"));

			helpInfo = Parser.Intellisense("OutRefTest.RefMethod").First();
			Assert.True(helpInfo.Details.Any(i => i.Type == SyntaxType.Keyword && i.String == "ref"));

			helpInfo = Parser.Intellisense("OutRefTest.OutMethod").First();
			Assert.True(helpInfo.Details.Any(i => i.Type == SyntaxType.Keyword && i.String == "out"));
		}

		[Test]
		public void PerformanceTest()
		{
			SpeedTestAction(() => Parser.Intellisense("Mat"), 100);
			SpeedTestAction(() => Parser.Intellisense("Math.PI."), 100);
			SpeedTestAction(() => Parser.Intellisense("Math.PI.ToString()."), 100);
			SpeedTestAction(() => Parser.Intellisense("Math.PI.ToString().Length.MaxValue"), 100);
		}

		[Test]
		public void ColorizingTest()
		{
			var syntax = new[] {
				Syntax.StaticKeyword,
				Syntax.Space,Syntax.NewType("int"),
				Syntax.Space,Syntax.Name("num"),
				Syntax.Space,Syntax.EqualsOp,
				Syntax.Space,Syntax.ConstVal("5")
			};

			var syntaxHighlighting = RexUtils.SyntaxHighlingting(new MemberDetails(syntax), RexUtils.SyntaxHighlightColors);
			var regex = new Regex("<color.*>(?<const>.*)</color> <color.*>(?<type>.*)</color> (?<name>.*) = <color.*>(?<value>.*)</color>");
			var match = regex.Match(syntaxHighlighting);
			Assert.IsTrue(match.Success);

			var _const = match.Groups["const"];
			var type = match.Groups["type"];
			var name = match.Groups["name"];
			var value = match.Groups["value"];

			Assert.AreEqual(syntax.First(i => i.Type == SyntaxType.Keyword).String, _const.Value);
			Assert.AreEqual(syntax.First(i => i.Type == SyntaxType.Type).String, type.Value);
			Assert.AreEqual(syntax.First(i => i.Type == SyntaxType.Name).String, name.Value);
			Assert.AreEqual(syntax.First(i => i.Type == SyntaxType.ConstVal).String, value.Value);
		}

		[Test]
		public void ColorizingSearchTest()
		{
			var syntax = new MemberDetails(new[] {
				Syntax.StaticKeyword,
				Syntax.Space, Syntax.NewType("int"),
				Syntax.Space, Syntax.Name("myNumber"),
				Syntax.Space, Syntax.EqualsOp,
				Syntax.Space, Syntax.ConstVal("5")
			});

			var syntaxHighlighting = RexUtils.SyntaxHighlingting(syntax, RexUtils.SyntaxHighlightColors, "myNum");
			TestRest(syntax, syntaxHighlighting, "<b>myNum</b>ber");

			syntaxHighlighting = RexUtils.SyntaxHighlingting(syntax, RexUtils.SyntaxHighlightColors, "Num");
			TestRest(syntax, syntaxHighlighting, "my<b>Num</b>ber");

			syntaxHighlighting = RexUtils.SyntaxHighlingting(syntax, RexUtils.SyntaxHighlightColors, "Number");
			TestRest(syntax, syntaxHighlighting, "my<b>Number</b>");
		}

		private static void TestRest(MemberDetails syntax, string highlight, string search)
		{
			var regex = new Regex("<color.*>(?<const>.*)</color> <color.*>(?<type>.*)</color> (?<name>.*) = <color.*>(?<value>.*)</color>");
			var match = regex.Match(highlight);
			Assert.IsTrue(match.Success);

			var name = match.Groups["name"];
			Assert.AreEqual(search, name.Value);
			var _const = match.Groups["const"];
			var type = match.Groups["type"];
			var value = match.Groups["value"];
			Assert.AreEqual(syntax.First(i => i.Type == SyntaxType.Keyword).String, _const.Value);
			Assert.AreEqual(syntax.First(i => i.Type == SyntaxType.Type).String, type.Value);
			Assert.AreEqual(syntax.First(i => i.Type == SyntaxType.ConstVal).String, value.Value);
		}

		[Test]
		public void ColorizingPerformanceTest()
		{
			var mathPIHelp = Parser.Intellisense("Math.PI.");
			SpeedTestAction(() =>
			{
				foreach (var help in mathPIHelp)
				{
					RexUtils.SyntaxHighlingting(help.Details, RexUtils.SyntaxHighlightColors);
				}
			}, 100);
		}

		[Test]
		public void ShouldNotGetHelp()
		{
			Assert.IsEmpty(Parser.Intellisense("Dictonary<int,int>"));
			Assert.IsEmpty(Parser.Intellisense("Func<int,int>(i => i."));
		}

		[Test]
		public void LoadNamspacePerformanceTest()
		{
			SpeedTestAction(() => RexUtils.LoadNamespaceInfos(true), 2);
		}


		[Test]
		public void ReplacementTest()
		{
			RexHelper.Variables.Clear();
			SetVar("MyDoubleX", 1.0);
			SetVar("MyDoubleY", 1.0);
			SetVar("MyDoubleZ", 1.0);
			var helpInfo = Parser.Intellisense("MyDoubl");
			foreach (var item in helpInfo)
			{
				Assert.IsTrue(item.ReplaceString.Contains("MyDouble"));
				Assert.AreEqual(0, item.Start);
				Assert.AreEqual(6, item.End);
			}

			helpInfo = Parser.Intellisense("Math.PI.GetHashCode().ToStr");
			Assert.IsNotEmpty(helpInfo);
			foreach (var item in helpInfo)
			{
				Assert.IsTrue(item.ReplaceString.Contains("ToString"));
				Assert.AreEqual(22, item.Start);
				Assert.AreEqual(26, item.End);
			}

			helpInfo = Parser.Intellisense("x = Math.PI.ToStr");
			foreach (var item in helpInfo)
			{
				Assert.IsTrue(item.ReplaceString.Contains("ToString"));
				Assert.AreEqual("x = Math.PI.".Length, item.Start);
				Assert.AreEqual("x = Math.PI.ToStr".Length - 1, item.End);
			}

			//var x = Math.Tan()
			helpInfo = Parser.Intellisense("x = Math.Tan(Math.P");
			foreach (var item in helpInfo)
			{
				if (!item.IsMethodOverload)
				{
					Assert.AreEqual("x = Math.Tan(Math.".Length, item.Start);
					Assert.AreEqual("x = Math.Tan(Math.P".Length - 1, item.End);
				}
			}

			SetVar<string>("MyVar", null);
			helpInfo = Parser.Intellisense("x = MyVar");
			foreach (var item in helpInfo)
			{
				if (!item.IsMethodOverload)
				{
					Assert.AreEqual("x = ".Length, item.Start);
					Assert.AreEqual("x = MyVar".Length - 1, item.End);
				}
			}

		}


		void SpeedTestAction(Action ToDo, int n)
		{
			var sw = new Stopwatch();
			sw.Start();
			for (int i = 0; i < n; i++)
			{
				ToDo();
			}
			sw.Stop();
			Console.WriteLine("Time: {0:0.000}", sw.ElapsedMilliseconds / (1.0 * n));
		}


		private static T SetVar<T>(string name, T val)
		{
			return (T)(RexHelper.Variables[name] = new RexHelper.Variable { VarValue = val, VarType = typeof(T) }).VarValue;
		}

	}

	public static class OutRefTest
	{
		public static void RefMethod(ref int x) { }
		public static void OutMethod(out int x) { x = 5; }
	}

	public interface I_B
	{
		double DoYouSeeMe { get; }
		I_B InnerB { get; }
	}

	public class B : I_B
	{
		public double DoYouSeeMe { get; set; }
		public I_B InnerB { get; set; }
	}

	public class VariableA
	{
		public I_B TheInterface { get; set; }
	}
	public static class StaticA
	{
		public static I_B TheInterface { get; set; }
	}


	public class IntellisenseInvokeTest
	{
		public int counter = 0;
		public int Counter { get { counter++; return counter; } }
	}


	public class RecursiveTest
	{
		public RecursiveTest(RecursiveTest x)
		{
			RecursionField = x;
			RecursionProp = x;
		}
		public RecursiveTest RecursionProp { get; set; }
		public readonly RecursiveTest RecursionField;
	}
}
