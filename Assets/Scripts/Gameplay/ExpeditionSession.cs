using System;
using System.Collections.Generic;
using UnityEngine;

namespace MouseKeeper
{
    public enum ExpeditionDifficulty { Normal, Easy, Hard, Swarm }
    public enum Specialty { None, Dps, Medic, Tank, Locksmith, Bard }

    [Serializable]
    public sealed class AgentProgress
    {
        public int bravery, accuracy, agility, distrust, greed, teamwork, luck;
        public int pending;
        public int expeditionsCompleted;
        public int ratsKilled;
        public int moneyCollected;
        public int Total => bravery + accuracy + agility + distrust + greed + teamwork + luck;
        public int Level => Mathf.Clamp(expeditionsCompleted, 1, AgentProfile.MaxLevel);
        public int RemainingUpgrades => Mathf.Max(0, AgentProfile.MaxLevel - 1 - Total);
    }

    [Serializable]
    public sealed class CampaignData
    {
        public int money;
        public int[] inventory;
        public int[] unavailable;
        public int[] selectedAgents;
        public int[] selectedWeapons;
        public int[] selectedMoney;
        public Specialty[] specialties;
        public string[] agentKeys;
        public string[] weaponKeys;
        public AgentProgress[] progress;
        public int upgradePoints;
        public int progressionVersion;
        public bool active;
        public ExpeditionDifficulty difficulty;
        public string lastResult;
        public string lastResultKey;
        public int[] lastResultValues;
        public double lastScore;
        public bool hasLastScore;
    }

    public static class ExpeditionSession
    {
        public const int SquadSize = 3;
        public const int FloorCount = 3;
        public const int LossLimit = 6;
        const string SaveKey = "MouseKeeper.Campaign.v1";
        static ExpeditionCatalog catalog;
        static CampaignData data;
        static CampaignData campaignBeforeAutoRun;
        static float timeScaleBeforeAutoRun;
        public static bool AutoRun { get; private set; }
        public static int AutoRunNumber { get; private set; }

        public static ExpeditionCatalog Catalog => catalog;
        public static CampaignData Data => data;
        public static bool Active => data != null && data.active;
        public static ExpeditionDifficulty Difficulty => Active ? data.difficulty : ExpeditionDifficulty.Normal;

        public static float DifficultyMultiplier(ExpeditionDifficulty difficulty) => difficulty switch
        {
            ExpeditionDifficulty.Easy => 0.5f,
            ExpeditionDifficulty.Hard => 1.5f,
            ExpeditionDifficulty.Swarm => 2.5f,
            _ => 1f
        };

        public static float EnemyCountMultiplier(ExpeditionDifficulty difficulty) =>
            difficulty == ExpeditionDifficulty.Swarm ? 2f : DifficultyMultiplier(difficulty);

        public static float BossDifficultyMultiplier(ExpeditionDifficulty difficulty) => difficulty switch
        {
            ExpeditionDifficulty.Easy => 0.5f,
            ExpeditionDifficulty.Hard => 1.25f,
            ExpeditionDifficulty.Swarm => 1.5f,
            _ => 0.75f
        };

        public static int ExtraRooms(ExpeditionDifficulty difficulty) => difficulty switch
        {
            ExpeditionDifficulty.Hard => 1,
            ExpeditionDifficulty.Swarm => 2,
            _ => 0
        };
        public static int Losses(CampaignData campaign)
        {
            int count = 0;
            foreach (int missing in campaign.unavailable) if (missing > 0) count++;
            return count;
        }
        public static bool GameOver => data != null && Losses(data) >= LossLimit;
        public static int HireCost(AgentDefinition agent) => agent.freeRecruit ? 0 : Mathf.Max(0, agent.missionCost);
        public static bool CanSelect(CampaignData campaign, ExpeditionCatalog source, int index) =>
            source.agents[index].freeRecruit || campaign.unavailable[index] == 0;

        public static bool CanHireForSlot(CampaignData campaign, ExpeditionCatalog source, int index, int slot)
        {
            if (campaign.active || slot < 0 || slot >= SquadSize || index < 0 ||
                index >= source.agents.Length || !CanSelect(campaign, source, index)) return false;

            int assignedSlot = Array.IndexOf(campaign.selectedAgents, index);
            return (assignedSlot < 0 || assignedSlot == slot) &&
                HireCost(source.agents[index]) <= HiringBudgetForSlot(campaign, source, slot);
        }

