using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Contains all static text displayed in the UI.
/// </summary>
[Serializable]
public class RexStaticTextCollection : ScriptableObject
{
	private static RexStaticTextCollection _instance;
	public static RexStaticTextCollection Instance
	{
		get
		{
			if (_instance == null)
				_instance = CreateInstance<RexStaticTextCollection>();
			return _instance;
		}
	}

	class TextEntry
	{
		public string Name;
		public string Text;
		public string Tooltip = string.Empty;
	}

	[SerializeField]
	private List<TextEntry> AllTexts;

	static Dictionary<string, GUIContent> _cache;

	public GUIContent this[string key]
	{
		get
		{
			if (_cache.ContainsKey(key))
				return _cache[key];

			var text = AllTexts.First(i => i.Name == key);
			return _cache[key] = new GUIContent(text.Text, text.Tooltip);
		}
	}

	void OnEnable()
	{
		if (_instance == null)
		{
			_instance = this;
		}
		else if (_instance != this)
		{
			Destroy(this);
			return;
		}
		if (_cache == null)
		{
			_cache = new Dictionary<string, GUIContent>();
		}

		if (AllTexts != null) return;
		InitializeEnglish();
	}

	private void InitializeEnglish()
	{
		AllTexts = new List<TextEntry>
		{
			new TextEntry
			{
				Name = "label_expression",
				Text = "Expression:",
			},
			new TextEntry
			{
				Name = "button_evaluate",
				Text = "Evaluate",
				Tooltip =  "Evaluates the expression"
			},
			new TextEntry
			{
				Name = "label_output_header",
				Text = "Output"
			},
			new TextEntry
			{
				Name = "button_output_clear",
				Text = "Clear",
				Tooltip = "Clear the Output pannel"
			},
			new TextEntry
			{
				Name = "foldout_output_details",
				Text = "Details"
			},
			new TextEntry
			{
				Name = "foldout_output_members",
				Text = "Members"
			},
			new TextEntry
			{
				Name = "toggle_scope_header",
				Text = "Scope",
				Tooltip = "Namespace selection"
			},
			new TextEntry
			{
				Name = "label_scope_use",
				Text = "Use",
				Tooltip = "Include in usings?"
			},
			new TextEntry
			{
				Name = "label_scope_namespace",
				Text = "Namespace"
			},
		};
	}
}
