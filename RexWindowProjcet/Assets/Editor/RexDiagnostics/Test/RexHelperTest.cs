using System;
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using Rex.Utilities.Helpers;

namespace Rex.Utilities.Test
{
    [TestFixture]
    public class RexHelperTest
    {
        [SetUp]
        public void ClassSetup()
        {
            RexUtils.UsingsFileName = "testUsings.txt";
            RexUtils.MacroDirectory = "TestMacros";
            RexHelper.Variables.Clear();
            RexHelper.ClearOutput();
            //History = new Dictionary<string, HistoryItem>();

            var expression = "1+1";
            var pResult = RexHelper.ParseAssigment(expression);
            var cResult = RexHelper.Compile(pResult);
            var output = RexHelper.Execute<DummyOutput>(cResult);
            Assert.AreEqual(2, output.Value);
        }

        [SetUp]
        public void Setup()
        {
            RexHelper.Variables.Clear();
            RexHelper.ClearOutput();
            foreach (var output in RexHelper.Messages) output.Value.Clear();
        }

        [Test]
        public void TopLevelNameSpaceTest()
        {
            Assert.AreEqual("System", RexUtils.TopLevelNameSpace("System.Linq"));

            Assert.AreEqual("System", RexUtils.TopLevelNameSpace("System.Linq"));
        }

        [Test]
        public void CompileFailTest()
        {
            var expression = "1 / 0";
            var pResult = RexHelper.ParseAssigment(expression);
            Assert.AreEqual(expression, pResult.ExpressionString);
            Assert.AreEqual(expression, pResult.WholeCode);
            Assert.IsFalse(pResult.IsDeclaring);

            var cResult = RexHelper.Compile(pResult);
            Assert.IsNotEmpty(cResult.Errors);
            Assert.Contains("Division by constant zero\r", cResult.Errors);
        }

        [Test]
        public void ExecuteFailTest()
        {
            SetVar("x", 0);
            var output = CompileAndRun("1 / x");
            Assert.NotNull(output.Exception);
            Assert.IsInstanceOf<DivideByZeroException>(output.Exception);
        }

        [Test]
        public void ValueTypeTest()
        {
            SetVar("x", 0);
            var output = CompileAndRun("x++");
            Assert.AreEqual(0, output.Value);
            Assert.AreEqual(1, RexHelper.Variables["x"].VarValue);

            //ComplexValueType id;

            SetVar("id", new ComplexValueType());
            output = CompileAndRun("id.MyString = \"Lol\"");
            //Value Types can not be modified... :(
            Assert.IsNull(output.Value);
            //Assert.IsNull(id.MyString);
        }

        private static void SetVar<T>(string name, T val)
        {
            RexHelper.Variables[name] = new RexHelper.Varible { VarValue = val, VarType = typeof(T) };
        }

        [Test]
        public void GetCSharpRepTest()
        {
            //Simple types:
            GetCSharpRepTest("System.Int32", typeof(int), true);
            GetCSharpRepTest("int", typeof(int), false);

            //Generic types:
            GetCSharpRepTest("Action<bool, int, Action<IEnumerable<string>>>", typeof(Action<bool, int, Action<IEnumerable<string>>>), false);
            GetCSharpRepTest(
                "System.Action<System.Boolean, System.Int32, System.Action<System.Collections.Generic.IEnumerable<System.String>>>",
                typeof(Action<bool, int, Action<IEnumerable<string>>>), true);

            //nested types:
            GetCSharpRepTest("RexHelperTest.ComplexValueType", typeof(ComplexValueType), false);
            GetCSharpRepTest("Rex.Utilities.Test.RexHelperTest.ComplexValueType", typeof(ComplexValueType), true);


            GetCSharpRepTest("RexHelperTest.ComplexValueType[]", typeof(ComplexValueType[]), false);
            GetCSharpRepTest("Rex.Utilities.Test.RexHelperTest.ComplexValueType[]", typeof(ComplexValueType[]), true);

            GetCSharpRepTest("Action<RexHelperTest.ComplexValueType[]>", typeof(Action<ComplexValueType[]>), false);
            GetCSharpRepTest("System.Action<Rex.Utilities.Test.RexHelperTest.ComplexValueType[]>", typeof(Action<ComplexValueType[]>), true);


            GetCSharpRepTest("Action<RexHelperTest.ComplexValueType>", typeof(Action<ComplexValueType>), false);
            GetCSharpRepTest("System.Action<Rex.Utilities.Test.RexHelperTest.ComplexValueType>", typeof(Action<ComplexValueType>), true);

            GetCSharpRepTest("Rex.Utilities.Test.RexHelperTest.ComplexValueType[]", typeof(ComplexValueType[]), true);


            //Nested Generic types:
            GetCSharpRepTest("RexHelperTest.ComplexValueType<int>", typeof(ComplexValueType<int>), false);
            GetCSharpRepTest("Rex.Utilities.Test.RexHelperTest.ComplexValueType<System.Int32>", typeof(ComplexValueType<int>), true);

            GetCSharpRepTest(
                "System.Action<Rex.Utilities.Test.RexHelperTest.ComplexValueType<System.Int32>>",
                typeof(Action<ComplexValueType<int>>), true);

            GetCSharpRepTest("System.Action", typeof(Action), true);

            Assert.NotNull(RexUtils.GetCSharpRepresentation(typeof(Action<>), true));
        }

