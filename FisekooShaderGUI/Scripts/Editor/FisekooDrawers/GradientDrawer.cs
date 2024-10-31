using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fisekoo.Drawers
{
    public class GradientDrawer : MaterialPropertyDrawer
    {
        private readonly int _resolution;
        private readonly bool _hdr;
        private MaterialProperty _prop;
        private string textureName => $"z_{_prop.name}Tex";

        public GradientDrawer() : this(256, false) { }
        public GradientDrawer(float resolution) : this((int)resolution, false) { }
        public GradientDrawer(bool hdr) : this(256, hdr) { }
        public GradientDrawer(float resolution, string parameters) : this((int)resolution, ExtractHdrParameter(parameters)) { }

        private GradientDrawer(int resolution, bool hdr)
        {
            _resolution = resolution;
            _hdr = hdr;
        }

        private static bool ExtractHdrParameter(string parameters) =>
            parameters.Split(',').Any(s => string.Equals(s, "true", StringComparison.OrdinalIgnoreCase));

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            OnGUI(position, prop, label, editor, string.Empty);
        }

        public void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor, string tooltip)
        {
            var guiContent = new GUIContent(label, tooltip);
            OnGUI(position, prop, guiContent, editor);
        }


        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (prop.type != MaterialProperty.PropType.Texture)
            {
                EditorGUI.HelpBox(position, $"[Gradient] used on property {prop.name} of type {prop.type}.", MessageType.Error);
                return;
            }

            if (!AssetDatabase.Contains(prop.targets.FirstOrDefault()))
            {
                EditorGUI.HelpBox(position, $"Material {prop.targets.FirstOrDefault()?.name} is not an Asset.", MessageType.Error);
                return;
            }

            _prop = prop;

            Gradient currentGradient = LoadCurrentGradient(prop);
            currentGradient ??= CreateDefaultGradient();

            EditorGUI.showMixedValue = prop.targets.Length > 1;

            using (var changeScope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.Space(-22);
                EditorGUILayout.BeginHorizontal();

                DrawLabel(label);
                currentGradient = DrawGradientFieldWithButton(currentGradient, prop, _hdr, () => { });

                if (changeScope.changed)
                {
                    UpdateGradient(currentGradient, prop);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.showMixedValue = false;
        }

        private Gradient DrawGradientFieldWithButton(Gradient currentGradient, MaterialProperty property, bool hdr, System.Action buttonAction)
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            float gradientHeight = EditorGUIUtility.singleLineHeight;
            var totalRect = EditorGUILayout.GetControlRect(true, gradientHeight, EditorStyles.colorField, new[] { GUILayout.MinWidth(0) });
            var buttonWidth = 50;

            var gradientRect = new Rect(totalRect.x, totalRect.y, totalRect.width - buttonWidth - 5, gradientHeight);
            gradientRect.xMin -= 15f;

            var buttonRect = new Rect(gradientRect.xMax + 5, totalRect.y, buttonWidth, gradientHeight);
            var buttonIcon = EditorGUIUtility.IconContent("CustomTool@2x");

            currentGradient = EditorGUI.GradientField(gradientRect, GUIContent.none, currentGradient, hdr, ColorSpace.Linear);

            if (GUI.Button(buttonRect, buttonIcon))
            {
                var contextMenu = new GenericMenu();
                contextMenu.AddItem(new GUIContent("Reverse"), false, () =>
                {
                    var colorKeys = currentGradient.colorKeys;
                    for (var i = 0; i < colorKeys.Length / 2; i++)
                    {
                        (colorKeys[i].color, colorKeys[colorKeys.Length - 1 - i].color) = (
                            colorKeys[colorKeys.Length - 1 - i].color, colorKeys[i].color);
                    }

                    currentGradient = new Gradient
                    {
                        colorKeys = colorKeys,
                        alphaKeys = currentGradient.alphaKeys,
                        mode = currentGradient.mode
                    };
                    UpdateGradient(currentGradient, property);
                });
                contextMenu.ShowAsContext();
            }

            EditorGUILayout.EndVertical();

            return currentGradient;
        }
        private Gradient LoadCurrentGradient(MaterialProperty prop)
        {
            if (prop.targets.Length != 1) return null;

            var target = (Material)prop.targets[0];
            var path = AssetDatabase.GetAssetPath(target);
            var textureAsset = LoadSubAsset(path, textureName);
            var materialReset = target.GetTexture(prop.name) == null;

            if (textureAsset != null && IsValidTextureFormat(textureAsset, _hdr))
            {
                return Decode(prop, textureAsset.name);
            }

            return materialReset ? CreateDefaultGradient() : null;
        }

        private static bool IsValidTextureFormat(Texture2D textureAsset, bool hdr) =>
            (textureAsset.format == TextureFormat.RGBAHalf) == hdr;

        private static Texture2D LoadSubAsset(string path, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Texture2D>().FirstOrDefault(asset => asset.name.StartsWith(name));

        private void DrawLabel(GUIContent label)
        {
            var guiContent = new GUIContent(label.text, label.tooltip);
            EditorGUILayout.LabelField(guiContent, EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth));
        }

        private static Texture2D CreateEmptySubAssetTexture(string path, string name, FilterMode filterMode, int resolution, bool hdr)
        {
            var textureAsset = new Texture2D(resolution, 1, hdr ? TextureFormat.RGBAHalf : TextureFormat.ARGB32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = filterMode
            };
            AssetDatabase.AddObjectToAsset(textureAsset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            return textureAsset;
        }

        private static Gradient CreateDefaultGradient()
        {
            return new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                alphaKeys = new[] { new GradientAlphaKey(1, 0f), new GradientAlphaKey(1, 1f) }
            };
        }

        private void UpdateGradient(Gradient gradient, MaterialProperty prop)
        {
            var encodedGradient = Encode(gradient);
            var fullAssetName = textureName + encodedGradient;
            foreach (var target in prop.targets)
            {
                if (!AssetDatabase.Contains(target)) continue;

                var path = AssetDatabase.GetAssetPath(target);
                var filterMode = gradient.mode == GradientMode.Blend ? FilterMode.Bilinear : FilterMode.Point;
                var textureAsset = GetOrCreateGradientTexture(path, textureName, filterMode, _resolution, _hdr);
                Undo.RecordObject(textureAsset, "Change Material Gradient");
                textureAsset.name = fullAssetName;
                Bake(gradient, textureAsset);

                var material = (Material)target;
                material.SetTexture(prop.name, textureAsset);
                EditorUtility.SetDirty(material);
            }
        }

        private static Texture2D GetOrCreateGradientTexture(string path, string name, FilterMode filterMode, int resolution, bool hdr)
        {
            var textureAsset = LoadSubAsset(path, name);

            if (textureAsset != null && ((hdr && textureAsset.format != TextureFormat.RGBAHalf) ||
                                          (!hdr && textureAsset.format == TextureFormat.RGBAHalf)))
            {
                AssetDatabase.RemoveObjectFromAsset(textureAsset);
            }

            if (textureAsset == null)
            {
                textureAsset = CreateEmptySubAssetTexture(path, name, filterMode, resolution, hdr);
            }

            textureAsset.filterMode = filterMode;

            if (textureAsset.width != resolution)
            {
#if UNITY_2021_2_OR_NEWER
                textureAsset.Reinitialize(resolution, 1);
#else
                textureAsset.Resize(resolution, 1);
#endif
            }

            return textureAsset;
        }

        public static void Bake(Gradient gradient, Texture2D texture)
        {
            if (gradient == null) return;

            for (var x = 0; x < texture.width; x++)
            {
                var color = gradient.Evaluate((float)x / (texture.width - 1));
                for (var y = 0; y < texture.height; y++) texture.SetPixel(x, y, color);
            }

            texture.Apply();
        }

        private static string Encode(Gradient gradient) => gradient == null ? null : JsonUtility.ToJson(new GradientData(gradient));

        private Gradient Decode(MaterialProperty prop, string name)
        {
            if (prop == null) return null;

            var json = name.Substring(textureName.Length);
            try
            {
                var gradientRepresentation = JsonUtility.FromJson<GradientData>(json);
                return gradientRepresentation?.ToGradient();
            }
            catch (Exception)
            {
                return null;
            }
        }

        [Serializable]
        internal class GradientData
        {
            public GradientMode mode;
            public ColorKey[] colorKeys;
            public AlphaKey[] alphaKeys;

            public GradientData() { }

            public GradientData(Gradient source)
            {
                FromGradient(source);
            }

            public void FromGradient(Gradient source)
            {
                mode = source.mode;
                colorKeys = source.colorKeys.Select(key => new ColorKey(key)).ToArray();
                alphaKeys = source.alphaKeys.Select(key => new AlphaKey(key)).ToArray();
            }

            public Gradient ToGradient()
            {
                var gradient = new Gradient();
                gradient.mode = mode;
                gradient.colorKeys = colorKeys.Select(key => key.ToGradientKey()).ToArray();
                gradient.alphaKeys = alphaKeys.Select(key => key.ToGradientKey()).ToArray();
                return gradient;
            }

            [Serializable]
            public struct ColorKey
            {
                public Color color;
                public float time;

                public ColorKey(GradientColorKey source)
                {
                    color = source.color;
                    time = source.time;
                }

                public GradientColorKey ToGradientKey() => new GradientColorKey(color, time);
            }

            [Serializable]
            public struct AlphaKey
            {
                public float alpha;
                public float time;

                public AlphaKey(GradientAlphaKey source)
                {
                    alpha = source.alpha;
                    time = source.time;
                }

                public GradientAlphaKey ToGradientKey() => new GradientAlphaKey(alpha, time);
            }
        }
    }
}
