using Rex.Utilities.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rex.Utilities.Input
{

    /// <summary>
    /// The State of the 
    /// </summary>
    [Flags]
    public enum InputState
    {
        NoInput,
        Typing,
        IntelliSelect,
        Execute = 4
    };

    /// <summary>
    /// Input State Machine.
    /// </summary>
    public static class ISM
    {
        public static List<CodeCompletion> IntelliSenceHelp { get; set; }
        /// <summary>
        /// Should the <see cref="IntelliSenceHelp"/> be displayed
        /// </summary>
        public static bool DisplayHelp { get; set; }
        /// <summary>
        /// Which index of the <see cref="IntelliSenceHelp"/> is currently selected.
        /// </summary>
        public static int SelectedHelp { get; set; }
        /// <summary>
        /// If the current <see cref="Code"/> should be replaced by <see cref="ReplacementCode"/>
        /// </summary>
        public static bool ShouldReplaceCode { get; set; }
        /// <summary>
        /// Code that will replace the current <see cref="ReplacementCode"/>
        /// </summary>
        public static string ReplacementCode { get; set; }

        /// <summary>
        /// Code string from the user. 
        /// </summary>
        public static string Code { get; set; }
        /// <summary>
        /// Action for loging debug messages.
        /// </summary>
        public static Action<string> DebugLog { get; set; }

        /// <summary>
        /// Action for repainting the window.
        /// </summary>
        public static Action Repaint { get; set; }
        /// <summary>
        /// Action which will execute the <see cref="Code"/> in the input.
        /// </summary>
        public static Action<string> ExecuteCode { get; set; }

        static InputState currState = InputState.NoInput;
        public static string IntelliSenceLastCode = string.Empty;

        static ISM()
        {
            Code = string.Empty;
            ShouldReplaceCode = false;
            IntelliSenceHelp = new List<CodeCompletion>();
            InputBuffer = new Dictionary<_KeyCode, KeyInput>();
        }

        /// <summary>
        /// The current <see cref="InputState"/> of the state machine.
        /// </summary>
        public static InputState State
        {
            get { return currState; }
            private set
            {
                //DebugLog(currState + " -> " + value);
                currState = value;
            }
        }

        /// <summary>
        /// Updates the <see cref="State"/> of the machine
        /// </summary>
        public static void Update()
        {
            switch (State)
            {
                case InputState.NoInput:
                    {
                        Update_NoInput();
                        return;
                    }
                case InputState.Typing:
                    {
                        Update_Typing();
                        return;
                    }
                case InputState.IntelliSelect:
                    {
                        Update_IntelliSelect();
                        return;
                    }
                default:
                    Enter_NoInput();
                    return;
            }
        }

        ///// <summary>
        ///// Takes the time it takes to pefrom the proved <see cref="Action"/>
        ///// </summary>
        ///// <param name="message"></param>
        ///// <param name="action"></param>
        //public static void StopwatchTest(string message, Action action)
        //{
        //    var sw = System.Diagnostics.Stopwatch.StartNew();
        //    action();
        //    sw.Stop();
        //    if (sw.Elapsed.TotalSeconds >= 0.5)
        //        DebugLog(message + " : " + sw.Elapsed);
        //}

        #region NoInput
        /// <summary>
        /// Clears the codestring and sets the state to NoInput
        /// </summary>
        public static void Enter_NoInput()
        {
            State = InputState.NoInput;
            DisplayHelp = false;
        }

        /// <summary>
        /// Moves to Typing when any key is pressed.
        /// </summary>
        public static void Update_NoInput()
        {
            // Clear code on state change
            //if (IsKeyDown(KeyCode.UpArrow))
            //{
            //    Enter_HistorySelection();
            //}
            if (AnyKeyDown(_KeyCode.LeftArrow, _KeyCode.RightArrow, _KeyCode.UpArrow, _KeyCode.DownArrow))
            {
                Enter_Typing();
            }
        }
        #endregion

        #region Typing
        /// <summary>
        /// Enters typing mode
        /// Intellisence is started and displayed.
        /// </summary>
        public static void Enter_Typing()
        {
            State = InputState.Typing;
            DisplayHelp = true;
            SelectedHelp = -1;
        }
        /// <summary>
        /// if downkey is pressed and intellisence is being displayed: enters intelliselect
        /// otherwise executes if Return is pressed.
        /// </summary>
        public static void Update_Typing()
        {
            if (IsKeyDown(_KeyCode.DownArrow) && IntelliSenceHelp.Any())
            {
                Enter_IntelliSelect();
            }
            else if (DisplayHelp && IsKeyDown(_KeyCode.Tab) && IntelliSenceHelp.Count > 0)
            {
                SelectedHelp = 0;
                UseIntelliSelection();
                Exit_IntelliSelect();
            }
            else if (IsKeyDown(_KeyCode.Return))
            {
                // Replace code with selected history element.
                Exit_Typing();
                Execute();
                return;
            }
            else if (IsKeyDown(_KeyCode.Escape))
            {
                DisplayHelp = false;
            }

            var parseResult = RexHelper.ParseAssigment(Code);
            if (IntelliSenceLastCode != parseResult.ExpressionString)
            {
                DisplayHelp = true;
                IntelliSenceLastCode = parseResult.ExpressionString;
                IntelliSenceHelp = RexHelper.Intellisence(Code).ToList();
                SelectedHelp = -1;
                Repaint();
            }
        }
        /// <summary>
        /// Stops displaying <see cref="IntelliSenceHelp"/>
        /// </summary>
        public static void Exit_Typing()
        {
            DisplayHelp = false;
        }
        #endregion

        #region HistorySelect
        //List<HistoryItem> history;
        //string originalCode = "";
        //void Enter_HistorySelection()
        //{
        //    if (ExpressionHistory.Count > 0)
        //    {
        //        history = ExpressionHistory.Values.ToList();
        //        history.Reverse();
        //        historySelect = 0;
        //        state = InputState.HistorySelect;
        //        originalCode = code;
        //        ReplaceCode(history[historySelect].Compile.Parse.ExpressionString);
        //    }
        //    displayIntelliSense = false;
        //    Repaint();
        //}
        //
        //void Update_HistorySelection()
        //{
        //    if (IsKeyDown(KeyCode.UpArrow))
        //    {
        //        // newer history element gets focus.
        //        historySelect++;
        //        if (historySelect == history.Count)
        //            historySelect = history.Count - 1;
        //        ReplaceCode(history[historySelect].Compile.Parse.ExpressionString);
        //        Repaint();
        //    }
        //    else if (IsKeyDown(KeyCode.DownArrow))
        //    {
        //        // earlier history element gets focus
        //        historySelect--;
        //        if (historySelect < 0)
        //            historySelect = 0;
        //        ReplaceCode(history[historySelect].Compile.Parse.ExpressionString);
        //        //DebugLog(history[historySelect].Compile.Parse.ExpressionString);
        //        Repaint();
        //    }
        //    else if (IsKeyDown(KeyCode.Return))
        //    {
        //        // Replace code with selected history element.
        //        Execute();
        //    }
        //    else if (IsKeyDown(KeyCode.Escape))
        //    {
        //        ReplaceCode(originalCode);
        //        Enter_Input();
        //    }
        //}
        #endregion

        #region IntelliSelect
        public static void Enter_IntelliSelect()
        {
            if (IntelliSenceHelp.Count > 0)
            {
                State = InputState.IntelliSelect;
                SelectedHelp++;
                if (SelectedHelp >= IntelliSenceHelp.Count)
                    SelectedHelp = IntelliSenceHelp.Count - 1;
            }
        }
        public static void Update_IntelliSelect()
        {
            if (IsKeyDown(_KeyCode.DownArrow))
            {
                SelectedHelp++;
                if (SelectedHelp >= IntelliSenceHelp.Count)
                    SelectedHelp = IntelliSenceHelp.Count - 1;
                Repaint();
            }
            else if (IsKeyDown(_KeyCode.UpArrow))
            {
                SelectedHelp--;
                if (SelectedHelp < 0)
                    SelectedHelp = 0;
                Repaint();
            }
            else if (IsKeyDown(_KeyCode.Return) || IsKeyDown(_KeyCode.Tab))
            {
                UseIntelliSelection();
                Exit_IntelliSelect();
            }
            else if (IsKeyDown(_KeyCode.Escape))
            {
                Exit_IntelliSelect();
            }
            else if (AnyKeyDown())
            {
                Exit_IntelliSelect();
            }
        }
        public static void Exit_IntelliSelect()
        {
            SelectedHelp = -1;
            DisplayHelp = false;
            Enter_Typing();
        }

        /// <summary>
        /// Signals the end of intelliselection.
        /// Adds the chosen intelliSence item to the current code string and replaces it.
        /// </summary>
        public static void UseIntelliSelection()
        {
            ReplacementCode = ReplacementString();
            if (ReplacementCode != Code)
            {
                ShouldReplaceCode = true;
                SelectedHelp = -1;
            }
        }

        public static string ReplacementString()
        {
            if (SelectedHelp < 0 || SelectedHelp >= IntelliSenceHelp.Count(i => !i.IsMethodOverload))
                return Code;

            var completion = IntelliSenceHelp[SelectedHelp];
            if (IntelliSenceHelp.Any(i => i.IsMethodOverload))
            {
                completion = IntelliSenceHelp.Where(i => !i.IsMethodOverload).ToArray()[SelectedHelp];
            }
            return Code.Substring(0, completion.Start) + completion.ReplaceString + Code.Substring(completion.End + 1);
        }
        #endregion

        #region Execute
        /// <summary>
        /// Executes the <see cref="Code"/>
        /// </summary>
        /// <param name="clearInput"></param>
        public static void Execute()
        {
            State = InputState.Execute;
            ExecuteCode(Code);
            Enter_NoInput();
        }
        #endregion

        #region Key Input
        public static Dictionary<_KeyCode, KeyInput> InputBuffer { get; private set; }

        public class KeyInput
        {
            public KeyInput()
            {
                LastPressed = DateTime.Now;
                IsHandled = false;
            }
            public DateTime LastPressed { get; set; }
            public bool IsHandled { get; set; }

            public static readonly TimeSpan DelayAmount = TimeSpan.FromMilliseconds(125);
            public override string ToString()
            {
                return LastPressed + "  H:" + IsHandled;
            }
        }

        /// <summary>
        /// Returns the state of the keypress and sets it to false
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsKeyDown(_KeyCode key)
        {
            if (InputBuffer.ContainsKey(key) &&
                InputBuffer[key].IsHandled == false)
            {
                InputBuffer[key].IsHandled = true;
                return true;
            }
            return false;
        }
        public static bool AnyKeyDown()
        {
            return InputBuffer.Count > 0 ? InputBuffer.Any(k => !k.Value.IsHandled) : false;
        }
        public static bool AnyKeyDown(params _KeyCode[] except)
        {
            return InputBuffer.Count > 0 ? InputBuffer.Any(k => !k.Value.IsHandled && !except.Contains(k.Key)) : false;
        }
        public static void PressKey(_KeyCode key)
        {
            if (key != _KeyCode.None)
                InputBuffer[key] = new KeyInput();
        }
        #endregion
    }
}
