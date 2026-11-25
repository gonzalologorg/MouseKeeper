using System.Collections.Generic;
using UnityEngine;

namespace MouseKeeper
{
    public sealed class HitFlash : MonoBehaviour
    {
        const float Duration = 0.3f;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int LegacyColor = Shader.PropertyToID("_Color");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        sealed class Target
        {
            public Renderer renderer;
            public Material material;
            public int index;
            public int colorProperty;
            public bool useEmission;
            public bool hadOverride;
            public Color originalColor;
            public Color originalEmission;
            public readonly MaterialPropertyBlock original = new MaterialPropertyBlock();
            public readonly MaterialPropertyBlock flash = new MaterialPropertyBlock();
            public readonly MaterialPropertyBlock beforeFreeze = new MaterialPropertyBlock();
            public bool hadFreezeOverride;
        }

        readonly List<Target> targets = new List<Target>();
        float remaining;
        bool frozen;

        public void SetFrozen(bool value)
        {
            if (frozen == value) return;
            if (remaining > 0f) { remaining = 0f; Restore(); }
            frozen = value;
            foreach (Target target in targets)
            {
                if (target.renderer == null) continue;
                if (value)
                {
                    target.renderer.GetPropertyBlock(target.beforeFreeze, target.index);
                    target.hadFreezeOverride = !target.beforeFreeze.isEmpty;
                    var tint = new MaterialPropertyBlock();
                    target.renderer.GetPropertyBlock(tint, target.index);
                    tint.SetColor(target.colorProperty, new Color(0.3f, 0.65f, 1f, 1f));
                    target.renderer.SetPropertyBlock(tint, target.index);
                }
                else target.renderer.SetPropertyBlock(target.hadFreezeOverride ? target.beforeFreeze : null, target.index);
            }
        }

        void Awake()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null) continue;
                    int colorProperty = material.HasProperty(BaseColor) ? BaseColor :
                        material.HasProperty(LegacyColor) ? LegacyColor : 0;
                    if (colorProperty == 0) continue;
                    targets.Add(new Target
                    {
                        renderer = renderer,
                        material = material,
                        index = i,
                        colorProperty = colorProperty,
                        useEmission = material.HasProperty(EmissionColor) && material.IsKeywordEnabled("_EMISSION")
                    });
                }
            }
        }

        public void Flash()
        {
            if (remaining <= 0f)
            {
                foreach (Target target in targets)
                {
                    if (target.renderer == null || target.material == null) continue;
                    target.renderer.GetPropertyBlock(target.original, target.index);
                    target.hadOverride = !target.original.isEmpty;
                    if (target.hadOverride)
                        target.renderer.GetPropertyBlock(target.flash, target.index);
                    else
                        target.renderer.GetPropertyBlock(target.flash);
                    target.originalColor = target.flash.HasColor(target.colorProperty)
                        ? target.flash.GetColor(target.colorProperty) : target.material.GetColor(target.colorProperty);
                    if (target.useEmission)
                        target.originalEmission = target.flash.HasColor(EmissionColor)
                            ? target.flash.GetColor(EmissionColor) : target.material.GetColor(EmissionColor);
                }
            }
            remaining = Duration;
            Apply(1f);
        }

        void Update()
        {
            if (remaining <= 0f) return;
            remaining = Mathf.Max(0f, remaining - Time.deltaTime);
            if (remaining == 0f) Restore();
            else Apply(remaining / Duration);
        }

        void Apply(float strength)
        {
            foreach (Target target in targets)
            {
                if (target.renderer == null) continue;
                Color red = new Color(1f, 0f, 0f, target.originalColor.a);
                target.flash.SetColor(target.colorProperty, Color.Lerp(target.originalColor, red, strength));
                if (target.useEmission)
                    target.flash.SetColor(EmissionColor, target.originalEmission + new Color(strength * 0.6f, 0f, 0f, 0f));
                target.renderer.SetPropertyBlock(target.flash, target.index);
            }
        }

        void Restore()
        {
            foreach (Target target in targets)
                if (target.renderer != null)
                    target.renderer.SetPropertyBlock(target.hadOverride ? target.original : null, target.index);
        }

        void OnDisable()
        {
            if (remaining <= 0f) return;
            remaining = 0f;
            Restore();
        }
    }
}
