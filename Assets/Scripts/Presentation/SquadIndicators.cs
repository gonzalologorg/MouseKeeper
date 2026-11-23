using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MouseKeeper
{
    [DefaultExecutionOrder(200)]
    public sealed class SquadIndicators : MonoBehaviour
    {
        sealed class Indicator
        {
            public PlayerController agent;
            public CharacterController controller;
            public GameObject root;
            public Text name;
            public AgentHealthRing health;
            public int displayedLevel;
            public readonly List<Speech> speeches = new List<Speech>();
            public readonly Queue<AgentEmotion> emotions = new Queue<AgentEmotion>();
            public Image emotion;
            public AgentEmotion currentEmotion;
            public float emotionStarted;
            public bool showingEmotion;
        }

        sealed class Speech
        {
            public Text text;
            public string content;
            public float started;
            public float typingDuration;
        }

        readonly List<Indicator> indicators = new List<Indicator>();
        Camera viewCamera;
        Canvas canvas;
        RectTransform canvasRect;
        Font font;
        readonly Sprite[] emotionSprites = new Sprite[6];

        public void Initialize(Camera camera)
        {
            viewCamera = camera;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (viewCamera == null || font == null)
            {
                Debug.LogError("Squad indicators require a camera and the built-in font.", this);
                return;
            }
            var root = new GameObject("Squad Indicators", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasRect = root.GetComponent<RectTransform>();
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            Texture2D atlas = Resources.Load<Texture2D>("EmotionIcons");
            if (atlas == null || atlas.width != 576 || atlas.height != 96)
                Debug.LogError("The emotion icon atlas is missing or has incorrect dimensions.", this);
            else
                for (int i = 0; i < emotionSprites.Length; i++)
                    emotionSprites[i] = Sprite.Create(atlas, new Rect(i * 96, 0, 96, 96), new Vector2(0.5f, 0.5f));
        }

        public void Register(PlayerController agent)
        {
            if (canvas == null || agent == null)
            {
                Debug.LogError("Squad indicators must be initialized before registering an agent.", this);
                return;
            }

            var root = new GameObject("Agent Indicator", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var labelObject = new GameObject("Agent Name", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            labelObject.transform.SetParent(root.transform, false);
            Text label = labelObject.GetComponent<Text>();
            label.font = font;
            label.fontSize = 22;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.supportRichText = true;
            label.text = agent.Profile.NameWithLevel(14);
            label.color = agent.Profile.LevelColor;
            label.rectTransform.sizeDelta = new Vector2(220f, 32f);
            Outline outline = labelObject.GetComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);

            var emotionObject = new GameObject("Agent Emotion", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            emotionObject.transform.SetParent(root.transform, false);
            Image emotion = emotionObject.GetComponent<Image>();
            emotion.raycastTarget = false;
            emotion.rectTransform.sizeDelta = new Vector2(52f, 52f);
            emotionObject.SetActive(false);

            var healthObject = new GameObject("Agent Health", typeof(MeshFilter), typeof(MeshRenderer), typeof(AgentHealthRing));
            healthObject.transform.SetParent(agent.transform, false);
            healthObject.transform.localPosition = Vector3.up * 0.03f;
            AgentHealthRing health = healthObject.GetComponent<AgentHealthRing>();
            health.Initialize();
            indicators.Add(new Indicator {
                agent = agent, controller = agent.GetComponent<CharacterController>(), root = root,
                name = label, health = health, emotion = emotion, displayedLevel = agent.Profile.Level
            });
        }

        Indicator FindIndicator(PlayerController agent)
        {
            foreach (Indicator indicator in indicators)
                if (indicator.agent == agent) return indicator;
            return null;
        }

        public bool QueueDialogue(PlayerController agent, string content)
        {
            Indicator indicator = FindIndicator(agent);
            if (indicator == null || string.IsNullOrEmpty(content)) return false;
            var root = new GameObject("Agent Dialogue", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            root.transform.SetParent(indicator.root.transform, false);
            Text text = root.GetComponent<Text>();
            text.font = font;
            text.fontSize = 19;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.rectTransform.sizeDelta = new Vector2(340f, 34f);
            Outline outline = root.GetComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);
            indicator.speeches.Add(new Speech {
                text = text, content = content, started = Time.time,
                typingDuration = Mathf.Max(0.1f, content.Length / 30f)
            });
            return true;
        }

        public void QueueEmotion(PlayerController agent, AgentEmotion emotion)
        {
            Indicator indicator = FindIndicator(agent);
            if (indicator == null || emotionSprites[(int)emotion] == null) return;
            if (indicator.showingEmotion && indicator.currentEmotion == emotion || indicator.emotions.Contains(emotion)) return;
            indicator.emotions.Enqueue(emotion);
        }

        public void ClearPresentation(PlayerController agent)
        {
            Indicator indicator = FindIndicator(agent);
            if (indicator == null) return;
            foreach (Speech speech in indicator.speeches) Destroy(speech.text.gameObject);
            indicator.speeches.Clear();
            indicator.emotions.Clear();
            indicator.showingEmotion = false;
            indicator.emotion.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            for (int i = indicators.Count - 1; i >= 0; i--)
            {
                Indicator indicator = indicators[i];
                if (indicator.agent == null)
                {
                    Destroy(indicator.root);
                    indicators.RemoveAt(i);
                    continue;
                }
                bool visible = !indicator.agent.Dead && indicator.agent.gameObject.activeInHierarchy;
                Vector3 head = viewCamera.WorldToScreenPoint(indicator.agent.transform.position +
                    Vector3.up * (indicator.controller.center.y + indicator.controller.height * 0.5f + 0.25f));
                visible &= head.z > 0f;
                indicator.root.SetActive(visible);
                if (!visible) continue;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, head, null, out Vector2 namePosition);
                AgentProfile profile = indicator.agent.Profile;
                if (indicator.displayedLevel != profile.Level)
                {
                    indicator.displayedLevel = profile.Level;
                    indicator.name.text = profile.NameWithLevel(14);
                    indicator.name.color = profile.LevelColor;
                }
                indicator.name.rectTransform.anchoredPosition = namePosition + Vector2.up * 8f;
                for (int j = indicator.speeches.Count - 1; j >= 0; j--)
                {
                    Speech speech = indicator.speeches[j];
                    float elapsed = Time.time - speech.started;
                    float duration = speech.typingDuration + 3f;
                    if (elapsed >= duration)
                    {
                        Destroy(speech.text.gameObject);
                        indicator.speeches.RemoveAt(j);
                        continue;
                    }
                    int count = Mathf.Clamp(Mathf.CeilToInt(speech.content.Length * elapsed / speech.typingDuration), 0, speech.content.Length);
                    speech.text.text = speech.content.Substring(0, count);
                    speech.text.color = new Color(1f, 1f, 1f,
                        Mathf.Min(1f, elapsed / 0.2f) * Mathf.Min(1f, (duration - elapsed) / 0.35f));
                    speech.text.rectTransform.anchoredPosition = namePosition +
                        Vector2.up * (45f + j * 34f + 12f * Mathf.Clamp01(elapsed / 0.35f));
                }
                if (indicator.showingEmotion && Time.time - indicator.emotionStarted >= 2f)
                {
                    indicator.showingEmotion = false;
                    indicator.emotion.gameObject.SetActive(false);
                }
                if (!indicator.showingEmotion && indicator.emotions.Count > 0)
                {
                    indicator.currentEmotion = indicator.emotions.Dequeue();
                    indicator.emotion.sprite = emotionSprites[(int)indicator.currentEmotion];
                    indicator.emotionStarted = Time.time;
                    indicator.showingEmotion = true;
                    indicator.emotion.gameObject.SetActive(true);
                }
                if (indicator.showingEmotion)
                {
                    float elapsed = Time.time - indicator.emotionStarted;
                    indicator.emotion.rectTransform.anchoredPosition = namePosition + new Vector2(50f, 0f + 8f * elapsed / 2f);
                    indicator.emotion.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(elapsed / 0.2f));
                    indicator.emotion.color = new Color(1f, 1f, 1f, Mathf.Min(1f, (2f - elapsed) / 0.35f));
                }
                indicator.health.FillAmount = indicator.agent.MaxHealth > 0f
                    ? indicator.agent.Health / indicator.agent.MaxHealth : 0f;
            }
        }

        void OnDestroy()
        {
            if (canvas != null) Destroy(canvas.gameObject);
            foreach (Sprite sprite in emotionSprites)
                if (sprite != null)
                {
                    if (Application.isPlaying) Destroy(sprite);
                    else DestroyImmediate(sprite);
                }
        }
    }

    public sealed class AgentHealthRing : MonoBehaviour
    {
        const int Segments = 64;
        const float Radius = 0.65f;
        float fillAmount = -1f;
        Mesh mesh;

        public Color32 HealthColor => fillAmount > 0.6f ? new Color32(70, 220, 100, 255) :
            fillAmount > 0.3f ? new Color32(255, 210, 45, 255) : new Color32(240, 65, 55, 255);

        public void Initialize()
        {
            Material material = Resources.Load<Material>("AgentHealth");
            if (material == null)
            {
                Debug.LogError("The agent health material is missing.", this);
                return;
            }
            mesh = new Mesh { name = "Agent Health Ring" };
            GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            FillAmount = 1f;
        }

        public float FillAmount
        {
            get => fillAmount;
            set
            {
                float next = Mathf.Clamp01(value);
                if (Mathf.Approximately(fillAmount, next)) return;
                fillAmount = next;
                if (mesh != null) Rebuild();
            }
        }

        void Rebuild()
        {
            var vertices = new List<Vector3>();
            var colors = new List<Color32>();
            var triangles = new List<int>();
            AddArc(1f, new Color32(30, 30, 30, 220), vertices, colors, triangles);
            AddArc(fillAmount, HealthColor, vertices, colors, triangles);
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        static void AddArc(float amount, Color32 color, List<Vector3> vertices, List<Color32> colors, List<int> triangles)
        {
            float inner = Radius * 0.77f;
            float end = Mathf.Clamp01(amount) * Segments;
            int segments = Mathf.CeilToInt(end);
            for (int i = 0; i < segments; i++)
            {
                float a = 2f * Mathf.PI * i / Segments;
                float b = 2f * Mathf.PI * Mathf.Min(i + 1, end) / Segments;
                Vector3 first = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                Vector3 second = new Vector3(Mathf.Sin(b), 0f, Mathf.Cos(b));
                int start = vertices.Count;
                vertices.Add(first * inner);
                vertices.Add(first * Radius);
                vertices.Add(second * Radius);
                vertices.Add(second * inner);
                for (int j = 0; j < 4; j++) colors.Add(color);
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            }
        }

        void LateUpdate()
        {
            transform.rotation = Quaternion.identity;
        }

        void OnDestroy()
        {
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
        }
    }
}
