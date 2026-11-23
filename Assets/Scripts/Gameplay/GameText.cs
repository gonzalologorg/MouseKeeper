using System;
using System.Collections.Generic;
using UnityEngine;

namespace MouseKeeper
{
    public static class GameText
    {
        [Serializable] sealed class Entry { public string key; public string spanish; public string english; }
        [Serializable] sealed class Table { public Entry[] entries; }

        const string LanguageKey = "MouseKeeper.Language";
        static Dictionary<string, Entry> entries;

        public static bool English => PlayerPrefs.GetString(LanguageKey, "Spanish") == "English";

        public static void SetEnglish(bool english)
        {
            PlayerPrefs.SetString(LanguageKey, english ? "English" : "Spanish");
            PlayerPrefs.Save();
        }

        static void Load()
        {
            if (entries != null) return;
            entries = new Dictionary<string, Entry>();
            TextAsset asset = Resources.Load<TextAsset>("GameText");
            if (asset == null) { Debug.LogError("Missing Resources/GameText localization file."); return; }
            Table table;
            try { table = JsonUtility.FromJson<Table>(asset.text); }
            catch (ArgumentException error) { Debug.LogError("Invalid GameText localization file: " + error.Message, asset); return; }
            if (table == null || table.entries == null) { Debug.LogError("GameText has no entries.", asset); return; }
            foreach (Entry entry in table.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key) || string.IsNullOrEmpty(entry.english) ||
                    entries.ContainsKey(entry.key))
                {
                    Debug.LogError("GameText contains an invalid or duplicate entry.", asset);
                    continue;
                }
                entries.Add(entry.key, entry);
            }
        }

        public static string Get(string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            Load();
            if (!entries.TryGetValue(source, out Entry entry)) return source;
            return English ? entry.english : string.IsNullOrEmpty(entry.spanish) ? source : entry.spanish;
        }

        public static string Format(string source, params object[] args) => string.Format(Get(source), args);
    }
}
