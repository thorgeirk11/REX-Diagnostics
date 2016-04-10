using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Rex.Utilities;
using Rex.Utilities.Helpers;
using Rex.Utilities.Input;

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

	[Serializable]
	public sealed class RexEditorWindow : EditorWindow
	{
		/// <summary>
		/// Maximunm length of the history.
		/// </summary>
		private const int INPUT_HISTORY_LENGTH = 50;
		private const string NAME_OF_INPUT_FIELD = "ExpressionInput";

		private readonly LinkedList<string> _inputHistroy = new LinkedList<string>();

		/// <summary>
		/// The continues compile engine that compiles on a seperate thread.
		/// </summary>
		[SerializeField]
		private RexCompileEngine _compileEngine;

		/// <summary>
		/// History of all the expressions executed in this editor session.
		/// </summary>
		[SerializeField]
		private List<RexHitoryItem> _expressionHistory = new List<RexHitoryItem>();

		[SerializeField]
		private List<string> _macros;

		[SerializeField]
		private RexStaticTextCollection _texts;

		private static Dictionary<MessageType, List<string>> Messages = new Dictionary<MessageType, List<string>>();

		/// <summary>
		/// Expression history item.
		/// </summary>
		[Serializable]
		private class RexHitoryItem
		{
			/// <summary>
			/// The code entered by user.
			/// </summary>
			public string Code = string.Empty;
			/// <summary>
			/// Is the history item expanded in UI.
			/// </summary>
			public bool IsExpanded;
		}

		#region UI
		const double stopwatchTime = 0.5;

		bool updateSkins = true;
		TextEditor _inputField;

		private DateTime lastExecute;
		private bool lastRunSuccesfull;

		private Vector2 usingScroll;
		private Vector2 scroll;
		private Vector2 scroll2;
		private Vector2 scroll3;
		private Vector2 scroll4;
		private Vector2 intelliScroll;
		private Vector2 intelliOverLoadScroll;

		[SerializeField]
		bool showHistory = true;
		[SerializeField]
		bool showVariables = true;
		[SerializeField]
		bool showMacros = true;
		[SerializeField]
		bool showUsings = false;

		bool refocus = false;
		float ratio = 0.6f;

		GUIStyle box;
		GUIStyle lightBox;
		GUIStyle slimBox;
		GUIStyle ttStyle;

		GUIStyle varLabelStyle;
		GUIStyle greenLabelStyle;
		#endregion

		// Add menu named "My Window" to the Window menu
		[MenuItem("Window/(REX) Runtime Expressions")]
		static void Init()
		{
			// Get existing open window or if none, make a new one:
			var window = GetWindow<RexEditorWindow>();
			window.Show();
		}

		void OnDestroy()
		{
			_compileEngine.Dispose();
		}

		void OnEnable()
		{
			hideFlags = HideFlags.HideAndDontSave;

			if (_compileEngine == null)
				_compileEngine = RexCompileEngine.Instance;

			if (_texts == null)
				_texts = RexStaticTextCollection.Instance;

			if (_macros == null)
				_macros = RexMacroHandler.LoadMacros();

			RexISM.Repaint = Repaint;
			RexISM.DebugLog = Debug.Log;
			RexISM.ExecuteCode = Execute;
			RexISM.Enter_NoInput();

			updateSkins = true;
			minSize = new Vector2(450f, 350f);
			autoRepaintOnSceneChange = true;
			titleContent.text = "REX";
			titleContent.tooltip = "Runtime Expressions";
		}

		/// <summary>
		/// Executes the expression once.
		/// </summary>
		private void Execute(string code)
		{
			lastRunSuccesfull = true;

			code = code.Trim().Trim(';');
			if (string.IsNullOrEmpty(code))
				return;

			RexISM.Code = code;
			var compile = _compileEngine.GetCompile(code, out Messages);
			if (compile != null)
			{
				if (_expressionHistory.Count == 0 ||
					_expressionHistory[0].Code != code)
				{
					_expressionHistory.Insert(0, new RexHitoryItem
					{
						Code = code
					});
				}

				var output = RexHelper.Execute<OutputEntry>(compile, out Messages);
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
			if (!EditorApplication.isPlaying)
			{
				EditorGUILayout.HelpBox("Need to be in play mode to evaluate expressions", MessageType.Info);
				return;
			}

			HandleTabKeyPress();
			UpdateSkins();

			Rect inpRect, inpLabelRect, inpStringRect, intelliRect, layoutRect, inpButRect;
			GetRects(out inpRect, out inpLabelRect, out inpButRect, out inpStringRect, out intelliRect, out layoutRect);
			HandleKeyInput();

			DisplayInputField(ref inpLabelRect, ref inpStringRect, ref inpButRect);

			if (refocus)
			{
				refocus = false;
				GUI.FocusControl(NAME_OF_INPUT_FIELD);
				_inputField = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
				if (_inputField != null)
				{
					_inputField.MoveLineEnd();
				}
			}
			var hasFocus = GUI.GetNameOfFocusedControl() == NAME_OF_INPUT_FIELD;

			HandleInputShortcuts(hasFocus);
			HandleInputHistory(hasFocus);
			var prevRect = new Rect(intelliRect);
			DisplayIntellisense(ref intelliRect, hasFocus, true);

			DisplayMessages(ref layoutRect);

			#region MainLayout
			GUILayout.BeginArea(layoutRect);
			{
				DrawMainLayout(ref layoutRect);
			}
			GUILayout.EndArea();
			#endregion

			DisplayIntellisense(ref prevRect, hasFocus, true);
			DisplayTooltip();
		}

		private void DisplayInputField(ref Rect inpLabelRect, ref Rect inpStringRect, ref Rect inpButRect)
		{
			GUI.Label(inpLabelRect, _texts["expression_header"]);
			var oldColor = ColorInput();
			GUI.SetNextControlName(NAME_OF_INPUT_FIELD);
			RexISM.Code = GUI.TextField(inpStringRect, RexISM.Code);

			//To have the cursor change to the 'I' on hover:
			GUI.color = Color.clear;
			EditorGUI.TextField(inpStringRect, "");
			GUI.color = oldColor;

			if (GUI.Button(inpButRect, _texts["evaluate_button"]))
			{
				GUI.FocusControl(NAME_OF_INPUT_FIELD);
				RexISM.PressKey(KeyCode.Return);
			}
		}

		private Color ColorInput()
		{
			var evaluateLerpTime = TimeSpan.FromSeconds(1.5);
			var oldColor = GUI.color;
			var timeSinceLastExecute = DateTime.Now - lastExecute;
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

				var width = Mathf.Min(300f, ttsize.x);
				var height = ttStyle.CalcHeight(cont, width);

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

		private static void DisplayMessages(ref Rect layoutRect)
		{
			if (Messages.Count > 0)
			{
				var helpboxStyle = GUI.skin.FindStyle("HelpBox");

				foreach (var msg in Messages)
				{
					foreach (var val in msg.Value)
					{
						EditorGUI.HelpBox(layoutRect, val, msg.Key);
						var rect = helpboxStyle.CalcSize(new GUIContent(val));
						layoutRect.yMin += rect.y * 2;
					}
				}
				var errorRect = new Rect(layoutRect)
				{
					height = 20
				};
				if (GUI.Button(errorRect, "Clear Messages"))
				{
					Messages.Values.ToList().ForEach(i => i.Clear());
					Messages.Clear();
				}
				layoutRect.yMin += errorRect.height;
			}
		}

		private void HandleInputHistory(bool hasFocus)
		{
			if (_inputHistroy.Count == 0 ||
				_inputHistroy.First.Value != RexISM.Code)
			{
				if (_inputHistroy.Count > INPUT_HISTORY_LENGTH)
				{
					_inputHistroy.RemoveLast();
				}
				_inputHistroy.AddFirst(RexISM.Code);
			}
			if (hasFocus && RexISM.AnyKeyDown() && Event.current.type == EventType.Layout)
			{
				UpdateStateMachine();
			}
		}

		private void HandleTabKeyPress()
		{
			if (Event.current.keyCode == KeyCode.Tab || Event.current.character == '\t')
			{
				if (RexISM.DisplayHelp) RexISM.PressKey(KeyCode.Tab);
				Event.current.Use();
			}
		}

		/// <summary>
		/// Registers a all keypress events and keeps track of keys being held down.
		/// </summary>
		static void HandleKeyInput()
		{
			if (Event.current.isKey)
			{
				var keyCode = Event.current.keyCode;
				var eventType = Event.current.type;
				if (RexISM.InputBuffer.ContainsKey(keyCode))
				{
					if (eventType == EventType.keyUp)
					{
						RexISM.InputBuffer.Remove(keyCode);
					}
					else if (eventType == EventType.keyDown &&
						DateTime.Now >= RexISM.InputBuffer[keyCode].LastPressed + RexISM.KeyInput.DelayAmount)
					{
						RexISM.InputBuffer[keyCode].LastPressed = DateTime.Now;
						RexISM.InputBuffer[keyCode].IsHandled = false;
					}
				}
				else if (eventType == EventType.keyDown)
				{
					foreach (var item in RexISM.InputBuffer.ToArray())
					{
						if (!item.Value.IsHandled && item.Value.LastPressed + RexISM.KeyInput.DelayAmount < DateTime.Now)
							RexISM.InputBuffer.Remove(item.Key);
					}

					RexISM.PressKey(keyCode);
				}
			}
		}

		private void UpdateStateMachine()
		{
			// Stop editing the textBox
			EditorGUIUtility.editingTextField = false;

			// Update state machine
			RexISM.Update();

			// Only reenable editing if the state is NOT intelliselect
			if (RexISM.State != RexInputState.IntelliSelect)
			{
				EditorGUIUtility.editingTextField = true;
			}
			else
			{
				_inputField.MoveLineEnd();
			}

			if (RexISM.ShouldReplaceCode)
			{
				RexISM.Code = RexISM.ReplacementCode;
				//EditorGUI.FocusTextInControl(NameOfInputField);

				RexISM.ShouldReplaceCode = false;
				// This doesnt seem to work in with EditorGUI TextField

				GUI.FocusControl(NAME_OF_INPUT_FIELD);
				if (_inputField != null)
				{
					_inputField.text = RexISM.Code;
					_inputField.MoveLineEnd();
				}
			}
		}

		private void DisplayIntellisense(ref Rect intelliRect, bool hasFocus, bool canSelect)
		{
			if (RexISM.DisplayHelp && RexISM.IntelliSenceHelp.Count > 0 && hasFocus)
			{
				var help = RexISM.IntelliSenceHelp.Where(i => !i.IsMethodOverload).ToList();
				DisplayHelp(ref intelliRect, help, canSelect, ref intelliScroll);

				// Deal with Overloads
				var overloads = RexISM.IntelliSenceHelp.Where(i => i.IsMethodOverload);
				intelliRect.y += intelliRect.height;
				if (overloads.Any())
					DisplayHelp(ref intelliRect, overloads.ToList(), false, ref intelliOverLoadScroll);
			}
		}

		private static void DisplayHelp(ref Rect intelliRect, List<CodeCompletion> help, bool IsSelectable, ref Vector2 scroll)
		{
			const float lineHeigth = 18.5f;
			intelliRect.height = Mathf.Min(lineHeigth * help.Count, 150);

			scroll = GUI.BeginScrollView(intelliRect, scroll, new Rect(0, 0, 250, lineHeigth * help.Count));

			GUI.Box(new Rect(0, -15, intelliRect.width, help.Count * lineHeigth + 15), "", GUI.skin.window);

			var style = new GUIStyle(GUI.skin.label)
			{
				richText = true
			};
			for (int i = 0; i < help.Count; i++)
			{
				var helpstr = RexUIUtils.SyntaxHighlingting(help[i].Details, help[i].Search);
				var rect = new Rect(1, i * lineHeigth, intelliRect.width, intelliRect.height);

				if (IsSelectable && i == RexISM.SelectedHelp)
				{
					GUI.Label(rect, "<b>" + helpstr + "</b>", style);
					GUI.ScrollTo(rect);
				}
				else
					GUI.Label(rect, helpstr, style);
			}
			style.richText = false;

			GUI.EndScrollView();
		}

		private void HandleInputShortcuts(bool hasFocus)
		{
			if (hasFocus)
			{
				_inputField = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
				if (Event.current.isKey && Event.current.control)
				{
					if (Event.current.keyCode == KeyCode.C)
					{
						_inputField.Copy();
					}
					if (Event.current.keyCode == KeyCode.V)
					{
						_inputField.Paste();
						RexISM.Code = _inputField.text;
						Repaint();
					}
					if (Event.current.keyCode == KeyCode.A)
					{
						_inputField.SelectAll();
						Repaint();
					}
					if (Event.current.keyCode == KeyCode.Z && _inputHistroy.Count > 1)
					{
						_inputHistroy.RemoveFirst();
						RexISM.Code = _inputHistroy.First.Value;
						_inputHistroy.RemoveFirst();
						Repaint();
					}
				}

			}
			else
				RexISM.DisplayHelp = false;
		}

		private static void GetRects(out Rect inpRect, out Rect inpLabelRect, out Rect inpButRect, out Rect inpStringRect, out Rect intelliRect, out Rect layoutRect)
		{
			#region Rect initialization
			var scrn = UStrap.ScreenRect;
			inpRect = UStrap.GiveRect(scrn.width, 50f, VerticalAnchor.Top, HorizontalAnchor.Center);
			inpLabelRect = new Rect(0, 0, 70f, inpRect.height / 3f).Place(inpRect, VerticalAnchor.Center, HorizontalAnchor.Left).SetMargin(left: 20f);
			inpStringRect = new Rect(inpLabelRect.xMax + 10f, inpLabelRect.y, inpRect.width - inpLabelRect.xMax - 100f, inpLabelRect.height);
			inpButRect = new Rect(inpStringRect.xMax + 5f, inpLabelRect.y, 70, inpLabelRect.height);

			intelliRect = new Rect(inpStringRect.xMin, inpStringRect.yMax, inpStringRect.width, 150f);
			layoutRect = UStrap.GiveRect(inpRect.width, scrn.height - inpRect.height, VerticalAnchor.Bottom, HorizontalAnchor.Center);
			#endregion
		}

		private void UpdateSkins()
		{
			//GUI.skin.FindStyle("Tooltip").richText = true;
			ttStyle = new GUIStyle(GUI.skin.FindStyle("Tooltip"))
			{
				richText = true,
				wordWrap = true
			};
			if (updateSkins)
			{
				updateSkins = false;
				box = new GUIStyle(GUI.skin.box)
				{
					stretchWidth = false,
					margin = new RectOffset(0, 0, 0, 0)
				};
				slimBox = new GUIStyle(box)
				{
					padding = new RectOffset(2, 2, 2, 2)
				};
				lightBox = new GUIStyle(GUI.skin.box)
				{
					stretchWidth = false,
					margin = new RectOffset(0, 0, 0, 0)
				};
				lightBox.normal.background = EditorGUIUtility.whiteTexture;

				varLabelStyle = new GUIStyle(GUI.skin.label)
				{
					richText = true,
					alignment = TextAnchor.UpperLeft,
					margin = GUI.skin.button.margin,
					padding = GUI.skin.button.padding
				};

				greenLabelStyle = new GUIStyle(GUI.skin.label);
				greenLabelStyle.normal.textColor = Color.green;
			}
		}

		#region GUI Layout functions
		private void DrawMainLayout(ref Rect layout)
		{
			ratio = (showHistory || showMacros || showVariables || showUsings) ? 0.6f : 0.9f;

			var inner = box.border.Remove(box.padding.Remove(layout));
			var outputWidth = layout.width * ratio;
			var sideBar = layout.width - outputWidth;

			// Ugly hack to fix the text overflow problem.
			if (sideBar < 130)
			{
				sideBar = 130f;
				outputWidth = layout.width - sideBar;
			}

			EditorGUILayout.BeginHorizontal(GUILayout.MaxHeight(inner.height - 10f));
			{
				var outputRect = EditorGUILayout.BeginVertical(box, GUILayout.ExpandHeight(true), GUILayout.Width(outputWidth), GUILayout.MaxWidth(outputWidth));
				{
					DrawOutputArea(outputRect, outputWidth);
				}
				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical(box, GUILayout.Width(sideBar), GUILayout.MaxWidth(sideBar));
				{
					DisplayScope();
					DisplayHistory();
					DisplayVariables();
					DisplayMacros();
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
				GUILayout.Label(_texts["output_header"]);

				if (RexHelper.Output.Any() && GUILayout.Button(_texts["output_clear"], GUILayout.Width(43f)))
					RexHelper.ClearOutput();
			}
			GUILayout.EndHorizontal();

			EditorGUILayout.BeginVertical(slimBox);
			scroll3 = EditorGUILayout.BeginScrollView(scroll3);
			{
				OutputEntry deleted = null;
				foreach (var o in RexHelper.Output)
				{
					EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
					o.DrawOutputUI();
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
		private void DisplayScope()
		{
			showUsings = RexUIUtils.Toggle(showUsings, _texts["scope_header"]);

			if (showUsings)
			{
				EditorGUILayout.BeginVertical(slimBox);
				{

					var useWidth = 25;
					EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
					EditorGUILayout.LabelField(_texts["scope_use"], GUILayout.Width(useWidth));
					EditorGUILayout.LabelField(_texts["scope_namespace"], GUILayout.ExpandWidth(false));
					EditorGUILayout.EndHorizontal();

					usingScroll = EditorGUILayout.BeginScrollView(usingScroll, GUILayout.Height(1), GUILayout.MaxHeight(150));
					{
						var depth = 0;
						foreach (var n in RexCompileEngine.Instance.NamespaceInfos)
						{
							if (n.Depth > depth) continue;
							if (n.Depth < depth) depth = n.Depth;

							EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
							{
								for (int i = 0; i < n.Depth; i++) GUILayout.Space(25f);

								var prev = n.Selected;
								n.Selected = GUILayout.Toggle(n.Selected, "", GUILayout.Width(useWidth));

								if (prev != n.Selected)
								{
									if (prev) { RexUsingsHandler.Remove(n.Name); }
									else { RexUsingsHandler.Save(n.Name); }
								}

								if (n.HasSubNamespaces)
								{
									EditorGUILayout.LabelField(n.Name);
								}
								else if (n.DisplaySubNamespaces = EditorGUILayout.Foldout(n.DisplaySubNamespaces, n.Name))
								{
									depth = n.Depth + 1;
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
		/// Displays the <see cref="_expressionHistory"/> at the given width with the given style
		/// </summary>
		private void DisplayHistory()
		{
			// Draw the foldout and the clear history button.
			GUILayout.BeginHorizontal();
			{
				showHistory = RexUIUtils.Toggle(showHistory, _texts["history_header"]);

				if (showHistory)
				{
					if (_expressionHistory.Count > 0 &&
						GUILayout.Button(_texts["history_clear"], GUILayout.Width(50)))
					{
						_expressionHistory.Clear();
					}
				}

			}
			GUILayout.EndHorizontal();
			if (!showHistory)
				return;

			EditorGUILayout.BeginVertical(slimBox);
			{
				scroll = EditorGUILayout.BeginScrollView(scroll);
				int deleteIndex = -1;

				for (int i = 0; i < _expressionHistory.Count; i++)
				{
					var expr = _expressionHistory[i];
					EditorGUILayout.BeginVertical();
					{
						GUILayout.BeginHorizontal();
						{
							GUILayout.Space(10f);
							var toolTip = _texts.GetText("history_item_" + (expr.IsExpanded ? "hide" : "show"), expr.Code);
							var isExpaned = GUILayout.Toggle(expr.IsExpanded, toolTip, EditorStyles.foldout, GUILayout.Width(20));

							if (isExpaned != expr.IsExpanded)
							{
								_expressionHistory.ForEach(e => e.IsExpanded = false);
								expr.IsExpanded = isExpaned;
							}
						}
						GUILayout.EndHorizontal();

						if (expr.IsExpanded)
						{
							// Draw dropdown
							EditorGUILayout.BeginVertical();
							{
								var IsDeleted = false;
								DisplayHistorySelectionToggle(expr.Code, out IsDeleted);
								if (IsDeleted) deleteIndex = i;
							}
							EditorGUILayout.EndVertical();
						}
					}
					EditorGUILayout.EndVertical();
					GUILayout.Box("", GUILayout.Height(1f), GUILayout.ExpandWidth(true));

				}
				EditorGUILayout.EndScrollView();
				if (deleteIndex >= 0) _expressionHistory.RemoveAt(deleteIndex);
			}
			EditorGUILayout.EndVertical();

		}

		/// <summary>
		/// Draws the selection dropdown for the given <see cref="HistoryItem"/> 
		/// </summary>
		/// <param name="code"></param>
		/// <param name="deleted"></param>
		private void DisplayHistorySelectionToggle(string code, out bool deleted)
		{
			deleted = false;
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10f);
				if (GUILayout.Button(_texts["history_item_run"], GUILayout.Height(16)))
				{
					Execute(code);
				}
				if (GUILayout.Button(_texts["history_item_macro"], GUILayout.Height(16)))
				{
					_macros = RexMacroHandler.Save(code);
				}
				if (GUILayout.Button(_texts["history_item_delete"], GUILayout.Height(16)))
				{
					deleted = true;
				}
			}
			GUILayout.EndHorizontal();
		}
		#endregion

		#region Variables
		/// <summary>
		/// Displays the <see cref="RexHelper.Variables"/>
		/// </summary>
		private void DisplayVariables()
		{
			showVariables = RexUIUtils.Toggle(showVariables, _texts["variables_header"]);

			if (showVariables)
			{
				// Outer box begins
				EditorGUILayout.BeginVertical(slimBox);
				{
					// Scroll begins
					scroll2 = EditorGUILayout.BeginScrollView(scroll2);
					{
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
		/// <param name="var">The varible to display. (Value of the <see cref="RexHelper.Variables"/>)</param>
		private bool DisplayVariable(string VarName, RexHelper.Varible var)
		{
			string highlightedString;
			string defaultMsg;

			var type = RexUtils.GetCSharpRepresentation(var.VarType);
			if (var.VarValue == null)
			{
				highlightedString = RexUIUtils.SyntaxHighlingting(type.Concat(new[] {
											Syntax.Name(VarName),
											Syntax.Space, Syntax.EqualsOp,
											Syntax.Space, Syntax.Keyword("null")
										}));
				defaultMsg = "null";
			}
			else
			{
				// Format the code for syntax highlighting using richtext.
				highlightedString = RexUIUtils.SyntaxHighlingting(type.Concat(new[] {
											Syntax.Space, Syntax.Name(VarName),
											Syntax.Space, Syntax.EqualsOp,
											Syntax.Space
										}).Concat(RexReflectionUtils.GetSyntaxForValue(var.VarValue))
									);
				defaultMsg = var.VarValue.ToString();
			}

			var shouldDelete = GUILayout.Button(_texts.GetText("remove_variable", tooltipFormat: VarName), GUILayout.Width(20));

			// Draw the button as a label.
			if (GUILayout.Button(_texts.GetText("inspect_variable", highlightedString, VarName), varLabelStyle, GUILayout.ExpandWidth(true)))
			{

				var ouput = new OutputEntry();
				ouput.LoadObject(var.VarValue);
				RexHelper.AddOutput(ouput);
			}

			return shouldDelete;
		}
		#endregion

		/// <summary>
		/// Display the <see cref="Macros"/> window at the given width using the given style.
		/// </summary>
		private void DisplayMacros()
		{
			showMacros = RexUIUtils.Toggle(showMacros, _texts["macros_header"]);

			if (showMacros)
			{
				// box begins
				EditorGUILayout.BeginVertical(slimBox);
				{
					// scroll begins
					scroll4 = EditorGUILayout.BeginScrollView(scroll4);
					{
						string deleted = null;
						foreach (var macro in _macros)
						{
							EditorGUILayout.BeginHorizontal();
							{
								// Draw the RUN button
								if (GUILayout.Button(_texts.GetText("macro_go", tooltipFormat: macro), GUILayout.Width(30)))
								{
									// parse, compile & execute
									Execute(macro);
								}

								// Remove the macro if the X button is pressed
								if (GUILayout.Button(_texts["macro_remove"], GUILayout.Width(20)))
									deleted = macro;

								// if the label is pressed... then run this code..?
								// TODO: Highlight this...
								if (GUILayout.Button(_texts.GetText("select_macro", macro, macro), GUI.skin.label))
								{
									RexISM.Code = macro;
									RexISM.DisplayHelp = false;
								}
							}
							EditorGUILayout.EndHorizontal();
						}
						if (deleted != null)
							_macros = RexMacroHandler.Remove(deleted);
					}
					EditorGUILayout.EndScrollView();
				}
				EditorGUILayout.EndVertical();
			}
		}
		#endregion
	}
}