using System.Linq;
using Rex.Utilities;
using Rex.Window;
using UnityEditor;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;


public class RexEditorWindowUIElements : EditorWindow
{
    private Box _outputPanel;
    private ScrollView scroll;

    [MenuItem("Window/Rex Window")]
    public static void ShowExample()
    {
        var wnd = GetWindow<RexEditorWindowUIElements>();
        wnd.titleContent = new GUIContent("RexEditorWindow");
    }

    public void OnEnable()
    {
        // Each editor window contains a root VisualElement object
        var root = this.GetRootVisualContainer();

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/RexDiagnostics/UI/RexEditorWindow.uxml");

        var labelFromUXML = visualTree.CloneTree(null);
        labelFromUXML.AddStyleSheetPath("Assets/Editor/RexDiagnostics/UI/RexEditorWindow.uss");
        root.Add(labelFromUXML);

        var text = new TextField(100, true, false, '*');
        text.OnValueChanged(InputChanged);
        root.Add(text);

        _outputPanel = new Box();
        scroll = new ScrollView();
        scroll.Add(_outputPanel);
        root.Add(_outputPanel);
    }

    private void InputChanged(ChangeEvent<string> evt)
    {
        if (!Input.GetKeyDown(KeyCode.LeftShift) &&
            evt.newValue.Except(evt.previousValue).Contains('\n'))
            Execute(evt.newValue);
    }


    void Execute(string code)
    {
        var rexParser = new RexParser();
        var parseResult = rexParser.ParseAssignment(code);
        var result = RexCompileEngine.Compile(parseResult);

        if (result != null)
        {
            var output = RexHelper.Execute<OutputEntry2>(result, out var messages);
            _outputPanel.Add(output);
        }
    }

}
