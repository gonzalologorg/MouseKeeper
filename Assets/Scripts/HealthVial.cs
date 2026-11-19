using UnityEngine;

namespace MouseKeeper
{
    public sealed class HealthVial : MonoBehaviour
    {
        [Min(0.01f)] public float amount = 25f;
    }
}
