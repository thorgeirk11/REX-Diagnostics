using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Rex.Utilities;
using Rex.Utilities.Helpers;
using System.IO;
using Rex.Utilities.Input;
using System.Threading;

namespace Rex.Window
{
	#region Help Classes
	public class CoroutineHandler : MonoBehaviour
	{
		private static CoroutineHandler instance = null;
		public static CoroutineHandler Instance
		{
			get
			{
				if (instance == null)
				{
					instance = (CoroutineHandler)FindObjectOfType(typeof(CoroutineHandler));
					if (instance == null)
						instance = (new GameObject("REX_CoroutineHandler")).AddComponent<CoroutineHandler>();
				}
				//Debug.Log(instance);
				return instance;
			}
			internal set { instance = value; }
		}
	}
	#endregion

	public sealed class RexEditorWindow : EditorWindow
	{
		public static RexEditorWindow instance;

		/// <summary>
		/// Maximunm length of the history.
		/// </summary>
		private const int InputHistoryLength = 50;
		private readonly LinkedList<string> InputHistroy = new LinkedList<string>();

		private readonly Dictionary<string, HistoryItem> ExpressionHistory = new Dictionary<string, HistoryItem>();

		private const string NameOfInputField = "ExpressionInput";

		static readonly string MacroDirectorPath = Application.persistentDataPath + Path.DirectorySeparatorChar + "REX_Macros";
		static readonly string UsingsFile = Application.persistentDataPath + "REX_Usings.txt";

		#region UI
		const double stopwatchTime = 0.5;
		bool updateSkins = true;
		TextEditor inp;

		private DateTime lastExecute;
		private bool lastRunSuccesfull;

		private Vector2 usingScroll;
		private Vector2 scroll;
		private Vector2 scroll2;
		private Vector2 scroll3;
		private Vector2 scroll4;
		private Vector2 intelliScroll;
		private Vector2 intelliOverLoadScroll;

		bool showHistory = true;
		bool showVariables = true;
		bool showMacros = true;
		bool showUsings = false;

		bool quellTab = false;
		bool refocus = false;
		float ratio = 0.6f;

		GUIStyle darkBox;
		GUIStyle box;
		GUIStyle lightBox;
		GUIStyle lightSlimBox;
		GUIStyle slimBox;
		GUIStyle ttStyle;

		// Red button for output removal
		GUIStyle rmvOutputBtn;

		GUIStyle varLabelStyle;
		GUIStyle greenLabelStyle;
		#endregion

		// Add menu named "My Window" to the Window menu
		[MenuItem("Window/(REX) Runtime Expressions")]
		static void Init()
		{
			// Get existing open window or if none, make a new one:
			instance = GetWindow<RexEditorWindow>();
			instance.outputIndex = 0;
			new Thread(() => RexHelper.SetupHelper()).Start();
		}

		void OnEnable()
		{
			ISM.Repaint = Repaint;
			ISM.DebugLog = Debug.Log;
			ISM.ExecuteCode = Execute;
			ISM.Enter_NoInput();

			if (!MacroHandler.Loaded)
			{
				Utils.MacroDirectory = MacroDirectorPath;
				Utils.UsingsFileName = UsingsFile;
				MacroHandler.LoadMacros();
			}

			updateSkins = true;
			minSize = new Vector2(450f, 350f);
			autoRepaintOnSceneChange = true;
			title = "REX";
		}

		/// <summary>
		/// Executes the expression once.
		/// </summary>
		private void Execute(string code)
		{
			RexHelper.Messages[MsgType.None].Clear();
			RexHelper.Messages[MsgType.Info].Clear();
			RexHelper.Messages[MsgType.Warning].Clear();
			RexHelper.Messages[MsgType.Error].Clear();
			lastRunSuccesfull = true;

			if (string.IsNullOrEmpty(code))
				return;

			var parseResult = RexHelper.ParseAssigment(code);
			var compileOutput = RexHelper.Compile(parseResult, ExpressionHistory);
			if (compileOutput != null)
			{
				var output = RexHelper.Execute<ConsoleOutput>(compileOutput);
				if (output != null)
					RexHelper.AddOutput(output);

				if (output == null || output.Exception != null)
					lastRunSuccesfull = false;
			}
			else
				lastRunSuccesfull = false;
			lastExecute = DateTime.Now;
		}

