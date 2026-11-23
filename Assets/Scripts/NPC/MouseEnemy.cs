using System.Collections;
using UnityEngine;

namespace MouseKeeper
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MouseEnemy : MonoBehaviour
    {
        public enum MouseVariant { Normal, Jumping, Tanky, Speedy }

        const float RecoveryDuration = 3f;
        const float RecoveryFraction = 0.5f;
        const float JumpInterval = 1f;
        const float JumpDuration = 0.65f;
        const float JumpDistance = 6f;
        const float JumpHeight = 1.2f;
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
        MouseVariant variant;
        float jumpCooldown;
        float jumpElapsed = -1f;
        Vector3 jumpStart;
        Vector3 jumpEnd;
        PlayerController jumpTarget;
        PlayerController previousJumpTarget;
        PlayerController player;
        PlayerController lastAttackedAgent;
        float lastAttackAt = float.NegativeInfinity;
        float retreatRemaining;
        Vector3 retreatDirection;
        CheeseDecoy jumpCheese;
        GameRun run;
        CharacterController controller;
        CharacterController targetController;
        Animator animator;
        HitFlash hitFlash;
        public bool IsTargetable => !defeated && !hidden;
        public bool IsDefeated => defeated;
        public MouseVariant Variant => variant;

        public void ConfigureVariant(MouseVariant value)
        {
            variant = value;
            if (value == MouseVariant.Jumping)
            {
                maxHealth *= 1.5f;
                attackDamage *= 1.5f;
            }
            else if (value == MouseVariant.Tanky)
            {
                maxHealth *= 2f;
                movementSpeed *= 0.5f;
                attackDamage *= 0.8f;
            }
            else if (value == MouseVariant.Speedy)
            {
                maxHealth *= 0.5f;
                movementSpeed *= 2f;
            }
        }

        public bool RecentlyAttacked(PlayerController agent)
        {
            return IsTargetable && !fleeing && agent != null && !agent.Dead &&
                lastAttackedAgent == agent && Time.time - lastAttackAt <= 2f &&
                (transform.position - agent.transform.position).sqrMagnitude <= 9f;
        }

        public void Initialize(GameRun gameRun, PlayerController target, Vector3 hideSpot, int level)
        {
            run = gameRun;
            player = target;
            controller = GetComponent<CharacterController>();
            targetController = target.GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();
            hitFlash = GetComponent<HitFlash>();
            if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();
            AddSelectionCollider();
            float depth = Mathf.Clamp(level - 1, 0, 4);
            maxHealth *= 1.5f * (1f + 0.08f * depth);
            attackDamage *= 1f + 0.05f * depth;
            health = maxHealth;
            hideTarget = hideSpot;
            jumpCooldown = JumpInterval;
            PickWanderTarget();
        }

        void AddSelectionCollider()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds localBounds = new Bounds();
            bool initialized = false;
            foreach (Renderer part in renderers)
            {
                Bounds bounds = part.bounds;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 point = transform.InverseTransformPoint(bounds.center +
                                Vector3.Scale(bounds.extents, new Vector3(x, y, z)));
                            if (!initialized) { localBounds = new Bounds(point, Vector3.zero); initialized = true; }
                            else localBounds.Encapsulate(point);
                        }
            }
            BoxCollider selection = gameObject.AddComponent<BoxCollider>();
            selection.isTrigger = true;
            selection.center = localBounds.center;
            selection.size = localBounds.size;
        }

        void Update()
        {
            if (defeated) return;
            if (run.Finished || run.IsTransitioning)
            {
                UpdateAnimation(0f, false);
                return;
            }
            if (retreatRemaining > 0f)
            {
                float step = Mathf.Min(Time.deltaTime, retreatRemaining);
                retreatRemaining -= step;
                controller.Move(retreatDirection * movementSpeed * step + Vector3.down * 2f * step);
                UpdateAnimation(movementSpeed, false);
                return;
            }
            if (variant == MouseVariant.Jumping)
            {
                UpdateJump(Time.deltaTime);
                return;
            }
            CheeseDecoy cheese = NearestCheese();
            if (cheese != null)
            {
                attackCooldown -= Time.deltaTime;
                Vector3 toCheese = cheese.transform.position - transform.position;
                toCheese.y = 0f;
                if (toCheese.magnitude < 1.3f)
                {
                    if (attackCooldown <= 0f)
                    {
                        cheese.TakeDamage(attackDamage);
                        attackCooldown = attackInterval;
                        if (animator != null) animator.SetTrigger(AttackParameter);
                    }
                    UpdateAnimation(0f, false);
                }
                else
                {
                    transform.rotation = Quaternion.LookRotation(toCheese);
                    controller.Move(toCheese.normalized * movementSpeed * Time.deltaTime + Vector3.down * 2f * Time.deltaTime);
                    UpdateAnimation(movementSpeed, false);
                }
                return;
            }
            player = run.NearestAgent(transform.position);
            if (player != null) targetController = player.GetComponent<CharacterController>();
            if (player == null || player.Dead)
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
                        lastAttackedAgent = player;
                        lastAttackAt = Time.time;
                        player.TakeDamage(attackDamage, this);
                        if (defeated) return;
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

        void UpdateJump(float dt)
        {
            jumpCooldown -= dt;
            if (jumpElapsed < 0f)
            {
                UpdateAnimation(0f, false);
                if (jumpCooldown > 0f)
                {
                    controller.Move(Vector3.down * 2f * dt);
                    return;
                }
                jumpCheese = NearestCheese();
                jumpTarget = jumpCheese == null ? run.NearestAgent(transform.position, previousJumpTarget) : null;
                if (jumpCheese == null && jumpTarget == null) jumpTarget = run.NearestAgent(transform.position);
                if (jumpCheese == null && jumpTarget == null) return;
                if (jumpTarget != null) previousJumpTarget = jumpTarget;
                jumpStart = transform.position;
                Vector3 direction = (jumpCheese != null ? jumpCheese.transform.position : jumpTarget.transform.position) - jumpStart;
                direction.y = 0f;
                jumpEnd = jumpStart + Vector3.ClampMagnitude(direction, JumpDistance);
                if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
                jumpElapsed = 0f;
                jumpCooldown = JumpInterval;
            }

            jumpElapsed = Mathf.Min(JumpDuration, jumpElapsed + dt);
            float progress = jumpElapsed / JumpDuration;
            Vector3 position = Vector3.Lerp(jumpStart, jumpEnd, progress);
            position.y += 4f * JumpHeight * progress * (1f - progress);
            controller.Move(position - transform.position);
            if (jumpElapsed < JumpDuration) return;

            jumpElapsed = -1f;
            if (jumpCheese != null)
            {
                Vector3 cheeseOffset = jumpCheese.transform.position - transform.position;
                cheeseOffset.y = 0f;
                if (cheeseOffset.sqrMagnitude < 1.3f * 1.3f) jumpCheese.TakeDamage(attackDamage);
                jumpCheese = null;
                return;
            }
            if (jumpTarget == null || jumpTarget.Dead) return;
            Vector3 offset = jumpTarget.transform.position - transform.position;
            offset.y = 0f;
            CharacterController target = jumpTarget.GetComponent<CharacterController>();
            if (offset.sqrMagnitude >= 1.3f * 1.3f || target == null) return;
            float contactTolerance = controller.skinWidth + target.skinWidth + 0.05f;
            if (controller.bounds.min.y > target.bounds.max.y + contactTolerance ||
                controller.bounds.max.y < target.bounds.min.y - contactTolerance) return;
            if (animator != null) animator.SetTrigger(AttackParameter);
            lastAttackedAgent = jumpTarget;
            lastAttackAt = Time.time;
            jumpTarget.TakeDamage(attackDamage, this);
        }

        CheeseDecoy NearestCheese()
        {
            if (run.CurrentRoom == null) return null;
            CheeseDecoy nearest = null;
            float best = float.PositiveInfinity;
            foreach (CheeseDecoy cheese in run.CurrentRoom.GetComponentsInChildren<CheeseDecoy>())
            {
                if (!cheese.Available) continue;
                float distance = (cheese.transform.position - transform.position).sqrMagnitude;
                if (distance < best) { best = distance; nearest = cheese; }
            }
            return nearest;
        }

        public void RetreatFrom(Vector3 source, float duration)
        {
            if (defeated || duration <= 0f) return;
            retreatDirection = transform.position - source;
            retreatDirection.y = 0f;
            if (retreatDirection.sqrMagnitude < 0.001f) retreatDirection = -transform.forward;
            retreatDirection.Normalize();
            retreatRemaining = duration;
            jumpElapsed = -1f;
            jumpTarget = null;
            jumpCheese = null;
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

        public void TakeDamage(float amount, PlayerController attacker, WeaponInstance weapon = null, bool scaleWeaponDamage = true, Vector3? impact = null, Vector3? sourcePosition = null)
        {
            if (defeated || amount <= 0f) return;
            amount = Mathf.Ceil(amount * (weapon != null && scaleWeaponDamage ? 0.7f : 1f));
            health -= amount;
            Bounds bounds = controller.bounds;
            Vector3 source = sourcePosition ?? (attacker != null ? attacker.transform.position : bounds.center - transform.forward);
            run.ShowBlood(impact ?? GameRun.BloodImpact(bounds, source), bounds.center - source);
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

            if (variant == MouseVariant.Normal && !defeated && !fleeing && !recovered)
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
