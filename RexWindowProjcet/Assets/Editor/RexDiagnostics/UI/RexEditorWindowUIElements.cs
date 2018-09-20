using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;


public class RexEditorWindowUIElements : EditorWindow
{
    [MenuItem("Window/UIElements/RexEditorWindowUIElements")]
    public static void ShowExample()
    {
        RexEditorWindowUIElements wnd = GetWindow<RexEditorWindowUIElements>();
        wnd.titleContent = new GUIContent("RexEditorWindowUIElements");
    }

    public void OnEnable()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = this.GetRootVisualContainer();

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        //VisualElement label = new Label("Hello World! From C#");
        //root.Add(label);

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath("Assets/Editor/RexDiagnostics/UI/RexEditorWindowUIElements.uxml", typeof(VisualTreeAsset)) as VisualTreeAsset;
        VisualElement labelFromUXML = visualTree.();
        root.Add(labelFromUXML);

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        VisualElement labelWithStyle = new Label("Hello World! With Style");
        labelWithStyle.AddStyleSheetPath("Assets/Editor/RexDiagnostics/UI/RexEditorWindowUIElements_style.uss");
        root.Add(labelWithStyle);
    }
}