		/// <summary>
		/// GUI Loop
		/// </summary>
		void OnGUI()
		{

			//#if !DEBUG
			//            if (!EditorApplication.isPlaying)
			//            {
			//                EditorGUILayout.HelpBox("Need to be in play mode to evaluate expressions", MessageType.Info);
			//                return;
			//            }
			//#endif

			HandleTabKeyPress();
			UpdateSkins();

			Rect inpRect, inpLabelRect, inpStringRect, intelliRect, layoutRect, inpButRect;
			GetRects(out inpRect, out inpLabelRect, out inpButRect, out inpStringRect, out intelliRect, out layoutRect);
			HandleKeyInput();

			DisplayInputField(ref inpLabelRect, ref inpStringRect, ref inpButRect);

			if (refocus)
			{
				refocus = false;
				GUI.FocusControl(NameOfInputField);
				inp = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
				if (inp != null)
				{
					inp.cursorIndex = ISM.Code.Length + 1;
					inp.selectIndex = ISM.Code.Length + 1;
				}
			}
			bool hasFocus = GUI.GetNameOfFocusedControl() == NameOfInputField;

			HandleInputShortcuts(hasFocus);
			HandleInputHistory(hasFocus);
			var prevRect = intelliRect;
			intelliRect = DisplayIntellisense(intelliRect, hasFocus, true);

			layoutRect = DisplayMessages(layoutRect);

			#region MainLayout
			GUILayout.BeginArea(layoutRect, "");
			{
				DrawMainLayout(layoutRect);
			}
			GUILayout.EndArea();
			#endregion

			DisplayIntellisense(prevRect, hasFocus, true);
			DisplayTooltip();
		}

		private void DisplayInputField(ref Rect inpLabelRect, ref Rect inpStringRect, ref Rect inpButRect)
		{
			GUI.Label(inpLabelRect, new GUIContent("Expression:"));
			Color oldColor = ColorInput();
			GUI.SetNextControlName(NameOfInputField);
			ISM.Code = GUI.TextField(inpStringRect, ISM.Code);

			//To have the cursor change to the 'I' on hover:
			GUI.color = Color.clear;
			EditorGUI.TextField(inpStringRect, "");

			GUI.color = oldColor;

			if (GUI.Button(inpButRect, new GUIContent("Evaluate", "Evaluates the expression")))
			{
				GUI.FocusControl(NameOfInputField);
				ISM.PressKey(_KeyCode.Return);
			}
		}

		private Color ColorInput()
		{
			var evaluateLerpTime = TimeSpan.FromSeconds(1.5);
			var oldColor = GUI.color;
			TimeSpan timeSinceLastExecute = DateTime.Now - lastExecute;
			if (timeSinceLastExecute < evaluateLerpTime)
			{
				var fromColor = Color.clear;
				if (lastRunSuccesfull)
				{
					fromColor = Color.green;
				}
				else
				{
					fromColor = Color.yellow;
				}

				var delta = timeSinceLastExecute.TotalMilliseconds / evaluateLerpTime.TotalMilliseconds;
				GUI.color = Color.Lerp(fromColor, oldColor, (float)delta);
			}

			return oldColor;
		}

