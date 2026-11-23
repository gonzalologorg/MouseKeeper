using System;
using System.Collections.Generic;
using UnityEngine;

namespace MouseKeeper
{
    public enum DialogueEvent { RoomCleared, Injured, Purchase, Healed, BossDefeated, AllyDied }
    public enum AgentEmotion { Anger, Fury, Sadness, Fear, Happiness, Desire }

    [Serializable]
    public sealed class AgentDialogueLines
    {
        public string[] roomCleared;
        public string[] injured;
        public string[] purchase;
        public string[] healed;
        public string[] bossDefeated;
        public string[] allyDied;

        public string[] For(DialogueEvent kind)
        {
            switch (kind)
            {
                case DialogueEvent.RoomCleared: return roomCleared;
                case DialogueEvent.Injured: return injured;
                case DialogueEvent.Purchase: return purchase;
                case DialogueEvent.Healed: return healed;
                case DialogueEvent.BossDefeated: return bossDefeated;
                case DialogueEvent.AllyDied: return allyDied;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }

    public sealed class AgentDialogue : MonoBehaviour
    {
        readonly Dictionary<DialogueEvent, HashSet<string>> used = new Dictionary<DialogueEvent, HashSet<string>>();
        readonly Dictionary<DialogueEvent, string> last = new Dictionary<DialogueEvent, string>();
        AgentDialogueLines lines;
        PlayerController agent;
        SquadIndicators indicators;
        bool injuredThisFloor;
        bool furious;
        int purchases;

        public void Initialize(PlayerController owner, AgentDefinition definition, SquadIndicators presentation)
        {
            agent = owner;
            indicators = presentation;
            TextAsset file = SelectFile(definition, GameText.English ? "English" : "Spanish");
            if (file == null)
            {
                Debug.LogWarning("No dialogue file configured for " + owner.Profile.name + ".", owner);
                return;
            }
            try { lines = JsonUtility.FromJson<AgentDialogueLines>(file.text); }
            catch (ArgumentException error)
            {
                Debug.LogError("Invalid dialogue file " + file.name + ": " + error.Message, file);
                return;
            }
            if (lines == null)
            {
                Debug.LogError("Dialogue file " + file.name + " is empty.", file);
                return;
            }
            foreach (DialogueEvent kind in Enum.GetValues(typeof(DialogueEvent)))
            {
                string[] entries = lines.For(kind);
                if (entries == null || entries.Length == 0)
                {
                    Debug.LogError("Dialogue file " + file.name + " has no lines for " + kind + ".", file);
                    lines = null;
                    return;
                }
                var unique = new HashSet<string>();
                foreach (string entry in entries)
                {
                    if (string.IsNullOrWhiteSpace(entry) ||
                        entry.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length > 10 || !unique.Add(entry))
                    {
                        Debug.LogError("Dialogue file " + file.name + " has an empty, repeated or overlong line for " + kind + ".", file);
                        lines = null;
                        return;
                    }
                }
            }
        }

        public static TextAsset SelectFile(AgentDefinition definition, string language)
        {
            if (definition == null || definition.dialogues == null) return null;
            TextAsset fallback = null;
            foreach (LocalizedAgentDialogue entry in definition.dialogues)
            {
                if (entry == null || entry.file == null) continue;
                if (string.Equals(entry.language, language, StringComparison.OrdinalIgnoreCase)) return entry.file;
                if (string.Equals(entry.language, "Spanish", StringComparison.OrdinalIgnoreCase)) fallback = entry.file;
            }
            return fallback;
        }

        public bool Speak(DialogueEvent kind, float chance)
        {
            if (lines == null || agent.Dead || indicators == null ||
                chance < 1f && UnityEngine.Random.value >= chance) return false;
            string[] entries = lines.For(kind);
            if (!used.TryGetValue(kind, out HashSet<string> spoken))
                used.Add(kind, spoken = new HashSet<string>());
            bool newCycle = spoken.Count >= entries.Length;
            if (newCycle) spoken.Clear();
            last.TryGetValue(kind, out string previous);
            var available = new List<string>();
            foreach (string entry in entries)
                if (!spoken.Contains(entry) &&
                    (!newCycle || entries.Length == 1 || entry != previous)) available.Add(entry);
            string chosen = available[UnityEngine.Random.Range(0, available.Count)];
            if (!indicators.QueueDialogue(agent, GameText.Get(chosen))) return false;
            spoken.Add(chosen);
            last[kind] = chosen;
            return true;
        }

        public bool AnnouncePickup(string itemName)
        {
            return agent != null && !agent.Dead && indicators != null &&
                !string.IsNullOrWhiteSpace(itemName) && indicators.QueueDialogue(agent, itemName);
        }

        public void ResetFloor()
        {
            used.Clear();
            last.Clear();
            injuredThisFloor = false;
            furious = false;
        }

        public void CheckInjury()
        {
            if (injuredThisFloor || agent.Dead || agent.Health / agent.MaxHealth > 0.25f) return;
            if (Speak(DialogueEvent.Injured, 1f)) injuredThisFloor = true;
        }

        public void Purchase()
        {
            purchases++;
            Speak(DialogueEvent.Purchase, 0.5f);
            if (purchases >= 2) Emotion(AgentEmotion.Desire);
        }

        public void SetFury(bool active)
        {
            if (active && !furious) Emotion(AgentEmotion.Fury);
            furious = active;
        }

        public void Emotion(AgentEmotion emotion)
        {
            if (indicators != null && !agent.Dead) indicators.QueueEmotion(agent, emotion);
        }
    }
}
