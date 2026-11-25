using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace MouseKeeper
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MouseEnemy : MonoBehaviour
    {
        public enum MouseVariant { Normal, Jumping, Tanky, Speedy, Explosive, Poison, Mushroom }

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
        public float explosionDamage = 15f;
        public float explosionRadius = 3f;
        public float poisonDamagePerSecond = 10f;
        public float poisonDuration = 5f;
        public float poisonRadius = 3f;
        public EnemyPreset Preset { get; set; }
        public int SplitGeneration { get; private set; }
        float health;
        float attackCooldown;
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
        NavigationMotor navigation;
        BoxCollider selectionCollider;
        CharacterController targetController;
        Animator animator;
        HitFlash hitFlash;
        float slowRemaining;
        float slowFraction;
        float bleedUntil;
        float explosiveWarningUntil;
        float freezeRemaining;
        float initialTankAttention = 3f;
        bool targetsTankInitially;
        float CurrentMovementSpeed => movementSpeed * (slowRemaining > 0f ? 1f - slowFraction : 1f);
        public float SlowFraction => slowRemaining > 0f ? slowFraction : 0f;
        public bool IsBleeding => Time.time < bleedUntil;
        public bool ExplosiveThreat => variant == MouseVariant.Explosive && !defeated && Time.time < explosiveWarningUntil;
        public bool IsTargetable => !defeated;
        public bool IsDefeated => defeated;
        public float Health => Mathf.Max(0f, health);
        public MouseVariant Variant => variant;
        public Vector3 AimPoint => controller != null ? controller.bounds.center : transform.position;

        public void SetBehavior(MouseVariant value) => variant = value;

        public static bool IsStandingOnAgent(Bounds mouse, Bounds agent)
        {
            float verticalGap = mouse.min.y - agent.max.y;
            if (verticalGap < -0.2f || verticalGap > 0.15f) return false;
            float dx = mouse.center.x - agent.center.x;
            float dz = mouse.center.z - agent.center.z;
            float mouseRadius = Mathf.Min(mouse.extents.x, mouse.extents.z);
            float agentRadius = Mathf.Min(agent.extents.x, agent.extents.z);
            float contactRadius = mouseRadius + agentRadius;
            return dx * dx + dz * dz < contactRadius * contactRadius;
        }

        public void InitializeOffspring(MouseEnemy parent)
        {
            SplitGeneration = parent.SplitGeneration + 1;
            maxHealth = parent.maxHealth * 0.65f;
            health = maxHealth;
            movementSpeed = parent.movementSpeed * (1f + 0.2f * SplitGeneration) /
                (1f + 0.2f * parent.SplitGeneration);
            // The model shrinks, but the collision and targeting volumes keep their world size.
            CharacterController childController = GetComponent<CharacterController>();
            CharacterController parentController = parent.GetComponent<CharacterController>();
            childController.center = parentController.center / 0.67f;
            childController.height = parentController.height / 0.67f;
            childController.radius = parentController.radius / 0.67f;
            if (selectionCollider != null && parent.selectionCollider != null)
            {
                selectionCollider.center = parent.selectionCollider.center / 0.67f;
                selectionCollider.size = parent.selectionCollider.size / 0.67f;
            }
        }

        public bool RecentlyAttacked(PlayerController agent)
        {
            return IsTargetable && agent != null && !agent.Dead &&
                lastAttackedAgent == agent && Time.time - lastAttackAt <= 2f &&
                (transform.position - agent.transform.position).sqrMagnitude <= 9f;
        }

        public void Initialize(GameRun gameRun, PlayerController target, Vector3 hideSpot, int level)
        {
            run = gameRun;
            targetsTankInitially = Random.value < 0.5f;
            player = target;
            controller = GetComponent<CharacterController>();
            navigation = gameObject.AddComponent<NavigationMotor>();
            targetController = target.GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();
            hitFlash = GetComponent<HitFlash>();
            if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();
            AddSelectionCollider();
            float depth = Mathf.Clamp(level - 1, 0, 4);
            maxHealth *= 0.4f * 1.5f * (1f + 0.08f * depth);
            attackDamage *= 0.4f * (1f + 0.05f * depth);
            health = maxHealth;
            jumpCooldown = 0f;
            if (variant == MouseVariant.Explosive && explosionRadius > 0f)
                HazardRadiusIndicator.Create(transform, transform, explosionRadius, 0.04f, this);
            else if (variant == MouseVariant.Poison && poisonRadius > 0f)
                HazardRadiusIndicator.Create(transform, transform, poisonRadius, 0.04f, this);
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
            selectionCollider = selection;
            selection.isTrigger = true;
            selection.center = localBounds.center;
            selection.size = localBounds.size;
        }

        void Update()
        {
            if (navigation != null) navigation.Pause();
            if (defeated) return;
            PlayerController supportingAgent = run.AgentSupportingMouse(controller);
            if (supportingAgent != null)
            {
                TakeDamage(health, supportingAgent);
                return;
            }
            // Jumping mice follow their own arc; all other states must keep
            // falling even when navigation has no path or the mouse is idle.
            if (jumpElapsed < 0f) controller.Move(Vector3.down * 2f * Time.deltaTime);
            if (run.Finished || run.IsTransitioning)
            {
                UpdateAnimation(0f, false);
                return;
            }
            slowRemaining = Mathf.Max(0f, slowRemaining - Time.deltaTime);
            initialTankAttention = Mathf.Max(0f, initialTankAttention - Time.deltaTime);
            if (freezeRemaining > 0f)
            {
                freezeRemaining = Mathf.Max(0f, freezeRemaining - Time.deltaTime);
                if (freezeRemaining <= 0f) hitFlash.SetFrozen(false);
                UpdateAnimation(0f, false);
                return;
            }
            if (retreatRemaining > 0f)
            {
                float step = Mathf.Min(Time.deltaTime, retreatRemaining);
                retreatRemaining -= step;
                controller.Move(retreatDirection * CurrentMovementSpeed * step);
                UpdateAnimation(CurrentMovementSpeed, false);
                return;
            }
            if (variant == MouseVariant.Jumping)
            {
                UpdateJump(Time.deltaTime * (slowRemaining > 0f ? 1f - slowFraction : 1f));
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
                    if (navigation.DirectionTo(cheese.transform.position, CurrentMovementSpeed, out Vector3 cheeseDirection))
                    {
                        transform.rotation = Quaternion.LookRotation(cheeseDirection);
                        controller.Move(cheeseDirection * CurrentMovementSpeed * Time.deltaTime);
                    }
                    UpdateAnimation(CurrentMovementSpeed, false);
                }
                return;
            }
            player = SelectAgentTarget();
            if (player != null) targetController = player.GetComponent<CharacterController>();
            if (player == null || player.Dead)
            {
                UpdateAnimation(0f, false);
                return;
            }
            float dt = Time.deltaTime;
            attackCooldown -= dt;
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            Vector3 target = player.transform.position;
            {
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
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            Vector3 previousPosition = transform.position;
            if (direction.sqrMagnitude > 0.1f)
            {
                if (navigation.DirectionTo(target, CurrentMovementSpeed, out direction))
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                    controller.Move(direction * CurrentMovementSpeed * dt);
                }
            }
            Vector3 displacement = transform.position - previousPosition;
            displacement.y = 0f;
            UpdateAnimation(dt > 0f ? displacement.magnitude / dt : 0f, false);
        }

        void UpdateJump(float dt)
        {
            jumpCooldown -= dt;
            if (jumpElapsed < 0f)
            {
                UpdateAnimation(0f, false);
                if (jumpCooldown > 0f)
                {
                    return;
                }
                jumpCheese = NearestCheese();
                jumpTarget = jumpCheese == null ? SelectAgentTarget(previousJumpTarget) : null;
                if (jumpCheese == null && jumpTarget == null) jumpTarget = run.NearestAgent(transform.position);
                if (jumpCheese == null && jumpTarget == null) return;
                if (jumpTarget != null) previousJumpTarget = jumpTarget;
                jumpStart = transform.position;
                Vector3 direction = (jumpCheese != null ? jumpCheese.transform.position : jumpTarget.transform.position) - jumpStart;
                direction.y = 0f;
                float distance = Mathf.Min(direction.magnitude, JumpDistance);
                Vector3 targetPosition = jumpCheese != null ? jumpCheese.transform.position : jumpTarget.transform.position;
                if (!navigation.DirectionTo(targetPosition, movementSpeed, out direction)) return;
                navigation.Pause();
                jumpEnd = jumpStart + direction * distance;
                if (NavMesh.SamplePosition(jumpStart, out NavMeshHit jumpOrigin, 1.5f, NavMesh.AllAreas) &&
                    NavMesh.Raycast(jumpOrigin.position, jumpEnd, out NavMeshHit edge, NavMesh.AllAreas))
                    jumpEnd = edge.position - direction * 0.1f;
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

        PlayerController SelectAgentTarget(PlayerController exclude = null)
        {
            PlayerController tank = run.NearestTank(transform.position);
            if (targetsTankInitially && tank != null && initialTankAttention > 0f) return tank;
            PlayerController nearest = run.NearestAgent(transform.position, exclude) ?? run.NearestAgent(transform.position);
            if (!targetsTankInitially) return nearest;
            if (tank == null || nearest == null || tank == nearest) return nearest ?? tank;
            float tankDistance = Vector3.Distance(tank.transform.position, transform.position);
            float nearestDistance = Vector3.Distance(nearest.transform.position, transform.position);
            return tankDistance <= nearestDistance + 2f ? tank : nearest;
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

        public void TakeDamage(float amount, PlayerController attacker, WeaponInstance weapon = null, bool scaleWeaponDamage = true, Vector3? impact = null, Vector3? sourcePosition = null, bool roundDamage = true, string damageSource = "passive_or_status", WeaponInstance attributionWeapon = null)
        {
            if (defeated || amount <= 0f) return;
            if (variant == MouseVariant.Explosive) explosiveWarningUntil = Time.time + 2.5f;
            bool critical = weapon != null && attacker != null && Random.value < PlayerController.CriticalChance(attacker.Profile.luck);
            amount *= (weapon != null && attacker != null ? attacker.WeaponDamageMultiplier : 1f) *
                (weapon != null && scaleWeaponDamage && weapon.Definition.ApplyDamageScaling ? 0.7f : 1f) * (critical ? 1.5f : 1f);
            if (roundDamage) amount = Mathf.Ceil(amount);
            float previousHealth = Mathf.Max(0f, health);
            health -= amount;
            run.QueueBossDamageSpawns(this, health);
            run.RecordBalanceEnemyDamage(attacker, this, weapon, attributionWeapon, Mathf.Min(amount, previousHealth), Mathf.Max(0f, amount - previousHealth), critical, damageSource);
            Bounds bounds = controller.bounds;
            Vector3 source = sourcePosition ?? (attacker != null ? attacker.transform.position : bounds.center - transform.forward);
            run.ShowBlood(impact ?? GameRun.BloodImpact(bounds, source), bounds.center - source);
            if (critical && health > 0f) attacker.NotifyCriticalHit(this);
            hitFlash.Flash();
            Vector3 indicatorPosition = transform.position;
            indicatorPosition.y = controller.bounds.max.y + 0.2f;
            run.ShowDamage(indicatorPosition, amount, false, critical);
            bool killed = health <= 0f;
            if (killed) defeated = true;
            if (weapon != null) weapon.Definition.OnDamage(attacker, weapon, this, amount);
            if (killed)
            {
                attacker.NotifyMouseKill(this, weapon ?? attributionWeapon);
                if (variant == MouseVariant.Explosive)
                    run.ExplodeMouse(this, explosionRadius, explosionDamage);
                else if (variant == MouseVariant.Poison)
                    run.SpawnPoisonCloud(transform.position, poisonRadius, poisonDamagePerSecond, poisonDuration);
                else if (variant == MouseVariant.Mushroom && SplitGeneration < 3)
                    run.SplitMouse(this);
                run.MouseDefeated(this);
                navigation.ResetNavigation();
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
            run?.RecordBalanceStatus(this, "push", distance);
            direction.y = 0f;
            if (direction.sqrMagnitude > 0f) controller.Move(direction.normalized * distance);
        }

        public void ApplySlow(float duration)
        {
            if (slowRemaining <= 0f) slowFraction = 0f;
            slowFraction = Mathf.Min(0.8f, slowFraction + 0.2f);
            slowRemaining = Mathf.Max(slowRemaining, duration);
            run?.RecordBalanceStatus(this, "slow", duration);
        }

        public void ApplyFreeze(float duration)
        {
            if (defeated || duration <= 0f) return;
            freezeRemaining = Mathf.Max(freezeRemaining, duration);
            run?.RecordBalanceStatus(this, "freeze", duration);
            hitFlash.SetFrozen(true);
        }

        public void ApplyDamageOverTime(PlayerController source, float totalDamage, int seconds, string damageSource = "damage_over_time", WeaponInstance attributionWeapon = null)
        {
            if (!defeated && source != null && totalDamage > 0f && seconds > 0)
            {
                run?.RecordBalanceStatus(this, damageSource, seconds);
                StartCoroutine(DamageOverTime(source, totalDamage, seconds, damageSource, attributionWeapon));
            }
        }

        IEnumerator DamageOverTime(PlayerController source, float totalDamage, int seconds, string damageSource, WeaponInstance attributionWeapon)
        {
            for (int i = 0; i < seconds && !defeated; i++)
            {
                yield return new WaitForSeconds(1f);
                if (defeated || source == null || run.Finished || run.CurrentRoom != transform.parent) yield break;
                TakeDamage(totalDamage / seconds, source, roundDamage: false, damageSource: damageSource, attributionWeapon: attributionWeapon);
            }
        }

        public void ApplyBleed(PlayerController source, WeaponInstance weapon = null)
        {
            if (IsBleeding || defeated || source == null) return;
            bleedUntil = Time.time + 3f;
            ApplyDamageOverTime(source, maxHealth * 0.3f, 3, "bleed", weapon);
        }
    }
}