		private void DisplayTooltip()
		{
			if (GUI.tooltip.Length > 0 && Application.isPlaying)
			{
				var cont = new GUIContent(GUI.tooltip);
				var ttsize = ttStyle.CalcSize(cont);

				float width = Mathf.Min(300f, ttsize.x);
				float height = ttStyle.CalcHeight(cont, width);

				var mousePosition = Event.current.mousePosition;
				var rect = new Rect(mousePosition.x + 10f, mousePosition.y + 20f, width, height);

				//Clamp to screen size
				if (rect.xMax > Screen.width) rect.x += Screen.width - rect.xMax;
				if (rect.height + rect.yMax > Screen.height) rect.y += Screen.height - (rect.height + rect.yMax);
				if (rect.xMin < 0) rect.x -= rect.xMin;
				if (rect.yMin < 0) rect.y -= rect.yMin;

				GUI.Box(rect, "");
				GUI.Label(rect, GUI.tooltip, ttStyle);
			}
		}

		private static Rect DisplayMessages(Rect layoutRect)
		{
			var helpboxStyle = GUI.skin.FindStyle("HelpBox");
			bool areAnyErrors = false;
			foreach (var infoType in MessageInfos)
			{
				foreach (var msg in RexHelper.Messages[infoType])
				{
					EditorGUI.HelpBox(layoutRect, msg, (MessageType)infoType);
					var rect = helpboxStyle.CalcSize(new GUIContent(msg));
					layoutRect.yMin += rect.y * 2;
					areAnyErrors = true;
				}
			}
			if (areAnyErrors)
			{
				var newRect = new Rect(layoutRect);
				newRect.height = 20;
				if (GUI.Button(newRect, "Clear Messages"))
					RexHelper.Messages.Values.ToList().ForEach(i => i.Clear());
				layoutRect.yMin += newRect.height;
			}
			return layoutRect;
		}

		private void HandleInputHistory(bool hasFocus)
		{
			if (InputHistroy.Count == 0 ||
							InputHistroy.First.Value != ISM.Code)
			{
				if (InputHistroy.Count > InputHistoryLength)
				{
					InputHistroy.RemoveLast();
				}
				InputHistroy.AddFirst(ISM.Code);
			}
			if (hasFocus && ISM.AnyKeyDown() && Event.current.type == EventType.Layout)
			{
				UpdateStateMachine();
			}
		}

		private void HandleTabKeyPress()
		{
			if (Event.current.isKey && Event.current.keyCode == KeyCode.Tab)
			{
				if (Event.current.alt)
				{
					Event.current.Use();
				}
				else if (quellTab)
				{
					if (Event.current.type == EventType.keyUp)
					{
						//Debug.Log("Reseting TAB Status");
						quellTab = false;
						refocus = true;
					}
					//else
					//Event.current.Use();
					//else
				}

				else if (ISM.DisplayHelp && Event.current.isKey && Event.current.keyCode == KeyCode.Tab)
				{
					//Event.current.Use();
					//ISM.PressKey(_KeyCode.Tab);
					quellTab = true;
					//UpdateStateMachine();
					//quellTab = true;
					//refocus = true;
				}
			}
		}

		/// <summary>
		/// Registers a all keypress events and keeps track of keys being held down.
		/// </summary>
		static void HandleKeyInput()
		{
			if (Event.current.isKey)
			{
				_KeyCode keyCode = (_KeyCode)Event.current.keyCode;
				EventType eventType = Event.current.type;
				if (ISM.InputBuffer.ContainsKey(keyCode))
				{
					if (eventType == EventType.keyUp)
					{
						ISM.InputBuffer.Remove(keyCode);
					}
					else if (eventType == EventType.keyDown &&
						DateTime.Now >= ISM.InputBuffer[keyCode].LastPressed + ISM.KeyInput.DelayAmount)
					{
						ISM.InputBuffer[keyCode].LastPressed = DateTime.Now;
						ISM.InputBuffer[keyCode].IsHandled = false;
					}
				}
				else if (eventType == EventType.keyDown)
				{
					foreach (var item in ISM.InputBuffer.ToArray())
					{
						if (!item.Value.IsHandled && item.Value.LastPressed + ISM.KeyInput.DelayAmount < DateTime.Now)
							ISM.InputBuffer.Remove(item.Key);
					}

					ISM.PressKey(keyCode);
				}
			}
		}

