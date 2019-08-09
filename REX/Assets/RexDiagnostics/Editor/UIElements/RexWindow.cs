using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using Rex.Utilities;
using Rex.Window;
using System.Collections.Generic;
using Rex.Utilities.Helpers;

public class RexWindow : EditorWindow
{
	[MenuItem("Window/Analysis/RexWindow")]
	public static void ShowExample()
	{
		var wnd = GetWindow<RexWindow>();
		wnd.titleContent = new GUIContent("RexWindow");
	}
	public List<CodeCompletion> CodeCompletionList = new List<CodeCompletion>();
	public TextField MainInput;
	public RexParser RexParser;
	private ListView listView;

	public void OnEnable()
	{
		// Each editor window contains a root VisualElement object
		var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/RexDiagnostics/Editor/UIElements/RexWindow.uss");
		var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/RexDiagnostics/Editor/UIElements/RexWindow.uxml");
		var tree = visualTree.CloneTree();
		tree.styleSheets.Add(styleSheet);
		rootVisualElement.Add(tree);

		listView = rootVisualElement.Q<ListView>("FormattedCodeList");
		listView.selectionType = SelectionType.Single;
		//listView.selectionType = SelectionType.Multiple;
		listView.onItemChosen += obj => Debug.Log(obj);
		listView.onSelectionChanged += objects => Debug.Log(objects);
		//listView.style.flexGrow = 1f;
		//listView.style.flexShrink = 0f;
		//listView.style.flexBasis = 0f;


		listView.itemsSource = CodeCompletionList;
		listView.makeItem = makeItem;
		listView.bindItem = bindItem;
		listView.SetEnabled(true);

		VisualElement makeItem() => new Label();
		void bindItem(VisualElement e, int i) => (e as Label).text = CodeCompletionList[i].Details.Name.String;


		MainInput = rootVisualElement.Q<TextField>("MainInput");
		MainInput.RegisterValueChangedCallback(InputChanged);

		RexParser = new RexParser();
	}

	public void OnDisable()
	{
		MainInput.UnregisterValueChangedCallback(InputChanged);
	}

	private void InputChanged(ChangeEvent<string> evt)
	{
		CodeCompletionList.Clear();
		CodeCompletionList.AddRange(RexParser.Intellisense(evt.newValue));
		listView.Refresh();

	}
}