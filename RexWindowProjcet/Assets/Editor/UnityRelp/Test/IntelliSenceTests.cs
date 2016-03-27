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
    class IntelliSenceTests
    {
        [SetUp]
        public void Setup()
        {
            RexUtils.UsingsFileName = "testUsings.txt";
            RexUtils.MacroDirectory = "TestMacros";
            RexUtils.LoadNamespaceInfos(true);
        }

        [Test]
        public void SimpleIntellisensTest()
        {
            SetVar("x", new DummyOutput());

            var helpInfo = RexHelper.Intellisence("x.").Select(i => i.Details.ToString());
            CollectionAssert.Contains(helpInfo, "object Value { get ; set ; }");
            CollectionAssert.Contains(helpInfo, "string ToString ( )");
            CollectionAssert.Contains(helpInfo, "Func < string > toString");

            helpInfo = RexHelper.Intellisence("x.ToStrin").Select(i => i.Details.ToString());
            Assert.AreEqual(2, helpInfo.Count());
            CollectionAssert.Contains(helpInfo, "string ToString ( )");
            CollectionAssert.Contains(helpInfo, "Func < string > toString"); ;
        }

        [Test]
        public void SimpleStaticIntellisensTest()
        {
            var helpInfo = RexHelper.Intellisence("Math.Ab").Select(i => i.Details.ToString());
            Assert.AreEqual(7, helpInfo.Count());
            CollectionAssert.Contains(helpInfo, "static sbyte Abs ( sbyte value )");

            helpInfo = RexHelper.Intellisence("Math.Abs.").Select(i => i.Details.ToString());
            Assert.IsEmpty(helpInfo);

            helpInfo = RexHelper.Intellisence("Math.Abs(").Select(i => i.Details.ToString());
            Assert.AreEqual(7, helpInfo.Count());
            CollectionAssert.Contains(helpInfo, "static sbyte Abs ( sbyte value )");
        }

        [Test]
        public void TypeNotFoundTest()
        {
            var helpInfo = RexHelper.Intellisence("ASDASDASDASDASDASD");
            Assert.IsEmpty(helpInfo);
            helpInfo = RexHelper.Intellisence("Math.PI.ToString().g");
            Assert.IsNotEmpty(helpInfo);
        }

        [Test]
        public void GetNestedNameTest()
        {
            var name = RexHelper.GetNestedName(typeof(NestedTest.MyNestedClass.NestTest2));
            Assert.AreEqual("NestedTest.MyNestedClass.NestTest2", name);

            name = RexHelper.GetNestedName(typeof(NestedTest.MyNestedClass));
            Assert.AreEqual("NestedTest.MyNestedClass", name);

            name = RexHelper.GetNestedName(typeof(NestedTest));
            Assert.AreEqual("NestedTest", name);
        }

        [Test]
        public void SimpleMethodTest()
        {
            var helpInfo = RexHelper.Intellisence("Math.PI.ToString()").Select(i => i.Details.ToString()); ;
            Assert.IsEmpty(helpInfo);

            helpInfo = RexHelper.Intellisence("a = new Action(").Select(i => i.Details.ToString());
            CollectionAssert.Contains(helpInfo, "Action");
            CollectionAssert.Contains(helpInfo, "Action < T >");
            CollectionAssert.Contains(helpInfo, "Action < T1 , T2 >");

            helpInfo = RexHelper.Intellisence("Math.PI.GetHashCode().ToString(").Select(i => i.Details.ToString());
            Assert.IsEmpty(helpInfo);
        }

        [Test]
        public void SimpleAfterMethodTest()
        {
            var helpInfo = RexHelper.Intellisence("Math.Abs(-2).MaxValue");
            Assert.IsEmpty(helpInfo);

            var match = RexUtils.DotAfterMethodRegex.Match("Math.Abs(-2).MaxValue");
            var possibleMethods = RexHelper.PossibleMethods(match);
            Assert.AreEqual(7, possibleMethods.Count());


            match = RexUtils.DotAfterMethodRegex.Match("Math.ToString().Lenght");
            possibleMethods = RexHelper.PossibleMethods(match);
            Assert.AreEqual(1, possibleMethods.Count());

            var afterMethod = RexHelper.Intellisence("Math.ToString().").ToList();
            var SameType = RexHelper.Intellisence("Math.PI.ToString().").ToList();

            Assert.AreEqual(SameType.Count, afterMethod.Count);
            for (int i = 0; i < afterMethod.Count; i++)
            {
                afterMethod[i].Details.IsEquivelent(SameType[i].Details);
            }
        }

        [Test]
        public void AfterMethodTest()
        {
            var afterMethod = RexHelper.Intellisence("Math.ToString().ToString().");
            afterMethod = RexHelper.Intellisence("Math.PI.ToString().Length.MaxValue");

            //Assert.IsEmpty(RexHelper.Intellisence("Math.Max(Math.Abs(),"));
            //var insideMeth = RexHelper.Intellisence("Math.Abs(Math.Abs(20),Math.").ToList();
            //var mathDot = RexHelper.Intellisence("Math.").ToList();
            //Assert.AreEqual(insideMeth, mathDot);
        }
        [Test]
        public void DeepIntellisensTest()
        {
            var x = new RecursiveTest(null);
            SetVar("x", new RecursiveTest(x));

            var helpInfo = RexHelper.Intellisence("x.Recursion").Select(i => i.Details.ToString());
            Assert.AreEqual(2, helpInfo.Count());
            CollectionAssert.Contains(helpInfo, "RecursiveTest RecursionProp { get ; set ; }");
            CollectionAssert.Contains(helpInfo, "readonly RecursiveTest RecursionField");


            helpInfo = RexHelper.Intellisence("x.RecursionProp.Recursion").Select(i => i.Details.ToString());
            Assert.AreEqual(2, helpInfo.Count());
            CollectionAssert.Contains(helpInfo, "RecursiveTest RecursionProp { get ; set ; }");
            CollectionAssert.Contains(helpInfo, "readonly RecursiveTest RecursionField");

            helpInfo = RexHelper.Intellisence("x.RecursionField.Recursion").Select(i => i.Details.ToString());
            Assert.AreEqual(2, helpInfo.Count());
            CollectionAssert.Contains(helpInfo, "RecursiveTest RecursionProp { get ; set ; }");
            CollectionAssert.Contains(helpInfo, "readonly RecursiveTest RecursionField");
        }

        [Test]
        public void VariableInterfaceTest()
        {
            VariableA TheA;
            SetVar("TheA", TheA = new VariableA
            {
                TheInterface = new B
                {
                    InnerB = new B()
                }
            });

            var helpInfo = RexHelper.Intellisence("TheA.TheIn").Select(i => i.Details.ToString());
            Assert.AreEqual(1, helpInfo.Count());
            CollectionAssert.Contains(helpInfo, "I_B TheInterface { get ; set ; }");

            helpInfo = RexHelper.Intellisence("TheA.TheInterface.").Select(i => i.Details.ToString());
            CollectionAssert.Contains(helpInfo, "double DoYouSeeMe { get ; }");

            helpInfo = RexHelper.Intellisence("TheA.TheInterface.InnerB.").Select(i => i.Details.ToString());
            CollectionAssert.Contains(helpInfo, "double DoYouSeeMe { get ; }");
        }

        [Test]
        public void KeywordToTypeTest()
        {
            var helpInfo1 = RexHelper.Intellisence("Double.").Select(i => i.Details.ToString());
            var helpInfo2 = RexHelper.Intellisence("double.").Select(i => i.Details.ToString());
            Assert.AreEqual(helpInfo1, helpInfo2);

            helpInfo1 = RexHelper.Intellisence("Int32.").Select(i => i.Details.ToString());
            helpInfo2 = RexHelper.Intellisence("int.").Select(i => i.Details.ToString());
            Assert.AreEqual(helpInfo1, helpInfo2);

            helpInfo1 = RexHelper.Intellisence("Boolean.").Select(i => i.Details.ToString());
            helpInfo2 = RexHelper.Intellisence("bool.").Select(i => i.Details.ToString());

            helpInfo1 = RexHelper.Intellisence("String.").Select(i => i.Details.ToString());
            helpInfo2 = RexHelper.Intellisence("string.").Select(i => i.Details.ToString());
            Assert.AreEqual(helpInfo1, helpInfo2);
        }

        [Test]
        public void DoNotShowStaticOnInstanceTest()
        {
            var helpInfo = RexHelper.Intellisence("Math.PI.");
            Assert.IsNotEmpty(helpInfo);
            Assert.False(helpInfo.Any(i => i.Details.Contains(Syntax.ConstKeyword) || i.Details.Contains(Syntax.StaticKeyword)));

            helpInfo = RexHelper.Intellisence("Math.PI.GetType().");
            Assert.IsNotEmpty(helpInfo);
        }

        [Test]
        public void ShowVariblesTest()
        {
            RexHelper.Variables.Clear();
            SetVar("DoubleX", 1.0);
            SetVar("DoubleY", 1.0);
            SetVar("DoubleZ", 1.0);
            var helpInfo = RexHelper.Intellisence("Double");
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

            var helpInfo = RexHelper.Intellisence("invoker.Counter").Select(i => i.Details.ToString()).First();
            Assert.AreEqual("int Counter { get ; }", helpInfo);
            Assert.AreEqual(0, invoker.counter);
        }

        [Test]
        public void StaticInterfaceTest()
        {
            var helpInfo = RexHelper.Intellisence("StaticA.TheIn").Select(i => i.Details.ToString());
            Assert.AreEqual(1, helpInfo.Count());
            CollectionAssert.Contains(helpInfo, "static I_B TheInterface { get ; set ; }");

            helpInfo = RexHelper.Intellisence("StaticA.TheInterface.").Select(i => i.Details.ToString());
            CollectionAssert.Contains(helpInfo, "double DoYouSeeMe { get ; }");

            helpInfo = RexHelper.Intellisence("StaticA.TheInterface.InnerB.").Select(i => i.Details.ToString());
            CollectionAssert.Contains(helpInfo, "double DoYouSeeMe { get ; }");
        }

        [Test]
        public void OutRefParaTest()
        {
            var helpInfo = RexHelper.Intellisence("Double.TryParse").First();
            Assert.True(helpInfo.Details.Any(i => i.Type == SyntaxType.Keyword && i.String == "out"));

            helpInfo = RexHelper.Intellisence("OutRefTest.RefMethod").First();
            Assert.True(helpInfo.Details.Any(i => i.Type == SyntaxType.Keyword && i.String == "ref"));

            helpInfo = RexHelper.Intellisence("OutRefTest.OutMethod").First();
            Assert.True(helpInfo.Details.Any(i => i.Type == SyntaxType.Keyword && i.String == "out"));
        }

        [Test]
        public void PerformanceTest()
        {
            SpeedTestAction(() => RexHelper.Intellisence("Mat"), 100);
            SpeedTestAction(() => RexHelper.Intellisence("Math.PI."), 100);
            SpeedTestAction(() => RexHelper.Intellisence("Math.PI.ToString()."), 100);
            SpeedTestAction(() => RexHelper.Intellisence("Math.PI.ToString().Length.MaxValue"), 100);
        }

        [Test]
        public void ColorizingTest()
        {
            var syntax = new[] {
                Syntax.StaticKeyword,
                Syntax.NewType("int"),
                Syntax.Name("num"),
                Syntax.EqualsOp,
                Syntax.ConstVal("5")
            };

            var syntaxHighlighting = RexUtils.SyntaxHighlingting(new MemberDetails(syntax), RexUtils.SyntaxHighlightColors);
            var regex = new Regex("<color.*>(?<const>.*)</color> <color.*>(?<type>.*)</color> (?<name>.*) = (?<value>.*)");
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
                Syntax.NewType("int"),
                Syntax.Name("myNumber"),
                Syntax.EqualsOp,
                Syntax.ConstVal("5")
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
            var regex = new Regex("<color.*>(?<const>.*)</color> <color.*>(?<type>.*)</color> (?<name>.*) = (?<value>.*)");
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
            var mathPIHelp = RexHelper.Intellisence("Math.PI.");
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
            Assert.IsEmpty(RexHelper.Intellisence("Dictonary<int,int>"));
            Assert.IsEmpty(RexHelper.Intellisence("Func<int,int>(i => i."));
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
            var helpInfo = RexHelper.Intellisence("MyDoubl");
            foreach (var item in helpInfo)
            {
                Assert.IsTrue(item.ReplaceString.Contains("MyDouble"));
                Assert.AreEqual(0, item.Start);
                Assert.AreEqual(6, item.End);
            }

            helpInfo = RexHelper.Intellisence("Math.PI.GetHashCode().ToStr");
            Assert.IsNotEmpty(helpInfo);
            foreach (var item in helpInfo)
            {
                Assert.IsTrue(item.ReplaceString.Contains("ToString"));
                Assert.AreEqual(22, item.Start);
                Assert.AreEqual(26, item.End);
            }

            helpInfo = RexHelper.Intellisence("x = Math.PI.ToStr");
            foreach (var item in helpInfo)
            {
                Assert.IsTrue(item.ReplaceString.Contains("ToString"));
                Assert.AreEqual("x = Math.PI.".Length, item.Start);
                Assert.AreEqual("x = Math.PI.ToStr".Length - 1, item.End);
            }

            //var x = Math.Tan()
            helpInfo = RexHelper.Intellisence("x = Math.Tan(Math.P");
            foreach (var item in helpInfo)
            {
                if (!item.IsMethodOverload)
                {
                    Assert.AreEqual("x = Math.Tan(Math.".Length, item.Start);
                    Assert.AreEqual("x = Math.Tan(Math.P".Length - 1, item.End);
                }
            }

            SetVar<string>("MyVar", null);
            helpInfo = RexHelper.Intellisence("x = MyVar");
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
            return (T)(RexHelper.Variables[name] = new RexHelper.Varible { VarValue = val, VarType = typeof(T) }).VarValue;
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