		private void UpdateStateMachine()
		{
			// Stop editing the textBox
			EditorGUIUtility.editingTextField = false;

			// Update state machine
			ISM.Update();

			// Only reenable editing if the state is NOT intelliselect
			if (ISM.State != InputState.IntelliSelect)
				EditorGUIUtility.editingTextField = true;
			else
			{
				inp.cursorIndex = ISM.Code.Length + 1;
				inp.selectIndex = ISM.Code.Length + 1;
			}

			if (ISM.ShouldReplaceCode)
			{
				ISM.Code = ISM.ReplacementCode;
				//EditorGUI.FocusTextInControl(NameOfInputField);

				ISM.ShouldReplaceCode = false;
				// This doesnt seem to work in with EditorGUI TextField

				GUI.FocusControl(NameOfInputField);
				if (inp != null)
				{

					inp.cursorIndex = ISM.Code.Length + 1;
					inp.selectIndex = ISM.Code.Length + 1;
					//inp.Copy();
					//inp.Paste();
				}
			}
		}

		private Rect DisplayIntellisense(Rect intelliRect, bool hasFocus, bool canSelect)
		{
			if (ISM.DisplayHelp && ISM.IntelliSenceHelp.Any() && hasFocus)
			{
				var help = ISM.IntelliSenceHelp.Where(i => !i.IsMethodOverload).ToList();
				intelliRect = DisplayHelp(intelliRect, help, canSelect, ref intelliScroll);

				//Deal with Overloads
				var overloads = ISM.IntelliSenceHelp.Where(i => i.IsMethodOverload);
				intelliRect.y += intelliRect.height;
				if (overloads.Any())
					intelliRect = DisplayHelp(intelliRect, overloads.ToList(), false, ref intelliOverLoadScroll);
			}
			return intelliRect;
		}

		private static Rect DisplayHelp(Rect intelliRect, List<CodeCompletion> help, bool IsSelectable, ref Vector2 scroll)
		{
			const float lineHeigth = 18.5f;
			intelliRect.height = Mathf.Min(lineHeigth * help.Count, 150);

			scroll = GUI.BeginScrollView(intelliRect, scroll, new Rect(0, 0, 250, lineHeigth * help.Count));

			GUI.Box(new Rect(0, -15, intelliRect.width, help.Count * lineHeigth + 15), "", GUI.skin.window);

			var style = new GUIStyle(GUI.skin.label);
			style.richText = true;
			for (int i = 0; i < help.Count; i++)
			{
				var helpstr = UIUtils.SyntaxHighlingting(help[i].Details, help[i].Search);
				var rect = new Rect(1, i * lineHeigth, intelliRect.width, intelliRect.height);

				if (IsSelectable && i == ISM.SelectedHelp)
				{
					GUI.Label(rect, "<b>" + helpstr + "</b>", style);
					GUI.ScrollTo(rect);
				}
				else
					GUI.Label(rect, helpstr, style);
			}
			style.richText = false;

			GUI.EndScrollView();
			return intelliRect;
		}

		private void HandleInputShortcuts(bool hasFocus)
		{
			if (hasFocus)
			{
				inp = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
				if (Event.current.isKey && Event.current.control)
				{
					if (Event.current.keyCode == KeyCode.C)
					{
						inp.Copy();
					}
					if (Event.current.keyCode == KeyCode.V)
					{
						inp.Paste();
						ISM.Code = inp.text;
						Repaint();
					}
					if (Event.current.keyCode == KeyCode.A)
					{
						inp.SelectAll();
						Repaint();
					}
					if (Event.current.keyCode == KeyCode.Z && InputHistroy.Count > 1)
					{
						InputHistroy.RemoveFirst();
						ISM.Code = InputHistroy.First.Value;
						InputHistroy.RemoveFirst();
						Repaint();
					}
				}

			}
			else
				ISM.DisplayHelp = false;
		}

