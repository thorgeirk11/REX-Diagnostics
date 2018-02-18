using Rex.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Rex.Utilities.Helpers;
using System.Collections;

namespace Rex.Window
{
	public class OutputEntry : AOutputEntry
	{
		/// <summary>
		/// Action Dictionary for displaying the details.
		/// </summary>
		public Dictionary<Action, GUIContent> Details { get; private set; }

		/// <summary>
		/// Action Dictionary for displaying the details.
		/// </summary>
		public List<OutputEntry> EnumerationItems { get; private set; }

		/// <summary>
		/// Action for displaying the message.
		/// </summary>
		public Action DisplayMessage { get; private set; }

		/// <summary>
		/// Shoud the <see cref="Details"/> be displayed in UI.
		/// </summary>
		public bool ShowDetails { get; set; }
		/// <summary>
		/// Should the <see cref="EnumerationItems" /> be displayed in UI. 
		/// </summary>
		public bool ShowEnumeration { get; set; }

        private MemberDetails _exceptionDetails;
		public override Exception Exception
		{
			get { return base.Exception; }
			set
			{
				base.Exception = value;
				if (value != null)
				{
					_exceptionDetails = RexUtils.GetCSharpRepresentation(Exception.GetType());
				}
			}
		}

		static GUIStyle _detailsStyle;
		/// <summary>
		/// Style used for the details section
		/// </summary>
		public static GUIStyle DetailsStyle
		{
			get
			{
				if (_detailsStyle == null)
				{
					_detailsStyle = new GUIStyle
					{
						alignment = TextAnchor.MiddleLeft,
						margin = new RectOffset(10, 10, 0, 0),
						wordWrap = true,
						richText = true
					};
				}
				return _detailsStyle;
			}
		}

		private static Dictionary<Type, Action<object>> _fieldForType;
		private static Dictionary<Type, Action<object>> FieldForType
		{
			get
			{
				if (_fieldForType == null)
				{
					_fieldForType = new Dictionary<Type, Action<object>>
					{
						{ typeof(string),           value => EditorGUILayout.TextArea(value.ToString()) },
						{ typeof(Vector2),          value => EditorGUILayout.Vector2Field("", (Vector2)value) },
						{ typeof(Vector3),          value => EditorGUILayout.Vector3Field("", (Vector3)value) },
						{ typeof(Vector4),          value => EditorGUILayout.Vector4Field("", (Vector4)value) },
						{ typeof(Color),            value => EditorGUILayout.ColorField((Color)value) },
						{ typeof(Rect),             value => EditorGUILayout.RectField((Rect)value) },
						{ typeof(AnimationCurve),   value => EditorGUILayout.CurveField((AnimationCurve)value) },
						{ typeof(Bounds),           value => EditorGUILayout.BoundsField((Bounds)value) },
						{ typeof(bool),             value => EditorGUILayout.ToggleLeft(value.ToString(), (bool)value, GUI.skin.textField) },
					};
				}
				return _fieldForType;
			}
		}

        public Dictionary<Action, GUIContent> FilteredDetails { get; private set; }
		private string lastFilterText;

        public OutputEntry() : base()
		{
			EnumerationItems = new List<OutputEntry>();
		}

		public override void LoadVoid()
		{
			base.LoadVoid();
			DisplayMessage = DisplayFieldFor(null, Text);
			FilteredDetails = Details = new Dictionary<Action, GUIContent>();
		}

		protected override void LoadSingleObject(object value)
		{
			base.LoadSingleObject(value);
			LoadInDetails(value, RexReflectionUtils.ExtractDetails(value));
		}

		protected override void LoadEnumeration(IEnumerable values)
		{
			LoadSingleObject(values);

			foreach (object o in values)
			{
				var member = new OutputEntry();
				member.LoadSingleObject(o);
				EnumerationItems.Add(member);
			}
		}