        void GetCSharpRepTest(string text, Type type, bool showFull)
        {
            Assert.AreEqual(text, RexUtils.GetCSharpRepresentation(type, showFull).ToString());
        }

        public struct ComplexValueType<T>
        {
            public string MyString { get; set; }
            public Guid ID { get; set; }
            public T Level { get; set; }
        }
        public struct ComplexValueType
        {
            public string MyString { get; set; }
            public Guid ID { get; set; }
            public int Level { get; set; }
        }

        [Test]
        public void ReferenceTypeTest()
        {
            SetVar("x", new List<int>());
            var output = CompileAndRun("x.Add(1)");
            Assert.IsNull(output.Value);
            Assert.AreEqual(new[] { 1 }, RexHelper.Variables["x"].VarValue);
        }

        [Test]
        public void SimpleExpressionTest()
        {
            var expression = "1+1";
            var pResult = RexHelper.ParseAssigment(expression);
            Assert.AreEqual(expression, pResult.ExpressionString);
            Assert.AreEqual(expression, pResult.WholeCode);
            Assert.IsFalse(pResult.IsDeclaring);

            var cResult = RexHelper.Compile(pResult);
            Assert.AreEqual(pResult, cResult.Parse);
            Assert.IsNotNull(cResult.Assembly);

            var output = RexHelper.Execute<DummyOutput>(cResult);
            Assert.AreEqual(2, output.Value);


            output = CompileAndRun("null");
            Assert.IsNull(output.Value);
        }


        [Test]
        public void EnumerableExpressionTest()
        {
            var output = CompileAndRun("new [] { 1, 2, 3, 4, 5 }");
            Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, output.Value);

            Assert.AreEqual(5, output.Members.Count);
            for (int i = 1; i <= output.Members.Count; i++)
            {
                Assert.AreEqual(i, (output.Members[i - 1] as DummyOutput).Value);
            }

            output = CompileAndRun("new [] { \"1\", \"2\", \"3\", null, \"5\" }");
            Assert.AreEqual(new[] { "1", "2", "3", null, "5" }, output.Value);
            Assert.AreEqual(5, output.Members.Count);
            for (int i = 1; i <= output.Members.Count; i++)
            {
                if (i == 4)
                    Assert.IsNull((output.Members[i - 1] as DummyOutput).Value);
                else
                    Assert.AreEqual(i.ToString(), (output.Members[i - 1] as DummyOutput).Value);
            }
        }

        [Test]
        public void AnonymouseExpressionTest()
        {
            var output = CompileAndRun(@"new { One = 1, Two = 2.0, Three = 3f }");
            Assert.AreEqual(new { One = 1, Two = 2.0, Three = 3f }.ToString(), output.Value.ToString());

            var details = RexReflectionHelper.ExtractDetails(output.Value).Select(i => i.ToString()).ToList();
            Assert.Contains("int One { get; } = 1", details);
            Assert.Contains("double Two { get; } = 2", details);
            Assert.Contains("float Three { get; } = 3", details);
        }

