using UnityEngine;

namespace MouseKeeper
{
    public sealed class PickupGlow : MonoBehaviour
    {
        Vector3 startPosition;
        Quaternion startRotation;
        Light glow;
        bool animate;
        GameRun run;

        public void Initialize(bool animateItem)
        {
            animate = animateItem;
            startPosition = transform.localPosition;
            startRotation = transform.localRotation;
            var lightObject = new GameObject("Pickup Glow");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.position = transform.position + Vector3.up * 0.6f;
            glow = lightObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.95f, 0.8f);
            glow.range = 2.5f;
            glow.shadows = LightShadows.None;
            run = GetComponentInParent<GameRun>();
        }

        void Update()
        {
            float wave = (Mathf.Sin(Time.time * 2f) + 1f) * 0.5f;
            if (run != null) glow.enabled = run.CurrentRoom != null && transform.IsChildOf(run.CurrentRoom);
            glow.intensity = Mathf.Lerp(0.6f, 1.2f, wave);
            if (!animate) return;
            transform.localPosition = startPosition + Vector3.up * (0.12f * Mathf.Sin(Time.time * 2f));
            transform.localRotation = Quaternion.Euler(0f, Time.time * 45f, 0f) * startRotation;
        }
    }
}