		/// <summary>
		/// Loads in the Details and converts them to Action and GUI contents.
		/// </summary>
		/// <param name="value">Ouput Value of the Expression</param>
		/// <param name="message">Message from the Expression execute</param>
		/// <param name="memberDetails"></param>
		private void LoadInDetails(object value, IEnumerable<MemberDetails> memberDetails)
		{
			DisplayMessage = DisplayFieldFor(value, Text);
			FilteredDetails = Details = (from detail in memberDetails
					   let tooltip = RexUIUtils.SyntaxHighlingting(detail.TakeWhile(i => i.Type != SyntaxType.EqualsOp))
					   let content = new GUIContent(detail.Name.String, tooltip)
					   let displayAction = DisplayFieldFor(detail.Value, detail.Constant.String)
					   select new { displayAction, content }).ToDictionary(i => i.displayAction, i => i.content);
		}

		private void DisplayExcetion()
		{
			if (!string.IsNullOrEmpty(Text))
				EditorGUILayout.HelpBox(Text, MessageType.Warning);

			EditorGUILayout.BeginVertical();
			{
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.TextArea(_exceptionDetails.Name.String, DetailsStyle);
					EditorGUILayout.TextArea(Exception.Message, GUI.skin.textArea);
				}
				EditorGUILayout.EndHorizontal();

				ShowDetails = EditorGUILayout.Foldout(ShowDetails, "Full StackTrace");
				if (ShowDetails)
				{
					EditorGUILayout.TextArea(Exception.ToString(), GUI.skin.textArea);
				}
			}
			EditorGUILayout.EndVertical();
		}

		/// <summary>
		/// Draws the output entry inside the UI.
		/// </summary>
		public override void DrawOutputUI()
		{
			if (Exception != null)
			{
				DisplayExcetion();
				return;
			}

			EditorGUILayout.BeginVertical();
			{
				EditorGUILayout.BeginHorizontal();
				var handler = DisplayMessage;
				if (handler != null)
					handler();
				EditorGUILayout.EndHorizontal();
				if (FilteredDetails.Count > 0)
				{
					ShowDetails = EditorGUILayout.Foldout(ShowDetails, RexStaticTextCollection.Instance["foldout_output_details"]);
					if (ShowDetails)
					{
						foreach (var detail in FilteredDetails)
						{
							EditorGUILayout.BeginHorizontal();
							{
								EditorGUILayout.LabelField(detail.Value, DetailsStyle, GUILayout.Width(150));
								detail.Key();
							}
							EditorGUILayout.EndHorizontal();
						}
					}
				}

				if (EnumerationItems.Count > 0)
				{
					if (ShowEnumeration = EditorGUILayout.Foldout(ShowEnumeration, RexStaticTextCollection.Instance["foldout_output_members"]))
					{
						EditorGUI.indentLevel++;
						foreach (var m in EnumerationItems)
							m.DrawOutputUI();
						EditorGUI.indentLevel--;
					}
				}
			}
			EditorGUILayout.EndVertical();
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
			if (type.IsEnum)
			{
				return () => EditorGUILayout.EnumPopup((Enum)value);
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

		private static bool NeedsSpecialField(Type type)
		{
			return FieldForType.ContainsKey(type) || type == typeof(UnityEngine.Object);
		}

        public override bool Filter(string text)
        {
			text = text.ToLower();
			if (string.IsNullOrEmpty(text) || Details == null)
			{
				FilteredDetails = Details;
				EnumerationItems.ForEach(o => o.Filter(text));
				return true;
			}
			if (lastFilterText != text)
			{
				string newValue = "<b>" + text + "</b>";
				FilteredDetails = (
					from d in Details
					where d.Value.text.Contains(text)
					select d).ToDictionary(
						x => x.Key, 
						y => new GUIContent(y.Value.text.Replace(text, newValue), y.Value.tooltip)
				);
				if (EnumerationItems.Count > 0)
				{
					bool shouldDisplay = false;
					foreach (var o in EnumerationItems)
					{	
						 shouldDisplay |=  o.Filter(text);
					}
					return shouldDisplay;
				}
				lastFilterText = text;
				return ShowDetails = FilteredDetails.Count > 0;
			}
			return FilteredDetails.Count > 0;
		}
    }
}