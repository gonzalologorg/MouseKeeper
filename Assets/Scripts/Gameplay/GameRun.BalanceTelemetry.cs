using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        [Serializable] sealed class BalanceReport
        {
            public int schema_version = 3;
            public string run_id, started_utc, finished_utc, outcome, game_version, difficulty;
            public bool editor_build, debug_commands_used, auto_run;
            public int auto_run_number, auto_run_worker, auto_run_batch, auto_run_repeat, map_seed;
            public float difficulty_multiplier, duration_game_seconds, duration_real_seconds, duration_wall_seconds;
            public int configured_mice_per_room;
            public float configured_spawn_interval, base_room_width, base_room_depth;
            public int final_floor, rooms_cleared, mice_killed;
            public double final_score;
            public List<string> map_floors = new List<string>();
            public List<BalancePulse> pulse = new List<BalancePulse>();
            public List<BalanceAgent> agents = new List<BalanceAgent>();
            public List<BalanceRoom> rooms = new List<BalanceRoom>();
            public List<BalanceEnemy> enemies = new List<BalanceEnemy>();
            public List<BalanceEvent> events = new List<BalanceEvent>();
        }

        [Serializable] sealed class BalancePulse
        {
            public string mode;
            public int group_size, activations, kills;
            public float energy_spent, effective_damage, energy_per_kill, damage_per_activation;
        }

        [Serializable] sealed class BalanceAgent
        {
            public int slot, level, bravery, accuracy, agility, distrust, greed, teamwork, luck;
            public string name, specialty, initial_weapon, final_weapon;
            public float initial_max_health, initial_movement_speed, initial_attack_speed;
            public float final_health, final_max_health, damage_dealt, damage_overkill, damage_received, healing_received, healing_given;
            public float alive_game_seconds, down_game_seconds, combat_game_seconds, effective_damage_per_combat_second, hit_events_per_attack;
            public int initial_money, final_money, money_collected, money_spent, money_transferred_in, money_transferred_out, initial_karma, final_karma;
            public int attacks, hits, critical_hits, kills, deaths, revivals;
            public List<string> final_passives = new List<string>();
            public List<BalanceWeapon> weapons = new List<BalanceWeapon>();
        }

        [Serializable] sealed class BalanceWeapon
        {
            public string name;
            public int activations, hits, critical_hits, kills;
            public float effective_damage, overkill_damage;
            public float equipped_game_seconds, combat_equipped_game_seconds;
            public float base_damage, range, attack_interval, energy_cost, max_energy, reload_duration;
        }

        [Serializable] sealed class BalanceEnemy
        {
            public string preset, variant;
            public bool boss;
            public int spawned, killed;
            public float damage_received, damage_dealt;
        }

        [Serializable] sealed class BalanceRoom
        {
            public string id, type, layout;
            public int floor, grid_x, grid_y, visit, enemies_spawned, enemies_killed, agents_down;
            public float width, depth, started_game_seconds, started_real_seconds, duration_game_seconds, duration_real_seconds, combat_game_seconds;
            public bool boss, cleared_on_entry, cleared, escaped;
            public double score_on_entry, score_on_exit;
        }

        [Serializable] sealed class BalanceEvent
        {
            public float game_seconds, real_seconds, value, secondary_value;
            public int floor, agent_slot = -1, other_agent_slot = -1;
            public string room_id, type, source, target, detail;
            public bool critical;
            public float enemy_movement_speed, enemy_attack_interval;
            public int enemy_generation;
            public bool enemy_is_boss;
        }

        BalanceReport balanceReport;
        BalanceRoom balanceRoom;
        Room balanceRoomObject;
        string balancePath;
        float balanceNextCheckpoint;
        bool balanceFinalized;
        System.Diagnostics.Stopwatch balanceWallClock;
        readonly Dictionary<PlayerController, BalanceAgent> balanceAgents = new Dictionary<PlayerController, BalanceAgent>();
        readonly Dictionary<string, int> balanceRoomVisits = new Dictionary<string, int>();

        void InitializeBalanceTelemetry(string outputDirectory = null)
        {
            if (balanceReport != null) return;
            balanceWallClock = System.Diagnostics.Stopwatch.StartNew();
            DateTime now = DateTime.UtcNow;
            string id = now.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            balanceReport = new BalanceReport {
                run_id = id, started_utc = now.ToString("o"), outcome = "in_progress",
                game_version = Application.version, difficulty = ExpeditionSession.Difficulty.ToString(),
                auto_run = ExpeditionSession.AutoRun, auto_run_number = ExpeditionSession.AutoRun ? ExpeditionSession.AutoRunNumber : 0,
                auto_run_worker = ExpeditionSession.AutoRun ? ExpeditionSession.AutoRunWorker : 0,
                auto_run_batch = ExpeditionSession.AutoRun ? ExpeditionSession.AutoRunBatch + 1 : 0,
                auto_run_repeat = ExpeditionSession.AutoRun ? ExpeditionSession.AutoRunRepeat : 0,
                map_seed = ExpeditionSession.AutoRun ? ExpeditionSession.AutoRunMapSeed : 0,
                difficulty_multiplier = ExpeditionSession.DifficultyMultiplier(ExpeditionSession.Difficulty), editor_build = Application.isEditor,
                configured_mice_per_room = micePerRoom, configured_spawn_interval = mouseSpawnInterval,
                base_room_width = roomWidth, base_room_depth = roomDepth
            };
            for (int i = 0; i < agents.Count; i++)
            {
                PlayerController agent = agents[i];
                if (agent == null || agent.Profile == null) continue;
                AgentProfile p = agent.Profile;
                var entry = new BalanceAgent {
                    slot = i, name = p.name, level = p.Level, specialty = agent.Specialty.ToString(),
                    bravery = p.bravery, accuracy = p.accuracy, agility = p.agility, distrust = p.distrust,
                    greed = p.greed, teamwork = p.teamwork, luck = p.luck,
                    initial_max_health = agent.MaxHealth, initial_movement_speed = agent.MovementSpeed,
                    initial_attack_speed = p.attackSpeed, initial_money = agent.Money, initial_karma = agent.Karma,
                    initial_weapon = WeaponName(agent.Weapons[agent.SelectedWeapon])
                };
                balanceReport.agents.Add(entry);
                balanceAgents[agent] = entry;
            }
            try
            {
                const string outputOption = "--balance-output=";
                string outputArgument = Array.Find(Environment.GetCommandLineArgs(),
                    argument => argument.StartsWith(outputOption, StringComparison.Ordinal));
                if (outputDirectory == null && outputArgument?.Length > outputOption.Length)
                    outputDirectory = outputArgument.Substring(outputOption.Length);
#if UNITY_EDITOR
                string basePath = outputDirectory ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BalanceRuns"));
#else
                string basePath = outputDirectory ?? Path.Combine(Application.persistentDataPath, "BalanceRuns");
#endif
                Directory.CreateDirectory(basePath);
                balancePath = Path.Combine(basePath, "run_" + id + ".json");
                Debug.Log("Registro de balance: " + balancePath);
                SaveBalanceTelemetry();
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException || e is System.Security.SecurityException)
            {
                Debug.LogWarning("No se pudo crear el registro de balance: " + e.Message);
            }
        }

        void RecordBalanceFloorLayout()
        {
            if (balanceReport == null) return;
            var layout = new System.Text.StringBuilder();
            foreach (Room room in rooms)
            {
                int doors = 0;
                for (int i = 0; i < room.neighbours.Length; i++)
                    if (room.neighbours[i] != null) doors |= 1 << i;
                RoomLayout template = room.root.GetComponentInChildren<RoomLayout>();
                layout.Append(room.position).Append(':').Append(room.kind).Append(':')
                    .Append(room.width.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('x')
                    .Append(room.depthSize.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(':')
                    .Append(doors).Append(':').Append(room == bossRoom ? 'B' : '-').Append(':')
                    .Append(template != null ? template.name : "").Append(';');
            }
            balanceReport.map_floors.Add(layout.ToString());
            SaveBalanceTelemetry();
        }

        static string WeaponName(WeaponInstance weapon) => weapon != null && weapon.Definition != null ? weapon.Definition.name : "";
        static string EnemyName(MouseEnemy mouse) => mouse == null ? "" : mouse.Preset != null ? mouse.Preset.name : mouse.Variant.ToString();
        int BalanceSlot(PlayerController agent) => agent != null && balanceAgents.TryGetValue(agent, out BalanceAgent value) ? value.slot : -1;
        string BalanceRoomId(Room room) => room == null ? "" : (levelIndex + 1) + ":" + room.position.x + "," + room.position.y;

        void TrackBalanceRoom()
        {
            if (balanceReport == null || balanceFinalized || currentRoom == balanceRoomObject) return;
            CloseBalanceRoom();
            balanceRoomObject = currentRoom;
            if (currentRoom == null) return;
            string id = BalanceRoomId(currentRoom);
            balanceRoomVisits.TryGetValue(id, out int visits);
            balanceRoomVisits[id] = ++visits;
            RoomLayout layout = currentRoom.root != null ? currentRoom.root.GetComponentInChildren<RoomLayout>() : null;
            balanceRoom = new BalanceRoom {
                id = id, floor = levelIndex + 1, grid_x = currentRoom.position.x, grid_y = currentRoom.position.y,
                type = currentRoom.kind.ToString(), layout = layout != null ? layout.gameObject.name.Replace("(Clone)", "") : "",
                visit = visits, width = currentRoom.width, depth = currentRoom.depthSize, boss = currentRoom == bossRoom,
                started_game_seconds = balanceReport.duration_game_seconds, started_real_seconds = balanceReport.duration_real_seconds,
                cleared_on_entry = currentRoom.cleared, score_on_entry = score
            };
            balanceReport.rooms.Add(balanceRoom);
            BalanceLog("room_enter", null, null, "", "", visits, 0f);
        }

        void CloseBalanceRoom()
        {
            if (balanceRoom == null) return;
            balanceRoom.cleared = balanceRoomObject != null && balanceRoomObject.cleared;
            balanceRoom.score_on_exit = score;
            balanceRoom = null;
            balanceRoomObject = null;
        }

        void TickBalanceTelemetry(float gameDelta, float realDelta)
        {
            if (balanceReport == null || balanceFinalized) return;
            TrackBalanceRoom();
            balanceReport.duration_game_seconds += Mathf.Max(0f, gameDelta);
            balanceReport.duration_real_seconds += Mathf.Max(0f, realDelta);
            if (balanceRoom != null)
            {
                balanceRoom.duration_game_seconds += Mathf.Max(0f, gameDelta);
                balanceRoom.duration_real_seconds += Mathf.Max(0f, realDelta);
                if (currentRoom != null && currentRoom.kind == RoomKind.Combat && !currentRoom.cleared)
                    balanceRoom.combat_game_seconds += Mathf.Max(0f, gameDelta);
            }
            bool combat = currentRoom != null && currentRoom.kind == RoomKind.Combat && !currentRoom.cleared;
            foreach (var pair in balanceAgents)
            {
                PlayerController agent = pair.Key;
                if (agent == null) continue;
                BalanceAgent stats = pair.Value;
                if (agent.Dead) { stats.down_game_seconds += Mathf.Max(0f, gameDelta); continue; }
                stats.alive_game_seconds += Mathf.Max(0f, gameDelta);
                if (combat) stats.combat_game_seconds += Mathf.Max(0f, gameDelta);
                WeaponInstance currentWeapon = agent.Weapons[agent.SelectedWeapon];
                BalanceWeapon equipped = BalanceWeaponFor(stats, WeaponName(currentWeapon));
                if (equipped != null)
                {
                    DescribeBalanceWeapon(equipped, currentWeapon);
                    equipped.equipped_game_seconds += Mathf.Max(0f, gameDelta);
                    if (combat) equipped.combat_equipped_game_seconds += Mathf.Max(0f, gameDelta);
                }
            }
            if (balanceReport.duration_real_seconds >= balanceNextCheckpoint)
            {
                balanceNextCheckpoint = balanceReport.duration_real_seconds + 30f;
                SaveBalanceTelemetry();
            }
        }

        void BalanceLog(string type, PlayerController agent, PlayerController other, string source, string target,
            float value = 0f, float secondary = 0f, bool critical = false, string detail = "")
        {
            if (balanceReport == null || balanceFinalized) return;
            balanceReport.events.Add(new BalanceEvent {
                type = type, game_seconds = balanceReport.duration_game_seconds, real_seconds = balanceReport.duration_real_seconds,
                floor = levelIndex + 1, room_id = BalanceRoomId(currentRoom), agent_slot = BalanceSlot(agent),
                other_agent_slot = BalanceSlot(other), source = source ?? "", target = target ?? "", value = value,
                secondary_value = secondary, critical = critical, detail = detail ?? ""
            });
        }

        BalanceWeapon BalanceWeaponFor(BalanceAgent agent, string name)
        {
            if (agent == null || string.IsNullOrEmpty(name)) return null;
            foreach (BalanceWeapon entry in agent.weapons) if (entry.name == name) return entry;
            var created = new BalanceWeapon { name = name };
            agent.weapons.Add(created);
            return created;
        }

        static void DescribeBalanceWeapon(BalanceWeapon entry, WeaponInstance weapon)
        {
            if (entry == null || weapon == null || weapon.Definition == null) return;
            WeaponDefinition definition = weapon.Definition;
            entry.base_damage = definition.damage; entry.range = definition.range; entry.attack_interval = definition.attackInterval;
            entry.energy_cost = definition.primaryCost; entry.max_energy = definition.maxEnergy; entry.reload_duration = definition.reloadDuration;
        }

        BalanceEnemy BalanceEnemyFor(MouseEnemy mouse)
        {
            if (balanceReport == null || mouse == null) return null;
            string name = EnemyName(mouse), variant = mouse.Variant.ToString();
            bool boss = currentRoom != null && currentRoom.boss == mouse;
            foreach (BalanceEnemy entry in balanceReport.enemies)
                if (entry.preset == name && entry.variant == variant && entry.boss == boss) return entry;
            var created = new BalanceEnemy { preset = name, variant = variant, boss = boss };
            balanceReport.enemies.Add(created);
            return created;
        }

        internal void RecordBalanceAttack(PlayerController agent, WeaponInstance weapon, float energyCost)
        {
            if (balanceReport == null || balanceFinalized || !balanceAgents.TryGetValue(agent, out BalanceAgent stats)) return;
            stats.attacks++;
            string name = WeaponName(weapon);
            BalanceWeapon item = BalanceWeaponFor(stats, name);
            if (item != null) { DescribeBalanceWeapon(item, weapon); item.activations++; }
            if (weapon.Definition is PulseWeaponDefinition)
            {
                weapon.PulseGroupSize = 0;
                if (CurrentRoom != null)
                    foreach (MouseEnemy mouse in CurrentRoom.GetComponentsInChildren<MouseEnemy>())
                        if (mouse.IsTargetable) weapon.PulseGroupSize++;

                BalancePulse pulse = PulseStats(weapon);
                pulse.activations++;
                pulse.energy_spent += energyCost;
                BalanceLog("weapon_activation", agent, null, name, "", energyCost, detail: pulse.mode);
            }
            else BalanceLog("weapon_activation", agent, null, name, "", energyCost);
        }

        BalancePulse PulseStats(WeaponInstance weapon)
        {
            string mode = weapon.SecondaryAttack ? "secondary" : "primary";
            foreach (BalancePulse row in balanceReport.pulse)
                if (row.mode == mode && row.group_size == weapon.PulseGroupSize) return row;
            var created = new BalancePulse { mode = mode, group_size = weapon.PulseGroupSize };
            balanceReport.pulse.Add(created);
            return created;
        }

        internal void RecordBalanceEnemyDamage(PlayerController agent, MouseEnemy mouse, WeaponInstance weapon,
            WeaponInstance attributionWeapon, float effective, float overkill, bool critical, string source)
        {
            if (balanceReport == null || balanceFinalized) return;
            if (agent != null && balanceAgents.TryGetValue(agent, out BalanceAgent stats))
            {
                stats.damage_dealt += effective; stats.damage_overkill += overkill;
                if (weapon != null) { stats.hits++; if (critical) stats.critical_hits++; }
                BalanceWeapon item = BalanceWeaponFor(stats, WeaponName(weapon ?? attributionWeapon));
                if (item != null) { if (weapon != null) item.hits++; item.effective_damage += effective; item.overkill_damage += overkill; if (critical) item.critical_hits++; }
            }
            if (weapon?.Definition is PulseWeaponDefinition) PulseStats(weapon).effective_damage += effective;
            BalanceEnemy enemy = BalanceEnemyFor(mouse);
            if (enemy != null) enemy.damage_received += effective;
            BalanceLog("enemy_damage", agent, null, weapon != null ? WeaponName(weapon) : source, EnemyName(mouse), effective, overkill, critical,
                (mouse != null ? mouse.Variant.ToString() : "") + (attributionWeapon != null ? "|weapon:" + WeaponName(attributionWeapon) : ""));
        }

        internal void RecordBalanceDamageReceived(PlayerController agent, MouseEnemy attacker, float effective, float overkill, string source)
        {
            if (balanceReport == null || balanceFinalized) return;
            if (agent != null && balanceAgents.TryGetValue(agent, out BalanceAgent stats)) stats.damage_received += effective;
            BalanceEnemy enemy = BalanceEnemyFor(attacker);
            if (enemy != null) enemy.damage_dealt += effective;
            BalanceLog("agent_damage", agent, null, attacker != null ? EnemyName(attacker) : source, "", effective, overkill, detail: source);
        }

        internal void RecordBalanceHeal(PlayerController recipient, PlayerController healer, float effective, string source)
        {
            if (balanceReport == null || balanceFinalized || effective <= 0f) return;
            if (recipient != null && balanceAgents.TryGetValue(recipient, out BalanceAgent target)) target.healing_received += effective;
            if (healer != null && balanceAgents.TryGetValue(healer, out BalanceAgent actor)) actor.healing_given += effective;
            BalanceLog("heal", recipient, healer, source, "", effective);
        }

        internal void RecordBalanceMoney(PlayerController agent, int amount, bool spent)
        {
            if (balanceReport == null || balanceFinalized || agent == null) return;
            if (balanceAgents.TryGetValue(agent, out BalanceAgent stats))
            {
                if (spent) stats.money_spent += amount; else stats.money_collected += amount;
            }
            BalanceLog(spent ? "money_spent" : "money_collected", agent, null, "", "", amount);
        }

        internal void RecordNavigationReplan(PlayerController agent) =>
            BalanceLog("navigation_replan", agent, null, "door_stall", "", 2f);

        internal void RecordBalanceKarma(PlayerController agent, int change, int current)
        {
            BalanceLog("karma_change", agent, null, "", "", change, current);
        }

        internal void RecordBalanceTransfer(PlayerController from, PlayerController to, int amount)
        {
            if (balanceReport == null || balanceFinalized || amount <= 0) return;
            if (balanceAgents.TryGetValue(from, out BalanceAgent donor)) donor.money_transferred_out += amount;
            if (balanceAgents.TryGetValue(to, out BalanceAgent recipient)) recipient.money_transferred_in += amount;
            BalanceLog("money_inheritance", to, from, "", "", amount);
        }

        internal void RecordBalancePickup(PlayerController agent, PickupKind kind, string name, int price)
        {
            BalanceLog(price > 0 ? "purchase" : "pickup", agent, null, kind.ToString(), name, price);
        }

        internal void RecordBalanceStatus(MouseEnemy mouse, string status, float duration)
        {
            BalanceLog("enemy_status", null, null, status, EnemyName(mouse), duration,
                detail: mouse != null ? mouse.Variant.ToString() : "");
        }

        void RecordBalanceSpawn(Room room, MouseEnemy mouse, bool boss)
        {
            if (balanceReport == null || balanceFinalized) return;
            TrackBalanceRoom();
            BalanceEnemy entry = BalanceEnemyFor(mouse);
            if (entry != null) entry.spawned++;
            if (balanceRoom != null && room == balanceRoomObject) balanceRoom.enemies_spawned++;
            BalanceLog("enemy_spawn", null, null, EnemyName(mouse), mouse.Variant.ToString(), mouse.maxHealth, mouse.attackDamage,
                detail: boss ? "boss" : "generation:" + mouse.SplitGeneration);
            BalanceEvent spawn = balanceReport.events[balanceReport.events.Count - 1];
            spawn.enemy_movement_speed = mouse.movementSpeed;
            spawn.enemy_attack_interval = mouse.attackInterval;
            spawn.enemy_generation = mouse.SplitGeneration;
            spawn.enemy_is_boss = boss;
        }

        internal void RecordBalanceKill(PlayerController agent, MouseEnemy mouse, WeaponInstance weapon)
        {
            if (balanceReport == null || balanceFinalized) return;
            if (agent != null && balanceAgents.TryGetValue(agent, out BalanceAgent stats))
            {
                stats.kills++;
                BalanceWeapon item = BalanceWeaponFor(stats, WeaponName(weapon));
                if (item != null) item.kills++;
            }
            if (weapon?.Definition is PulseWeaponDefinition) PulseStats(weapon).kills++;
            BalanceEnemy enemy = BalanceEnemyFor(mouse);
            if (enemy != null) enemy.killed++;
            if (balanceRoom != null) balanceRoom.enemies_killed++;
            BalanceLog("enemy_killed", agent, null, WeaponName(weapon), EnemyName(mouse), detail: mouse.Variant.ToString());
        }

        void RecordBalanceDeath(PlayerController agent)
        {
            if (balanceReport == null || balanceFinalized) return;
            if (agent != null && balanceAgents.TryGetValue(agent, out BalanceAgent stats)) stats.deaths++;
            if (balanceRoom != null) balanceRoom.agents_down++;
            BalanceLog("agent_down", agent, null, "", "");
        }

        void RecordBalanceRevival(PlayerController agent, PlayerController helper, string source)
        {
            if (balanceReport == null || balanceFinalized) return;
            if (agent != null && balanceAgents.TryGetValue(agent, out BalanceAgent stats)) stats.revivals++;
            BalanceLog("agent_revived", agent, helper, source, "", agent != null ? agent.Health : 0f);
        }

        internal void RecordBalancePassiveRevival(PlayerController agent) => RecordBalanceRevival(agent, agent, "revive_passive");

        void SnapshotBalanceTelemetry()
        {
            if (balanceReport == null) return;
            foreach (BalancePulse pulse in balanceReport.pulse)
            {
                pulse.energy_per_kill = pulse.kills > 0 ? pulse.energy_spent / pulse.kills : 0f;
                pulse.damage_per_activation = pulse.activations > 0 ? pulse.effective_damage / pulse.activations : 0f;
            }
            if (balanceWallClock != null) balanceReport.duration_wall_seconds = (float)balanceWallClock.Elapsed.TotalSeconds;
            balanceReport.final_floor = levelIndex + 1;
            balanceReport.final_score = score;
            balanceReport.rooms_cleared = clearedCombatRooms;
            balanceReport.mice_killed = miceDefeated;
            foreach (var pair in balanceAgents)
            {
                PlayerController agent = pair.Key;
                if (agent == null) continue;
                BalanceAgent stats = pair.Value;
                stats.final_health = agent.Health; stats.final_max_health = agent.MaxHealth;
                stats.final_money = agent.Money; stats.final_karma = agent.Karma;
                stats.final_weapon = WeaponName(agent.Weapons[agent.SelectedWeapon]);
                stats.effective_damage_per_combat_second = stats.combat_game_seconds > 0f ? stats.damage_dealt / stats.combat_game_seconds : 0f;
                stats.hit_events_per_attack = stats.attacks > 0 ? (float)stats.hits / stats.attacks : 0f;
                stats.final_passives.Clear();
                foreach (PassiveItemDefinition passive in agent.Passives) if (passive != null) stats.final_passives.Add(passive.displayName);
            }
            if (balanceRoom != null)
            {
                balanceRoom.cleared = balanceRoomObject != null && balanceRoomObject.cleared;
                balanceRoom.score_on_exit = score;
            }
        }

        void SaveBalanceTelemetry()
        {
            if (balanceReport == null || string.IsNullOrEmpty(balancePath)) return;
            try
            {
                SnapshotBalanceTelemetry();
                string temporary = balancePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(balanceReport, true));
                if (File.Exists(balancePath)) File.Replace(temporary, balancePath, null);
                else File.Move(temporary, balancePath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException || e is System.Security.SecurityException)
            {
                Debug.LogWarning("No se pudo guardar el registro de balance: " + e.Message);
            }
        }

        void FinishBalanceTelemetry(string outcome)
        {
            if (balanceReport == null || balanceFinalized) return;
            TrackBalanceRoom();
            balanceReport.outcome = outcome;
            balanceReport.finished_utc = DateTime.UtcNow.ToString("o");
            balanceWallClock?.Stop();
            SnapshotBalanceTelemetry();
            CloseBalanceRoom();
            balanceFinalized = true;
            SaveBalanceTelemetry();
        }

        void OnApplicationPause(bool paused) { if (paused) SaveBalanceTelemetry(); }
        void OnApplicationQuit()
        {
            FinishBalanceTelemetry("interrupted");
            if (ExpeditionSession.AutoRun) ExpeditionSession.StopAutoRun();
        }
    }
}
