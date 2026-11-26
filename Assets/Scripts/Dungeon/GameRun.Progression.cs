using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        void SpawnVent(Room room)
        {
            var vent = new GameObject("Next Level Vent");
            vent.transform.SetParent(room.root.transform, false);
            Visual(vent.transform, PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f),
                new Vector3(1.8f, 0.12f, 1.8f), new Color(0.45f, 0.45f, 0.5f));
            for (int i = -2; i <= 2; i++)
                Visual(vent.transform, PrimitiveType.Cube, new Vector3(i * 0.3f, 0.13f, 0f),
                    new Vector3(0.12f, 0.03f, 1.4f), new Color(0.12f, 0.12f, 0.14f));
            BoxCollider trigger = vent.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.up;
            trigger.size = new Vector3(2f, 2f, 2f);
            vent.AddComponent<PickupGlow>().Initialize(false);
            room.vent = trigger;
        }

        bool CanUseVent()
        {
            if (Finished || IsTransitioning || currentRoom != bossRoom || currentRoom == null ||
                !currentRoom.bossDefeated || !CanLeave(currentRoom) || currentRoom.vent == null) return false;
            foreach (PlayerController agent in agents)
                if (!agent.Dead && currentRoom.vent.bounds.Contains(agent.transform.position + Vector3.up * 0.5f))
                    return true;
            return false;
        }

        public bool TryUseVent()
        {
            if (!CanUseVent()) return false;
            StartNextLevel();
            return true;
        }

        void StartNextLevel()
        {
            ClearWeaponOrder();
            RecordCompletedFloor();
            foreach (PlayerController agent in agents)
                if (agent != null && agent.Dialogue != null) agent.Dialogue.ResetFloor();
            ReturnDownedAgents();
            foreach (PlayerController agent in agents)
                if (!agent.Dead) agent.RoomExit();
            levelIndex++;
            GenerateFloor();
        }

        void GenerateFloor()
        {
            ClearPendingCorridorDoor();
            ResetFloorSpecialties();
            if (hexBackground != null) hexBackground.NewFloorPalette();
            floorGore?.ReleaseFloor();
            foreach (Room room in rooms)
            {
                if (room.root == null) continue;
                room.root.SetActive(false);
                Destroy(room.root);
            }
            foreach (Object asset in levelAssets)
                if (asset != null) Destroy(asset);
            levelAssets.Clear();
            rooms.Clear();
            currentRoom = null;
            bossRoom = null;
            if (minimap != null) minimap.ClearRooms();
            Finished = false;
            message = null;
            BuildExpeditionFloor();
            EnterRoom(rooms[0], -1);
        }

        bool pendingVent;
        bool pendingExit;

        void RecordCompletedFloor()
        {
            if (FloorCleared())
            {
                fullyClearedFloors++;
                completedFloorCredit += 1f;
            }
            else
                completedFloorCredit += 1f - (float)ClearedRoomCount() / Mathf.Max(1, ChamberCount());
        }

        bool FloorCleared()
        {
            if (bossRoom == null || !bossRoom.bossDefeated) return false;
            foreach (Room room in rooms)
                if (room.kind != RoomKind.Corridor && (!room.visited || !room.cleared || room.spawning)) return false;
            return true;
        }

        bool CanUseExit()
        {
            if (Finished || IsTransitioning || currentRoom != bossRoom || !currentRoom.bossDefeated || !CanLeave(currentRoom) ||
                currentRoom.exitDoor == null) return false;
            Vector3 position = ExitApproach(currentRoom);
            foreach (PlayerController agent in agents)
                if (!agent.Dead && (agent.transform.position - position).sqrMagnitude < 2.5f * 2.5f)
                    return true;
            return false;
        }

        Vector3 ExitApproach(Room room)
        {
            Vector2Int direction = Directions[room.exitDirection];
            return room.exitDoor.transform.position - new Vector3(direction.x, 0f, direction.y) * 1.5f;
        }

        IEnumerator TravelClearedRoute(Room destination)
        {
            var previous = new Dictionary<Room, Room>();
            var queue = new Queue<Room>();
            previous[currentRoom] = null;
            queue.Enqueue(currentRoom);
            while (queue.Count > 0 && !previous.ContainsKey(destination))
            {
                Room room = queue.Dequeue();
                foreach (Room neighbour in room.neighbours)
                    if (neighbour != null && CanLeave(neighbour) &&
                        (neighbour.visited || neighbour.kind == RoomKind.Corridor && room.kind == RoomKind.Corridor) &&
                        !previous.ContainsKey(neighbour))
                    {
                        previous[neighbour] = room;
                        queue.Enqueue(neighbour);
                    }
            }
            if (!previous.ContainsKey(destination))
            {
                ShowMessage("No hay un camino despejado hasta esa sala.");
                yield break;
            }
            var path = new Stack<Room>();
            for (Room room = destination; previous[room] != null; room = previous[room]) path.Push(room);
            while (path.Count > 0)
            {
                Room next = path.Pop();
                int direction = System.Array.IndexOf(currentRoom.neighbours, next);
                yield return TravelToRoom(next, direction);
            }
        }

        void TravelToClearedRoom(Room destination)
        {
            if (destination == currentRoom) return;
            bool hallwayRoute = currentRoom.kind == RoomKind.Corridor && destination.kind == RoomKind.Corridor;
            if ((!destination.visited && !hallwayRoute) || !CanLeave(destination))
            {
                ShowMessage("Solo puedes seleccionar salas ya despejadas.");
                return;
            }
            if (!CanLeave(currentRoom) || transitionDelay > 0f)
            {
                ShowMessage("Limpia la sala antes de salir.");
                return;
            }
            if (currentRoom.kind == RoomKind.Corridor)
            {
                if (destination.kind == RoomKind.Corridor) { OrderCorridorSquad(destination.center, destination); return; }
                if (QueueCorridorDoor(destination)) return;
            }
            pendingVent = false;
            pendingExit = false;
            StartCoroutine(TravelClearedRoute(destination));
        }