        public static long HiringBudgetForSlot(CampaignData campaign, ExpeditionCatalog source, int slot)
        {
            long budget = campaign.money;
            for (int i = 0; i < SquadSize; i++)
            {
                budget -= campaign.selectedMoney[i];
                if (i != slot) budget -= HireCost(source.agents[campaign.selectedAgents[i]]);
            }
            return budget;
        }

        public static AgentProfile Profile(CampaignData campaign, ExpeditionCatalog source, int index)
        {
            AgentProfile profile = source.agents[index].profile.Copy();
            AgentProgress progress = campaign.progress[index];
            profile.bravery += progress.bravery;
            profile.accuracy += progress.accuracy;
            profile.agility += progress.agility;
            profile.distrust += progress.distrust;
            profile.greed += progress.greed;
            profile.teamwork += progress.teamwork;
            profile.luck = Mathf.Clamp(profile.luck, 1, 10) + progress.luck;
            profile.healthMultiplier = 1f + 0.1f * progress.Total;
            profile.speedMultiplier = 1f + 0.05f * progress.Total;
            profile.Level = progress.Level;
            return profile;
        }

        public static bool CanUpgrade(CampaignData campaign, ExpeditionCatalog source, int index, AgentProfile profile)
        {
            AgentProgress progress = campaign.progress[index];
            return !campaign.active && Losses(campaign) < LossLimit && CanSelect(campaign, source, index) &&
                progress.RemainingUpgrades > 0 && (progress.pending > 0 || campaign.upgradePoints > 0) &&
                (profile.bravery < 10 || profile.accuracy < 10 || profile.agility < 10 ||
                    profile.distrust < 10 || profile.greed < 10 || profile.teamwork < 10 || profile.luck < 10);
        }

        public static bool Upgrade(CampaignData campaign, ExpeditionCatalog source, int index, int skill)
        {
            if (index < 0 || index >= source.agents.Length) return false;
            AgentProfile profile = Profile(campaign, source, index);
            if (!CanUpgrade(campaign, source, index, profile)) return false;
            AgentProgress progress = campaign.progress[index];
            switch (skill)
            {
                case 0: if (profile.bravery >= 10) return false; progress.bravery++; break;
                case 1: if (profile.accuracy >= 10) return false; progress.accuracy++; break;
                case 2: if (profile.agility >= 10) return false; progress.agility++; break;
                case 3: if (profile.distrust >= 10) return false; progress.distrust++; break;
                case 4: if (profile.greed >= 10) return false; progress.greed++; break;
                case 5: if (profile.teamwork >= 10) return false; progress.teamwork++; break;
                case 6: if (profile.luck >= 10) return false; progress.luck++; break;
                default: return false;
            }
            if (progress.pending > 0) progress.pending--;
            else campaign.upgradePoints--;
            return true;
        }

