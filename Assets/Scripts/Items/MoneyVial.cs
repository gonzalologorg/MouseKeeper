using UnityEngine;

namespace MouseKeeper
{
    public sealed class MoneyVial : MonoBehaviour
    {
        [Min(1)] public int amount = 1;
    }
}
