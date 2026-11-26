using System;
using UnityEngine;

namespace MouseKeeper
{
    [Serializable]
    public sealed class LocalizedAgentDialogue
    {
        [Tooltip("Unity system language name, for example Spanish or English.")]
        public string language = "Spanish";
        public TextAsset file;
    }

    public enum AgentVoice { Male, Female }

    [CreateAssetMenu(menuName = "MouseKeeper/Agente", fileName = "Nuevo Agente")]
    public sealed class AgentDefinition : ScriptableObject
    {
        public AgentProfile profile = new AgentProfile();
        [Tooltip("Retrato opcional para la planificación.")]
        public Sprite portrait;
        [TextArea] public string description;
        [Tooltip("Visual prefab only; leave empty to use a placeholder.")]
        public GameObject modelPrefab;
        public Color placeholderColor = new Color(0.2f, 0.55f, 1f);
        [Tooltip("Legacy default; expeditions use the weapon assigned during planning.")]
        public WeaponDefinition startingWeapon;
        [Min(0)] public int missionCost = 5;
        public bool freeRecruit;
        [Header("Voz")]
        public AgentVoice voice;
        [Range(-0.1f, 0.1f)] public float voicePitchOffset;
        public float VoicePitch => (voice == AgentVoice.Female ? 1.12f : 0.96f) + voicePitchOffset;

        [Header("Diálogos localizables")]
        public LocalizedAgentDialogue[] dialogues;
    }
}