        public static bool Initialize()
        {
            if (data != null && catalog != null) return true;
            catalog = Resources.Load<ExpeditionCatalog>("ExpeditionCatalog");
            if (catalog == null || catalog.agents == null || catalog.agents.Length < SquadSize ||
                catalog.weapons == null || catalog.weapons.Length == 0 ||
                catalog.basicWeapon == null || Array.IndexOf(catalog.weapons, catalog.basicWeapon) < 0)
            {
                Debug.LogError("El catálogo de expedición no está configurado.");
                return false;
            }
            foreach (AgentDefinition agent in catalog.agents)
                if (agent == null || agent.profile == null) { Debug.LogError("El catálogo contiene un agente vacío."); return false; }
            int missingWeapon = Array.FindIndex(catalog.weapons, weapon => weapon == null);
            if (missingWeapon >= 0)
            {
                Debug.LogError($"El catálogo contiene un arma vacía en weapons[{missingWeapon}].", catalog);
                return false;
            }
            bool migrated = false;
            if (PlayerPrefs.HasKey(SaveKey))
            {
                try { data = JsonUtility.FromJson<CampaignData>(PlayerPrefs.GetString(SaveKey)); }
                catch (ArgumentException error) { Debug.LogError("No se pudo leer la campaña: " + error.Message); return false; }
                migrated = data != null && data.progressionVersion < 2;
                if (data != null && data.selectedMoney == null) data.selectedMoney = new int[SquadSize];
                if (data != null && data.specialties == null) { data.specialties = new Specialty[SquadSize]; migrated = true; }
                if (data == null || data.inventory == null || data.unavailable == null ||
                    data.selectedAgents == null || data.selectedAgents.Length != SquadSize ||
                    data.selectedWeapons == null || data.selectedWeapons.Length != SquadSize ||
                    data.selectedMoney.Length != SquadSize || data.specialties.Length != SquadSize ||
                    !ReconcileCatalog(data, catalog))
                {
                    Debug.LogError("La campaña guardada es incompatible con el catálogo.");
                    data = null;
                    return false;
                }
            }
            else
            {
                data = NewCampaign(catalog);
                if (!ReconcileCatalog(data, catalog))
                {
                    Debug.LogError("El catálogo contiene nombres de assets repetidos.");
                    data = null;
                    return false;
                }
                Save();
            }
            bool changed = migrated;
            if (!data.active && !GameOver) changed |= EnsureAvailableAgents(data);
            if (!data.active) changed |= EnsureAvailableWeapons(data, catalog);
            if (changed) Save();
            return true;
        }

        public static bool EnsureAvailableAgents(CampaignData campaign)
        {
            bool changed = false;
            var used = new HashSet<int>();
            int budget = Mathf.Max(0, campaign.money);
            for (int i = 0; i < campaign.selectedMoney.Length; i++)
            {
                int amount = Mathf.Clamp(campaign.selectedMoney[i], 0, budget);
                if (amount != campaign.selectedMoney[i]) { campaign.selectedMoney[i] = amount; changed = true; }
                budget -= amount;
            }
            for (int slot = 0; slot < SquadSize; slot++)
            {
                int current = campaign.selectedAgents[slot];
                if (current >= 0 && current < catalog.agents.Length && !used.Contains(current) &&
                    CanSelect(campaign, catalog, current) && HireCost(catalog.agents[current]) <= budget)
                {
                    used.Add(current);
                    budget -= HireCost(catalog.agents[current]);
                    continue;
                }
                int candidate = -1;
                for (int i = 0; i < catalog.agents.Length; i++)
                    if (!used.Contains(i) && CanSelect(campaign, catalog, i) &&
                        HireCost(catalog.agents[i]) <= budget &&
                        (candidate < 0 || HireCost(catalog.agents[i]) < HireCost(catalog.agents[candidate])))
                        candidate = i;
                if (candidate < 0) { Debug.LogError("No hay suficientes agentes asequibles para completar el escuadrón."); return changed; }
                campaign.selectedAgents[slot] = candidate;
                used.Add(candidate);
                budget -= HireCost(catalog.agents[candidate]);
                changed = true;
            }
            return changed;
        }

        public static CampaignData NewCampaign(ExpeditionCatalog source)
        {
            int basic = Array.IndexOf(source.weapons, source.basicWeapon);
            var campaign = new CampaignData {
                inventory = new int[source.weapons.Length],
                unavailable = new int[source.agents.Length],
                selectedAgents = new[] { 0, 1, 2 },
                selectedWeapons = new[] { basic, basic, basic },
                selectedMoney = new int[SquadSize],
                specialties = new Specialty[SquadSize],
                progress = Array.ConvertAll(source.agents, agent => new AgentProgress()),
                progressionVersion = 2,
                agentKeys = Array.ConvertAll(source.agents, agent => agent.name),
                weaponKeys = Array.ConvertAll(source.weapons, weapon => weapon.name)
            };
            return campaign;
        }