		private static void GetRects(out Rect inpRect, out Rect inpLabelRect, out Rect inpButRect, out Rect inpStringRect, out Rect intelliRect, out Rect layoutRect)
		{
			#region Rect initialization
			RectOffset padding = GUI.skin.window.padding;
			RectOffset overflow = GUI.skin.window.overflow;

			Rect scrn = UStrap.ScreenRect;
			inpRect = UStrap.GiveRect(scrn.width, 50f, VerticalAnchor.Top, HorizontalAnchor.Center);
			inpLabelRect = new Rect(0, 0, 70f, inpRect.height / 3f).Place(inpRect, VerticalAnchor.Center, HorizontalAnchor.Left).SetMargin(left: 20f);
			inpStringRect = new Rect(inpLabelRect.xMax + 10f, inpLabelRect.y, inpRect.width - inpLabelRect.xMax - 100f, inpLabelRect.height);
			inpButRect = new Rect(inpStringRect.xMax + 5f, inpLabelRect.y, 70, inpLabelRect.height);

			intelliRect = new Rect(inpStringRect.xMin, inpStringRect.yMax, inpStringRect.width, 150f);
			layoutRect = UStrap.GiveRect(inpRect.width, scrn.height - inpRect.height, VerticalAnchor.Bottom, HorizontalAnchor.Center);
			Rect sliderRect = inpRect.SubRect(rows: 4, row: 3);
			#endregion
		}

		private void UpdateSkins()
		{
			//GUI.skin.FindStyle("Tooltip").richText = true;
			ttStyle = new GUIStyle(GUI.skin.FindStyle("Tooltip"));
			ttStyle.richText = true;
			ttStyle.wordWrap = true;
			if (updateSkins)
			{
				updateSkins = false;
				darkBox = new GUIStyle(GUI.skin.box);
				//darkBox.normal.background = ;
				darkBox.margin = new RectOffset(0, 0, 0, 0);
				box = new GUIStyle(GUI.skin.box);
				box.stretchWidth = false;
				box.margin = new RectOffset(0, 0, 0, 0);
				slimBox = new GUIStyle(box);
				slimBox.padding = new RectOffset(2, 2, 2, 2);
				lightBox = new GUIStyle(GUI.skin.box);
				lightBox.normal.background = EditorGUIUtility.whiteTexture;
				lightBox.stretchWidth = false;
				lightBox.margin = new RectOffset(0, 0, 0, 0);

				lightSlimBox = new GUIStyle(lightBox);
				lightSlimBox.padding = new RectOffset(0, 0, 0, 0);


				rmvOutputBtn = GUI.skin.FindStyle("WinBtnCloseMac");

				varLabelStyle = new GUIStyle(GUI.skin.label);
				varLabelStyle.richText = true;
				varLabelStyle.alignment = TextAnchor.UpperLeft;
				varLabelStyle.margin = GUI.skin.button.margin;
				varLabelStyle.padding = GUI.skin.button.padding;

				greenLabelStyle = new GUIStyle(GUI.skin.label);
				greenLabelStyle.normal.textColor = Color.green;
			}
		}

		private int outputIndex = 0;

		#region GUI Layout functions

