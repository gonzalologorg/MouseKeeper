using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MouseKeeper
{
    [DefaultExecutionOrder(100)]
    public sealed class DamageIndicators : MonoBehaviour
    {
        const float Duration = 1f;

        sealed class Indicator
        {
            public Text text;
            public Vector3 origin;
            public float elapsed;
            public Color color;
        }

        readonly List<Indicator> indicators = new List<Indicator>();
        Camera viewCamera;
        Canvas canvas;
        RectTransform canvasRect;
        Font font;

        public void Initialize(Camera camera)
        {
            viewCamera = camera;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                Debug.LogError("The built-in font for damage indicators is unavailable.", this);
                return;
            }
            var root = new GameObject("Damage Indicators", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            canvasRect = root.GetComponent<RectTransform>();
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        public void Show(Vector3 origin, float amount, bool isPlayer)
        {
            if (canvas == null || viewCamera == null)
            {
                Debug.LogError("Damage indicators require an initialized canvas and camera.", this);
                return;
            }
            var root = new GameObject("Damage Number", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            root.transform.SetParent(canvas.transform, false);
            Text text = root.GetComponent<Text>();
            text.font = font;
            text.fontSize = 36;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.text = amount.ToString("G5");
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200f, 60f);
            Outline outline = root.GetComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2f, -2f);
            indicators.Add(new Indicator { text = text, origin = origin, color = isPlayer ? Color.red : Color.white });
        }

        void LateUpdate()
        {
            for (int i = indicators.Count - 1; i >= 0; i--)
            {
                Indicator indicator = indicators[i];
                indicator.elapsed += Time.deltaTime;
                if (indicator.elapsed >= Duration)
                {
                    Destroy(indicator.text.gameObject);
                    indicators.RemoveAt(i);
                    continue;
                }
                float t = indicator.elapsed / Duration;
                float height = 0.5f * t + 1.1f * 4f * t * (1f - t);
                Vector3 screen = viewCamera.WorldToScreenPoint(indicator.origin + Vector3.up * height);
                indicator.text.enabled = screen.z > 0f;
                if (screen.z > 0f && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 position))
                    indicator.text.rectTransform.anchoredPosition = position;
                Color color = indicator.color;
                color.a = Mathf.Clamp01((1f - t) * 3f);
                indicator.text.color = color;
            }
        }

        void OnDestroy()
        {
            if (canvas != null) Destroy(canvas.gameObject);
        }
    }
}
