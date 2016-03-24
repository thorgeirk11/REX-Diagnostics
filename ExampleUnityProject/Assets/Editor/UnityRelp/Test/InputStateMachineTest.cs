using NUnit.Framework;
using Rex.Utilities;
using Rex.Utilities.Helpers;
using Rex.Utilities.Input;
using Rex.Utilities.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rex.Utilities.Test
{
    [TestFixture]
    class InputStateMachineTest
    {
        [SetUp]
        public void ClassSetup()
        {
            ISM.Repaint = () => { };
            ISM.DebugLog = msg => Console.WriteLine(msg);
            ISM.ExecuteCode = TestExecute;
            ISM.Code = string.Empty;
            ISM.Enter_NoInput();
            ISM.IntelliSenceHelp.Clear();
            ISM.IntelliSenceLastCode = string.Empty;
            ISM.InputBuffer.Clear();
        }

        [Test]
        public void UpdateTest()
        {
            Assert.AreEqual(InputState.NoInput, ISM.State);

            ISM.Update();
            Assert.AreEqual(InputState.NoInput, ISM.State);

            //ISM.Code = "Math.PI";
            ISM.Enter_Typing();
            Assert.AreEqual(InputState.Typing, ISM.State);
            Assert.IsTrue(ISM.DisplayHelp);

            ISM.Update();
            Assert.AreEqual(InputState.Typing, ISM.State);
            Assert.IsTrue(ISM.DisplayHelp);

            PressKey(_KeyCode.Escape);
            ISM.Update();
            Assert.AreEqual(InputState.Typing, ISM.State);
            Assert.IsFalse(ISM.DisplayHelp);
            Assert.IsEmpty(ISM.IntelliSenceHelp);
        }

        [Test]
        public void IntellisenseTest()
        {
            ISM.Enter_Typing();
            ISM.Code = "Math.P";
            ISM.Update();
            Assert.AreEqual(InputState.Typing, ISM.State);
            Assert.IsTrue(ISM.DisplayHelp);
            Assert.IsNotEmpty(ISM.IntelliSenceHelp);
        }

        [Test]
        public void IntellisenseSelectionTest()
        {
            ISM.Enter_Typing();
            ISM.Code = "Math.P";
            ISM.Update();
            Assert.AreEqual(InputState.Typing, ISM.State);
            Assert.IsTrue(ISM.DisplayHelp);
            Assert.IsNotEmpty(ISM.IntelliSenceHelp);
            Assert.AreEqual(ISM.SelectedHelp, -1);

            PressKey(_KeyCode.DownArrow);
            Assert.AreEqual(InputState.IntelliSelect, ISM.State);
            Assert.AreEqual(ISM.SelectedHelp, 0);

            ISM.Enter_Typing();
            Assert.AreEqual(InputState.Typing, ISM.State);
            Assert.AreEqual(ISM.SelectedHelp, -1);

            ISM.Code = "x = Math.";
            ISM.Update();
            PressKey(_KeyCode.DownArrow);
            Assert.AreEqual(InputState.IntelliSelect, ISM.State);
            Assert.AreEqual(ISM.SelectedHelp, 0);


            PressKey(_KeyCode.DownArrow);
            Assert.AreEqual(InputState.IntelliSelect, ISM.State);
            Assert.AreEqual(ISM.SelectedHelp, 1);

            PressKey(_KeyCode.Return);
            Assert.IsTrue(ISM.Code.Contains("x = Math."));
        }

        [Test]
        public void SelectHelpTest()
        {
            TestIntelliSelect("Math.Abs(Math.P",
                new[] { "Math.Abs(Math.PI", "Math.Abs(Math.Pow" });
            TestIntelliSelect("Math.Abs(String.Em",
                new[] { "Math.Abs(String.Empty", "Math.Abs(String.IsNullOrEmpty" });
            TestIntelliSelect("Math.Abs(TypeCode.",
                Enum.GetNames(typeof(TypeCode)).OrderBy(i => i).Select(i => "Math.Abs(TypeCode." + i));

            TestIntelliSelect("Math.Abs( 1, 2, Math.P",
                new[] { "Math.Abs( 1, 2, Math.PI", "Math.Abs( 1, 2, Math.Pow" });
            TestIntelliSelect("Math.Abs( 1, 2, String.Em",
                new[] { "Math.Abs( 1, 2, String.Empty", "Math.Abs( 1, 2, String.IsNullOrEmpty" });
            TestIntelliSelect("Math.Abs( 1, 2, TypeCode.",
                Enum.GetNames(typeof(TypeCode)).OrderBy(i => i).Select(i => "Math.Abs( 1, 2, TypeCode." + i));

            //Math.Cos(20).E
            TestIntelliSelect("Math.Cos(20).E",
                new[] { "Math.Cos(20).Equals" }, false);

            TestIntelliSelect("Math.P",
                new[] { "Math.PI", "Math.Pow" }, false);
            TestIntelliSelect("Math.E",
                new[] { "Math.E", "Math.Exp", "Math.IEEERemainder" }, false);

            TestIntelliSelect("Environme",
                new[] { "Environment" }, false);
        }

        [Test]
        public void NestedTypeTest()
        {
            // Nested Types, need to chcek them.
            TestIntelliSelect("MyNestedClass",
                new[] { "NestedTest.MyNestedClass" }, false);
        }

        void TestIntelliSelect(string code, IEnumerable<string> selections, bool withMethodOverloads = true)
        {
            ISM.Enter_Typing();
            ISM.Code = code;
            ISM.Update();
            Assert.AreEqual(InputState.Typing, ISM.State);
            Assert.IsTrue(ISM.DisplayHelp);

            var help = ISM.IntelliSenceHelp.Where(i => !i.IsMethodOverload);
            var meth = ISM.IntelliSenceHelp.Where(i => i.IsMethodOverload);

            Assert.IsNotEmpty(help);
            if (withMethodOverloads)
                Assert.IsNotEmpty(meth);
            else
                Assert.IsEmpty(meth);

            Assert.AreEqual(-1, ISM.SelectedHelp);
            var count = 0;
            foreach (var select in selections)
            {
                PressKey(_KeyCode.DownArrow);
                Assert.AreEqual(select, ISM.ReplacementString(), "At i = " + count);
                Assert.AreEqual(count++, ISM.SelectedHelp);
            }
        }

        public void TestExecute(string code)
        {
            Console.WriteLine(code);
            RexHelper.Messages[MsgType.None].Clear();
            RexHelper.Messages[MsgType.Info].Clear();
            RexHelper.Messages[MsgType.Warning].Clear();
            RexHelper.Messages[MsgType.Error].Clear();
            RexHelperTest.CompileAndRun(code);
        }
        public static void PressKey(_KeyCode key, int repeat = 1)
        {
            for (int i = 0; i < repeat; i++)
            {
                ISM.PressKey(key);
                ISM.Update();
            }
        }
    }

    public class NestedTest
    {
        public class MyNestedClass
        {
            public class NestTest2 { }
        }
    }
}