#if UNITY_EDITOR
        bool HandleEditorDebugKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;
            if (keyboard.f1Key.wasPressedThisFrame) ApplyEditorDebug(1);
            else if (keyboard.f2Key.wasPressedThisFrame) ApplyEditorDebug(2);
            else if (keyboard.f3Key.wasPressedThisFrame) ApplyEditorDebug(3);
            else if (keyboard.f4Key.wasPressedThisFrame) ApplyEditorDebug(4);
            else if (keyboard.f5Key.wasPressedThisFrame) ApplyEditorDebug(5);
            else if (keyboard.f6Key.wasPressedThisFrame) ApplyEditorDebug(6);
            else return false;
            return true;
        }

        void ApplyEditorDebug(int command)
        {
            if (Finished || currentRoom == null) { ShowMessage("Depuración no disponible en este momento."); return; }
            if (balanceReport != null) balanceReport.debug_commands_used = true;
            BalanceLog("editor_debug", null, null, "", "", command);
            switch (command)
            {
                case 1:
                    foreach (PlayerController agent in agents)
                        if (agent != null && !agent.Dead && agent.Health < agent.MaxHealth)
                            agent.Heal(agent.MaxHealth - agent.Health, source: "editor_debug");
                    ShowMessage("Agentes vivos curados.");
                    break;
                case 2:
                case 3:
                    foreach (PlayerController agent in agents)
                    {
                        if (agent == null) continue;
                        int before = agent.Karma;
                        agent.ChangeKarma(command == 2 ? 5 : -5);
                        if (before < -4 && agent.Karma >= -4)
                            foreach (PlayerController observer in agents)
                                if (observer != null) observer.ForgetReprimand(agent);
                    }
                    ShowMessage(command == 2 ? "Karma +5." : "Karma -5.");
                    break;
                case 4:
                    if (IsTransitioning) { ShowMessage("Espera a terminar la transición."); return; }
                    DebugClearRoom();
                    ShowMessage("Ratas de la sala eliminadas.");
                    break;
                case 5:
                    StopAllCoroutines();
                    ClearWeaponOrder();
                    pendingVent = pendingExit = false;
                    IsTransitioning = false;
                    foreach (PlayerController agent in agents)
                    {
                        if (agent == null) continue;
                        if (agent.Dialogue != null) agent.Dialogue.ResetFloor();
                        if (!agent.Dead) agent.RoomExit();
                    }
                    GenerateFloor();
                    ShowMessage("Piso reiniciado.");
                    break;
                case 6:
                    var living = new List<PlayerController>();
                    foreach (PlayerController agent in agents)
                        if (agent != null && !agent.Dead) living.Add(agent);
                    if (living.Count == 0) { ShowMessage("No quedan agentes vivos."); return; }
                    living[Random.Range(0, living.Count)].DebugKill();
                    break;
                default: Debug.LogError("Comando de depuración de expedición desconocido."); break;
            }
        }

        void DebugClearRoom()
        {
            if (currentRoom.cleared) return;
            MouseEnemy[] mice = currentRoom.root.GetComponentsInChildren<MouseEnemy>();
            bool interruptedSpawn = currentRoom.spawning;
            if (interruptedSpawn)
            {
                currentRoom.spawning = false;
                currentRoom.enemies = 0;
                foreach (MouseEnemy mouse in mice)
                    if (mouse != null && !mouse.IsDefeated) currentRoom.enemies++;
                foreach (PlayerController agent in agents)
                    if (agent != null && !agent.Dead) agent.RoomEnter();
            }
            for (int generation = 0; generation < 4 && !currentRoom.cleared; generation++)
            {
                mice = currentRoom.root.GetComponentsInChildren<MouseEnemy>();
                foreach (MouseEnemy mouse in mice)
                    if (mouse != null && !mouse.IsDefeated) mouse.TakeDamage(float.MaxValue, player);
            }
            if (currentRoom.enemies == 0 && !currentRoom.cleared) CompleteCombatRoom(currentRoom);
        }