        public static bool ReconcileCatalog(CampaignData campaign, ExpeditionCatalog source)
        {
            if (campaign.agentKeys == null || campaign.weaponKeys == null ||
                campaign.agentKeys.Length != campaign.unavailable.Length ||
                campaign.weaponKeys.Length != campaign.inventory.Length) return false;
            if (new HashSet<string>(campaign.agentKeys).Count != campaign.agentKeys.Length ||
                new HashSet<string>(campaign.weaponKeys).Count != campaign.weaponKeys.Length) return false;
            if (campaign.progress == null)
                campaign.progress = Array.ConvertAll(campaign.agentKeys, key => new AgentProgress());
            if (campaign.progress.Length != campaign.agentKeys.Length) return false;
            if (campaign.upgradePoints < 0 || campaign.progressionVersion < 0 || campaign.progressionVersion > 2) return false;
            foreach (AgentProgress progress in campaign.progress)
                if (progress == null || progress.pending < 0 || progress.bravery < 0 || progress.accuracy < 0 ||
                    progress.agility < 0 || progress.distrust < 0 || progress.greed < 0 || progress.teamwork < 0 || progress.luck < 0 ||
                    progress.expeditionsCompleted < 0 || progress.ratsKilled < 0 || progress.moneyCollected < 0) return false;
            string[] agentKeys = Array.ConvertAll(source.agents, agent => agent.name);
            string[] weaponKeys = Array.ConvertAll(source.weapons, weapon => weapon.name);
            if (new HashSet<string>(agentKeys).Count != agentKeys.Length ||
                new HashSet<string>(weaponKeys).Count != weaponKeys.Length) return false;
            var agents = new int[campaign.agentKeys.Length];
            var weapons = new int[campaign.weaponKeys.Length];
            for (int i = 0; i < agents.Length; i++)
                if ((agents[i] = Array.IndexOf(agentKeys, campaign.agentKeys[i])) < 0 || campaign.unavailable[i] < 0) return false;
            for (int i = 0; i < weapons.Length; i++)
                if ((weapons[i] = Array.IndexOf(weaponKeys, campaign.weaponKeys[i])) < 0 || campaign.inventory[i] < 0) return false;
            var selectedAgents = new int[SquadSize];
            var selectedWeapons = new int[SquadSize];
            for (int i = 0; i < SquadSize; i++)
            {
                int agent = campaign.selectedAgents[i];
                int weapon = campaign.selectedWeapons[i];
                if (agent < 0 || agent >= agents.Length || weapon < 0 || weapon >= weapons.Length) return false;
                selectedAgents[i] = agents[agent];
                selectedWeapons[i] = weapons[weapon];
            }
            if (campaign.progressionVersion < 2) campaign.progressionVersion = 2;
            var unavailable = new int[agentKeys.Length];
            var progressByAgent = Array.ConvertAll(agentKeys, key => new AgentProgress());
            var inventory = new int[weaponKeys.Length];
            for (int i = 0; i < agents.Length; i++)
            {
                unavailable[agents[i]] = campaign.unavailable[i];
                progressByAgent[agents[i]] = campaign.progress[i];
            }
            for (int i = 0; i < weapons.Length; i++) inventory[weapons[i]] = campaign.inventory[i];
            campaign.agentKeys = agentKeys;
            campaign.weaponKeys = weaponKeys;
            campaign.unavailable = unavailable;
            campaign.progress = progressByAgent;
            campaign.inventory = inventory;
            campaign.selectedAgents = selectedAgents;
            campaign.selectedWeapons = selectedWeapons;
            return true;
        }

        public static bool CanLaunch(CampaignData campaign, ExpeditionCatalog source, out string error)
        {
            if (campaign.specialties == null) campaign.specialties = new Specialty[SquadSize];
            if (campaign.specialties.Length != SquadSize) { error = "Especialidades inválidas."; return false; }
            var usedSpecialties = new HashSet<Specialty>();
            foreach (Specialty specialty in campaign.specialties)
                if ((int)specialty < 0 || (int)specialty > (int)Specialty.Bard ||
                    (specialty != Specialty.None && !usedSpecialties.Add(specialty)))
                { error = "Las especialidades deben ser distintas."; return false; }
            var usedAgents = new HashSet<int>();
            var usedWeapons = new int[source.weapons.Length];
            int basic = Array.IndexOf(source.weapons, source.basicWeapon);
            long assignedMoney = 0;
            long hireCost = 0;
            if (Losses(campaign) >= LossLimit) { error = "La campaña ha terminado."; return false; }
            for (int i = 0; i < SquadSize; i++)
            {
                int agent = campaign.selectedAgents[i];
                int weapon = campaign.selectedWeapons[i];
                if (agent < 0 || agent >= source.agents.Length || !usedAgents.Add(agent) || !CanSelect(campaign, source, agent))
                { error = "Selecciona tres agentes disponibles y distintos."; return false; }
                hireCost += HireCost(source.agents[agent]);
                if (weapon < 0 || weapon >= usedWeapons.Length ||
                    (weapon != basic && ++usedWeapons[weapon] > campaign.inventory[weapon]))
                { error = "No hay suficientes armas para el escuadrón."; return false; }
                if (campaign.selectedMoney[i] < 0) { error = "El dinero asignado no puede ser negativo."; return false; }
                assignedMoney += campaign.selectedMoney[i];
            }
            if (assignedMoney + hireCost > campaign.money) { error = "No hay dinero suficiente para contratar y equipar al escuadrón."; return false; }
            error = null;
            return true;
        }

