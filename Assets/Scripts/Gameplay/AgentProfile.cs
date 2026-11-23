using System;
using UnityEngine;

namespace MouseKeeper
{
    [Serializable]
    public sealed class AgentProfile
    {
        public string name = "Agente";
        [HideInInspector]
        public int model;
        [Range(20f, 200f)]
        public float health = 100f;
        [Range(2f, 8f)]
        public float speed = 5f;
        [Range(0.5f, 2f)]
        public float attackSpeed = 1f;
        [HideInInspector]
        public int weapon;
        [Range(1, 10)]
        public int bravery = 5;
        [Range(1, 10)]
        public int accuracy = 5;
        [Range(1, 10)]
        public int agility = 5;
        [Range(1, 10)]
        public int distrust = 1;
        [Range(1, 10)]
        public int greed = 1;
        [Range(1, 10)]
        public int teamwork = 5;

        [NonSerialized] public float healthMultiplier = 1f;
        [NonSerialized] public float speedMultiplier = 1f;
        public float EffectiveHealth => Mathf.Ceil(health * Mathf.Lerp(2.5f, 1f, Mathf.InverseLerp(2f, 4f, speed)) * healthMultiplier);
        public float EffectiveSpeed => speed * speedMultiplier;

        public const int MaxLevel = 10;
        public int Level { get; internal set; } = 1;
        public Color LevelColor => Level switch
        {
            <= 3 => Color.white,
            <= 5 => new Color32(102, 174, 255, 255),
            <= 7 => new Color32(119, 223, 122, 255),
            <= 9 => new Color32(255, 215, 88, 255),
            _ => new Color32(255, 104, 104, 255)
        };

        public string NameWithLevel(int levelFontSize) => $"{name} <size={levelFontSize}>| lvl {Level}</size>";

        public int Rating
        {
            get
            {
                float value = 0.4f * Mathf.InverseLerp(20f, 200f, EffectiveHealth) +
                    0.3f * Mathf.InverseLerp(2f, 8f, EffectiveSpeed) +
                    0.3f * Mathf.InverseLerp(0.5f, 2f, attackSpeed);
                return Mathf.Clamp(Mathf.CeilToInt(value * 5f), 1, 5);
            }
        }

        public AgentProfile Copy() => (AgentProfile)MemberwiseClone();
    }

    [Serializable]
    public sealed class AgentRoster
    {
        public AgentProfile[] agents;
    }
}