#endif

        bool speedingUp;
        float previousTimeScale;

        void SetFastForward(bool enabled)
        {
            if (speedingUp == enabled) return;
            speedingUp = enabled;
            if (enabled)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = previousTimeScale * 2f;
            }
            else Time.timeScale = previousTimeScale;
        }

        void Update()
        {
            if (!Finished && currentRoom != null) TickBalanceTelemetry(Time.deltaTime, Time.unscaledDeltaTime);
            if (!Finished && currentRoom != null)
            {
                scoreElapsed += Time.deltaTime;
                int seconds = Mathf.FloorToInt(scoreElapsed);
                if (seconds > 0)
                {
                    scoreElapsed -= seconds;
                    ChangeScore(-(long)seconds * 10);
                }
            }
            Keyboard keyboard = Keyboard.current;
            SetFastForward(!ExpeditionSession.AutoRun && !Finished && currentRoom != null && Application.isFocused &&
                keyboard != null && keyboard.spaceKey.isPressed);
            if (ExpeditionSession.AutoRun)
            {
                Time.timeScale = ExpeditionSession.AutoRunSpeed;
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { StopAutoRun(); return; }
                if (Finished) { RestartAutoRun(); return; }
            }
#if UNITY_EDITOR
            if (!ExpeditionSession.AutoRun && HandleEditorDebugKeys()) return;
#endif
            if (Finished)
            {
                if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    if (returnToPlanningAfterDefeat) SceneManager.LoadScene("Planning");
                    else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                return;
            }
            transitionDelay -= Time.deltaTime;
            if (IsTransitioning || currentRoom == null) return;
            UpdateCorridorContext();
            UpdatePendingCorridorDoor();
            if (IsTransitioning) return;
            UpdateSpecialties();
            if (IsTransitioning) return;
            EnsureEmergencyDamageWeapon();
            if (ExpeditionSession.AutoRun) SetFormation(SquadFormation.KeepDistance);
            else if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                    SetFormation(SquadFormation.Focus);
                else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                    SetFormation(SquadFormation.KeepDistance);
                else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
                    SetFormation(SquadFormation.Nearest);
            }
            TryBuyShopRevival();
            if (weaponAssignee != null && (orderedWeapon == null || !orderedWeapon.Available))
            {
                ClearWeaponOrder();
                ShowMessage("El arma ya no está disponible.");
            }
            if (pendingVent && CanUseVent())
            {
                pendingVent = false;
                StartNextLevel();
                return;
            }
            if (pendingExit && CanUseExit())
            {
                pendingExit = false;
                Finished = true;
                RecordCompletedFloor();
                FinishBalanceTelemetry("completed");
                ExpeditionSession.Settle(completedFloorCredit, agents.ToArray(), true, score);
                if (ExpeditionSession.AutoRun) RestartAutoRun();
                else SceneManager.LoadScene("Planning");
                return;
            }
            if (ExpeditionSession.AutoRun) { UpdateAutoRun(); return; }
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mousePosition = mouse.position.ReadValue();
            Vector2 guiPosition = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
            if (mouse.leftButton.wasPressedThisFrame)
                for (int i = 0; i < 3; i++)
                    if (FormationButtonRect(i).Contains(guiPosition))
                    {
                        SetFormation((SquadFormation)i);
                        return;
                    }
            if (!mouse.rightButton.wasPressedThisFrame || FormationButtonsRect().Contains(guiPosition)) return;
            ClearPendingCorridorDoor();
            if (minimap != null && minimap.ContainsScreenPoint(mousePosition))
            {
                if (minimap.TryGetRoomAtScreenPoint(mousePosition, out Vector2Int position))
                    foreach (Room room in rooms)
                        if (room.position == position)
                        {
                            if (currentRoom.kind == RoomKind.Corridor && room.kind == RoomKind.Corridor)
                                OrderCorridorSquad(room.center, room);
                            else if (!QueueCorridorDoor(room)) TravelToClearedRoom(room);
                            break;
                        }
                return;
            }
            Ray ray = viewCamera.ScreenPointToRay(mousePosition);
            Plane ground = new Plane(Vector3.up, currentRoom.center);
            bool hitsGround = ground.Raycast(ray, out float distance);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hitsGround && hit.distance > distance + 0.01f) break;
                MouseEnemy enemy = hit.collider.GetComponentInParent<MouseEnemy>();
                if (enemy != null && FocusMouse(enemy)) return;
                ItemPickup pickup = hit.collider.GetComponentInParent<ItemPickup>();
                if (pickup != null && pickup.Available && pickup.Kind == PickupKind.Weapon &&
                    pickup.transform.IsChildOf(currentRoom.root.transform))
                {
                    AssignWeaponPickup(pickup);
                    return;
                }
                RoomDoor door = hit.collider.GetComponentInParent<RoomDoor>();
                if (door != null)
                {
                    if (currentRoom.kind == RoomKind.Corridor)
                    {
                        foreach (Room candidate in rooms)
                            for (int direction = 0; direction < 4; direction++)
                                if (candidate.doors[direction] == door)
                                {
                                    Room destination = candidate.kind == RoomKind.Corridor ? candidate.neighbours[direction] : candidate;
                                    if (destination != null && destination.kind != RoomKind.Corridor && QueueCorridorDoor(destination)) return;
                                }
                    }
                    if (door == currentRoom.exitDoor)
                    {
                        if (!currentRoom.bossDefeated || !CanLeave(currentRoom)) ShowMessage("Derrota al jefe y despeja esta sala.");
                        else
                        {
                            pendingExit = true;
                            pendingVent = false;
                            OrderSquad(ExitApproach(currentRoom));
                        }
                        return;
                    }
                    for (int i = 0; i < 4; i++)
                        if (currentRoom.doors[i] == door && currentRoom.neighbours[i] != null)
                        {
                            if (currentRoom.neighbours[i] == bossRoom && !BossRevealed())
                                ShowMessage("La puerta está cerrada.");
                            else if (!CanLeave(currentRoom) || transitionDelay > 0f)
                                ShowMessage("Limpia la sala antes de salir.");
                            else
                            {
                                pendingVent = false;
                                pendingExit = false;
                                StartCoroutine(TravelToRoom(currentRoom.neighbours[i], i));
                            }
                            return;
                        }
                }
                if (currentRoom.vent != null && hit.collider == currentRoom.vent && currentRoom.cleared)
                {
                    pendingVent = true;
                    pendingExit = false;
                    OrderSquad(currentRoom.vent.transform.position);
                    return;
                }
            }
            if (hitsGround)
            {
                Vector3 point = ray.GetPoint(distance);
                if (currentRoom.kind == RoomKind.Corridor)
                {
                    Room corridor = ClosestCorridor(point);
                    if (corridor != null)
                    {
                        Vector3 offset = ProjectOntoCorridor(corridor, point) - point;
                        offset.y = 0f;
                        if (offset.sqrMagnitude <= 1f)
                        {
                            OrderCorridorSquad(point, corridor);
                            return;
                        }
                    }
                }
                foreach (Room room in rooms)
                {
                    bool hallwayRoute = currentRoom.kind == RoomKind.Corridor && room.kind == RoomKind.Corridor;
                    if (room == currentRoom || (!room.visited && !hallwayRoute) || !CanLeave(room)) continue;
                    Vector3 target = point - room.center;
                    if (Mathf.Abs(target.x) > room.width * 0.5f || Mathf.Abs(target.z) > room.depthSize * 0.5f) continue;
                    TravelToClearedRoom(room);
                    return;
                }
                Vector3 local = point - currentRoom.center;
                if (Mathf.Abs(local.x) <= currentRoom.width * 0.5f && Mathf.Abs(local.z) <= currentRoom.depthSize * 0.5f)
                {
                    pendingVent = false;
                    pendingExit = false;
                    OrderSquad(point);
                }
            }
        }

        void OrderSquad(Vector3 point)
        {
            if (currentRoom != null && currentRoom.kind == RoomKind.Corridor)
            {
                OrderCorridorSquad(point, ClosestCorridor(point));
                return;
            }
            ClearWeaponOrder();
            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i].Dead) continue;
                Vector3 offset = Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 0.9f;
                agents[i].IssueMoveOrder(ClampToCurrentRoom(point + offset));
            }
        }

        Room autoObservedRoom;
        int autoObservedKills;
        float autoLastProgress, autoActionAt, autoPendingSince;
        Room autoLootRoom;
        int autoMoneyRemaining;
        float autoMoneyProgressAt;
        bool autoLootDone;

        sealed class AutoAgentProgress
        {
            public Vector3 position, goal;
            public float movedAt;
            public bool tracking;
        }
        readonly Dictionary<PlayerController, AutoAgentProgress> autoAgentProgress = new Dictionary<PlayerController, AutoAgentProgress>();

        void EnsureEmergencyDamageWeapon()
        {
            if (currentRoom == null || currentRoom.root == null || CanLeave(currentRoom) || EnemyCount <= 0 ||
                ExpeditionSession.Catalog == null || ExpeditionSession.Catalog.basicWeapon == null) return;
            PlayerController recipient = null;
            foreach (PlayerController agent in agents)
            {
                if (agent == null || agent.Dead) continue;
                if (recipient == null) recipient = agent;
                WeaponInstance weapon = agent.Weapons[0];
                if (weapon != null && (weapon.Definition.tags & WeaponTags.DPS) != 0) return;
            }
            if (recipient == null) return;
            foreach (ItemPickup pickup in currentRoom.root.GetComponentsInChildren<ItemPickup>())
                if (pickup.Available && pickup.IsEmergencyWeapon) return;
            ItemPickup fallback = SpawnWeaponPickup(new WeaponInstance(ExpeditionSession.Catalog.basicWeapon), recipient.transform.position);
            fallback.MarkEmergencyWeapon();
            BalanceLog("emergency_weapon_spawned", recipient, null, "no_offensive_weapon", fallback.DisplayName);
        }

        void UpdateAutoRun()
        {
            CheckAutoAgentProgress();
            if (currentRoom != autoObservedRoom || miceDefeated != autoObservedKills)
            {
                bool changedRoom = currentRoom != autoObservedRoom;
                autoObservedRoom = currentRoom;
                autoObservedKills = miceDefeated;
                autoLastProgress = Time.time;
                if (changedRoom) autoActionAt = Time.time + (currentRoom.kind == RoomKind.Corridor ? 0.5f : 5f);
            }
            // Non-damaging loadouts or blocked navigation must not stop a sampling batch forever.
            if (Time.time - autoLastProgress > 120f)
            {
                BalanceLog("auto_run_stalled", null, null, "no_progress", "", 120f);
                FinishBalanceTelemetry("interrupted");
                RestartAutoRun();
                return;
            }
            if (!CanLeave(currentRoom)) { autoLootRoom = null; return; }
            if (transitionDelay > 0f || Time.time < autoActionAt) return;
            if (currentRoom.kind != RoomKind.Corridor && WaitForAutoMoney()) return;
            if (pendingCorridorDestination != null)
            {
                if (Time.time - autoPendingSince < 20f) return;
                ClearPendingCorridorDoor();
            }
            if (pendingVent || pendingExit)
            {
                if (Time.time - autoPendingSince < 8f) return;
                pendingVent = pendingExit = false;
            }
            autoActionAt = Time.time + 1f;
            autoPendingSince = Time.time;
            if (currentRoom == bossRoom && currentRoom.bossDefeated)
            {
                if (currentRoom.vent != null) { pendingVent = true; OrderSquad(currentRoom.vent.transform.position); }
                else if (currentRoom.exitDoor != null) { pendingExit = true; OrderSquad(ExitApproach(currentRoom)); }
                return;
            }
            if (currentRoom.kind == RoomKind.Corridor)
            {
                var options = new List<Room>();
                foreach (Room room in rooms)
                    if (room.kind != RoomKind.Corridor && room != bossRoom && (!room.visited || !room.cleared)) options.Add(room);
                if (options.Count == 0 && bossRoom != null && BossRevealed()) options.Add(bossRoom);
                if (options.Count > 0) QueueCorridorDoor(options[Random.Range(0, options.Count)]);
                return;
            }
            var exits = new List<int>();
            for (int i = 0; i < currentRoom.neighbours.Length; i++)
                if (currentRoom.neighbours[i] != null && currentRoom.neighbours[i].kind == RoomKind.Corridor) exits.Add(i);
            if (exits.Count > 0)
            {
                int direction = exits[Random.Range(0, exits.Count)];
                StartCoroutine(TravelToRoom(currentRoom.neighbours[direction], direction));
            }
        }

        bool WaitForAutoMoney()
        {
            if (currentRoom.root == null) return false;
            int available = 0, collectible = 0;
            foreach (ItemPickup pickup in currentRoom.root.GetComponentsInChildren<ItemPickup>())
            {
                if (!pickup.Available || pickup.Kind != PickupKind.Money) continue;
                available++;
                if (PickupOwner(pickup) != null) collectible++;
            }
            if (autoLootRoom != currentRoom)
            {
                autoLootRoom = currentRoom;
                autoMoneyRemaining = available;
                autoMoneyProgressAt = Time.time;
                autoLootDone = false;
            }
            if (autoLootDone) return false;
            if (available < autoMoneyRemaining) autoMoneyProgressAt = Time.time;
            autoMoneyRemaining = available;
            if (collectible > 0 && Time.time - autoMoneyProgressAt < 20f) return true;
            if (available > 0)
                BalanceLog("auto_run_loot_unclaimed", null, null, collectible == 0 ? "no_eligible_agent" : "timeout", "Money", available);
            autoLootDone = true;
            return false;
        }

        void CheckAutoAgentProgress()
        {
            foreach (PlayerController agent in agents)
            {
                if (agent == null || agent.Dead) continue;
                if (!autoAgentProgress.TryGetValue(agent, out AutoAgentProgress state))
                    autoAgentProgress[agent] = state = new AutoAgentProgress();
                ItemPickup pickup = CanLeave(currentRoom) && agent.ReachedDestination ? SelectPickup(agent) : null;
                bool moving = !agent.ReachedDestination || pickup != null;
                Vector3 goal = !agent.ReachedDestination ? agent.MoveDestination : pickup != null ? pickup.transform.position : agent.transform.position;
                if (!moving) { state.tracking = false; continue; }
                if (!state.tracking || (state.goal - goal).sqrMagnitude > 0.5f * 0.5f ||
                    (state.position - agent.transform.position).sqrMagnitude > 0.2f * 0.2f)
                {
                    state.position = agent.transform.position;
                    state.goal = goal;
                    state.movedAt = Time.time;
                    state.tracking = true;
                    continue;
                }
                if (Time.time - state.movedAt < 5f) continue;
                if (TryFindAutoRecoveryPosition(agent, out Vector3 safe))
                {
                    float distance = Vector3.Distance(agent.transform.position, safe);
                    agent.RecoverNavigationPosition(safe, false);
                    BalanceLog("agent_unstuck", agent, null, pickup != null ? "auto_loot" : "auto_navigation", "", distance);
                }
                state.position = agent.transform.position;
                state.movedAt = Time.time;
            }
        }

        bool TryFindAutoRecoveryPosition(PlayerController agent, out Vector3 safe)
        {
            safe = agent.transform.position;
            var anchors = new List<Vector3>();
            foreach (PlayerController ally in agents)
                if (ally != null && ally != agent && !ally.Dead) anchors.Add(ally.transform.position);
            anchors.Add(agent.transform.position);
            anchors.Add(currentRoom.center);
            foreach (Vector3 anchor in anchors)
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI * 0.25f;
                    Vector3 candidate = anchor + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.2f;
                    if (!UnityEngine.AI.NavMesh.SamplePosition(candidate, out UnityEngine.AI.NavMeshHit hit, 1f, UnityEngine.AI.NavMesh.AllAreas)) continue;
                    Vector3 local = hit.position - currentRoom.center;
                    if (currentRoom.kind == RoomKind.Corridor)
                    {
                        Room corridor = ClosestCorridor(hit.position);
                        if (corridor == null || (ProjectOntoCorridor(corridor, hit.position) - hit.position).sqrMagnitude > 0.25f) continue;
                    }
                    else if (Mathf.Abs(local.x) > currentRoom.width * 0.5f - 0.6f || Mathf.Abs(local.z) > currentRoom.depthSize * 0.5f - 0.6f) continue;
                    CharacterController shape = agent.GetComponent<CharacterController>();
                    float radius = shape != null ? Mathf.Max(0.1f, shape.radius * 0.85f) : 0.35f;
                    float height = shape != null ? Mathf.Max(radius * 2f, shape.height) : 1.8f;
                    Vector3 bottom = hit.position + Vector3.up * (radius + 0.08f);
                    Vector3 top = hit.position + Vector3.up * (height - radius + 0.08f);
                    bool blocked = false;
                    foreach (Collider obstacle in Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore))
                        if (!obstacle.transform.IsChildOf(agent.transform)) { blocked = true; break; }
                    if (blocked) continue;
                    safe = hit.position + Vector3.up * 0.05f;
                    return true;
                }
            return false;
        }

        void RestartAutoRun()
        {
            ExpeditionSession.NextAutoRun();
            SceneManager.LoadScene("SampleScene");
        }

        void StopAutoRun()
        {
            FinishBalanceTelemetry("interrupted");
            SetFastForward(false);
            ExpeditionSession.StopAutoRun();
            SceneManager.LoadScene("Planning");
        }

        const float DeathShakeDuration = 0.25f;
        float deathShakeRemaining;
        Vector3 cameraShakeOffset;

        void ShakeCameraOnDeath() => deathShakeRemaining = DeathShakeDuration;

        void LateUpdate()
        {
            if (viewCamera == null) return;

            if (hexBackground != null)
            {
                float health = 0f, maximum = 0f;
                foreach (PlayerController agent in agents)
                    if (agent != null) { maximum += agent.MaxHealth; if (!agent.Dead) health += agent.Health; }
                hexBackground.SetHealthFraction(maximum > 0f ? health / maximum : 1f);
            }

            Transform cameraTransform = viewCamera.transform;
            Vector3 position = cameraTransform.position - cameraShakeOffset;
            if (player != null)
            {
                if (currentRoom != null && Mathf.Abs(viewCamera.aspect - cameraAspect) > 0.001f)
                    FitCameraToRoom(currentRoom);
                if (currentRoom != null && currentRoom.kind == RoomKind.Corridor)
                {
                    Vector3 center = Vector3.zero;
                    int living = 0;
                    foreach (PlayerController agent in agents)
                        if (agent != null && !agent.Dead) { center += agent.transform.position; living++; }
                    if (living > 0) { cameraFocus = center / living; cameraFocus.y = 0f; }
                }
                position = Vector3.Lerp(position, cameraFocus - cameraTransform.forward * cameraDistance,
                    1f - Mathf.Exp(-8f * Time.deltaTime));
            }

            cameraShakeOffset = Vector3.zero;
            if (deathShakeRemaining > 0f)
            {
                deathShakeRemaining = Mathf.Max(0f, deathShakeRemaining - Time.deltaTime);
                float remaining = deathShakeRemaining / DeathShakeDuration;
                float phase = (1f - remaining) * (4f * Mathf.PI);
                cameraShakeOffset = (cameraTransform.right * Mathf.Sin(phase) +
                    cameraTransform.up * (0.45f * Mathf.Sin(phase * 1.5f))) * (0.11f * remaining * remaining);
            }
            cameraTransform.position = position + cameraShakeOffset;
        }

        void OnDisable()
        {
            SetFastForward(false);
            if (viewCamera != null) viewCamera.transform.position -= cameraShakeOffset;
            cameraShakeOffset = Vector3.zero;
            deathShakeRemaining = 0f;
        }

    }
}
