using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fisekoo.Drawers
{
    public class VectorDrawer : MaterialPropertyDrawer
    {
        private float _vType;
        private Vector4 _vec;
        public VectorDrawer() : this(2) { }
        public VectorDrawer(float vType)
        {
            _vType = (int)vType;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            OnGUI(position, prop, label, editor);
        }

        private bool IsPropertyVector(MaterialProperty prop)
        {
            return prop.type == MaterialProperty.PropType.Vector;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (!IsPropertyVector(prop))
            {
                EditorGUI.HelpBox(position, $"[Vector2] used on non-vector property \"{prop.name}\"", MessageType.Error);
                return;
            }
            editor.BeginAnimatedCheck(prop);
            using var changeScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.Space(-15);
            switch (_vType)
            {
                case 2:
                    _vec = EditorGUILayout.Vector2Field(label, prop.vectorValue);
                    break;
                case 3:
                    _vec = EditorGUILayout.Vector3Field(label, prop.vectorValue);
                    break;
                default:
                    _vec = EditorGUILayout.Vector4Field(label, prop.vectorValue);
                    break;
            }
            if (changeScope.changed)
            {
                foreach (Object target in prop.targets)
                {
                    if (!AssetDatabase.Contains(target))
                    {
                        // Failsafe for non-asset materials - should never trigger.
                        continue;
                    }

                    Undo.RecordObject(target, $"Change {prop.displayName}");
                    var material = (Material)target;
                    prop.vectorValue = _vec;
                    EditorUtility.SetDirty(material);
                }
            }
            editor.EndAnimatedCheck();
        }
    }
}