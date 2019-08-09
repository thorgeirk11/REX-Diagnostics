using Rex.Utilities.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Rex.Utilities.Input
{

	/// <summary>
	/// The State of the 
	/// </summary>
	[Flags]
	public enum RexInputState
	{
		NoInput,
		Typing,
		IntelliSelect,
		Execute = 4
	};

    /// <summary>
    /// Input State Machine.
    /// </summary>
    public static class RexISM
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

        static RexInputState currState = RexInputState.NoInput;
        public static string IntelliSenceLastCode = string.Empty;
        static IRexParser Parser { get; set; }
        static IRexIntellisenseProvider IntellisenseProvider { get; set; }

        static RexISM()
        {
            Code = string.Empty;
            ShouldReplaceCode = false;
            IntelliSenceHelp = new List<CodeCompletion>();
            InputBuffer = new Dictionary<KeyCode, KeyInput>();
            var parser = new RexParser();
            Parser = parser;
            IntellisenseProvider = parser;
        }

        /// <summary>
        /// The current <see cref="RexInputState"/> of the state machine.
        /// </summary>
        public static RexInputState State
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
                case RexInputState.NoInput:
                    {
                        Update_NoInput();
                        return;
                    }
                case RexInputState.Typing:
                    {
                        Update_Typing();
                        return;
                    }
                case RexInputState.IntelliSelect:
                    {
                        Update_IntelliSelect();
                        return;
                    }
                default:
                    Enter_NoInput();
                    return;
            }
        }

        #region NoInput
        /// <summary>
        /// Clears the codestring and sets the state to NoInput
        /// </summary>
        public static void Enter_NoInput()
        {
            State = RexInputState.NoInput;
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
            if (AnyKeyDown(KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.DownArrow))
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
            State = RexInputState.Typing;
            DisplayHelp = true;
            SelectedHelp = -1;
        }
        /// <summary>
        /// if downkey is pressed and intellisence is being displayed: enters intelliselect
        /// otherwise executes if Return is pressed.
        /// </summary>
        public static void Update_Typing()
        {
            if (IsKeyDown(KeyCode.DownArrow) && IntelliSenceHelp.Any())
            {
                Enter_IntelliSelect();
            }
            else if (IsKeyDown(KeyCode.Tab) && IntelliSenceHelp.Count > 0)
            {
                SelectedHelp = 0;
                UseIntelliSelection();
                return;
            }
            else if (IsKeyDown(KeyCode.Return) || IsKeyDown(KeyCode.KeypadEnter))
			{
				// Replace code with selected history element.
				Exit_Typing();
				Execute();
				return;
			}
			else if (IsKeyDown(KeyCode.Escape))
			{
				DisplayHelp = false;
			}

			var parseResult = Parser.ParseAssignment(Code);
			if (IntelliSenceLastCode != parseResult.ExpressionString)
			{
				DisplayHelp = true;
				IntelliSenceLastCode = parseResult.ExpressionString;
				IntelliSenceHelp = IntellisenseProvider.Intellisense(Code).ToList();
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

		#region IntelliSelect
		public static void Enter_IntelliSelect()
		{
			if (IntelliSenceHelp.Count > 0)
			{
				State = RexInputState.IntelliSelect;
				SelectedHelp++;
				if (SelectedHelp >= IntelliSenceHelp.Count)
					SelectedHelp = IntelliSenceHelp.Count - 1;
			}
		}
		public static void Update_IntelliSelect()
		{
			if (IsKeyDown(KeyCode.DownArrow))
			{
				SelectedHelp++;
				if (SelectedHelp >= IntelliSenceHelp.Count)
					SelectedHelp = IntelliSenceHelp.Count - 1;
				Repaint();
			}
			else if (IsKeyDown(KeyCode.UpArrow))
			{
				SelectedHelp--;
				if (SelectedHelp < 0)
					SelectedHelp = 0;
				Repaint();
			}
			else if (IsKeyDown(KeyCode.Return) || IsKeyDown(KeyCode.Tab))
			{
				UseIntelliSelection();
				Exit_IntelliSelect();
			}
			else if (IsKeyDown(KeyCode.Escape))
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
				IntelliSenceHelp.Clear();
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
		public static void Execute()
		{
			State = RexInputState.Execute;
			ExecuteCode(Code);
			Enter_NoInput();
		}
		#endregion

		#region Key Input
		public static Dictionary<KeyCode, KeyInput> InputBuffer { get; private set; }

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
		public static bool IsKeyDown(KeyCode key)
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
		public static bool AnyKeyDown(params KeyCode[] except)
		{
			return InputBuffer.Count > 0 ? InputBuffer.Any(k => !k.Value.IsHandled && !except.Contains(k.Key)) : false;
		}
		public static void PressKey(KeyCode key)
		{
			if (key != KeyCode.None)
				InputBuffer[key] = new KeyInput();
		}
		#endregion
	}
}