		private static readonly MsgType[] MessageInfos = new MsgType[] { MsgType.Error, MsgType.Warning, MsgType.Info, MsgType.None };
		private void DrawMainLayout(Rect layout)
		{
			ratio = (showHistory || showMacros || showVariables || showUsings) ? 0.6f : 0.9f;

			//GUIStyle box = new GUIStyle(GUI.skin.box);
			Rect inner = box.border.Remove(box.padding.Remove(layout));
			float outputWidth = layout.width * ratio;
			float sideBar = layout.width - outputWidth;
			// Ugly hack to fix the text overflow problem.
			if (sideBar < 130)
			{
				sideBar = 130f;
				outputWidth = layout.width - sideBar;
			}

			//DisplayUsingsSettings();
			EditorGUILayout.BeginHorizontal(GUILayout.MaxHeight(inner.height - 10f));
			{
				var outputRect = EditorGUILayout.BeginVertical(box, GUILayout.ExpandHeight(true), GUILayout.Width(outputWidth), GUILayout.MaxWidth(outputWidth));
				{
					DrawOutputArea(outputRect, outputWidth);
				}
				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical(box, GUILayout.Width(sideBar), GUILayout.MaxWidth(sideBar));
				{
					DisplayScope(box, sideBar);
					DisplayHistory(box, sideBar);
					DisplayVariables(box, sideBar);
					DisplayMacros(box, sideBar);
				}
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndHorizontal();
		}

		#region Output
		private void DrawOutputArea(Rect areaRect, float outputWidth)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Output");

				if (RexHelper.Output.Any() && GUILayout.Button(new GUIContent("Clear", "Clear the Output pannel"), GUILayout.Width(43f)))
					RexHelper.ClearOutput();
			}
			GUILayout.EndHorizontal();

			EditorGUILayout.BeginVertical(slimBox);
			scroll3 = EditorGUILayout.BeginScrollView(scroll3);
			{
				AConsoleOutput deleted = null;
				foreach (var o in RexHelper.Output)
				{
					EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
					o.Display();
					DisplayLine();
					EditorGUILayout.EndVertical();
				}
				RexHelper.RemoveOutput(deleted);
			}
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
		}

		private static void DisplayLine()
		{
			GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
			//EditorGUILayout.Separator();
		}

		#endregion


		/// <summary>
		/// Displays the selection screen for Namespaces.
		/// </summary>
		private void DisplayScope(GUIStyle box, float width)
		{
			showUsings = UIUtils.Toggle(showUsings, new GUIContent("Scope", "Namespace selection"));

			if (showUsings)
			{
				EditorGUILayout.BeginVertical(slimBox);
				{

					var useWidth = 25;
					EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
					EditorGUILayout.LabelField("Use", GUILayout.Width(useWidth));
					EditorGUILayout.LabelField("Namespace", GUILayout.ExpandWidth(false));
					EditorGUILayout.EndHorizontal();

					usingScroll = EditorGUILayout.BeginScrollView(usingScroll, GUILayout.Height(1), GUILayout.MaxHeight(150));
					{
						int depth = 0;
						foreach (var n in Utils.NamespaceInfos)
						{
							if (n.IndetLevel > depth) continue;
							if (n.IndetLevel < depth) depth = n.IndetLevel;

							EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
							{
								for (int i = 0; i < n.IndetLevel; i++) GUILayout.Space(25f);

								bool prev = n.Selected;
								n.Selected = GUILayout.Toggle(n.Selected, "", GUILayout.Width(useWidth));

								if (prev != n.Selected)
								{
									if (prev) { UsingsHandler.Remove(n.Name); }
									else { UsingsHandler.Save(n.Name); }
								}

								if (n.AtMaxIndent)
								{
									EditorGUILayout.LabelField(n.Name);
								}
								else if (n.Folded = EditorGUILayout.Foldout(n.Folded, n.Name))
								{
									depth = n.IndetLevel + 1;
								}
								//EditorGUILayout.LabelField(n.Assembly.Location);
							}
							EditorGUILayout.EndHorizontal();
						}
					}
					EditorGUILayout.EndScrollView();
				}
				EditorGUILayout.EndVertical();
			}
		}

