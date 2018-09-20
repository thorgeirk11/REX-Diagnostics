using System;
using UnityEditor;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;


namespace Rex.Window
{
	[Serializable]
	public sealed class RexEditorWindow : EditorWindow
	{
		// Add menu named "My Window" to the Window menu
		[MenuItem("Window/(REX) Runtime Expressions")]
		static void Init()
		{
			// Get existing open window or if none, make a new one:
			var window = GetWindow<RexEditorWindow>();
			var elem = new Button();
			elem.Add(new Label("Button Wow"));
			elem.Add(new Label("Button Wow"));
			elem.Add(new Label("Button Wow"));
			elem.Add(new Label("Button Wow"));
			window.GetRootVisualContainer().Add(elem);
			window.Show();
		}
	}
}