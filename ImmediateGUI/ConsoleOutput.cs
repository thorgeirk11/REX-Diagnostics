using Rex.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Rex.Utilities.Helpers;

namespace Rex.Window
{

	public class ConsoleOutput : AConsoleOutput
	{
		/// <summary>
		/// Action Dictionary for displaying the details.
		/// </summary>
		public Dictionary<Action, GUIContent> Details { get; set; }
		/// <summary>
		/// Action for displaying the message.
		/// </summary>
		public Action DisplayMessage { get; private set; }

		public ConsoleOutput()
		{
			DisplayMessage = DisplayFieldFor(null, "null");
			Details = new Dictionary<Action, GUIContent>();
		}

		/// <summary>
		/// Loads in the Details and converts them to Action and GUI contents.
		/// </summary>
		/// <param name="value">Ouput Value of the Expression</param>
		/// <param name="message">Message from the Expression execute</param>
		/// <param name="details"></param>
		public override void LoadInDetails(object value, string message, IEnumerable<MemberDetails> details)
		{
			Message = message;
			var messageField = DisplayFieldFor(message, message);
			if (NeedsSpecialField(value))
			{
				var valueField = DisplayFieldFor(value, message);
				DisplayMessage = () =>
				{
					messageField();
					valueField();
				};
			}
			else
			{
				DisplayMessage = () =>
				{
					messageField();
				};
			}
			Details = (from detail in details
					   let highlight = UIUtils.SyntaxHighlingting(detail.Where(i => i.Type != SyntaxType.EqualsOp && i.Type != SyntaxType.ConstVal))
					   let content = new GUIContent(detail.Name.String, highlight)
					   let displayAction = DisplayFieldFor(detail.Value, detail.Constant.String)
					   select new { displayAction, content }).ToDictionary(i => i.displayAction, i => i.content);
		}

		public override void Display()
		{
			if (Exception != null)
			{
				if (!string.IsNullOrEmpty(Message))
					EditorGUILayout.HelpBox(Message, MessageType.Warning);
				//else if (Exception != null)
				//    EditorGUILayout.HelpBox(Exception.Message, MessageType.Warning);

				EditorGUILayout.TextArea(Exception.ToString(), GUI.skin.textArea);
			}
			else
			{
				EditorGUILayout.BeginVertical();
				{
					EditorGUILayout.BeginHorizontal();
					DisplayMessage?.Invoke();
					EditorGUILayout.EndHorizontal();
					if (Details.Any())
					{
						ShowDetails = EditorGUILayout.Foldout(ShowDetails, "Details");
						if (ShowDetails)
						{
							foreach (var detail in Details)
							{
								EditorGUILayout.BeginHorizontal();
								{
									var style = new GUIStyle(GUI.skin.label)
									{
										alignment = TextAnchor.MiddleLeft
									};
									GUILayout.Label(detail.Value, style, GUILayout.Width(150));
									detail.Key();
								}
								EditorGUILayout.EndHorizontal();
							}
						}
					}

					if (Members.Count > 0)
					{
						if (ShowMembers = EditorGUILayout.Foldout(ShowMembers, "Members"))
						{
							EditorGUI.indentLevel++;
							foreach (var m in Members)
								m.Display();
							EditorGUI.indentLevel--;
						}
					}
				}
				EditorGUILayout.EndVertical();
			}
		}

		/// <summary>
		/// Returns a display action for the value.
		/// </summary>
		/// <param name="value">Value to be displayed</param>
		/// <param name="defaultString">default string for the value</param>
		private static Action DisplayFieldFor(object value, string defaultString)
		{
			if (value == null)
			{
				return () => EditorGUILayout.SelectableLabel(defaultString, GUI.skin.textField, GUILayout.ExpandWidth(true), GUILayout.Height(17));
			}

			var type = value.GetType();
			if (FieldForType.ContainsKey(type))
			{
				return () => FieldForType[type](value);
			}
			else if (value is UnityEngine.Object)
			{
				return () => EditorGUILayout.ObjectField(value as UnityEngine.Object, type, allowSceneObjects: true);
			}
			else
			{
				return () => EditorGUILayout.SelectableLabel(defaultString, GUI.skin.textField, GUILayout.ExpandWidth(true), GUILayout.Height(17));
			}
		}

		private static bool NeedsSpecialField(object value)
		{
			if (value == null)
			{
				return false;
			}

			var type = value.GetType();
			return FieldForType.ContainsKey(type) || value is UnityEngine.Object;
		}
		private readonly static Dictionary<Type, Action<object>> FieldForType = new Dictionary<Type, Action<object>>
		{
			{ typeof(Vector2),          value => EditorGUILayout.Vector2Field("", (Vector2)value) },
			{ typeof(Vector3),          value => EditorGUILayout.Vector3Field("", (Vector3)value) },
			{ typeof(Vector4),          value => EditorGUILayout.Vector4Field("", (Vector4)value) },
			{ typeof(Color),            value => EditorGUILayout.ColorField((Color)value) },
			{ typeof(Rect),             value => EditorGUILayout.RectField((Rect)value) },
			{ typeof(AnimationCurve),   value => EditorGUILayout.CurveField((AnimationCurve)value) },
			{ typeof(Enum),             value => EditorGUILayout.EnumPopup((Enum)value) },
		};

	}
}