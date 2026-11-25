using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MouseKeeper
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public float baseEnergyRecharge = 1f;
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public float MovementSpeed { get; private set; }
        public float EnergyRecharge { get; private set; }
        public int Money { get; private set; }
        public int Karma { get; private set; }
        public Specialty Specialty { get; private set; }
        bool bardBuff;
        public float WeaponDamageMultiplier => Specialty == Specialty.Dps ? 1.2f : 1f;
        internal bool EscapeInvulnerable { get; set; }
        public int SelectedWeapon { get; private set; }
        public IReadOnlyList<PassiveItemDefinition> Passives => passives;
        public WeaponInstance[] Weapons { get; } = new WeaponInstance[1];
        public bool Dead { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public float TargetDistance { get; private set; }

        readonly List<PassiveItemDefinition> passives = new List<PassiveItemDefinition>();
        readonly HashSet<PlayerController> reprimanded = new HashSet<PlayerController>();
        readonly HashSet<WeaponInstance> equippedWeapons = new HashSet<WeaponInstance>();
        CharacterController controller;
        NavigationMotor navigation;
        WeaponVisual weaponVisual;
        GameRun run;
        AgentProfile profile;
        Vector3 destination;
        readonly Queue<Vector3> corridorWaypoints = new Queue<Vector3>();
        bool hasDestination;
        Vector3 doorProgressAnchor, doorProgressGoal;
        float doorProgressSince;
        bool monitoringDoorProgress;
        bool overcomingFear;
        bool fearTriggered;
        float fearRemaining;
        float stamina = 1f;
        float pushCooldown;
        float rescueCooldown;
        float pushRemaining;
        Vector3 pushVelocity;
        AgentDialogue dialogue;
        float emotionCheck;
        float emergencySpeedMultiplier = 1f;
        float medicRegenerationTimer;
        public const float TankHealthMultiplier = 3.5f;
        public AgentProfile Profile => profile;
        public AgentDialogue Dialogue => dialogue;
        public bool ReachedDestination => !hasDestination;
        internal Vector3 MoveDestination => destination;
        public bool BeingPushed => pushRemaining > 0f;
        internal bool CanAttemptRescue => !hasDestination && !BeingPushed && rescueCooldown <= 0f &&
            run.OrderedWeaponFor(this) == null;
        float FleeThreshold => Mathf.Lerp(0.65f, 0.2f, Mathf.InverseLerp(1f, 10f, profile.bravery));

        public void Initialize(GameRun gameRun, AgentProfile settings)
        {
            run = gameRun;
            profile = settings;
            controller = GetComponent<CharacterController>();
            navigation = gameObject.AddComponent<NavigationMotor>();
            weaponVisual = gameObject.AddComponent<WeaponVisual>();
            MovementSpeed = settings.EffectiveSpeed;
            MaxHealth = settings.EffectiveHealth;
            EnergyRecharge = baseEnergyRecharge;
            Health = MaxHealth;
        }

        public void SetDialogue(AgentDialogue value) => dialogue = value;

        public void SetSpecialty(Specialty specialty)
        {
            Specialty = specialty;
            if (specialty == Specialty.Tank) { MaxHealth *= TankHealthMultiplier; Health *= TankHealthMultiplier; }
            RefreshWeaponSpeed();
        }

        internal void SetBardBuff(bool enabled)
        {
            if (bardBuff == enabled) return;
            bardBuff = enabled;
            RefreshWeaponSpeed();
        }

        void RefreshWeaponSpeed()
        {
            if (profile == null) return;
            foreach (WeaponInstance weapon in Weapons)
                if (weapon != null) weapon.Configure(profile.attackSpeed * (bardBuff ? 1.25f : 1f),
                    profile.agility, Specialty == Specialty.Dps ? 1.15f : 1f);
        }

        public static float AimErrorDegrees(int accuracy, float distance)
        {
            float inaccuracy = 1f - Mathf.InverseLerp(1f, 10f, accuracy);
            return inaccuracy * Mathf.Lerp(3f, 30f, Mathf.InverseLerp(2f, 12f, distance));
        }

        void Update()
        {
            if (navigation != null) navigation.Pause();
            if (Dead || run == null || run.Finished) return;
            float dt = Time.deltaTime;
            UpdateMedicRegeneration(dt);
            fearRemaining = Mathf.Max(0f, fearRemaining - dt);
            stamina = Mathf.Min(1f, stamina + dt * 0.04f);
            pushCooldown = Mathf.Max(0f, pushCooldown - dt);
            rescueCooldown = Mathf.Max(0f, rescueCooldown - dt);
            foreach (WeaponInstance weapon in Weapons)
                if (weapon != null) weapon.Tick(dt, EnergyRecharge);
            if (!run.IsTransitioning && run.EnemyCount > 0)
                foreach (PassiveItemDefinition passive in passives.ToArray()) passive.OnCombatTick(this, dt);
            if (run.IsTransitioning)
            {
                pushRemaining = 0f;
                MoveToDestination(dt);
                return;
            }

            if (dialogue != null)
            {
                dialogue.CheckInjury();
                emotionCheck -= dt;
                if (emotionCheck <= 0f)
                {
                    emotionCheck = 0.3f;
                    dialogue.SetFury(Health / MaxHealth <= 0.25f && run.IsSurrounded(this));
                }
            }
            MouseEnemy target = run.SelectTarget(this);
            float healthFraction = Health / MaxHealth;
            float fleeThreshold = FleeThreshold;
            RefreshFear(target, healthFraction);
            bool obeyingLastSurvivor = hasDestination && run.IsLastSurvivingAgent(this);
            if (target != null && fearRemaining > 0f && healthFraction <= 0.2f && overcomingFear && !obeyingLastSurvivor)
            {
                overcomingFear = false;
                hasDestination = false;
            }
            bool fleeing = target != null && healthFraction <= fleeThreshold && !overcomingFear && !obeyingLastSurvivor;
            bool panicking = fleeing && fearRemaining > 0f;
            PlayerController rescueAlly = null;
            MouseEnemy rescueThreat = null;
            bool rescuing = !panicking && CanAttemptRescue && run.TrySelectRescue(this, out rescueAlly, out rescueThreat);
            if (!run.HasFocusTarget && !fleeing && !hasDestination && !BeingPushed && run.OrderedWeaponFor(this) == null &&
                !rescuing && TryReprimandLootHoarder(dt)) return;
            WeaponInstance current = null;
            if (target != null)
            {
                Vector3 aim = target.AimPoint - (transform.position + Vector3.up * 0.65f);
                Vector3 horizontalAim = aim;
                horizontalAim.y = 0f;
                float distance = horizontalAim.magnitude;
                TargetDistance = distance;
                if (aim.sqrMagnitude > 0.0001f)
                {
                    float error = AimErrorDegrees(profile.accuracy, distance);
                    AimDirection = Quaternion.Euler(0f, Random.Range(-error, error), 0f) * aim.normalized;
                    if (distance > 0.01f) transform.rotation = Quaternion.LookRotation(horizontalAim);
                }
                current = ChooseWeapon(distance);
                if (current != null)
                {
                    current.Definition.Think(this, current, dt);
                    if (current.Energy < current.Definition.primaryCost && !current.IsReloading)
                        current.Definition.Reload(this, current);
                    if (!panicking && distance <= current.Definition.range)
                    {
                        if (current.Definition is PulseWeaponDefinition && run.EnemyCount >= 3 &&
                            current.Energy >= current.Definition.secondaryCost)
                            current.Definition.SecondaryAttack(this, current);
                        else
                            current.Definition.PrimaryAttack(this, current);
                    }
                }
            }
            if (MoveDuringPush(dt)) return;
            if (run.TryAvoidHazard(this, out Vector3 safePosition))
            {
                MoveTowards(safePosition, dt);
                return;
            }
            if (rescuing && TryRescueAlly(rescueAlly, rescueThreat, dt)) return;
            ItemPickup ordered = run.OrderedWeaponFor(this);
            if (!panicking && ordered != null)
            {
                if ((ordered.transform.position - transform.position).sqrMagnitude < 1.8f * 1.8f)
                {
                    if (run.WeaponOrderReady(this)) ordered.PickUp(this);
                }
                else MoveTowards(ordered.transform.position, dt);
                return;
            }
            if (!panicking && !fleeing && !hasDestination)
            {
                ItemPickup fallenWeapon = run.SelectFallenWeapon(this);
                if (fallenWeapon != null)
                {
                    if ((fallenWeapon.transform.position - transform.position).sqrMagnitude < 1.8f * 1.8f)
                        fallenWeapon.PickUp(this);
                    else MoveTowards(fallenWeapon.transform.position, dt);
                    return;
                }
            }
            if (target != null)
            {
                if (fleeing)
                {
                    if (!panicking && current != null && current.Definition.range > 0f)
                    {
                        MaintainRetreatDistance(target.transform.position, current.Definition.range, dt);
                        return;
                    }

                    Vector3 away = transform.position - target.transform.position;
                    away.y = 0f;
                    PlayerController shield = profile.distrust >= 5 ? run.NearestAlly(this) : null;
                    Vector3 flee = shield != null
                        ? shield.transform.position + (shield.transform.position - target.transform.position).normalized * 2f
                        : transform.position + away.normalized * 3f;
                    MoveTowards(run.ClampToCurrentRoom(flee), dt);
                    return;
                }
                if (!hasDestination)
                {
                    if (current != null && current.Definition is TazerWeaponDefinition)
                    {
                        Vector3 offset = transform.position - target.transform.position;
                        offset.y = 0f;
                        Vector3 toward = offset.sqrMagnitude > 0.01f ? -offset.normalized : transform.forward;
                        Vector3 direction = offset.magnitude > current.Definition.range * 0.7f ? toward :
                            Vector3.Cross(Vector3.up, toward).normalized;
                        MoveTowards(run.ClampToCurrentRoom(transform.position + direction * 2f), dt);
                        return;
                    }
                    if (run.TryProtectMedicPosition(this, out Vector3 protectionPosition))
                    {
                        Vector3 offset = protectionPosition - transform.position;
                        offset.y = 0f;
                        if (offset.sqrMagnitude > 0.8f * 0.8f) MoveTowards(protectionPosition, dt);
                        return;
                    }
                    if (Specialty == Specialty.Tank && run.TryTankPosition(this, target, out Vector3 tankPosition))
                    {
                        MoveTowards(tankPosition, dt);
                        return;
                    }
                    if (!run.HasFocusTarget && TryPushLootHoarder(target, dt)) return;
                    if (current != null)
                    {
                        if (run.Formation == GameRun.SquadFormation.KeepDistance)
                            MaintainFormationDistance(target.transform.position, current.Definition.range, dt);
                        else if (current.Definition.UsesMeleeDistance)
                            MaintainMeleeDistance(target.transform.position, current.Definition.range, dt);
                        else if (Vector3.Distance(transform.position, target.transform.position) > current.Definition.range)
                            MoveTowards(run.ClampToCurrentRoom(target.transform.position), dt);
                    }
                }
            }
            else if (!hasDestination)
            {
                ItemPickup pickup = run.SelectPickup(this);
                if (pickup != null)
                {
                    if ((pickup.transform.position - transform.position).sqrMagnitude < 1.8f * 1.8f)
                        pickup.PickUp(this);
                    else MoveTowards(pickup.transform.position, dt);
                }
            }
            if (hasDestination) MoveToDestination(dt);
        }

        internal void UpdateMedicRegeneration(float deltaTime)
        {
            if (Specialty != Specialty.Medic || Dead || deltaTime <= 0f) return;
            medicRegenerationTimer += deltaTime;
            int ticks = Mathf.FloorToInt(medicRegenerationTimer);
            if (ticks <= 0) return;
            medicRegenerationTimer -= ticks;
            float recovery = Mathf.Min(Mathf.Max(0f, MaxHealth * 0.5f - Health), MaxHealth * 0.01f * ticks);
            if (recovery > 0f) Heal(recovery, false, "medic_regeneration", this);
        }

        bool TryReprimandLootHoarder(float dt)
        {
            PlayerController ally = run.SelectReprimandTarget(this);
            WeaponInstance weapon = Weapons[SelectedWeapon];
            if (ally == null || weapon == null) return false;
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            Vector3 end = ally.transform.position + Vector3.up * 0.9f;
            Vector3 direction = end - origin;
            float distance = direction.magnitude;
            if (distance < 0.01f)
            {
                MoveTowards(run.ClampToCurrentRoom(transform.position - transform.forward), dt);
                return true;
            }
            if (distance > 8f || ReprimandBlocked(origin, direction, distance, ally))
            {
                MoveTowards(run.ClampToCurrentRoom(ally.transform.position), dt);
                return true;
            }
            if (weapon.Energy < weapon.Definition.primaryCost && !weapon.IsReloading)
                weapon.Definition.Reload(this, weapon);
            if (!weapon.TrySpend(weapon.Definition.primaryCost, weapon.Definition.attackInterval)) return false;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                AimDirection = direction.normalized;
                transform.rotation = Quaternion.LookRotation(direction);
            }
            reprimanded.Add(ally);
            run.StartCoroutine(ShowReprimandShot(origin, end));
            ally.TakeDamage(weapon.Definition.damage * 0.7f, impact: end, sourcePosition: origin);
            return true;
        }

        bool ReprimandBlocked(Vector3 origin, Vector3 direction, float distance, PlayerController ally)
        {
            foreach (RaycastHit hit in Physics.RaycastAll(origin, direction / distance, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                PlayerController agent = hit.collider.GetComponentInParent<PlayerController>();
                if (agent == this || agent == ally) continue;
                return true;
            }
            return false;
        }

        IEnumerator ShowReprimandShot(Vector3 origin, Vector3 end)
        {
            var shot = new GameObject("Reprimand Shot");
            shot.transform.SetParent(run.CurrentRoom, true);
            LineRenderer line = shot.AddComponent<LineRenderer>();
            line.sharedMaterial = PrimitiveMaterial.Shared;
            line.startWidth = line.endWidth = 0.08f;
            line.startColor = line.endColor = Color.yellow;
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, end);
            yield return new WaitForSeconds(0.15f);
            Destroy(shot);
        }

        internal bool HasReprimanded(PlayerController ally) => reprimanded.Contains(ally);
        internal void ForgetReprimand(PlayerController ally) => reprimanded.Remove(ally);

        bool TryPushLootHoarder(MouseEnemy target, float dt)
        {
            if (pushCooldown > 0f || !target.IsTargetable) return false;
            PlayerController ally = run.SelectLootHoarder(this);
            if (ally == null) return false;
            Vector3 direction = target.transform.position - ally.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) return false;
            direction.Normalize();
            Vector3 behind = run.ClampToCurrentRoom(ally.transform.position - direction);
            Vector3 offset = behind - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > 0.6f * 0.6f)
            {
                MoveTowards(behind, dt);
                return true;
            }

            float distrust = (Mathf.Clamp(profile.distrust, 1, 10) - 1) / 9f;
            ally.pushVelocity = direction * (Mathf.Lerp(0.8f, 2f, distrust) / 0.4f);
            ally.pushRemaining = 0.4f;
            pushCooldown = Mathf.Lerp(10f, 3f, distrust);
            return true;
        }

        bool TryRescueAlly(PlayerController ally, MouseEnemy threat, float dt)
        {
            if (ally == null || ally.Dead || ally.BeingPushed || threat == null || !threat.RecentlyAttacked(ally))
                return false;
            Vector3 away = ally.transform.position - threat.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) return false;
            away.Normalize();
            Vector3 offset = ally.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > 1.5f * 1.5f)
            {
                MoveTowards(run.ClampToCurrentRoom(ally.transform.position), dt);
                return true;
            }
            ally.pushVelocity = away * (1.5f / 0.4f);
            ally.pushRemaining = 0.4f;
            rescueCooldown = 5f;
            return true;
        }

        bool MoveDuringPush(float dt)
        {
            if (pushRemaining <= 0f) return false;
            float step = Mathf.Min(dt, pushRemaining);
            Vector3 destination = run.ClampToCurrentRoom(transform.position + pushVelocity * step);
            controller.Move(destination - transform.position + Vector3.down * 2f * step);
            pushRemaining -= step;
            return true;
        }

        internal void ChangeKarma(int amount)
        {
            Karma += amount;
            if (amount != 0) run.RecordBalanceKarma(this, amount, Karma);
        }

        WeaponInstance ChooseWeapon(float distance)
        {
            int best = SelectedWeapon;
            float score = float.NegativeInfinity;
            for (int i = 0; i < Weapons.Length; i++)
            {
                WeaponInstance weapon = Weapons[i];
                if (weapon == null) continue;
                float value = (weapon.IsReloading || weapon.Energy < weapon.Definition.primaryCost ? -100f : 0f)
                    + (distance <= weapon.Definition.range ? 20f : 0f) + weapon.Definition.damage;
                if (value > score) { score = value; best = i; }
            }
            SelectedWeapon = best;
            weaponVisual.SetWeapon(Weapons[best]);
            return Weapons[best];
        }

        public void SetDestination(Vector3 point)
        {
            corridorWaypoints.Clear();
            destination = point;
            hasDestination = true;
        }

        public void IssueCorridorPath(IReadOnlyList<Vector3> points)
        {
            if (points == null || points.Count == 0) return;
            IssueMoveOrder(points[0]);
            if (!hasDestination) return;
            for (int i = 1; i < points.Count; i++) corridorWaypoints.Enqueue(points[i]);
        }

        public void CancelMoveOrder()
        {
            hasDestination = false;
            corridorWaypoints.Clear();
            overcomingFear = false;
        }

        internal void RecoverNavigationPosition(Vector3 position, bool cancelOrder)
        {
            if (controller == null) return;
            navigation?.ResetNavigation();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.position = position;
            controller.enabled = wasEnabled;
            if (cancelOrder) CancelMoveOrder();
        }

        public void IssueMoveOrder(Vector3 point)
        {
            if (Dead) return;
            if (run.IsLastSurvivingAgent(this))
            {
                SetDestination(point);
                return;
            }
            MouseEnemy target = run.SelectTarget(this);
            float healthFraction = Health / MaxHealth;
            RefreshFear(target, healthFraction);
            if (target != null && fearRemaining > 0f && healthFraction <= 0.2f)
            {
                overcomingFear = false;
                hasDestination = false;
                return;
            }
            if (target != null && fearRemaining > 0f && !overcomingFear)
            {
                if (Random.value >= Mathf.Clamp(profile.bravery, 1, 10) / 10f)
                {
                    hasDestination = false;
                    return;
                }
            }
            if (target != null && healthFraction <= FleeThreshold) overcomingFear = true;
            SetDestination(point);
        }

        public static float FearDuration(int bravery)
        {
            return bravery <= 5
                ? Mathf.Lerp(10f, 3f, Mathf.InverseLerp(1f, 5f, bravery))
                : Mathf.Lerp(3f, 0f, Mathf.InverseLerp(5f, 10f, bravery));
        }

        void RefreshFear(MouseEnemy target, float healthFraction)
        {
            if (target == null || healthFraction > FleeThreshold)
            {
                fearTriggered = false;
                fearRemaining = 0f;
                overcomingFear = false;
                return;
            }
            if (fearTriggered) return;
            fearTriggered = true;
            fearRemaining = FearDuration(profile.bravery);
        }

        public static float RetreatDistance(int accuracy, float range) =>
            Mathf.Max(0f, range) * Mathf.Lerp(0.35f, 0.85f, Mathf.InverseLerp(1f, 10f, accuracy));

        void MaintainRetreatDistance(Vector3 targetPosition, float range, float dt)
        {
            Vector3 away = transform.position - targetPosition;
            away.y = 0f;
            float distance = away.magnitude;
            float preferred = RetreatDistance(profile.accuracy, range);
            if (Mathf.Abs(distance - preferred) <= Mathf.Min(0.4f, range * 0.1f)) return;
            Vector3 direction = distance > 0.01f ? away / distance : -AimDirection;
            Vector3 point = run.ClampToCurrentRoom(targetPosition + direction * preferred);
            if (distance < preferred && Vector3.Dot(point - transform.position, direction) <= 0f) return;
            MoveTowards(point, dt);
        }

        void MaintainMeleeDistance(Vector3 targetPosition, float range, float dt)
        {
            if (range <= 0f) return;
            Vector3 away = transform.position - targetPosition;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance >= range * 0.7f && distance <= range * 0.95f) return;
            away = distance > 0.01f ? away / distance : -AimDirection;
            Vector3 point = run.ClampToCurrentRoom(targetPosition + away * (range * 0.85f));
            if (distance < range * 0.7f && Vector3.Dot(point - transform.position, away) <= 0f) return;
            MoveTowards(point, dt);
        }

        void MaintainFormationDistance(Vector3 targetPosition, float range, float dt)
        {
            if (range <= 0f) return;
            Vector3 away = transform.position - targetPosition;
            away.y = 0f;
            float distance = away.magnitude;
            float safeDistance = Mathf.Min(range * 0.85f, Mathf.Clamp(range * 0.75f, 1.5f, 6f));
            if (distance < safeDistance)
            {
                Vector3 direction = distance > 0.01f ? away / distance : -AimDirection;
                MoveTowards(run.ClampToCurrentRoom(transform.position + direction * (safeDistance - distance + 1f)), dt);
            }
            else if (distance > range * 0.9f)
                MoveTowards(run.ClampToCurrentRoom(targetPosition), dt);
        }

        void MoveToDestination(float dt)
        {
            if (hasDestination && run.IsDoorTravel)
            {
                if (!monitoringDoorProgress || (doorProgressGoal - destination).sqrMagnitude > 0.25f ||
                    (doorProgressAnchor - transform.position).sqrMagnitude > 0.16f)
                {
                    monitoringDoorProgress = true;
                    doorProgressAnchor = transform.position;
                    doorProgressGoal = destination;
                    doorProgressSince = Time.time;
                }
                else if (Time.time - doorProgressSince >= 2f)
                {
                    navigation.ReplanAfterStall();
                    doorProgressSince = Time.time;
                    run.RecordNavigationReplan(this);
                }
            }
            else monitoringDoorProgress = false;
            Vector3 delta = destination - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.18f * 0.18f)
            {
                if (corridorWaypoints.Count > 0) destination = corridorWaypoints.Dequeue();
                else hasDestination = false;
                return;
            }
            MoveTowards(destination, dt);
        }

        void MoveTowards(Vector3 point, float dt)
        {
            float agility = (profile.agility - 1) / 9f;
            bool traveling = run.UsesSquadTravelSpeed;
            if (!traveling)
                stamina = Mathf.Max(0.35f, stamina - dt * Mathf.Lerp(0.075f, 0.045f, agility));
            float speed = (traveling ? run.SquadTravelSpeed : MovementSpeed * stamina * emergencySpeedMultiplier) *
                (bardBuff ? 1.25f : 1f) * (EscapeInvulnerable ? 2f : 1f) *
                (run.IsLastSurvivingAgent(this) ? 1.25f : 1f);
            if (!navigation.DirectionTo(point, speed, out Vector3 direction)) return;
            float step = Mathf.Min(speed * dt, Vector3.Distance(transform.position, point));
            controller.Move(direction * step + Vector3.down * 2f * dt);
        }

        public ItemPickup NearbyPickup()
        {
            ItemPickup nearest = null;
            float best = 2.25f;
            foreach (ItemPickup pickup in FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
            {
                if (!pickup.CanPickUp(this)) continue;
                float distance = (pickup.transform.position - transform.position).sqrMagnitude;
                if (distance < best) { best = distance; nearest = pickup; }
            }

            return nearest;
        }

        internal bool TryPayForPickup(ItemPickup pickup) => run.TryPayForPickup(this, pickup);

        public bool CanCollect(ItemPickup pickup) => run.PickupOwner(pickup) == this;
        public bool CanTakeHealth() => run.CanTakeHealth(this);
        public bool IsLowestHealthAgent() => run.IsLowestHealthAgent(this);
        public bool IsLastSurvivor => run != null && run.IsLastSurvivingAgent(this);
        public bool IsGreedClaim(ItemPickup pickup) => run.IsGreedClaim(pickup, this);

        internal float WeaponPreference(WeaponDefinition definition)
        {
            if (definition == null) return 0f;
            WeaponTags tags = definition.tags;
            float preference = 0f;
            if (Specialty == Specialty.Dps && (tags & WeaponTags.DPS) != 0) preference += 1.2f;
            if ((Specialty == Specialty.Medic || Specialty == Specialty.Bard) && (tags & WeaponTags.Support) != 0) preference += 1.2f;
            if ((Specialty == Specialty.Tank || Specialty == Specialty.Locksmith) && (tags & WeaponTags.Utility) != 0) preference += 1.2f;
            if ((Specialty == Specialty.Medic || Specialty == Specialty.Bard || Specialty == Specialty.Locksmith) && (tags & WeaponTags.Utility) != 0) preference += 0.8f;
            if (Specialty == Specialty.Tank && (tags & WeaponTags.Support) != 0) preference += 0.8f;
            if (Specialty == Specialty.Tank && (tags & WeaponTags.Melee) != 0) preference += 0.4f;
            if (Specialty == Specialty.Dps && (tags & (WeaponTags.Electric | WeaponTags.Poison | WeaponTags.Fire | WeaponTags.Bleeding)) != 0) preference += 0.2f;
            return preference;
        }

        float WeaponQuality(WeaponInstance weapon)
        {
            WeaponDefinition definition = weapon.Definition;
            float preference = WeaponPreference(definition);
            float utilityValue = preference > 0f && (definition.tags & (WeaponTags.Utility | WeaponTags.Support)) != 0 ? 20f : 0f;
            return (definition.EstimatedDps + utilityValue) * (1f + preference) * Mathf.Sqrt(Mathf.Max(1f, definition.range));
        }

        public bool WantsWeapon(WeaponInstance weapon)
        {
            if (weapon == null) return false;
            if (Weapons[0] == null) return true;
            return WeaponQuality(weapon) > WeaponQuality(Weapons[0]) * 1.15f;
        }

        internal bool CanUseFallenWeapon(WeaponInstance candidate)
        {
            if (candidate == null || candidate.Definition == null) return false;
            WeaponTags tags = candidate.Definition.tags;
            if ((tags & WeaponTags.DPS) == 0 || (tags & (WeaponTags.Support | WeaponTags.Utility)) != 0) return false;
            return Weapons[0] == null || candidate.Definition.EstimatedDps > Weapons[0].Definition.EstimatedDps;
        }

        public bool CanAutoEquip(WeaponInstance weapon)
        {
            if (weapon == null || equippedWeapons.Contains(weapon)) return false;
            if (run != null && !run.CanAdoptWeapon(this, weapon)) return false;
            if (Weapons[0] == null) return true;
            return WantsWeapon(weapon) || Karma > 3 && WeaponQuality(weapon) >= WeaponQuality(Weapons[0]) * 0.85f;
        }

        internal bool CanAcceptEmergencyWeapon(WeaponInstance weapon) => weapon != null && weapon.Definition != null &&
            (weapon.Definition.tags & WeaponTags.DPS) != 0 &&
            (Weapons[0] == null || (Weapons[0].Definition.tags & WeaponTags.DPS) == 0);

        public void Equip(WeaponInstance weapon)
        {
            int slot = 0;
            if (Weapons[slot] != null)
            {
                WeaponInstance dropped = Weapons[slot];
                dropped.SetOwner(null);
                run.SpawnWeaponPickup(dropped, transform.position + AimDirection * 1.2f);
                dropped.Definition.OnDrop(this, dropped);
            }
            Weapons[slot] = weapon;
            weapon.SetOwner(this);
            equippedWeapons.Add(weapon);
            SelectedWeapon = slot;
            RefreshWeaponSpeed();
            weaponVisual.SetWeapon(weapon);
            weapon.Definition.OnPickup(this, weapon);
        }

        internal WeaponInstance ReleaseWeaponOnDeath()
        {
            WeaponInstance weapon = Weapons[SelectedWeapon];
            if (weapon == null) return null;
            Weapons[SelectedWeapon] = null;
            weapon.SetOwner(null);
            weapon.Definition.OnDrop(this, weapon);
            if (weaponVisual != null) weaponVisual.SetWeapon(null);
            return weapon;
        }

        internal int ReleaseMoneyOnDeath()
        {
            int amount = Money;
            Money = 0;
            return amount;
        }

        public void SwingWeapon(WeaponInstance weapon)
        {
            if (weaponVisual != null) weaponVisual.Swing(weapon);
        }

        public void AddPassive(PassiveItemDefinition passive)
        {
            float previousHealth = Health;
            passives.Add(passive);
            MovementSpeed += passive.movementSpeedBonus;
            MaxHealth += passive.maxHealthBonus;
            Health = Mathf.Min(MaxHealth, Health + Mathf.Max(0f, passive.maxHealthBonus));
            run.RecordBalanceHeal(this, null, Health - previousHealth, "passive_max_health");
            EnergyRecharge += passive.energyRechargeBonus;
            if (Health > previousHealth && dialogue != null) dialogue.Speak(DialogueEvent.Healed, 1f);
            passive.OnPickUp(this);
        }

        public void Fire(float damage, float range, WeaponInstance weapon = null)
        {
            Fire(damage, range, AimDirection, weapon);
        }

        public void Fire(float damage, float range, Vector3 direction, WeaponInstance weapon)
        {
            Vector3 origin = transform.position + Vector3.up * 0.65f + AimDirection * 0.6f;
            bool hitSomething = Physics.Raycast(origin, direction, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore);
            if (weapon != null && weapon.Definition is RaytraceWeaponDefinition raytrace && raytrace.projectileEffect != null)
                SpawnDartEffect(raytrace.projectileEffect, origin, direction, hitSomething ? hit.distance : range);
            if (hitSomething)
            {
                MouseEnemy enemy = hit.collider.GetComponentInParent<MouseEnemy>();
                if (enemy != null) enemy.TakeDamage(damage, this, weapon, impact: hit.point, sourcePosition: origin);
            }
        }

        void SpawnDartEffect(GameObject prefab, Vector3 origin, Vector3 direction, float distance)
        {
            if (direction.sqrMagnitude < 0.0001f) return;
            GameObject effect = Instantiate(prefab, origin, Quaternion.LookRotation(direction));
            foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                float speed = main.startSpeed.constantMax;
                if (speed > 0f) main.startLifetime = Mathf.Max(0.001f, Mathf.Min(main.startLifetime.constantMax, distance / speed));
                var collision = particles.collision;
                collision.enabled = true;
                collision.type = ParticleSystemCollisionType.World;
                int world = LayerMask.NameToLayer("World");
                if (world >= 0) collision.collidesWith = 1 << world;
                collision.lifetimeLoss = 1f;
                collision.bounce = 0f;
                particles.Play();
            }
            Destroy(effect, 2f);
        }

        public void TakeDamage(float amount, MouseEnemy attacker = null, Vector3? impact = null, Vector3? sourcePosition = null, string damageSource = "unknown")
        {
            if (Dead || EscapeInvulnerable || amount <= 0f) return;
            amount = Mathf.Ceil(amount);
            float previousHealth = Health;
            Health = Mathf.Max(0f, Health - amount);
            run.RecordBalanceDamageReceived(this, attacker, previousHealth - Health, Mathf.Max(0f, amount - previousHealth), damageSource);
            Bounds bounds = controller.bounds;
            Vector3 source = sourcePosition ?? (attacker != null ? attacker.transform.position : bounds.center - transform.forward);
            run.ShowBlood(impact ?? GameRun.BloodImpact(bounds, source), bounds.center - source);
            Vector3 indicatorPosition = transform.position;
            indicatorPosition.y = controller.bounds.max.y + 0.2f;
            run.ShowDamage(indicatorPosition, amount, true);
            foreach (PassiveItemDefinition passive in passives) passive.OnTakeDamage(this, amount);
            foreach (PassiveItemDefinition passive in passives) passive.OnAttacked(this, attacker, amount);
            if (Health > 0f)
            {
                if (dialogue != null) dialogue.CheckInjury();
                return;
            }
            foreach (PassiveItemDefinition passive in passives.ToArray())
            {
                if (!passives.Contains(passive)) continue;
                passive.OnDeath(this);
                if (Health > 0f) { run.ResetAgentProgress(this); return; }
            }
            Dead = true;
            navigation.ResetNavigation();
            controller.enabled = false;
            run.AgentDied(this);
        }

