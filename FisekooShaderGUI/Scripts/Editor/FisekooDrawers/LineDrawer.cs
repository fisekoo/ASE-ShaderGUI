using UnityEditor;
using UnityEngine;
namespace Fisekoo.Drawers
{
    public class LineDrawer : MaterialPropertyDrawer
    {
        private Color _color;

        public LineDrawer(float r, float g, float b, float thickness)
        {
            _color = new Color(r / 255f, g / 255f, b / 255f, thickness);
        }

        public LineDrawer(float r, float g, float b) : this(r, g, b, 2f) { }

        public LineDrawer() : this(1f, 1f, 1f, 2f) { }
        public override void OnGUI(Rect position, MaterialProperty property, GUIContent label, MaterialEditor editor)
        {
            GUILayout.Space(-25);
            DrawLine(_color, thickness: _color.a);
        }
        public static void DrawLine(Color color, float thickness = 2, float padding = 10, float hOffset = 0)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.x -= -2 - hOffset;
            r.y += padding / 2f;
            r.width -= hOffset + 2;
            r.height = thickness;
            EditorGUI.DrawRect(r, color);
        }
    }
}

