using UnityEngine;

namespace MouseKeeper
{
    public sealed class CheeseDecoy : MonoBehaviour
    {
        float health = 50f;
        public bool Available => health > 0f;
        public void TakeDamage(float amount)
        {
            if (!Available || amount <= 0f) return;
            health -= Mathf.Ceil(amount);
            if (health <= 0f) Destroy(gameObject);
        }
    }
}