        public static int AvailableForSlot(CampaignData campaign, ExpeditionCatalog source, int weapon, int selectedSlot)
        {
            if (source.weapons[weapon] == source.basicWeapon) return int.MaxValue;
            int available = campaign.inventory[weapon];
            for (int i = 0; i < SquadSize; i++)
                if (i != selectedSlot && campaign.selectedWeapons[i] == weapon) available--;
            return Mathf.Max(0, available);
        }

        public static bool EnsureAvailableWeapons(CampaignData campaign, ExpeditionCatalog source)
        {
            int basic = Array.IndexOf(source.weapons, source.basicWeapon);
            var used = new int[source.weapons.Length];
            bool changed = false;
            for (int i = 0; i < SquadSize; i++)
            {
                int weapon = campaign.selectedWeapons[i];
                if (weapon == basic) continue;
                if (weapon < 0 || weapon >= used.Length)
                {
                    Debug.LogError("La selección de armas contiene un índice inválido.");
                    continue;
                }
                if (++used[weapon] <= campaign.inventory[weapon]) continue;
                campaign.selectedWeapons[i] = basic;
                changed = true;
            }
            return changed;
        }

        public static bool RandomizePlan(CampaignData campaign, ExpeditionCatalog source)
        {
            var availableAgents = new List<int>();
            for (int i = 0; i < source.agents.Length; i++)
                if (CanSelect(campaign, source, i) && HireCost(source.agents[i]) <= campaign.money) availableAgents.Add(i);
            if (availableAgents.Count < SquadSize)
            {
                Debug.LogError("No hay suficientes agentes disponibles para planificar.");
                return false;
            }
            int basic = Array.IndexOf(source.weapons, source.basicWeapon);
            int[] remaining = (int[])campaign.inventory.Clone();
            int budget = campaign.money;
            for (int i = 0; i < SquadSize; i++)
            {
                var candidates = availableAgents.FindAll(index => HireCost(source.agents[index]) <= budget);
                if (candidates.Count == 0) { Debug.LogError("No hay suficientes agentes asequibles para planificar."); return false; }
                int selectedAgent = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                availableAgents.Remove(selectedAgent);
                campaign.selectedAgents[i] = selectedAgent;
                budget -= HireCost(source.agents[selectedAgent]);
                var availableWeapons = new List<int> { basic };
                for (int weapon = 0; weapon < remaining.Length; weapon++)
                    if (weapon != basic && remaining[weapon] > 0) availableWeapons.Add(weapon);
                int selected = availableWeapons[UnityEngine.Random.Range(0, availableWeapons.Count)];
                campaign.selectedWeapons[i] = selected;
                if (selected != basic) remaining[selected]--;
                campaign.selectedMoney[i] = 0;
            }
            return true;
        }

        public static bool Launch(out string error) => Launch(ExpeditionDifficulty.Normal, out error);

        public static bool StartAutoRun(out string error)
        {
            if (AutoRun || !Initialize()) { error = "No se pudo iniciar Auto-run."; return false; }
            campaignBeforeAutoRun = data;
            timeScaleBeforeAutoRun = Time.timeScale;
            AutoRun = true;
            AutoRunNumber = 0;
            NextAutoRun();
            error = null;
            return true;
        }

