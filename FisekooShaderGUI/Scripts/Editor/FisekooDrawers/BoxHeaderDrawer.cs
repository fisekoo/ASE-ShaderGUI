using UnityEditor;
using UnityEngine;
namespace Fisekoo.Drawers
{
    public class BoxHeaderDrawer : MaterialPropertyDrawer
    {
        private int fontSize;
        private GUIStyle headerStyle = new GUIStyle()
        {
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            stretchWidth = true
        };
        public BoxHeaderDrawer() : this(16) { }
        public BoxHeaderDrawer(int size)
        {
            this.fontSize = size;
        }
        public override void OnGUI(Rect position, MaterialProperty property, GUIContent label, MaterialEditor editor)
        {
            headerStyle.fontSize = fontSize;
            headerStyle.normal.textColor = Color.white;
            GUILayout.Space(-15);
            DrawBoxHeader(property.displayName, headerStyle);
        }
        public static void DrawBox(Rect position, Color color)
        {
            Color oldColor = GUI.color;

            GUI.color = color;
            GUI.Box(position, string.Empty);

            GUI.color = oldColor;
        }
        public static float DrawBoxHeader(string label, GUIStyle style, int height = 20, float padding = 5)
        {
            float totalHeight = height + (padding * 2);

            Rect r = EditorGUILayout.GetControlRect(false, totalHeight);
            r.height = totalHeight;

            DrawBox(r, Color.black);

            Rect labelRect = new Rect(r.x, r.y + padding, r.width, height);
            EditorGUI.LabelField(labelRect, label, style);

            return totalHeight;
        }
    }
}