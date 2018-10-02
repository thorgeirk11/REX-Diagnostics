using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rex.Utilities;
using Rex.Utilities.Helpers;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace Rex.Window
{
    public class OutputEntry2 : VisualElement, IOutputEntry
    {
        /// <summary>
        /// Action Dictionary for displaying the details.
        /// </summary>
        public List<OutputEntry2> EnumerationItems { get; private set; }

        public Exception Exception { get; set; }

        public Foldout ExtraItemFoldout { get; private set; }

        /// <summary>
        /// Returns a display action for the value.
        /// </summary>
        /// <param name="value">Value to be displayed</param>
        /// <param name="defaultString">default string for the value</param>
        private static VisualElement DisplayFieldFor(object value, string defaultString, string tooltip)
        {
            switch (value)
            {
                case string s: return new Label(s) { tooltip = tooltip };
                case Vector2 v: return new Vector2Field { value = v, tooltip = tooltip };
                case Vector3 v: return new Vector3Field { value = v, tooltip = tooltip };
                case Vector4 v: return new Vector4Field { value = v, tooltip = tooltip };
                case Color v: return new ColorField { value = v, tooltip = tooltip };
                case Rect v: return new RectField { value = v, tooltip = tooltip };
                case Bounds v: return new BoundsField { value = v, tooltip = tooltip };
                case bool v: return new Toggle { value = v, text = defaultString, tooltip = tooltip };
                case Enum v: return new EnumField { value = v, tooltip = tooltip };
                case UnityEngine.Object obj: return new ObjectField { value = obj, tooltip = tooltip };
                default: return new Label(defaultString);
            }
        }

        public void LoadVoid()
        {
            Add(DisplayFieldFor("Statement Executed succesfully!", "", ""));
        }

        private void LoadSingleObject(object obj)
        {
            Add(DisplayFieldFor(obj, obj.ToString(), ""));
            var elements = from detail in RexReflectionUtils.ExtractDetails(obj)
                           let tooltip = RexUIUtils.SyntaxHighlingting(detail.TakeWhile(i => i.Type != SyntaxType.EqualsOp))
                           select DisplayFieldFor(detail.Value, detail.Constant.String, tooltip);
            if (elements.Any())
            {
                ExtraItemFoldout = new Foldout() { tooltip = "Click to expand" };
                foreach (var element in elements)
                {
                    ExtraItemFoldout.Add(element);
                }
                Add(ExtraItemFoldout);
            }
        }

        private void LoadEnumeration(IEnumerable enumerable)
        {
            ExtraItemFoldout = new Foldout() { text = enumerable.ToString(), tooltip = "Click to expand" };
            foreach (var element in enumerable)
            {
                var entry = new OutputEntry2();
                entry.LoadSingleObject(element);
                ExtraItemFoldout.Add(entry);
            }
            Add(ExtraItemFoldout);
        }

        /// <summary>
        /// Setups an output with the given value.
        /// </summary>
        /// <param name="value">Object returned by the expression</param>
        public void LoadObject(object value)
        {
            if (value != null &&
                !(value is string || value is Enum) &&
                value is IEnumerable)
            {
                LoadEnumeration(value as IEnumerable);
            }
            else
            {
                LoadSingleObject(value);
            }
        }
    }
}