        [Test]
        public void SimpleAssigmentTest()
        {
            var expression = "x = 1 + 1";
            var pResult = RexHelper.ParseAssigment(expression);
            Assert.AreEqual("1 + 1", pResult.ExpressionString);
            Assert.AreEqual(expression, pResult.WholeCode);
            Assert.IsTrue(pResult.IsDeclaring);

            var cResult = RexHelper.Compile(pResult);
            Assert.AreEqual(pResult, cResult.Parse);
            Assert.IsNotNull(cResult.Assembly);


            var output = RexHelper.Execute<DummyOutput>(cResult);
            Assert.AreEqual(2, output.Value);
            Assert.AreEqual(2, RexHelper.Variables["x"].VarValue);
        }

        [Test]
        public void AdvancedAssigmentTest()
        {
            var expr = "new Func<int, bool>(i => i < 5)";
            var expression = "x = " + expr;
            var pResult = RexHelper.ParseAssigment(expression);
            Assert.AreEqual(expr, pResult.ExpressionString);
            Assert.AreEqual(expression, pResult.WholeCode);
            Assert.IsTrue(pResult.IsDeclaring);

            var cResult = RexHelper.Compile(pResult);
            Assert.AreEqual(pResult, cResult.Parse);
            Assert.IsNotNull(cResult.Assembly);

            var output = RexHelper.Execute<DummyOutput>(cResult);
            Assert.IsInstanceOf<Func<int, bool>>(output.Value);
            var func = RexHelper.Variables["x"].VarValue as Func<int, bool>;
            Assert.IsTrue(func(1));
            Assert.IsFalse(func(6));
        }

        [Test]
        public void LinqAssigmentTest()
        {
            var expr = "new[] { 1, 2, 3 }.Select(i => i)";
            var expression = "x = " + expr;
            var pResult = RexHelper.ParseAssigment(expression);
            Assert.AreEqual(expr, pResult.ExpressionString);
            Assert.AreEqual(expression, pResult.WholeCode);
            Assert.IsTrue(pResult.IsDeclaring);
            var cResult = RexHelper.Compile(pResult);
            Assert.AreEqual(pResult, cResult.Parse);
            Assert.IsNotNull(cResult.Assembly);

            var output = RexHelper.Execute<DummyOutput>(cResult);
            Assert.IsInstanceOf<IEnumerable<int>>(output.Value);
            var list = RexHelper.Variables["x"].VarValue as IEnumerable<int>;
            Assert.AreEqual(new[] { 1, 2, 3 }, list);

            Assert.AreEqual(CompileAndRun("1+1").Value, 2);
        }


        [Test]
        public void AnonymousAssigmentTest()
        {
            var expr = @"new [] { new { MyString = ""Lol1"", Number = 1 },new { MyString = ""Lol2"", Number = 1 } }";
            var expression = "x = " + expr;
            var output = CompileAndRun(expression);
            Assert.IsEmpty(RexHelper.Variables);
            //Assert.AreEqual("Expression returned an compiler generated class, cannot declare variable 'x'", RexHelper.Messages[MsgType.Warning].Single());
        }
        [Test]
        public void AssigmentPerformaceTest()
        {
            CompileAndRun("x = new [] {1,2,3,4,5}.Select(i => i * 2)");
        }

        public static DummyOutput CompileAndRun(string code)
        {
            var pResult = RexHelper.ParseAssigment(code);
            var cResult = RexHelper.Compile(pResult);
            return RexHelper.Execute<DummyOutput>(cResult);
        }
    }


    public class DummyOutput : AConsoleOutput
    {
        public override void Display()
        {
            throw new NotImplementedException();
        }

        public Func<string> toString;

        public override void LoadInDetails(object value, string message, IEnumerable<MemberDetails> details)
        {
            Value = value;
            Message = message;
        }
        public object Value { get; set; }

        public override string ToString()
        {
            return toString();
        }
    }
}
