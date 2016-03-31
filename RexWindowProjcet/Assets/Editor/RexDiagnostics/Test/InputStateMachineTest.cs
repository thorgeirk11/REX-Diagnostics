using NUnit.Framework;
using Rex.Utilities;
using Rex.Utilities.Helpers;
using Rex.Utilities.Input;
using Rex.Utilities.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Rex.Utilities.Test
{
    [TestFixture]
    class InputStateMachineTest
    {
        [SetUp]
        public void ClassSetup()
        {
            RexISM.Repaint = () => { };
            RexISM.DebugLog = msg => Console.WriteLine(msg);
            RexISM.ExecuteCode = TestExecute;
            RexISM.Code = string.Empty;
            RexISM.Enter_NoInput();
            RexISM.IntelliSenceHelp.Clear();
            RexISM.IntelliSenceLastCode = string.Empty;
            RexISM.InputBuffer.Clear();
        }

        [Test]
        public void UpdateTest()
        {
            Assert.AreEqual(RexInputState.NoInput, RexISM.State);

            RexISM.Update();
            Assert.AreEqual(RexInputState.NoInput, RexISM.State);

            RexISM.Enter_Typing();
            Assert.AreEqual(RexInputState.Typing, RexISM.State);
            Assert.IsTrue(RexISM.DisplayHelp);

            RexISM.Update();
            Assert.AreEqual(RexInputState.Typing, RexISM.State);
            Assert.IsTrue(RexISM.DisplayHelp);

            PressKey(KeyCode.Escape);
            RexISM.Update();
            Assert.AreEqual(RexInputState.Typing, RexISM.State);
            Assert.IsFalse(RexISM.DisplayHelp);
            Assert.IsEmpty(RexISM.IntelliSenceHelp);
        }

        [Test]
        public void IntellisenseTest()
        {
            RexISM.Enter_Typing();
            RexISM.Code = "Math.P";
            RexISM.Update();
            Assert.AreEqual(RexInputState.Typing, RexISM.State);
            Assert.IsTrue(RexISM.DisplayHelp);
            Assert.IsNotEmpty(RexISM.IntelliSenceHelp);
        }

        [Test]
        public void IntellisenseSelectionTest()
        {
            RexISM.Enter_Typing();
            RexISM.Code = "Math.P";
            RexISM.Update();
            Assert.AreEqual(RexInputState.Typing, RexISM.State);
            Assert.IsTrue(RexISM.DisplayHelp);
            Assert.IsNotEmpty(RexISM.IntelliSenceHelp);
            Assert.AreEqual(RexISM.SelectedHelp, -1);

            PressKey(KeyCode.DownArrow);
            Assert.AreEqual(RexInputState.IntelliSelect, RexISM.State);
            Assert.AreEqual(RexISM.SelectedHelp, 0);

            RexISM.Enter_Typing();
            Assert.AreEqual(RexInputState.Typing, RexISM.State);
            Assert.AreEqual(RexISM.SelectedHelp, -1);

            RexISM.Code = "x = Math.";
            RexISM.Update();
            PressKey(KeyCode.DownArrow);
            Assert.AreEqual(RexInputState.IntelliSelect, RexISM.State);
            Assert.AreEqual(RexISM.SelectedHelp, 0);


            PressKey(KeyCode.DownArrow);
            Assert.AreEqual(RexInputState.IntelliSelect, RexISM.State);
            Assert.AreEqual(RexISM.SelectedHelp, 1);

            PressKey(KeyCode.Return);
            Assert.IsTrue(RexISM.Code.Contains("x = Math."));
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
            RexISM.Enter_Typing();
            RexISM.Code = code;
            RexISM.Update();
            Assert.AreEqual(RexInputState.Typing, RexISM.State);
            Assert.IsTrue(RexISM.DisplayHelp);

            var help = RexISM.IntelliSenceHelp.Where(i => !i.IsMethodOverload);
            var meth = RexISM.IntelliSenceHelp.Where(i => i.IsMethodOverload);

            Assert.IsNotEmpty(help);
            if (withMethodOverloads)
                Assert.IsNotEmpty(meth);
            else
                Assert.IsEmpty(meth);

            Assert.AreEqual(-1, RexISM.SelectedHelp);
            var count = 0;
            foreach (var select in selections)
            {
                PressKey(KeyCode.DownArrow);
                Assert.AreEqual(select, RexISM.ReplacementString(), "At i = " + count);
                Assert.AreEqual(count++, RexISM.SelectedHelp);
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
        public static void PressKey(KeyCode key, int repeat = 1)
        {
            for (int i = 0; i < repeat; i++)
            {
                RexISM.PressKey(key);
                RexISM.Update();
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
