using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        public enum SquadFormation { Focus, KeepDistance, Nearest }

        const int ShopRevivalPrice = 10;
        readonly HashSet<PlayerController> greedClaims = new HashSet<PlayerController>();
        ItemPickup orderedWeapon;
        MouseEnemy focusedMouse;
        MouseEnemy fallbackFocus;
        PlayerController weaponAssignee;
        int weaponAssigneeIndex = -1;
        float weaponOrderReadyAt;
        bool anyAgentDied;

        public int EnemyCount => currentRoom != null ? currentRoom.enemies : 0;
        public SquadFormation Formation { get; private set; } = SquadFormation.Nearest;
        public bool HasFocusTarget => currentRoom != null && currentRoom.root != null &&
            Formation == SquadFormation.Focus &&
            focusedMouse != null && focusedMouse.IsTargetable &&
            focusedMouse.transform.IsChildOf(currentRoom.root.transform);

        public void SetFormation(SquadFormation formation)
        {
            if (formation != SquadFormation.Focus && formation != SquadFormation.KeepDistance &&
                formation != SquadFormation.Nearest)
            {
                Debug.LogError("Formación de escuadrón desconocida.", this);
                return;
            }
            if (Formation == formation) return;
            Formation = formation;
            focusedMouse = null;
            fallbackFocus = null;
            ShowMessage(formation == SquadFormation.Focus ? "Formación: concentrar fuego" :
                formation == SquadFormation.KeepDistance ? "Formación: mantener distancia" :
                "Formación: enemigo más cercano");
        }

        public bool FocusMouse(MouseEnemy mouse)
        {
            if (currentRoom == null || currentRoom.root == null || mouse == null || !mouse.IsTargetable ||
                !mouse.transform.IsChildOf(currentRoom.root.transform)) return false;
            SetFormation(SquadFormation.Focus);
            focusedMouse = mouse;
            fallbackFocus = null;
            ClearWeaponOrder();
            pendingVent = false;
            pendingExit = false;
            foreach (PlayerController agent in agents)
                if (agent != null && !agent.Dead) agent.CancelMoveOrder();
            ShowMessage("Concentren el fuego en esa rata.");
            return true;
        }

        public PlayerController NearestAgent(Vector3 position, PlayerController exclude = null)
        {
            PlayerController best = null;
            float distance = float.PositiveInfinity;
            foreach (PlayerController agent in agents)
            {
                if (agent == null || agent.Dead || agent == exclude) continue;
                float candidate = (agent.transform.position - position).sqrMagnitude;
                if (candidate < distance) { distance = candidate; best = agent; }
            }
            return best;
        }

        public PlayerController AgentSupportingMouse(CharacterController mouseController)
        {
            if (mouseController == null || !mouseController.enabled) return null;
            Bounds mouseBounds = mouseController.bounds;
            foreach (PlayerController agent in agents)
            {
                if (agent == null || agent.Dead || !agent.gameObject.activeInHierarchy) continue;
                CharacterController agentController = agent.GetComponent<CharacterController>();
                if (agentController == null || !agentController.enabled) continue;
                if (MouseEnemy.IsStandingOnAgent(mouseBounds, agentController.bounds)) return agent;
            }
            return null;
        }

        public PlayerController NearestAlly(PlayerController agent) => NearestAgent(agent.transform.position, agent);

        public bool IsLastSurvivingAgent(PlayerController agent)
        {
            return agent != null && !agent.Dead && NearestAgent(agent.transform.position, agent) == null;
        }

        public float SquadTravelSpeed
        {
            get
            {
                float fastest = 0f;
                foreach (PlayerController agent in agents)
                    if (agent != null && !agent.Dead) fastest = Mathf.Max(fastest, agent.MovementSpeed);
                return fastest * 1.5f;
            }
        }

        public bool UsesSquadTravelSpeed
        {
            get
            {
                if (IsTransitioning) return true;
                if (currentRoom == null || !CanLeave(currentRoom)) return false;
                if (currentRoom.kind == RoomKind.Corridor) return true;
                foreach (RoomDoor door in currentRoom.doors)
                    if (door != null && door.IsOpen) return true;
                return currentRoom.exitDoor != null && currentRoom.exitDoor.IsOpen;
            }
        }

        public void RecordLoot(PlayerController collector, int karmaCost = 1, bool usedGreed = false)
        {
            if (usedGreed) greedClaims.Add(collector);
            foreach (PlayerController agent in agents)
            {
                if (agent == null || agent.Dead) continue;
                int previousKarma = agent.Karma;
                int greedPenalty = agent == collector ? Mathf.Max(0, (agent.Profile.greed - 5) / 2) : 0;
                agent.ChangeKarma(agent == collector ? -karmaCost - greedPenalty : 1);
                if (previousKarma < -4 && agent.Karma >= -4)
                    foreach (PlayerController observer in agents)
                        if (observer != null) observer.ForgetReprimand(agent);
            }

            int highest = int.MinValue;
            foreach (PlayerController agent in agents)
                if (agent != null && !agent.Dead) highest = Mathf.Max(highest, agent.Karma);
            foreach (PlayerController agent in agents)
                if (agent != null && !agent.Dead && agent.Karma >= highest) greedClaims.Remove(agent);
        }

        public void BalanceSquadKarma()
        {
            greedClaims.Clear();
            foreach (PlayerController agent in agents)
            {
                if (agent == null) continue;
                agent.ChangeKarma(-agent.Karma);
                foreach (PlayerController observer in agents)
                    if (observer != null) observer.ForgetReprimand(agent);
            }
        }

        public void BalanceSquadHealth()
        {
            var remaining = new List<PlayerController>();
            float health = 0f;
            foreach (PlayerController agent in agents)
                if (agent != null && !agent.Dead) { remaining.Add(agent); health += agent.Health; }
            while (remaining.Count > 0)
            {
                float average = health / remaining.Count;
                int capped = remaining.FindIndex(agent => agent.MaxHealth < average);
                if (capped < 0)
                {
                    foreach (PlayerController agent in remaining) agent.SetBalancedHealth(average);
                    break;
                }
                PlayerController limited = remaining[capped];
                limited.SetBalancedHealth(limited.MaxHealth);
                health -= limited.MaxHealth;
                remaining.RemoveAt(capped);
            }
        }

        public PlayerController SelectLootHoarder(PlayerController agent)
        {
            PlayerController best = null;
            float distance = float.PositiveInfinity;
            foreach (PlayerController ally in agents)
            {
                if (ally == null || ally == agent || ally.Dead || ally.BeingPushed || ally.Karma >= 0) continue;
                float candidate = (ally.transform.position - agent.transform.position).sqrMagnitude;
                if (candidate < distance) { distance = candidate; best = ally; }
            }
            return best;
        }

        public PlayerController SelectReprimandTarget(PlayerController agent)
        {
            PlayerController best = null;
            float distance = float.PositiveInfinity;
            foreach (PlayerController ally in agents)
            {
                if (ally == null || ally == agent || ally.Dead || ally.Karma >= -4 ||
                    agent.HasReprimanded(ally)) continue;
                float candidate = (ally.transform.position - agent.transform.position).sqrMagnitude;
                if (candidate < distance) { distance = candidate; best = ally; }
            }
            return best;
        }

        public MouseEnemy SelectTarget(PlayerController agent)
        {
            if (currentRoom == null || currentRoom.root == null || agent == null) return null;
            if (HasFocusTarget)
            {
                fallbackFocus = null;
                return focusedMouse;
            }
            if (Formation == SquadFormation.Focus)
            {
                if (focusedMouse == null) focusedMouse = NearestMouseToSquad();
                if (HasFocusTarget) return focusedMouse;
                if (fallbackFocus == null || !fallbackFocus.IsTargetable ||
                    !fallbackFocus.transform.IsChildOf(currentRoom.root.transform))
                    fallbackFocus = NearestMouseToSquad();
                return fallbackFocus;
            }
            return NearestMouse(agent.transform.position);
        }

        MouseEnemy NearestMouseToSquad()
        {
            Vector3 center = Vector3.zero;
            int count = 0;
            foreach (PlayerController member in agents)
                if (member != null && !member.Dead) { center += member.transform.position; count++; }
            return count > 0 ? NearestMouse(center / count) : null;
        }

        MouseEnemy NearestMouse(Vector3 position)
        {
            MouseEnemy best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (MouseEnemy mouse in currentRoom.root.GetComponentsInChildren<MouseEnemy>())
            {
                if (!mouse.IsTargetable) continue;
                float distance = (position - mouse.transform.position).sqrMagnitude;
                if (distance < bestDistance) { bestDistance = distance; best = mouse; }
            }
            return best;
        }

        public bool IsSurrounded(PlayerController agent)
        {
            if (currentRoom == null || currentRoom.root == null || agent == null) return false;
            int nearby = 0;
            foreach (MouseEnemy mouse in currentRoom.root.GetComponentsInChildren<MouseEnemy>())
                if (mouse.IsTargetable && (mouse.transform.position - agent.transform.position).sqrMagnitude <= 9f &&
                    ++nearby >= 2) return true;
            return false;
        }

        void BossDialogue()
        {
            var living = new List<PlayerController>();
            foreach (PlayerController agent in agents)
            {
                if (agent == null || agent.Dead) continue;
                living.Add(agent);
                if (!anyAgentDied && agent.Dialogue != null) agent.Dialogue.Emotion(AgentEmotion.Happiness);
            }
            if (living.Count == 0) return;
            int start = Random.Range(0, living.Count);
            for (int i = 0; i < living.Count; i++)
            {
                AgentDialogue dialogue = living[(start + i) % living.Count].Dialogue;
                if (dialogue != null && dialogue.Speak(DialogueEvent.BossDefeated, 1f)) break;
            }
        }

        public bool TrySelectRescue(PlayerController rescuer, out PlayerController ally, out MouseEnemy threat)
        {
            ally = null;
            threat = null;
            if (currentRoom == null || currentRoom.root == null || rescuer == null || !rescuer.CanAttemptRescue)
                return false;
            int endangered = 0;
            foreach (PlayerController agent in agents)
                if (agent != null && !agent.Dead && !agent.BeingPushed && agent.Health / agent.MaxHealth <= 0.4f)
                    endangered++;
            if (endangered < 2) return false;

            endangered = 0;
            MouseEnemy[] mice = currentRoom.root.GetComponentsInChildren<MouseEnemy>();
            foreach (PlayerController agent in agents)
            {
                if (agent == null || agent.Dead || agent.BeingPushed || agent.Health / agent.MaxHealth > 0.4f) continue;
                MouseEnemy nearestThreat = null;
                float nearestDistance = float.PositiveInfinity;
                foreach (MouseEnemy mouse in mice)
                {
                    if (!mouse.RecentlyAttacked(agent)) continue;
                    float distance = (mouse.transform.position - agent.transform.position).sqrMagnitude;
                    if (distance < nearestDistance) { nearestThreat = mouse; nearestDistance = distance; }
                }
                if (nearestThreat == null) continue;
                endangered++;
                if (ally == null || agent.Health / agent.MaxHealth < ally.Health / ally.MaxHealth)
                {
                    ally = agent;
                    threat = nearestThreat;
                }
            }
            if (endangered < 2 || ally == null) return false;
            PlayerController best = null;
            foreach (PlayerController candidate in agents)
                if (candidate != null && candidate != ally && !candidate.Dead && candidate.CanAttemptRescue &&
                    (best == null || candidate.Profile.teamwork > best.Profile.teamwork))
                    best = candidate;
            return best == rescuer;
        }

        public Vector3 ClampToCurrentRoom(Vector3 point)
        {
            Vector3 local = point - currentRoom.center;
            if (currentRoom.kind == RoomKind.Corridor)
            {
                Room nearest = ClosestCorridor(point);
                return nearest != null ? ProjectOntoCorridor(nearest, point) : point;
            }
            local.x = Mathf.Clamp(local.x, -currentRoom.width * 0.5f + 0.7f, currentRoom.width * 0.5f - 0.7f);
            local.z = Mathf.Clamp(local.z, -currentRoom.depthSize * 0.5f + 0.7f, currentRoom.depthSize * 0.5f - 0.7f);
            return currentRoom.center + local;
        }

        static Vector3 ProjectOntoCorridor(Room room, Vector3 point)
        {
            Vector3 local = point - room.center;
            float half = CorridorWidth * 0.5f - 0.7f;
            Vector3 best = new Vector3(Mathf.Clamp(local.x, -half, half), local.y, Mathf.Clamp(local.z, -half, half));
            float bestDistance = new Vector2(local.x - best.x, local.z - best.z).sqrMagnitude;
            for (int i = 0; i < 4; i++)
            {
                if (room.neighbours[i] == null) continue;
                bool horizontal = i % 2 == 1;
                float sign = i < 2 ? 1f : -1f;
                float edge = (horizontal ? room.width : room.depthSize) * 0.5f -
                    (room.neighbours[i].kind == RoomKind.Corridor ? 0f : 0.7f);
                float axis = Mathf.Clamp(horizontal ? local.x : local.z, Mathf.Min(0f, sign * edge), Mathf.Max(0f, sign * edge));
                Vector3 candidate = horizontal ? new Vector3(axis, local.y, Mathf.Clamp(local.z, -half, half)) :
                    new Vector3(Mathf.Clamp(local.x, -half, half), local.y, axis);
                float distance = new Vector2(local.x - candidate.x, local.z - candidate.z).sqrMagnitude;
                if (distance < bestDistance) { best = candidate; bestDistance = distance; }
            }
            return room.center + best;
        }

        Room ClosestCorridor(Vector3 point)
        {
            Room best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (Room room in rooms)
            {
                if (room.kind != RoomKind.Corridor) continue;
                Vector3 offset = ProjectOntoCorridor(room, point) - point;
                offset.y = 0f;
                if (offset.sqrMagnitude < bestDistance) { best = room; bestDistance = offset.sqrMagnitude; }
            }
            return best;
        }

        bool EligibleForPickup(PlayerController agent, ItemPickup pickup)
        {
            return agent != null && pickup.CanPickUp(agent) && CanAffordPickup(agent, pickup) &&
                (pickup.Kind != PickupKind.Weapon || agent.CanAutoEquip(pickup.weapon));
        }

        bool CanAffordPickup(PlayerController buyer, ItemPickup pickup)
        {
            if (buyer.Money >= pickup.Price) return true;
            if (currentRoom == null || currentRoom.kind != RoomKind.Shop ||
                pickup.Kind != PickupKind.Health || buyer.Health >= buyer.MaxHealth * 0.3f ||
                !pickup.transform.IsChildOf(currentRoom.root.transform)) return false;

            int shortfall = pickup.Price - buyer.Money;
            foreach (PlayerController ally in agents)
            {
                if (ally == null || ally == buyer || ally.Dead) continue;
                shortfall -= Mathf.Min(shortfall, ally.Money);
                if (shortfall == 0) return true;
            }
            return false;
        }

        internal bool TryPayForPickup(PlayerController buyer, ItemPickup pickup)
        {
            if (pickup.Price <= 0) return true;
            if (buyer.TrySpendMoney(pickup.Price)) return true;
            if (!CanAffordPickup(buyer, pickup)) return false;

            int remaining = pickup.Price - buyer.Money;
            if (buyer.Money > 0) buyer.TrySpendMoney(buyer.Money);
            foreach (PlayerController ally in agents)
            {
                if (ally == null || ally == buyer || ally.Dead || ally.Money <= 0) continue;
                int donation = Mathf.Min(remaining, ally.Money);
                ally.TrySpendMoney(donation);
                remaining -= donation;
                if (remaining == 0) break;
            }
            ShowMessage(GameText.Format("El escuadrón ayudó a {0} a comprar un botiquín.", buyer.Profile.name));
            return true;
        }

        public void ReactToContestedPickup(PlayerController collector, ItemPickup pickup)
        {
            if (collector == null || pickup == null || collector.Karma >= 0) return;
            foreach (PlayerController agent in agents)
                if (agent != null && agent != collector && agent.Karma > 7 &&
                    agent.Dialogue != null && EligibleForPickup(agent, pickup))
                    agent.Dialogue.Emotion(AgentEmotion.Anger);
        }

        public bool CanTakeHealth(PlayerController agent)
        {
            const double fractionTolerance = 0.000001;
            foreach (PlayerController ally in agents)
                if (ally != null && ally != agent && !ally.Dead &&
                    (double)agent.Health * ally.MaxHealth >
                    (double)ally.Health * agent.MaxHealth +
                    (0.25 + fractionTolerance) * agent.MaxHealth * ally.MaxHealth) return false;
            return true;
        }

        public bool IsLowestHealthAgent(PlayerController agent)
        {
            foreach (PlayerController ally in agents)
                if (ally != null && !ally.Dead && ally.Health * agent.MaxHealth < agent.Health * ally.MaxHealth)
                    return false;
            return true;
        }

        public PlayerController PickupOwner(ItemPickup pickup)
        {
            if (currentRoom == null || pickup == null || !pickup.Available ||
                !pickup.transform.IsChildOf(currentRoom.root.transform)) return null;
            if (pickup.Price > 0 && pickup.Kind == PickupKind.Health)
            {
                PlayerController critical = null;
                float lowest = 0.3f;
                foreach (PlayerController agent in agents)
                {
                    if (!EligibleForPickup(agent, pickup)) continue;
                    float healthFraction = agent.Health / agent.MaxHealth;
                    if (healthFraction >= lowest) continue;
                    critical = agent;
                    lowest = healthFraction;
                }
                if (critical != null) return critical;
            }
            if (pickup == orderedWeapon && weaponAssignee != null && !weaponAssignee.Dead &&
                weaponAssignee.Money >= pickup.Price) return weaponAssignee;
            int highest = int.MinValue;
            foreach (PlayerController agent in agents)
                if (EligibleForPickup(agent, pickup)) highest = Mathf.Max(highest, agent.Karma);
            PlayerController best = null;
            long bestScore = long.MinValue;
            float bestDistance = float.PositiveInfinity;
            foreach (PlayerController agent in agents)
            {
                if (!EligibleForPickup(agent, pickup)) continue;
                long score = agent.Karma;
                if (agent.Karma < highest && !greedClaims.Contains(agent))
                    score += Mathf.FloorToInt(Mathf.Clamp(agent.Profile.greed, 1, 10) * 2f);
                float distance = (agent.transform.position - pickup.transform.position).sqrMagnitude;
                if (best == null || score > bestScore || score == bestScore &&
                    (agent.Karma > best.Karma || agent.Karma == best.Karma && distance < bestDistance))
                {
                    best = agent;
                    bestScore = score;
                    bestDistance = distance;
                }
            }
            return best;
        }

        public bool IsGreedClaim(ItemPickup pickup, PlayerController collector)
        {
            if (pickup == orderedWeapon || PickupOwner(pickup) != collector) return false;
            foreach (PlayerController agent in agents)
                if (EligibleForPickup(agent, pickup) && agent.Karma > collector.Karma) return true;
            return false;
        }

        public bool AssignWeaponPickup(ItemPickup pickup)
        {
            if (currentRoom == null || pickup == null || !pickup.Available || pickup.Kind != PickupKind.Weapon ||
                !pickup.transform.IsChildOf(currentRoom.root.transform)) return false;
            int start = pickup == orderedWeapon ? weaponAssigneeIndex + 1 : 0;
            for (int offset = 0; offset < agents.Count; offset++)
            {
                int index = (start + offset) % agents.Count;
                PlayerController agent = agents[index];
                if (agent == null || agent.Dead || agent.Money < pickup.Price) continue;
                orderedWeapon = pickup;
                weaponAssignee = agent;
                weaponAssigneeIndex = index;
                weaponOrderReadyAt = Time.time + 0.75f;
                pendingVent = false;
                pendingExit = false;
                agent.CancelMoveOrder();
                ShowMessage(GameText.Format("{0} recogerá {1}.", agent.Profile.name, pickup.DisplayName));
                return true;
            }
            ShowMessage("Ningún agente disponible puede recoger esa arma.");
            return false;
        }

        public ItemPickup OrderedWeaponFor(PlayerController agent)
        {
            if (agent != weaponAssignee || orderedWeapon == null || !orderedWeapon.Available) return null;
            if (agent.Money >= orderedWeapon.Price) return orderedWeapon;
            ClearWeaponOrder();
            ShowMessage("La orden de recoger el arma fue cancelada: dinero insuficiente.");
            return null;
        }

        public bool WeaponOrderReady(PlayerController agent) => agent == weaponAssignee && Time.time >= weaponOrderReadyAt;

        void ClearWeaponOrder()
        {
            orderedWeapon = null;
            weaponAssignee = null;
            weaponAssigneeIndex = -1;
        }

        public void WeaponPickupCompleted(ItemPickup pickup)
        {
            if (pickup == orderedWeapon) ClearWeaponOrder();
        }

        public ItemPickup SelectPickup(PlayerController agent)
        {
            if (currentRoom == null || !currentRoom.cleared) return null;
            ItemPickup best = null;
            int bestPriority = -1;
            float bestDistance = float.PositiveInfinity;
            foreach (ItemPickup pickup in currentRoom.root.GetComponentsInChildren<ItemPickup>())
            {
                if (PickupOwner(pickup) != agent) continue;
                int priority = pickup.Kind == PickupKind.Money && (agent.Karma > 7 || IsLastSurvivingAgent(agent)) ? 2 :
                    pickup.Kind == PickupKind.Weapon && agent.Karma > 3 ? 1 : 0;
                float distance = (pickup.transform.position - agent.transform.position).sqrMagnitude;
                if (priority > bestPriority || priority == bestPriority && distance < bestDistance)
                {
                    bestPriority = priority;
                    bestDistance = distance;
                    best = pickup;
                }
            }
            return best;
        }

        internal void RecordAgentTotals(PlayerController agent, int ratsKilled, int moneyCollected)
        {
            ExpeditionSession.RecordAgentTotals(agents.IndexOf(agent), ratsKilled, moneyCollected);
        }

        public void ResetAgentProgress(PlayerController agent)
        {
            int slot = agents.IndexOf(agent);
            if (ExpeditionSession.Active && slot >= 0 && slot < ExpeditionSession.SquadSize)
                agent.ResetProgression(ExpeditionSession.ResetProgress(ExpeditionSession.Data.selectedAgents[slot]));
        }

        public void AgentDied(PlayerController agent)
        {
            ShakeCameraOnDeath();
            anyAgentDied = true;
            ResetAgentProgress(agent);
            if (agent == weaponAssignee)
            {
                ClearWeaponOrder();
                ShowMessage("La orden de recoger el arma fue cancelada: agente caído.");
            }
            PlayerController survivor = NearestAgent(agent.transform.position);
            var observers = new List<PlayerController>();
            int fallen = 0;
            foreach (PlayerController other in agents)
            {
                if (other == null) continue;
                if (other.Dead) { fallen++; continue; }
                if (other.Dialogue == null) continue;
                observers.Add(other);
                other.Dialogue.Emotion(AgentEmotion.Sadness);
            }
            if (fallen >= 2)
                foreach (PlayerController observer in observers) observer.Dialogue.Emotion(AgentEmotion.Fear);
            if (observers.Count > 0) StartCoroutine(DeathReactions(observers));
            if (squadIndicators != null) squadIndicators.ClearPresentation(agent);
            agent.gameObject.SetActive(false);
            if (survivor == null) EndRun();
            else if (player == agent) player = survivor;
        }

        IEnumerator DeathReactions(List<PlayerController> observers)
        {
            foreach (PlayerController observer in observers)
            {
                yield return new WaitForSeconds(1f);
                if (observer != null && !observer.Dead && observer.Dialogue != null)
                    observer.Dialogue.Speak(DialogueEvent.AllyDied, 1f);
            }
        }

        void ReturnDownedAgents()
        {
            if (Finished) return;
            PlayerController survivor = NearestAgent(currentRoom.center);
            if (survivor == null) return;
            for (int i = 0; i < agents.Count; i++)
            {
                PlayerController agent = agents[i];
                if (!agent.Dead) continue;
                ReviveAgent(agent, survivor, i);
                ShowMessage(GameText.Format("{0} ha regresado.", agent.Profile.name));
            }
        }

        void ReviveAgent(PlayerController agent, PlayerController survivor, int index)
        {
            Vector3 position = ClampToCurrentRoom(survivor.transform.position +
                Quaternion.Euler(0f, index * 120f, 0f) * Vector3.forward);
            position.y = survivor.transform.position.y;
            agent.ReturnToSquad(position);
            CharacterController revived = agent.GetComponent<CharacterController>();
            foreach (PlayerController other in agents)
                if (other != agent && !other.Dead)
                    Physics.IgnoreCollision(revived, other.GetComponent<CharacterController>());
        }

        bool TryBuyShopRevival()
        {
            if (Finished || IsTransitioning || currentRoom == null || !currentRoom.visited ||
                currentRoom.kind != RoomKind.Shop) return false;
            for (int i = 0; i < agents.Count; i++)
            {
                PlayerController fallen = agents[i];
                if (fallen == null || !fallen.Dead) continue;
                foreach (PlayerController buyer in agents)
                {
                    if (buyer == null || buyer.Dead || buyer.Profile.greed >= 7 || buyer.Money < ShopRevivalPrice) continue;
                    if (!buyer.TrySpendMoney(ShopRevivalPrice)) return false;
                    ReviveAgent(fallen, buyer, i);
                    ShowMessage(GameText.Format("{0} revivió a {1} por ${2}.", buyer.Profile.name, fallen.Profile.name, ShopRevivalPrice));
                    return true;
                }
            }
            return false;
        }
    }
}
