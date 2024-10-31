using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fisekoo
{
    public class BaseShaderGUI : ShaderGUI
    {
        protected enum SurfaceType { Opaque, Transparent }

        protected enum BlendMode { Alpha, Additive, Premultiply, Multiply }

        protected enum RenderFace { Both = 0, Front = 2, Back = 1 }

        protected enum DepthWrite { Enabled = 1, Disabled = 0, }

        protected enum DepthTest { Never = 7, Less = 6, Equal = 5, LEqual = 4, Greater = 3, NotEqual = 2, GEqual = 1, Always = 0 }

        protected const string JsonKeyPrefix = "MaterialData_";

        protected MaterialProperty _srcBlendProp, _dstBlendProp;
        protected MaterialProperty _sfProp, _blendProp, _zWrtProp, _clipProp, _zTstProp, _cullProp, _queueOffProp, _castShdProp;
        protected LocalKeyword _ALPHATEST_ON, _ALPHABLEND_ON, _ALPHAPREMULTIPLY_ON, _SURFACE_TYPE_TRANSPARENT, _BUILTIN_AlphaClip;

        protected int _renderQueue;
        protected bool _mFirstTimeApply = true;
        protected string _materialKey;
        protected Material _targetMat;
        protected readonly Dictionary<Material, CustomMaterialData> _materialDataCache = new();

        protected delegate void DrawPropertiesMethod(MaterialEditor materialEditor, MaterialProperty[] properties);

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (_mFirstTimeApply)
            {
                OnOpenGUI(materialEditor, properties);
                _mFirstTimeApply = false;
            }
            FindSurfaceProperties(properties);
            FindSurfaceKeywords(_targetMat.shader.keywordSpace);
            // Surface options
            DrawSplitter();
            DrawSection(Styles.SfOptionsText, ref _materialDataCache[_targetMat].showSFOptions, materialEditor, properties, DrawSurfaceOptions);
            // Surface props
            DrawSplitter();
            DrawSection(Styles.AddPropsText, ref _materialDataCache[_targetMat].showSFProperties, materialEditor, properties, DrawSurfaceProperties);
            // RenderProps
            DrawSplitter();
            DrawSection(Styles.RndPropsText, ref _materialDataCache[_targetMat].showRenderOptions, materialEditor, properties, DrawRenderProperties);

            SaveGUIData();
        }

        protected virtual void OnOpenGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            InitializeMaterial(materialEditor, properties);
        }

        protected void InitializeMaterial(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            _targetMat = materialEditor.target as Material;
            _materialKey = JsonKeyPrefix + _targetMat!.GetInstanceID();
            if (!_materialDataCache.TryGetValue(_targetMat, out _))
            {
                var jsonData = EditorPrefs.GetString(_materialKey, JsonUtility.ToJson(new CustomMaterialData(true)));
                _materialDataCache[_targetMat] = JsonUtility.FromJson<CustomMaterialData>(jsonData);
            }
            FindSurfaceProperties(properties);
            FindSurfaceKeywords(_targetMat.shader.keywordSpace);
            UpdateMaterialProperties((SurfaceType)_sfProp.floatValue, (BlendMode)_blendProp.floatValue, (RenderFace)_cullProp.floatValue,
                                    (DepthWrite)_zWrtProp.floatValue, (DepthTest)_zTstProp.floatValue, (byte)_castShdProp.floatValue, (byte)_clipProp.floatValue);
        }

        protected void DrawSection(string title, ref bool state, MaterialEditor materialEditor, MaterialProperty[] properties, DrawPropertiesMethod drawer)
        {
            state = DrawHeaderFoldout(title, state);
            DrawPropertiesWithBool(state, materialEditor, properties, drawer);
        }

        protected void SaveGUIData()
        {
            var json = JsonUtility.ToJson(_materialDataCache[_targetMat]);
            EditorPrefs.SetString(_materialKey, json);
        }


        protected void DrawSurfaceOptions(MaterialEditor editor, MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();
            var surfaceType = (SurfaceType)EditorGUILayout.EnumPopup(Styles.SfTypeText, (SurfaceType)_sfProp.floatValue);
            var blendMode = (BlendMode)_blendProp.floatValue;

            if (surfaceType == SurfaceType.Transparent)
            {
                blendMode = (BlendMode)EditorGUILayout.EnumPopup(Styles.BlendingModeText, blendMode);
            }

            var cull = (RenderFace)EditorGUILayout.EnumPopup(Styles.RenderFaceText, (RenderFace)_cullProp.floatValue);
            var zWrite = (DepthWrite)EditorGUILayout.EnumPopup(Styles.DepthWriteText, (DepthWrite)_zWrtProp.floatValue);
            var zTest = (DepthTest)EditorGUILayout.EnumPopup(Styles.DepthTestText, (DepthTest)_zTstProp.floatValue);
            var alphaClip = EditorGUILayout.Toggle(Styles.AlphaClipText, _clipProp.floatValue == 1) ? 1 : 0;
            var castShadows = 0;
#if USING_URP || USING_HDRP
            castShadows = EditorGUILayout.Toggle(Styles.CastShadowsText, _castShdProp.floatValue == 1) ? 1 : 0;
#endif

            if (EditorGUI.EndChangeCheck())
            {
                UpdateMaterialProperties(surfaceType, blendMode, cull, zWrite, zTest, (byte)castShadows, (byte)alphaClip);
            }
        }

        protected void UpdateMaterialProperties(SurfaceType surfaceType, BlendMode blendMode, RenderFace cull, DepthWrite zWrite, DepthTest zTest, byte castShadows, byte alphaClip)
        {
            _sfProp.floatValue = (byte)surfaceType;
            _cullProp.floatValue = (byte)cull;
            _zWrtProp.floatValue = (byte)zWrite;
            _zTstProp.floatValue = (byte)zTest;
            _clipProp.floatValue = alphaClip;
            _targetMat.SetShaderPassEnabled("DepthOnly", (byte)zWrite == 1);
#if USING_URP || USING_HDRP
            _castShdProp.floatValue = castShadows;
            _targetMat.SetShaderPassEnabled("ShadowCaster", castShadows == 1);
#endif

            if (alphaClip == 1)
            {
                _targetMat.EnableKeyword(_ALPHATEST_ON);
#if !(USING_URP || USING_HDRP)
                _targetMat.EnableKeyword(_BUILTIN_AlphaClip);
#endif
            }
            else
            {
                _targetMat.DisableKeyword(_ALPHATEST_ON);
#if !(USING_URP || USING_HDRP)
                _targetMat.DisableKeyword(_BUILTIN_AlphaClip);
#endif
            }

            switch (surfaceType)
            {
                case SurfaceType.Opaque:
                    SetOpaqueMaterialProperties(alphaClip);
                    break;

                case SurfaceType.Transparent:
                    SetTransparentMaterialProperties(blendMode);
                    break;
            }

            _renderQueue += _queueOffProp.intValue;
            _targetMat.renderQueue = _renderQueue;

            MaterialEditor.ApplyMaterialPropertyDrawers(_targetMat);
        }

        protected void SetMaterialBlendMode(UnityEngine.Rendering.BlendMode srcBlend, UnityEngine.Rendering.BlendMode dstBlend)
        {
            _srcBlendProp.floatValue = (float)srcBlend;
            _dstBlendProp.floatValue = (float)dstBlend;
        }

        protected void SetOpaqueMaterialProperties(int alphaClip)
        {
            _targetMat.SetOverrideTag("RenderType", "Opaque");
            _targetMat.DisableKeyword(_ALPHAPREMULTIPLY_ON);
            _targetMat.DisableKeyword(_SURFACE_TYPE_TRANSPARENT);
            _renderQueue = alphaClip == 1 ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
            SetMaterialBlendMode(UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.Zero);
        }

        protected void SetTransparentMaterialProperties(BlendMode blendMode)
        {
            _blendProp.floatValue = (float)blendMode;
            _targetMat.SetOverrideTag("RenderType", "Transparent");
            _targetMat.DisableKeyword(_ALPHAPREMULTIPLY_ON);
            _targetMat.DisableKeyword(_ALPHABLEND_ON);
            _targetMat.EnableKeyword(_SURFACE_TYPE_TRANSPARENT);
            _renderQueue = (int)RenderQueue.Transparent;

            switch (blendMode)
            {
                case BlendMode.Alpha:
                    SetMaterialBlendMode(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.Premultiply:
                    SetMaterialBlendMode(UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    _targetMat.EnableKeyword(_ALPHAPREMULTIPLY_ON);
                    break;
                case BlendMode.Additive:
                    SetMaterialBlendMode(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.One);
                    break;
                case BlendMode.Multiply:
                    SetMaterialBlendMode(UnityEngine.Rendering.BlendMode.DstColor, UnityEngine.Rendering.BlendMode.Zero);
                    _targetMat.EnableKeyword(_ALPHAPREMULTIPLY_ON);
                    break;
            }
        }
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            var sfTypeRef = Shader.PropertyToID(_sfProp?.name);
            if (!material.HasProperty(sfTypeRef)) return;
            FindSurfaceKeywords(newShader.keywordSpace);
            float GetFromMat(int id) => material.GetFloat(id);
            var blendModeRef = Shader.PropertyToID(_blendProp.name);
            var cullReff = Shader.PropertyToID(_cullProp.name);
            var zWrtReff = Shader.PropertyToID(_zWrtProp.name);
            var zTstReff = Shader.PropertyToID(_zTstProp.name);
            var clip = (byte)GetFromMat(Shader.PropertyToID(_clipProp.name));
            var sfType = (SurfaceType)material.GetFloat(sfTypeRef);
            material.SetFloat(sfTypeRef, (float)sfType);
            var cstShd = byte.MinValue;
#if (USING_URP || USING_HDRP)
            cstShd = (byte)GetFromMat(Shader.PropertyToID(_castShdProp.name));
#endif
            UpdateMaterialProperties(sfType, (BlendMode)GetFromMat(blendModeRef), (RenderFace)GetFromMat(cullReff), (DepthWrite)GetFromMat(zWrtReff), (DepthTest)GetFromMat(zTstReff), cstShd, clip);
        }
        protected void DrawSurfaceProperties(MaterialEditor editor, MaterialProperty[] properties)
        {
            foreach (var prop in properties)
            {
                if ((prop.flags & MaterialProperty.PropFlags.HideInInspector) == 0)
                {
                    editor.ShaderProperty(prop, prop.displayName);
                }
            }
        }

        protected void DrawRenderProperties(MaterialEditor editor, MaterialProperty[] properties)
        {
            editor.RenderQueueField();
            editor.EnableInstancingField();
            editor.DoubleSidedGIField();
        }

        protected void FindSurfaceProperties(MaterialProperty[] properties)
        {
#if USING_URP
            _sfProp = FindProperty("_Surface", properties);
            _blendProp = FindProperty("_Blend", properties);
            _srcBlendProp = FindProperty("_SrcBlend", properties);
            _dstBlendProp = FindProperty("_DstBlend", properties);
            _zWrtProp = FindProperty("_ZWrite", properties);
            _clipProp = FindProperty("_AlphaClip", properties);
            _zTstProp = FindProperty("_ZTest", properties);
            _cullProp = FindProperty("_Cull", properties);
            _queueOffProp = FindProperty("_QueueOffset", properties);
            _castShdProp = FindProperty("_CastShadows", properties);
#elif USING_HDRP
    // Add HDRP specific properties here if needed
#else
            _sfProp = FindProperty("_BUILTIN_Surface", properties);
            _blendProp = FindProperty("_BUILTIN_Blend", properties);
            _srcBlendProp = FindProperty("_BUILTIN_SrcBlend", properties);
            _dstBlendProp = FindProperty("_BUILTIN_DstBlend", properties);
            _zWrtProp = FindProperty("_BUILTIN_ZWrite", properties);
            _clipProp = FindProperty("_BUILTIN_AlphaClip", properties);
            _zTstProp = FindProperty("_BUILTIN_ZTest", properties);
            _cullProp = FindProperty("_BUILTIN_CullMode", properties);
            _queueOffProp = FindProperty("_BUILTIN_QueueOffset", properties);
#endif
        }

        protected void FindSurfaceKeywords(LocalKeywordSpace localKeywordSpace)
        {
#if USING_URP
            _ALPHATEST_ON = localKeywordSpace.FindKeyword("_ALPHATEST_ON");
            _ALPHABLEND_ON = localKeywordSpace.FindKeyword("_ALPHABLEND_ON");
            _ALPHAPREMULTIPLY_ON = localKeywordSpace.FindKeyword("_ALPHAPREMULTIPLY_ON");
            _SURFACE_TYPE_TRANSPARENT = localKeywordSpace.FindKeyword("_SURFACE_TYPE_TRANSPARENT");
#elif USING_HDRP
    // Add HDRP specific keywords here if needed
#else
            _ALPHATEST_ON = localKeywordSpace.FindKeyword("_BUILTIN_ALPHATEST_ON");
            _ALPHABLEND_ON = localKeywordSpace.FindKeyword("_BUILTIN_ALPHABLEND_ON");
            _ALPHAPREMULTIPLY_ON = localKeywordSpace.FindKeyword("_BUILTIN_ALPHAPREMULTIPLY_ON");
            _SURFACE_TYPE_TRANSPARENT = localKeywordSpace.FindKeyword("_BUILTIN_SURFACE_TYPE_TRANSPARENT");
            _BUILTIN_AlphaClip = localKeywordSpace.FindKeyword("_BUILTIN_AlphaClip");
#endif
        }
        protected void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);
            float xMin = rect.xMin;

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;

            if (isBoxed)
            {
                rect.xMin = xMin == 7.0 ? 4.0f : EditorGUIUtility.singleLineHeight;
                rect.width -= 1;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }

        protected bool DrawHeaderFoldout(string title, bool state)
        {
            const float height = 17f;
            var backgroundRect = GUILayoutUtility.GetRect(1f, height);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;
            foldoutRect.x = labelRect.xMin + 15 * (EditorGUI.indentLevel - 1); //fix for presset

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.07f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
            {
                state = !state;
                e.Use();
            }

            return state;
        }

        protected static void DrawPropertiesWithBool(bool active, MaterialEditor editor, MaterialProperty[] properties, DrawPropertiesMethod drawer)
        {
            if (!active) return;
            EditorGUI.indentLevel++;
            drawer(editor, properties);
            EditorGUILayout.Space();
            EditorGUI.indentLevel--;
        }
    }

    public static class Styles
    {
        public const string SfOptionsText = "Surface Options";
        public const string AddPropsText = "Surface Properties";
        public const string RndPropsText = "Advanced Options";
        public const string SfTypeText = "Surface Type";
        public const string BlendingModeText = "Blending Mode";
        public const string RenderFaceText = "Render Face";
        public const string DepthWriteText = "Depth Write";
        public const string DepthTestText = "Depth Test";
        public const string AlphaClipText = "Alpha Clipping";
        public const string CastShadowsText = "Cast Shadows";
    }

    [Serializable]
    public class CustomMaterialData
    {
        public bool showSFOptions, showSFProperties, showRenderOptions;
        public CustomMaterialData(bool defaultValue)
        {
            showSFOptions = showSFProperties = showRenderOptions = defaultValue;
        }
    }
}