        public static void NextAutoRun()
        {
            if (!AutoRun) return;
            data = NewCampaign(catalog);
            var availableAgents = new List<int>();
            var availableSpecialties = new List<Specialty> { Specialty.Dps, Specialty.Medic, Specialty.Tank, Specialty.Locksmith, Specialty.Bard };
            var availableWeapons = new List<int>();
            for (int i = 0; i < catalog.weapons.Length; i++) availableWeapons.Add(i);
            for (int i = 0; i < catalog.agents.Length; i++) availableAgents.Add(i);
            for (int i = 0; i < SquadSize; i++)
            {
                int agent = UnityEngine.Random.Range(0, availableAgents.Count);
                data.selectedAgents[i] = availableAgents[agent];
                availableAgents.RemoveAt(agent);
                if (availableWeapons.Count == 0)
                    for (int w = 0; w < catalog.weapons.Length; w++) availableWeapons.Add(w);
                int weapon = UnityEngine.Random.Range(0, availableWeapons.Count);
                data.selectedWeapons[i] = availableWeapons[weapon];
                availableWeapons.RemoveAt(weapon);
                int specialty = UnityEngine.Random.Range(0, availableSpecialties.Count);
                data.specialties[i] = availableSpecialties[specialty];
                availableSpecialties.RemoveAt(specialty);
            }
            bool hasDamage = false;
            foreach (int index in data.selectedWeapons)
                if ((catalog.weapons[index].tags & WeaponTags.DPS) != 0) hasDamage = true;
            if (!hasDamage) data.selectedWeapons[UnityEngine.Random.Range(0, SquadSize)] = Array.IndexOf(catalog.weapons, catalog.basicWeapon);
            data.difficulty = (ExpeditionDifficulty)UnityEngine.Random.Range(0, 4);
            data.active = true;
            AutoRunNumber++;
            Time.timeScale = 3f;
        }

        public static void StopAutoRun()
        {
            if (!AutoRun) return;
            data = campaignBeforeAutoRun;
            campaignBeforeAutoRun = null;
            AutoRun = false;
            Time.timeScale = timeScaleBeforeAutoRun;
        }

        public static bool Launch(ExpeditionDifficulty difficulty, out string error)
        {
            if (data.active) { error = "Ya hay una expedición en curso."; return false; }
            bool changed = EnsureAvailableWeapons(data, catalog);
            if (!CanLaunch(data, catalog, out error))
            {
                if (changed) Save();
                return false;
            }
            int basic = Array.IndexOf(catalog.weapons, catalog.basicWeapon);
            foreach (int weapon in data.selectedWeapons)
                if (weapon != basic) data.inventory[weapon]--;
            foreach (int amount in data.selectedMoney) data.money -= amount;
            data.difficulty = difficulty;
            data.active = true;
            data.lastResult = null;
            data.lastResultKey = null;
            data.lastResultValues = null;
            data.hasLastScore = false;
            Save();
            return true;
        }

