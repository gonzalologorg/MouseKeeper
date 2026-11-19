using System.Collections;
using UnityEngine;

namespace MouseKeeper
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MouseEnemy : MonoBehaviour
    {
        const float RecoveryDuration = 3f;
        const float RecoveryFraction = 0.5f;
        static readonly int SpeedParameter = Animator.StringToHash("Speed");
        static readonly int FleeingParameter = Animator.StringToHash("Fleeing");
        static readonly int AttackParameter = Animator.StringToHash("Attack");
        static readonly int DeathParameter = Animator.StringToHash("Death");
        static readonly int DeathState = Animator.StringToHash("Base Layer.die");

        public float movementSpeed = 2.4f;
        public float attackInterval = 1f;
        public float attackDamage = 8f;
        public float sightRange = 7f;
        public float maxHealth = 40f;
        float health;
        float attackCooldown;
        float decisionTimer;
        float recoveryRemaining;
        Vector3 wanderTarget;
        Vector3 hideTarget;
        bool fleeing;
        bool hidden;
        bool recovered;
        bool defeated;
        PlayerController player;
        GameRun run;
        CharacterController controller;
        CharacterController targetController;
        Animator animator;
        HitFlash hitFlash;

        public void Initialize(GameRun gameRun, PlayerController target, Vector3 hideSpot, int level)
        {
            run = gameRun;
            player = target;
            controller = GetComponent<CharacterController>();
            targetController = target.GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();
            hitFlash = GetComponent<HitFlash>();
            if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();
            maxHealth *= 1f + 0.3f * Mathf.Max(0, level - 1);
            attackDamage *= 1f + 0.15f * Mathf.Max(0, level - 1);
            health = maxHealth;
            hideTarget = hideSpot;
            PickWanderTarget();
        }

        void Update()
        {
            if (defeated) return;
            if (player == null || player.Dead || run.Finished)
            {
                UpdateAnimation(0f, false);
                return;
            }
            float dt = Time.deltaTime;
            attackCooldown -= dt;
            if (fleeing)
            {
                float recoveryTime = Mathf.Min(dt, recoveryRemaining);
                health = Mathf.Min(maxHealth, health + maxHealth * RecoveryFraction * recoveryTime / RecoveryDuration);
                recoveryRemaining -= recoveryTime;
                if (recoveryRemaining <= 0f)
                {
                    fleeing = false;
                    hidden = false;
                    recovered = true;
                }
            }
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (hidden)
            {
                UpdateAnimation(0f, true);
                return;
            }

            Vector3 target;
            if (fleeing)
            {
                target = hideTarget;
                if (Vector3.Distance(transform.position, hideTarget) < 0.8f)
                {
                    hidden = true;
                    UpdateAnimation(0f, true);
                    return;
                }
            }
            else if (recovered || (distance < sightRange && HasSight(toPlayer, distance)))
            {
                target = player.transform.position;
                if (distance < 1.3f && attackCooldown <= 0f)
                {
                    Bounds mouseBounds = controller.bounds;
                    Bounds playerBounds = targetController.bounds;
                    if (mouseBounds.min.y <= playerBounds.max.y && mouseBounds.max.y >= playerBounds.min.y)
                    {
                        if (animator != null) animator.SetTrigger(AttackParameter);
                        player.TakeDamage(attackDamage);
                        attackCooldown = attackInterval;
                    }
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
            Vector3 previousPosition = transform.position;
            if (direction.sqrMagnitude > 0.1f)
            {
                direction.Normalize();
                transform.rotation = Quaternion.LookRotation(direction);
                controller.Move(direction * movementSpeed * dt + Vector3.down * 2f * dt);
            }
            Vector3 displacement = transform.position - previousPosition;
            displacement.y = 0f;
            UpdateAnimation(dt > 0f ? displacement.magnitude / dt : 0f, fleeing);
        }

        void UpdateAnimation(float speed, bool isFleeing)
        {
            if (animator == null) return;
            if (speed < 0.1f) speed = 0f;
            animator.SetFloat(SpeedParameter, speed);
            animator.SetBool(FleeingParameter, isFleeing && speed == 0f);
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

        public void TakeDamage(float amount, PlayerController attacker, WeaponInstance weapon = null)
        {
            if (defeated || amount <= 0f) return;
            health -= amount;
            hitFlash.Flash();
            Vector3 indicatorPosition = transform.position;
            indicatorPosition.y = controller.bounds.max.y + 0.2f;
            run.ShowDamage(indicatorPosition, amount, false);
            bool killed = health <= 0f;
            if (killed) defeated = true;
            if (weapon != null) weapon.Definition.OnDamage(attacker, weapon, this, amount);
            if (killed)
            {
                attacker.NotifyMouseKill(this);
                run.MouseDefeated(this);
                controller.enabled = false;
                if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null &&
                    animator.speed > 0f && animator.HasState(0, DeathState))
                {
                    UpdateAnimation(0f, false);
                    animator.ResetTrigger(AttackParameter);
                    animator.SetTrigger(DeathParameter);
                    StartCoroutine(WaitForDeathAnimation());
                }
                else
                {
                    if (animator != null)
                        Debug.LogWarning("Mouse animator cannot play the Base Layer.die state.", this);
                    Destroy(gameObject);
                }
                return;
            }

            if (!defeated && !fleeing && !recovered)
            {
                fleeing = true;
                recoveryRemaining = RecoveryDuration;
            }
        }

        IEnumerator WaitForDeathAnimation()
        {
            float waitForState = 0f;
            while (animator != null && animator.isActiveAndEnabled && animator.speed > 0f)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash == DeathState)
                {
                    if (state.normalizedTime >= 1f && !animator.IsInTransition(0)) break;
                }
                else
                {
                    waitForState += Time.deltaTime;
                    if (waitForState >= 5f)
                    {
                        Debug.LogWarning("Mouse animator did not enter the death state.", this);
                        break;
                    }
                }
                yield return null;
            }
            if (animator == null || !animator.isActiveAndEnabled || animator.speed <= 0f)
                Debug.LogWarning("Mouse death animation was interrupted.", this);
            Destroy(gameObject);
        }

        void OnDisable()
        {
            if (defeated) Destroy(gameObject);
        }

        public void Push(Vector3 direction, float distance)
        {
            if (defeated || controller == null || distance <= 0f) return;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0f) controller.Move(direction.normalized * distance);
        }
    }
}
