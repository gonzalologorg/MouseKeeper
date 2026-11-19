using UnityEngine;

namespace MouseKeeper
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MouseEnemy : MonoBehaviour
    {
        public float movementSpeed = 2.4f;
        public float attackInterval = 1f;
        public float attackDamage = 8f;
        public float sightRange = 7f;
        public float maxHealth = 40f;
        float health;
        float attackCooldown;
        float decisionTimer;
        Vector3 wanderTarget;
        Vector3 hideTarget;
        bool fleeing;
        bool hidden;
        bool defeated;
        PlayerController player;
        GameRun run;
        CharacterController controller;

        public void Initialize(GameRun gameRun, PlayerController target, Vector3 hideSpot, int level)
        {
            run = gameRun;
            player = target;
            controller = GetComponent<CharacterController>();
            maxHealth *= 1f + 0.3f * Mathf.Max(0, level - 1);
            attackDamage *= 1f + 0.15f * Mathf.Max(0, level - 1);
            health = maxHealth;
            hideTarget = hideSpot;
            PickWanderTarget();
        }

        void Update()
        {
            if (defeated || player == null || player.Dead || run.Finished) return;
            float dt = Time.deltaTime;
            attackCooldown -= dt;
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (hidden)
            {
                if (distance > 2.5f) return;
                hidden = false;
                fleeing = false;
            }

            Vector3 target;
            if (fleeing)
            {
                target = hideTarget;
                if (Vector3.Distance(transform.position, hideTarget) < 0.8f)
                {
                    hidden = true;
                    return;
                }
            }
            else if (distance < sightRange && HasSight(toPlayer, distance))
            {
                target = player.transform.position;
                if (distance < 1.3f && attackCooldown <= 0f)
                {
                    player.TakeDamage(attackDamage);
                    attackCooldown = attackInterval;
                }
            }
            else
            {
                decisionTimer -= dt;
                if (decisionTimer <= 0f || Vector3.Distance(transform.position, wanderTarget) < 0.7f)
                    PickWanderTarget();
                target = wanderTarget;
            }
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.1f)
            {
                direction.Normalize();
                transform.rotation = Quaternion.LookRotation(direction);
                controller.Move(direction * movementSpeed * dt + Vector3.down * 2f * dt);
            }
        }

        bool HasSight(Vector3 direction, float distance)
        {
            if (distance < 0.01f) return true;
            if (Physics.Raycast(transform.position + Vector3.up * 0.4f, direction / distance,
                    out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
                return hit.collider.GetComponentInParent<PlayerController>() != null;
            return true;
        }

        void PickWanderTarget()
        {
            wanderTarget = run.RandomPointInRoom();
            decisionTimer = Random.Range(1.5f, 4f);
        }

        public void TakeDamage(float amount, PlayerController attacker)
        {
            if (defeated || amount <= 0f) return;
            health -= amount;
            if (health <= 0f)
            {
                defeated = true;
                attacker.NotifyMouseKill(this);
                run.MouseDefeated(this);
                Destroy(gameObject);
                return;
            }
            fleeing = true;
            hidden = false;
        }
    }
}