		#region History
		/// <summary>
		/// Displays the <see cref="ExpressionHistory"/> at the given width with the given style
		/// </summary>
		/// <param name="box"></param>
		/// <param name="width"></param>
		private void DisplayHistory(GUIStyle box, float width)
		{
			// Draw the foldout and the clear history button.
			GUILayout.BeginHorizontal();
			{
				showHistory = UIUtils.Toggle(showHistory, new GUIContent("History", "Succesfully complied expressions"));

				if (showHistory)
				{
					if (ExpressionHistory.Count > 0 &&
						GUILayout.Button(new GUIContent("Clear", "Clear History"), GUILayout.Width(50)))
					{
						ExpressionHistory.Clear();
					}
				}

			}
			GUILayout.EndHorizontal();
			if (!showHistory)
				return;

			EditorGUILayout.BeginVertical(slimBox);
			{
				scroll = EditorGUILayout.BeginScrollView(scroll);
				string deleted = null;

				foreach (var expr in ExpressionHistory)
				{
					EditorGUILayout.BeginVertical();
					{
						GUILayout.BeginHorizontal();
						{
							GUILayout.Space(10f);
							var before = expr.Value.IsExpanded;
							expr.Value.IsExpanded = GUILayout.Toggle(expr.Value.IsExpanded, new GUIContent("", (expr.Value.IsExpanded ? "Hide" : "Show") + " options"), EditorStyles.foldout, GUILayout.Width(20));
							if (before != expr.Value.IsExpanded &&
								!before)
							{
								foreach (var expr2 in ExpressionHistory)
									if (expr.Key != expr2.Key)
										expr2.Value.IsExpanded = false;
							}
							EditorGUILayout.SelectableLabel(expr.Key, GUILayout.Height(18));
						}
						GUILayout.EndHorizontal();

						if (expr.Value.IsExpanded)
						{
							// Draw dropdown
							EditorGUILayout.BeginVertical();
							{
								var IsDeleted = false;
								DisplayHistorySelectionToggle(expr.Key, expr.Value, out IsDeleted);
								if (IsDeleted) deleted = expr.Key;
							}
							EditorGUILayout.EndVertical();
						}
					}
					EditorGUILayout.EndVertical();
					GUILayout.Box("", GUILayout.Height(1f), GUILayout.ExpandWidth(true));

				}
				EditorGUILayout.EndScrollView();
				if (deleted != null) ExpressionHistory.Remove(deleted);
			}
			EditorGUILayout.EndVertical();

		}

