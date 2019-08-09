using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Rex.Window
{
    public enum VerticalAnchor { NA, Top, Bottom, Center }

    public enum HorizontalAnchor { NA, Left, Right, Center }


    [Flags]
    public enum InputType
    {
        All = -1,
        None = 0,
        LeftClick = 1 << 0,
        RightClick = 1 << 1,
        Click = 3,
        Scroll = 1 << 2,
        Keyboard = 1 << 3,
    }

    /// <summary>
    /// Unity bootstrap. This masterpiece of a class holds functionality to simplify GUI placement and Rect initialization
    /// </summary>
    public static class UStrap
    {
        public static Rect ScreenRect
        {
            get { return new Rect(0, 0, Screen.width, Screen.height); }
        }

        /// <summary>
        /// Returns a rectangle of the given width and height, anchored as instructed to the Screen rect.
        /// </summary>
        /// <param name="width">Desired Width</param>
        /// <param name="height">Desired Height</param>
        /// <param name="verticalAlignment">Vertical placement</param>
        /// <param name="horizontalAlignment">Horizontal placement</param>
        /// <returns></returns>
        public static Rect GiveRect(float width, float height, VerticalAnchor verticalAlignment, HorizontalAnchor horizontalAlignment, float vOffset = 0f, float hOffset = 0f)
        {
            var screen = ScreenRect;
            float x = 0;
            float y = 0;

            if (verticalAlignment == VerticalAnchor.Bottom)
            {
                y = screen.yMax - height;
            }
            else if (verticalAlignment == VerticalAnchor.Center)
            {
                y = screen.center.y - height / 2;
            }

            if (horizontalAlignment == HorizontalAnchor.Right)
            {
                x = screen.xMax - width;
            }
            else if (horizontalAlignment == HorizontalAnchor.Center)
            {
                x = screen.center.x - width / 2;
            }

            x += hOffset;
            y += vOffset;

            return new Rect(x, y, width, height);
        }
        public static Rect GiveRect(int cols = 1, int col = 0, int colSpan = 1, int rows = 1, int row = 0, int rowSpan = 1)
        {
            return ScreenRect.SubRect(cols, col, colSpan, rows, row, rowSpan);
        }

        public static Rect[,] Grid(Rect super, int cols = 1, int rows = 1)
        {
            var grid = new Rect[cols, rows];
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    grid[c, r] = super.SubRect(cols: cols, col: c, rows: rows, row: r, grouped: true);
                }
            }
            return grid;
        }
        public static Rect[,] Grid(int cols = 1, int rows = 1)
        {
            return Grid(ScreenRect, cols, rows);
        }

        #region Extension methods
        /// <summary>
        /// Check to see if a flags enumeration has a specific flag set.
        /// </summary>
        /// <param name="variable">Flags enumeration to check</param>
        /// <param name="value">Flag to check for</param>
        /// <returns></returns>
        public static bool HasFlag<T>(this T variable, T value) where T : struct, IComparable, IFormattable, IConvertible
        {
            if (typeof(T).IsEnum)
            {
                var num = Convert.ToUInt64(value);
                var checking = Convert.ToUInt64(variable);
                return (checking & num) == num;
            }
            return false;
        }

        public static InputType GetInputLocks(this Rect r, InputType inputToBlock)
        {
            var locks = InputType.None;
            var mouseOver = r.Contains(Event.current.mousePosition);
            if (mouseOver)
            {
                locks |= inputToBlock;
            }

            return locks;
        }

        /// <summary>
        /// Splits the rectangle into a given number of rows and columns,
        /// and returns a subRect at the specified column and row, 
        /// that spans the given number of rows and columns.
        /// </summary>
        /// <param name="super">the frame</param>
        /// <param name="cols">the number of columns to create.</param>
        /// <param name="col">the column you want to place the rectangle</param>
        /// <param name="colSpan">how many columns to span</param>
        /// <param name="rows">the number of rows to create</param>
        /// <param name="row">the row you want to place the rect</param>
        /// <param name="rowSpan">how many rows to span</param>
        /// <returns>Returns a subrect spanning the given number of rows and cols. </returns>
        public static Rect SubRect(this Rect super, int cols = 1, int col = 0, int colSpan = 1, int rows = 1, int row = 0, int rowSpan = 1, float vSpacing = 0.0f, float hSpacing = 0.0f, bool grouped = false)
        {
            // Determine column width and row height
            var totalVS = rows * vSpacing;
            var totalHS = cols * hSpacing;
            var colWidth = ((super.width - totalHS) / cols);
            var rowHeight = ((super.height - totalVS) / rows);

            var x = ((hSpacing + colWidth) * col) + hSpacing;
            var y = ((vSpacing + rowHeight) * row) + vSpacing;
            var width = ((colWidth) * colSpan) - hSpacing;
            var height = ((rowHeight) * rowSpan) - vSpacing;

            if (!grouped)
            {
                x += super.xMin;
                y += super.yMin;
            }

            return new Rect(x, y, width, height);
        }

        /// <summary>
        /// Places the rectangle on the screen according to the given Anchors.
        /// If no anchors are provided the rect stays the same.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="verticalAlignment"></param>
        /// <param name="horizontalAlignment"></param>
        public static Rect Place(this Rect r, VerticalAnchor verticalAlignment = VerticalAnchor.NA, HorizontalAnchor horizontalAlignment = HorizontalAnchor.NA)
        {
            return r.Place(ScreenRect, verticalAlignment, horizontalAlignment);
        }

        /// <summary>
        /// Places the rectangle inside the given rectangle according to the given Anchors;
        /// if no anchors are given the rect stays the same.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="other"></param>
        /// <param name="verticalAlignment"></param>
        /// <param name="horizontalAlignment"></param>
        public static Rect Place(this Rect r, Rect other, VerticalAnchor verticalAlignment = VerticalAnchor.NA, HorizontalAnchor horizontalAlignment = HorizontalAnchor.NA)
        {
            var x = r.x;
            var y = r.y;
            var width = r.width;
            var height = r.height;

            if (verticalAlignment == VerticalAnchor.Top)
            {
                y = other.yMin;
            }
            else if (verticalAlignment == VerticalAnchor.Bottom)
            {
                y = other.yMax - r.height;
            }
            else if (verticalAlignment == VerticalAnchor.Center)
            {
                y = other.center.y - r.height / 2;
            }

            if (horizontalAlignment == HorizontalAnchor.Left)
            {
                x = other.xMin;
            }
            else if (horizontalAlignment == HorizontalAnchor.Right)
            {
                x = other.xMax - r.width;
            }
            else if (horizontalAlignment == HorizontalAnchor.Center)
            {
                x = other.center.x - r.width / 2;
            }
            return new Rect(x, y, width, height);
        }

        /// <summary>
        /// Applies padding to the given rect and returns the result
        /// </summary>
        /// <param name="r"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <param name="left"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Rect Padding(this Rect r, float top = 0f, float right = 0f, float bottom = 0f, float left = 0f, float padding = 0f)
        {
            var dt = top + padding;
            var dl = left + padding;
            var db = bottom + dt + padding;
            var dr = right + dl + padding;

            var x = r.xMin + dl;
            var y = r.yMin + dt;
            var w = r.width - dr;
            var h = r.height - db;

            return new Rect(x, y, w, h);
        }

        public static Rect SetMargin(this Rect r, float top = 0f, float bottom = 0f, float left = 0f, float right = 0f)
        {
            return new Rect(r.x + left, r.y + top, r.width - right, r.height - bottom);
        }
        #endregion
    }

    public class UStrapTest : MonoBehaviour
    {
        public Rect rectToPlace;
        public Rect rectToMake;
        public Rect superRect;
        public Rect subRect;

        public VerticalAnchor verticalAlignment = VerticalAnchor.Top;
        public HorizontalAnchor horizontalAlignment = HorizontalAnchor.Left;
        public float makeWidth = 100f;
        public float makeHeight = 50f;

        public VerticalAnchor placeVert = VerticalAnchor.NA;
        public HorizontalAnchor placeHoriz = HorizontalAnchor.NA;

        public int cols = 1;
        public int col = 0;
        public int colSpan = 1;
        public int rows = 1;
        public int row = 0;
        public int rowSpan = 1;

        void Start()
        {
            superRect = UStrap.GiveRect(200, 200, VerticalAnchor.Center, HorizontalAnchor.Center);
            rectToMake = UStrap.GiveRect(makeWidth, makeHeight, verticalAlignment, horizontalAlignment);
            rectToPlace = new Rect(0, 0, 50f, 50f);
            rectToPlace.Place(placeVert, placeHoriz);
            placeRect();
            createSubRect();
        }

        void OnGUI()
        {
            GUI.Box(superRect, "super");
            {
                GUI.Box(subRect, "sub");
            }

            GUI.Box(rectToMake, "GivenRect");

            GUI.Box(rectToPlace, "PlacedRect");


            GUILayout.BeginArea(UStrap.GiveRect(300f, 50f, VerticalAnchor.Bottom, HorizontalAnchor.Center));
            {
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("MakeSubRect"))
                    {
                        createSubRect();
                    }
                    if (GUILayout.Button("PlaceRect"))
                    {
                        placeRect();
                    }
                    if (GUILayout.Button("CreateRect"))
                    {
                        createRect();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private void createRect()
        {
            rectToMake = UStrap.GiveRect(makeWidth, makeHeight, verticalAlignment, horizontalAlignment);
        }
        private void createSubRect()
        {
            subRect = superRect.SubRect(cols, col, colSpan, rows, row, rowSpan);
        }

        private void placeRect()
        {
            rectToPlace = rectToPlace.Place(placeVert, placeHoriz);
        }
    }
}