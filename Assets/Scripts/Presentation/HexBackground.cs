using UnityEngine;
using UnityEngine.Rendering;

namespace MouseKeeper
{
    public sealed class HexBackground : MonoBehaviour
    {
        Camera viewCamera;
        GameObject quad;
        Material material;
        float motion;
        float healthFraction = 1f;
        float flashUntil;
        float baseHue;
        float palettePhase;
        const float FlashDuration = 0.45f;
        readonly System.Random paletteRandom = new System.Random();

        public void Initialize(Camera camera)
        {
            viewCamera = camera;
            Shader shader = Resources.Load<Shader>("FX/HexBackground");
            if (viewCamera == null || shader == null)
            {
                Debug.LogError("No se pudo cargar el shader de fondo hexagonal.", this);
                enabled = false;
                return;
            }
            material = new Material(shader);
            viewCamera.cullingMask |= 1 << 2;
            quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Hexagonal Background";
            quad.layer = 2;
            quad.transform.SetParent(viewCamera.transform, false);
            Collider collider = quad.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            NewFloorPalette();
            UpdateQuad();
        }

        public void NewFloorPalette()
        {
            if (material == null) return;
            baseHue = (float)paletteRandom.NextDouble();
            palettePhase = (float)paletteRandom.NextDouble() * Mathf.PI * 2f;
            UpdatePalette();
        }

        void UpdatePalette()
        {
            float time = Time.time * 0.08f + palettePhase;
            float hue = Mathf.Repeat(baseHue + 0.12f * Mathf.Sin(time), 1f);
            float brightness = 0.5f + 0.5f * Mathf.Sin(time * 0.67f);
            material.SetColor("_ColorA", Color.HSVToRGB(hue, 0.68f, Mathf.Lerp(0.16f, 0.22f, brightness)));
            material.SetColor("_ColorB", Color.HSVToRGB(Mathf.Repeat(hue + 0.08f, 1f), 0.58f, Mathf.Lerp(0.42f, 0.52f, brightness)));
        }

        public void SetHealthFraction(float value) => healthFraction = Mathf.Clamp01(value);
        public void Flash() => flashUntil = Time.unscaledTime + FlashDuration;
        public static float MotionSpeed(float healthFraction) => Mathf.Lerp(2.2f, 0.55f, Mathf.Clamp01(healthFraction));

        void LateUpdate()
        {
            if (material == null || viewCamera == null) return;
            motion += Time.deltaTime * MotionSpeed(healthFraction);
            UpdatePalette();
            material.SetFloat("_Motion", motion);
            material.SetFloat("_Flash", Mathf.Clamp01((flashUntil - Time.unscaledTime) / FlashDuration));
            UpdateQuad();
        }

        void UpdateQuad()
        {
            float height = viewCamera.orthographicSize * 2f;
            quad.transform.localPosition = new Vector3(0f, 0f, Mathf.Max(viewCamera.nearClipPlane + 1f, viewCamera.farClipPlane - 1f));
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(height * viewCamera.aspect * 1.01f, height * 1.01f, 1f);
        }

        void OnDestroy()
        {
            if (quad != null) Destroy(quad);
            if (material != null) Destroy(material);
        }
    }
}