		/// <summary>
		/// Draws the selection dropdown for the given <see cref="history"/> 
		/// </summary>
		/// <param name="toggleSelection"></param>
		/// <param name="code"></param>
		/// <param name="history"></param>
		/// <param name="deleted"></param>
		private void DisplayHistorySelectionToggle(string code, HistoryItem history, out bool deleted)
		{
			deleted = false;
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10f);
				if (GUILayout.Button(new GUIContent("Run", "Run Expression"), GUILayout.Height(16)))
					Execute(code);
				if (GUILayout.Button(new GUIContent("Macro", "Save as Macro"), GUILayout.Height(16)))
					MacroHandler.Save(code);
				if (GUILayout.Button(new GUIContent("Delete", "Delete the history item"), GUILayout.Height(16)))
					deleted = true;
			}
			GUILayout.EndHorizontal();
		}
		#endregion

		#region Variables
		/// <summary>
		/// Displays the <see cref="Variables"/> box at the given width using the given style
		/// </summary>
		/// <param name="box"></param>
		/// <param name="width"></param>
		private void DisplayVariables(GUIStyle box, float width)
		{
			showVariables = UIUtils.Toggle(showVariables, new GUIContent("Variables", "User declared variables"));

			if (showVariables)
			{
				// Outer box begins
				EditorGUILayout.BeginVertical(slimBox);
				{
					// Scroll begins
					scroll2 = EditorGUILayout.BeginScrollView(scroll2);
					{
						// inner box begins
						EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(false), GUILayout.Width(width - 20));
						{
							// Used to pick up which variables were delete in the gui run
							string deleted = null;
							foreach (var var in RexHelper.Variables)
							{
								EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
								{
									var shouldDelete = DisplayVariable(var.Key, var.Value);
									if (shouldDelete)
										deleted = var.Key;
								}
								EditorGUILayout.EndHorizontal();
							}
							if (deleted != null)
								RexHelper.Variables.Remove(deleted);
						}
						EditorGUILayout.EndVertical();
						// inner box ends
					}
					EditorGUILayout.EndScrollView();
					// Scroll ends
				}
				EditorGUILayout.EndVertical();
				// outer box ends
			}
		}

		/// <summary>
		/// Displays a single variable, returns true if the varible was deleted else false.
		/// </summary>
		/// <param name="VarName">Name of the var. (key of the <see cref="RexHelper.Variables"/>)</param>
		/// <param name="var">Varible it self. (Value of the <see cref="RexHelper.Variables"/>)</param>
		private bool DisplayVariable(string VarName, RexHelper.Varible var)
		{
			string highlightedString;
			string defaultMsg;

			var type = Utils.GetCSharpRepresentation(var.VarType);
			if (var.VarValue == null)
			{
				highlightedString = UIUtils.SyntaxHighlingting(type.Concat(new[] {
											Syntax.Name(VarName),
											Syntax.EqualsOp,
											Syntax.Keyword("null")
										}));
				defaultMsg = "null";
			}
			else
			{
				// Format the code for syntax highlighting using richtext.
				highlightedString = UIUtils.SyntaxHighlingting(type.Concat(new[] {
											Syntax.Name(VarName),
											Syntax.EqualsOp,
											Syntax.ConstVal(var.VarValue.ToString())
										}));
				defaultMsg = var.VarValue.ToString();
			}

			bool shouldDelete = false;
			// Draw the delete button.
			if (GUILayout.Button(new GUIContent("X", $"Remove <b>{VarName}</b>"), GUILayout.Width(20)))
				shouldDelete = true;

			// Draw the button as a label.
			if (GUILayout.Button(new GUIContent(highlightedString, $"Click to inspect <b>{VarName}</b>"), varLabelStyle, GUILayout.ExpandWidth(true)))
			{
				// Construct a new output entry...
				var ouput = new ConsoleOutput();
				ouput.LoadInDetails(var.VarValue, defaultMsg, Utilities.Helpers.Logger.ExtractDetails(var.VarValue));
				RexHelper.AddOutput(ouput);
			}

			return shouldDelete;
		}
		#endregion

		/// <summary>
		/// Display the <see cref="Macros"/> window at the given width using the given style.
		/// </summary>
		/// <param name="box"></param>
		/// <param name="width"></param>
		private void DisplayMacros(GUIStyle box, float width)
		{
			showMacros = UIUtils.Toggle(showMacros, new GUIContent("Macros", "Saved expressions"));

			if (showMacros)
			{
				// box begins
				EditorGUILayout.BeginVertical(slimBox);
				{
					// scroll begins
					scroll4 = EditorGUILayout.BeginScrollView(scroll4);
					{
						string deleted = null;
						foreach (var macro in MacroHandler.Macros)
						{
							EditorGUILayout.BeginHorizontal();
							{
								// Draw the RUN button
								if (GUILayout.Button(new GUIContent("Go", "Evaluate: <b>" + macro + "</b>"), GUILayout.Width(30)))
								{
									// parse, compile & execute
									Execute(macro);
								}

								// Remove the macro if the X button is pressed
								if (GUILayout.Button(new GUIContent("X", "Remove macro"), GUILayout.Width(20)))
									deleted = macro;

								// if the label is pressed... then run this code..?
								// TODO: Highlight this...
								if (GUILayout.Button(new GUIContent(macro, "Select macro: <b>" + macro + "</b>"), GUI.skin.label))
								{
									ISM.Code = macro;
									ISM.DisplayHelp = false;
								}
							}
							EditorGUILayout.EndHorizontal();
						}
						if (deleted != null)
							MacroHandler.Remove(deleted);
					}
					EditorGUILayout.EndScrollView();
				}
				EditorGUILayout.EndVertical();
			}
		}
		#endregion
	}
}