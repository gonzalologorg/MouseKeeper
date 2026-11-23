using UnityEngine;

namespace MouseKeeper
{
    public sealed class WeaponParticle : MonoBehaviour
    {
        PlayerController player;
        WeaponInstance weapon;
        Vector3 direction;
        float damage;
        float speed;
        float remaining;

        public void Initialize(PlayerController owner, WeaponInstance source, float attackDamage,
            Vector3 travelDirection, float particleSpeed, float duration)
        {
            player = owner;
            weapon = source;
            damage = attackDamage;
            direction = travelDirection;
            speed = particleSpeed;
            remaining = duration;
        }

        void Update()
        {
            if (player == null || player.CurrentRoom != transform.parent)
            {
                Destroy(gameObject);
                return;
            }
            float delta = Mathf.Min(Time.deltaTime, remaining);
            float distance = speed * delta;
            if (distance > 0f && Physics.SphereCast(transform.position, 0.1f, direction, out RaycastHit hit,
                distance, ~0, QueryTriggerInteraction.Ignore))
            {
                MouseEnemy mouse = hit.collider.GetComponentInParent<MouseEnemy>();
                if (mouse != null) mouse.TakeDamage(damage, player, weapon, impact: hit.point, sourcePosition: hit.point - direction);
                Destroy(gameObject);
                return;
            }
            transform.position += direction * distance;
            remaining -= delta;
            if (remaining <= 0f) Destroy(gameObject);
        }

        void OnDisable()
        {
            Destroy(gameObject);
        }
    }
}