#if UNITY_EDITOR
        public void DebugKill()
        {
            if (Dead) return;
            Health = 0f;
            Dead = true;
            navigation.ResetNavigation();
            controller.enabled = false;
            run.AgentDied(this);
        }
#endif

        public void ReturnToSquad(Vector3 position)
        {
            if (!Dead) return;
            transform.position = position;
            Health = MaxHealth * 0.5f;
            stamina = 1f;
            hasDestination = false;
            overcomingFear = false;
            fearTriggered = false;
            fearRemaining = 0f;
            pushRemaining = 0f;
            Dead = false;
            gameObject.SetActive(true);
            controller.enabled = true;
            if (dialogue != null) dialogue.Speak(DialogueEvent.Healed, 1f);
        }

        public void ResetProgression(AgentProfile original)
        {
            MaxHealth += (original.EffectiveHealth - profile.EffectiveHealth) * (Specialty == Specialty.Tank ? TankHealthMultiplier : 1f);
            MovementSpeed += original.EffectiveSpeed - profile.EffectiveSpeed;
            profile = original;
            Health = Mathf.Min(Health, MaxHealth);
            RefreshWeaponSpeed();
        }

        public bool ConsumePassiveAndHeal(PassiveItemDefinition passive, float amount)
        {
            if (Dead || Health > 0f || amount <= 0f || MaxHealth - passive.maxHealthBonus <= 0f ||
                !passives.Remove(passive)) return false;
            MovementSpeed -= passive.movementSpeedBonus;
            MaxHealth -= passive.maxHealthBonus;
            EnergyRecharge -= passive.energyRechargeBonus;
            Health = Mathf.Min(MaxHealth, amount);
            run.RecordBalanceHeal(this, this, Health, "revive_passive");
            run.RecordBalancePassiveRevival(this);
            if (dialogue != null) dialogue.Speak(DialogueEvent.Healed, 1f);
            return true;
        }

        public void Heal(float amount, bool announce = true, string source = "other", PlayerController healer = null)
        {
            if (amount <= 0f)
            {
                Debug.LogError("Healing amount must be positive.", this);
                return;
            }
            if (Dead) return;
            float previousHealth = Health;
            Health = Mathf.Min(MaxHealth, Health + amount);
            run.RecordBalanceHeal(this, healer, Health - previousHealth, source);
            if (announce && Health > previousHealth && dialogue != null) dialogue.Speak(DialogueEvent.Healed, 1f);
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogError("Money amount must be positive.", this);
                return;
            }
            Money += amount;
        }

        public bool TrySpendMoney(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogError("Purchase amount must be positive.", this);
                return false;
            }
            if (Money < amount) return false;
            Money -= amount;
            run.RecordBalanceMoney(this, amount, true);
            return true;
        }

        public void ShowPurchaseFailure(int price)
        {
            run.ShowMessage(GameText.Format("Dinero insuficiente (se necesitan ${0}).", price));
        }

        public void DamageMiceInRoom(float amount, WeaponInstance weapon = null)
        {
            run.DamageMiceInCurrentRoom(amount, this, weapon);
        }

        public Transform CurrentRoom => run.CurrentRoom;
        public Vector3 ClampToCurrentRoom(Vector3 point) => run.ClampToCurrentRoom(point);
        public void SetEmergencySpeedMultiplier(float multiplier) => emergencySpeedMultiplier = Mathf.Max(1f, multiplier);
        public void BalanceSquadKarma() => run.BalanceSquadKarma();
        public void BalanceSquadHealth() => run.BalanceSquadHealth();
        internal void SetBalancedHealth(float amount)
        {
            if (Dead) return;
            float previous = Health;
            Health = Mathf.Clamp(amount, 0f, MaxHealth);
            if (Health > previous) run.RecordBalanceHeal(this, null, Health - previous, "health_balance");
            if (Health > previous && dialogue != null) dialogue.Speak(DialogueEvent.Healed, 1f);
        }

        public void NotifyLootCollected(PickupKind kind = PickupKind.Money, bool usedGreed = false, bool purchased = false)
        {
            if (purchased || kind == PickupKind.Weapon || kind == PickupKind.Money && IsLastSurvivor)
            {
                run.RefreshMinimap();
                return;
            }
            int cost = kind == PickupKind.Passive ? 3 :
                kind == PickupKind.Money && Karma > 15 ? 3 :
                kind == PickupKind.Money && Karma > 10 ? 2 : 1;
            run.RecordLoot(this, cost, usedGreed);
            run.RefreshMinimap();
        }

        public void NotifyWeaponPickupCompleted(ItemPickup pickup) => run.WeaponPickupCompleted(pickup);

        public void RecordPickupScore(PickupKind kind, int price) => run.RecordPickupScore(kind, price);

        internal void RecordBalanceAttack(WeaponInstance weapon, float cost) => run.RecordBalanceAttack(this, weapon, cost);

        public void RecordBalancePickup(PickupKind kind, string name, int price) => run.RecordBalancePickup(this, kind, name, price);

        public void NotifyContestedPickup(ItemPickup pickup) => run.ReactToContestedPickup(this, pickup);

        internal void CollectMoney(int amount)
        {
            AddMoney(amount);
            if (amount > 0) run.RecordBalanceMoney(this, amount, false);
            if (amount > 0) run.RecordAgentTotals(this, 0, amount);
        }

        public void NotifyMouseKill(MouseEnemy mouse, WeaponInstance weapon = null)
        {
            run.RecordBalanceKill(this, mouse, weapon);
            run.RecordAgentTotals(this, 1, 0);
            foreach (PassiveItemDefinition passive in passives) passive.OnMouseKill(this, mouse);
        }

        public static float CriticalChance(int luck) => Mathf.InverseLerp(1f, 10f, luck) * 0.25f;

        public void NotifyCriticalHit(MouseEnemy mouse)
        {
            foreach (PassiveItemDefinition passive in passives.ToArray()) passive.OnCriticalHit(this, mouse);
        }

        public void RoomEnter()
        {
            foreach (PassiveItemDefinition passive in passives) passive.OnRoomEnter(this);
        }

        public void RoomExit()
        {
            foreach (PassiveItemDefinition passive in passives) passive.OnRoomExit(this);
        }
    }
}
