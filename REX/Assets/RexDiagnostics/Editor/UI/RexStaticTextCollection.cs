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
			GUIContent cached;
			if (_cache.TryGetValue(key, out cached))
			{
				return cached;
			}

			var text = AllTexts.First(i => i.Name == key);
			return _cache[key] = new GUIContent(text.Text, text.Tooltip);
		}
	}
	public GUIContent GetText(string key, string textFormat = null, string tooltipFormat = null)
	{
		var cachekey = textFormat + tooltipFormat + key;
		GUIContent cached;
		if (_cache.TryGetValue(cachekey, out cached))
		{
			return cached;
		}

		var textEntry = AllTexts.First(i => i.Name == key);

		var text = textEntry.Text;
		var tooltip = textEntry.Tooltip;
		if (textFormat != null)
		{
			text = string.Format(text, textFormat);
		}
		if (tooltipFormat != null)
		{
			tooltip = string.Format(tooltip, tooltipFormat);
		}
		return _cache[cachekey] = new GUIContent(text, tooltip);
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
				Name = "expression_header",
				Text = "Expression:",
			},
			new TextEntry
			{
				Name = "evaluate_button",
				Text = "Evaluate",
				Tooltip =  "Evaluates the expression"
			},
			new TextEntry
			{
				Name = "output_header",
				Text = "Output"
			},
			new TextEntry
			{
				Name = "output_clear",
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
				Name = "scope_header",
				Text = "Scope",
				Tooltip = "Namespace selection"
			},
			new TextEntry
			{
				Name = "scope_use",
				Text = "Use",
				Tooltip = "Include in usings?"
			},
			new TextEntry
			{
				Name = "scope_namespace",
				Text = "Namespace"
			},
			new TextEntry
			{
				Name = "variables_header",
				Text = "Variables",
				Tooltip = "Declared variables"
			},
			new TextEntry
			{
				Name = "remove_variable",
				Text = "X",
				Tooltip = "Remove <b>{0}</b>"
			},
			new TextEntry
			{
				Name = "inspect_variable",
				Text = "{0}",
				Tooltip = "Click to inspect <b>{0}</b>"
			},
			new TextEntry
			{
				Name = "macros_header",
				Text = "Macros",
				Tooltip = "Saved expressions"
			},
			new TextEntry
			{
				Name = "macro_go",
				Text = "Go",
				Tooltip = "Evaluate: <b>{0}</b>"
			},
			new TextEntry
			{
				Name = "macro_remove",
				Text = "X",
				Tooltip = "Remove macro"
			},
			new TextEntry
			{
				Name = "select_macro",
				Text = "{0}",
				Tooltip = "Select macro: <b>{0}</b>"
			},
			new TextEntry
			{
				Name = "history_header",
				Text = "History",
				Tooltip = "Succesfully evaluated expressions"
			},
			new TextEntry
			{
				Name = "history_clear",
				Text = "Clear",
				Tooltip = "Clear History"
			},
			new TextEntry
			{
				Name = "history_item_show",
				Text = "{0}",
				Tooltip = "Show options"
			},
			new TextEntry
			{
				Name = "history_item_hide",
				Text = "{0}",
				Tooltip = "Hide options"
			},
			new TextEntry
			{
				Name = "history_item_run",
				Text = "Run",
				Tooltip = "Run Expression"
			},
			new TextEntry
			{
				Name = "history_item_macro",
				Text = "Macro",
				Tooltip = "Save as Macro"
			},
			new TextEntry
			{
				Name = "history_item_delete",
				Text = "Delete",
				Tooltip = "Delete the history item"
			},
		};
	}
}
