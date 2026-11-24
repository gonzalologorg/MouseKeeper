using System;
using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Enemigos/Pool de aparición")]
    public sealed class EnemySpawnPool : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public EnemyPreset enemy;
            [Tooltip("Peso relativo por piso, empezando por el piso 1. Los pisos posteriores reutilizan el último valor.")]
            public float[] floorWeights;

            public float Weight(int floor)
            {
                if (enemy == null || floorWeights == null || floorWeights.Length == 0) return 0f;
                return Mathf.Max(0f, floorWeights[Mathf.Clamp(floor, 0, floorWeights.Length - 1)]);
            }
        }

        public EnemyPreset boss;
        public Entry[] enemies;

        // Weights are relative; zero disables an entry for that floor.
        public EnemyPreset Choose(int floor, float roll)
        {
            float total = 0f;
            if (enemies != null)
                foreach (Entry entry in enemies) if (entry != null) total += entry.Weight(floor);
            if (total <= 0f) return null;
            float value = Mathf.Clamp01(roll) * total;
            EnemyPreset last = null;
            foreach (Entry entry in enemies)
            {
                if (entry == null) continue;
                float weight = entry.Weight(floor);
                if (weight <= 0f) continue;
                last = entry.enemy;
                if (value < weight) return last;
                value -= weight;
            }
            return last;
        }
    }
}
