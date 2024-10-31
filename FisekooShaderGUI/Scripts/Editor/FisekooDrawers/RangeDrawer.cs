using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fisekoo.Drawers
{
    public class RangeDrawer : MaterialPropertyDrawer
    {
        private Vector2 _value;
        private readonly Vector2 _range;

        public RangeDrawer() : this(0f, 1f) { }

        public RangeDrawer(float rx, float ry)
        {
            _value = _range = new Vector2(rx, ry);
        }

        private static bool IsPropertyVector(MaterialProperty prop)
        {
            return prop.type == MaterialProperty.PropType.Vector;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            var guiContent = new GUIContent(label);
            OnGUI(position, prop, guiContent, editor);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (!IsPropertyVector(prop))
            {
                EditorGUI.HelpBox(position, $"[Range] used on non-vector property \"{prop.name}\"", MessageType.Error);
                return;
            }
            editor.BeginAnimatedCheck(prop);
            using var changeScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.Space(-18);

            _value = prop.vectorValue;

            if (!string.IsNullOrEmpty(label.tooltip))
            {
                label.tooltip += "\n";
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(label);

            float labelWidth = EditorGUIUtility.labelWidth;
            float valueFieldWidth = 50f;
            float spacing = -5f;

            Rect valueXRect = new Rect(position.x + labelWidth + spacing - 10, position.y + 5f, valueFieldWidth, EditorGUIUtility.singleLineHeight);
            _value.x = EditorGUI.FloatField(valueXRect, _value.x);

            Rect sliderRect = new Rect(valueXRect.xMax + spacing, position.y + 5f, position.width - (labelWidth + valueFieldWidth * 2 + spacing * 3), EditorGUIUtility.singleLineHeight);
            sliderRect.xMax += 10;
            EditorGUI.MinMaxSlider(sliderRect, ref _value.x, ref _value.y, _range.x, _range.y);

            Rect valueYRect = new Rect(sliderRect.xMax + spacing, position.y + 5f, valueFieldWidth, EditorGUIUtility.singleLineHeight);
            _value.y = EditorGUI.FloatField(valueYRect, _value.y);

            EditorGUILayout.EndHorizontal();
            if (changeScope.changed)
            {
                foreach (Object target in prop.targets)
                {
                    if (!AssetDatabase.Contains(target)) continue;

                    Undo.RecordObject(target, $"Change {prop.displayName} Range");
                    var material = (Material)target;
                    prop.vectorValue = _value;
                    EditorUtility.SetDirty(material);
                }
            }
            editor.EndAnimatedCheck();
        }
    }
}