        public static void Settle(float floorCredit, PlayerController[] agents, bool completed, double? score = null)
        {
            if (!Active) { Debug.LogError("No hay una expedición activa para liquidar."); return; }
            if (agents == null || agents.Length != SquadSize) { Debug.LogError("Falta el escuadrón para liquidar."); return; }
            if (AutoRun) { data.active = false; return; }
            float progress = Mathf.Clamp(floorCredit, 0f, FloorCount) / FloorCount;
            int specialtyCount = 0;
            if (data.specialties != null) foreach (Specialty specialty in data.specialties)
                if (specialty != Specialty.None) specialtyCount++;
            int reward = Mathf.RoundToInt(50f * progress * DifficultyMultiplier(data.difficulty) * (1f - 0.1f * specialtyCount));
            int collected = 0;
            for (int i = 0; i < SquadSize; i++)
                if (!agents[i].Dead) collected += agents[i].Money;
            int available = reward + collected;
            int paid = 0;
            RecoverOne(data);
            for (int i = 0; i < SquadSize; i++)
            {
                PlayerController agent = agents[i];
                WeaponInstance weapon = agent.Weapons[0];
                if (weapon != null)
                {
                    int index = Array.IndexOf(catalog.weapons, weapon.Definition);
                    if (index < 0) Debug.LogError("El arma recuperada no está en el catálogo: " + weapon.Definition.displayName);
                    else if (weapon.Definition != catalog.basicWeapon) data.inventory[index]++;
                }
                if (agent.Dead)
                {
                    MarkMissing(data, data.selectedAgents[i]);
                    continue;
                }
                AgentProgress progressForAgent = data.progress[data.selectedAgents[i]];
                if (completed)
                {
                    if (progressForAgent.expeditionsCompleted < int.MaxValue) progressForAgent.expeditionsCompleted++;
                    if (progressForAgent.pending < progressForAgent.RemainingUpgrades) progressForAgent.pending++;
                }
                int fee = Mathf.CeilToInt(HireCost(catalog.agents[data.selectedAgents[i]]) *
                    (1f + Mathf.Clamp(agent.Profile.greed, 1, 10) / 10f));
                int payment = Mathf.Min(available, fee);
                available -= payment;
                paid += payment;
            }
            data.money += available;
            data.lastResultKey = "Contrato: {0}% (${1}) | Efectivo recuperado: ${2} | Pagos: ${3} | Para ti: ${4}";
            data.lastResultValues = new[] { Mathf.RoundToInt(progress * 100f), reward, collected, paid, available };
            data.lastResult = GameText.Format(data.lastResultKey, Array.ConvertAll(data.lastResultValues, value => (object)value));
            data.hasLastScore = score.HasValue;
            if (score.HasValue) data.lastScore = score.Value;
            data.active = false;
            if (!GameOver) EnsureAvailableAgents(data);
            EnsureAvailableWeapons(data, catalog);
            Save();
        }

        public static void Abandon()
        {
            if (!Active) return;
            RecoverOne(data);
            foreach (int index in data.selectedAgents) MarkMissing(data, index);
            int basic = Array.IndexOf(catalog.weapons, catalog.basicWeapon);
            foreach (int index in data.selectedWeapons)
                if (index != basic) data.inventory[index]++;
            data.lastResultKey = "Expedición abandonada: agentes perdidos, equipo recuperado.";
            data.lastResultValues = null;
            data.hasLastScore = false;
            data.lastResult = GameText.Get(data.lastResultKey);
            data.active = false;
            if (!GameOver) EnsureAvailableAgents(data);
            EnsureAvailableWeapons(data, catalog);
            Save();
        }

        public static void Save()
        {
            if (AutoRun) return;
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        static void RecoverOne(CampaignData campaign)
        {
            int oldest = -1;
            for (int i = 0; i < campaign.unavailable.Length; i++)
                if (campaign.unavailable[i] > 0 &&
                    (oldest < 0 || campaign.unavailable[i] < campaign.unavailable[oldest])) oldest = i;
            if (oldest >= 0) campaign.unavailable[oldest] = 0;
        }

        static void MarkMissing(CampaignData campaign, int index)
        {
            if (campaign.unavailable[index] == 0)
            {
                int latest = 0;
                foreach (int missing in campaign.unavailable) latest = Mathf.Max(latest, missing);
                campaign.unavailable[index] = latest + 1;
            }
            ResetLevel(campaign, index);
        }

        public static void Restart()
        {
            data = NewCampaign(catalog);
            Save();
        }

        static void ResetLevel(CampaignData campaign, int index)
        {
            AgentProgress previous = campaign.progress[index];
            campaign.progress[index] = new AgentProgress
            {
                expeditionsCompleted = previous.expeditionsCompleted,
                ratsKilled = previous.ratsKilled,
                moneyCollected = previous.moneyCollected
            };
        }

        internal static void RecordAgentTotals(int slot, int ratsKilled, int moneyCollected)
        {
            if (!Active || slot < 0 || slot >= SquadSize) return;
            AgentProgress progress = data.progress[data.selectedAgents[slot]];
            progress.ratsKilled = (int)Math.Min(int.MaxValue, (long)progress.ratsKilled + ratsKilled);
            progress.moneyCollected = (int)Math.Min(int.MaxValue, (long)progress.moneyCollected + moneyCollected);
            Save();
        }

        public static AgentProfile ResetProgress(int index)
        {
            ResetLevel(data, index);
            Save();
            return Profile(data, catalog, index);
        }
    }
}
