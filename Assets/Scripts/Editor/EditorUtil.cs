using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Dungeon.EditorTools
{
    /// <summary>
    /// EDITOR. Utilidades para los constructores automáticos: asignar campos privados
    /// [SerializeField], crear materiales, carpetas y elementos de UI.
    /// </summary>
    public static class EditorUtil
    {
        public const string MaterialsFolder = "Assets/Materials";

        // ------------------------------------------------------------------
        // Campos serializados (equivale a arrastrar referencias en el Inspector)
        // ------------------------------------------------------------------
        public static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = Find(so, field);
            if (prop == null) return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var prop = Find(so, field);
            if (prop == null) return;
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var prop = Find(so, field);
            if (prop == null) return;
            if (prop.propertyType == SerializedPropertyType.Enum) prop.enumValueIndex = value;
            else prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string field, Color value)
        {
            var so = new SerializedObject(target);
            var prop = Find(so, field);
            if (prop == null) return;
            prop.colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = Find(so, field);
            if (prop == null) return;
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Find(SerializedObject so, string field)
        {
            var prop = so.FindProperty(field);
            if (prop == null)
                Debug.LogError($"[Dungeon] El campo '{field}' no existe en {so.targetObject.GetType().Name}.");
            return prop;
        }

        // ------------------------------------------------------------------
        // Carpetas, materiales y jerarquía
        // ------------------------------------------------------------------
        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        /// <summary>Material de color liso sin iluminación (brillos, avisos). Funciona con URP o con el pipeline integrado.</summary>
        public static Material GetUnlitMaterial(string name, Color color)
        {
            EnsureFolder(MaterialsFolder);
            string path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool urp = GraphicsSettings.defaultRenderPipeline != null;
            Shader shader = Shader.Find(urp ? "Universal Render Pipeline/Unlit" : "Unlit/Color");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        public static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
                                                 Vector3 localScale, Material material, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        public static Light CreatePointLight(string name, Transform parent, Vector3 localPosition, Color color,
                                             float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        // ------------------------------------------------------------------
        // UI (uGUI con texto clásico: no requiere importar TextMeshPro)
        // ------------------------------------------------------------------
        private static Font _font;

        public static Font UIFont
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        public static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 pivot,
                                               Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Hace que el RectTransform ocupe todo su padre.</summary>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Text CreateText(string name, Transform parent, string content, int size, TextAnchor alignment,
                                      Color color, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 boxSize,
                                      bool shadow = true, FontStyle style = FontStyle.Normal)
        {
            var rt = CreateRect(name, parent, anchor, pivot, position, boxSize);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = UIFont;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.85f);
                s.effectDistance = new Vector2(2f, -2f);
            }
            return text;
        }

        public static Image CreateImage(string name, Transform parent, Color color, Vector2 anchor, Vector2 pivot,
                                        Vector2 position, Vector2 size)
        {
            var rt = CreateRect(name, parent, anchor, pivot, position, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Barra con fondo y relleno; devuelve el RectTransform del relleno.</summary>
        public static RectTransform CreateBar(string name, Transform parent, Vector2 anchor, Vector2 pivot,
                                              Vector2 position, Vector2 size, Color background, Color fillColor,
                                              out Image fillImage)
        {
            var bg = CreateImage(name, parent, background, anchor, pivot, position, size);
            // Área interna con margen (borde de 3 px); el relleno se estira dentro de ella con UIBar.
            var area = CreateRect("FillArea", bg.transform, Vector2.zero, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(3f, 3f);
            area.offsetMax = new Vector2(-3f, -3f);
            var fillRt = CreateRect("Fill", area, Vector2.zero, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(fillRt);
            fillImage = fillRt.gameObject.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
            return fillRt;
        }

        public static Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 position,
                                          Vector2 size)
        {
            var rt = CreateRect(name, parent, anchor, new Vector2(0.5f, 0.5f), position, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(0.22f, 0.16f, 0.12f, 0.95f);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.85f, 0.5f);
            colors.selectedColor = new Color(1f, 0.85f, 0.5f);
            colors.pressedColor = new Color(0.8f, 0.6f, 0.3f);
            button.colors = colors;

            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.75f, 0.3f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            var text = CreateText("Label", rt, label, 34, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.8f),
                                  new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            Stretch(text.rectTransform);
            return button;
        }

        public static Canvas CreateScreenCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>Canvas pequeño en el mundo (barras de vida y avisos sobre objetos).</summary>
        public static Canvas CreateWorldCanvas(string name, Transform parent, Vector3 localPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localPosition = localPosition;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one * 0.01f;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            return canvas;
        }
    }
}
