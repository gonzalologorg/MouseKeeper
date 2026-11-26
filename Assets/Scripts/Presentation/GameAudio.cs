using System.Collections.Generic;
using UnityEngine;

namespace MouseKeeper
{
    internal sealed class GameAudio : MonoBehaviour
    {
        const int MaxVoices = 24;
        const string Root = "Sounds/";
        static readonly System.Random Random = new System.Random();
        static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        static GameAudio instance;
        readonly List<AudioSource> voices = new List<AudioSource>();

        internal static float Range(float minimum, float maximum) =>
            Mathf.Lerp(minimum, maximum, (float)Random.NextDouble());

        internal static bool Play(string path, Vector3 position, float pitch = 1f, float volume = 0.65f)
        {
            if (!Application.isPlaying) return false;
            if (!Clips.TryGetValue(path, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>(Root + path);
                Clips.Add(path, clip);
            }
            return Play(clip, position, pitch, volume);
        }

        internal static void PlayVariant(AudioClip[] clips, Vector3 position)
        {
            if (!Application.isPlaying || clips == null || clips.Length == 0) return;
            Play(clips[Random.Next(clips.Length)], position, 1f, 0.5f);
        }

        internal static void PlayRatDeath(Vector3 position)
        {
            if (Application.isPlaying) Play("death/squelching_" + Random.Next(1, 5), position, volume: 0.45f);
        }

        internal static void PlayPickup(PickupKind kind, Vector3 position)
        {
            string path = kind switch
            {
                PickupKind.Weapon => "item_equip",
                PickupKind.Money => "coin_jingle_small",
                PickupKind.Passive => "keys_jingling",
                PickupKind.Health => "cardboard_pick_up",
                _ => null
            };
            if (path != null) Play("weapons/" + path, position);
        }

        static bool Play(AudioClip clip, Vector3 position, float pitch, float volume)
        {
            if (clip == null) return false;
            if (instance == null) instance = new GameObject("Game Audio").AddComponent<GameAudio>();

            AudioSource voice = null;
            foreach (AudioSource candidate in instance.voices)
            {
                if (candidate.isPlaying) continue;
                voice = candidate;
                break;
            }
            if (voice == null)
            {
                if (instance.voices.Count >= MaxVoices) return false;
                var emitter = new GameObject("Audio Voice");
                emitter.transform.SetParent(instance.transform, false);
                voice = emitter.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.spatialBlend = 0.25f;
                voice.minDistance = 8f;
                voice.maxDistance = 45f;
                voice.dopplerLevel = 0f;
                instance.voices.Add(voice);
            }

            voice.transform.position = position;
            voice.clip = clip;
            voice.volume = volume;
            voice.pitch = pitch;
            voice.Play();
            return true;
        }
    }
}
