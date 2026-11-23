using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Room Architecture")]
    public sealed class RoomArchitecture : ScriptableObject
    {
        public GameObject[] walls;
        public GameObject[] windows;
        public GameObject[] pillars;
        public GameObject arch;
        public GameObject door;

        public bool IsValid()
        {
            return HasModels(walls) && HasModels(windows) && HasModels(pillars) && arch != null && door != null;
        }

        static bool HasModels(GameObject[] models)
        {
            if (models == null || models.Length == 0) return false;
            foreach (GameObject model in models)
                if (model == null) return false;
            return true;
        }
    }
}
