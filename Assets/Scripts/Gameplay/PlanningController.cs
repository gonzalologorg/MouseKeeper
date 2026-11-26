using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace MouseKeeper
{
    public sealed partial class PlanningController : MonoBehaviour
    {
        [SerializeField] VisualTreeAsset layout;
        [SerializeField] PanelSettings panelSettings;
        [SerializeField] VisualTreeAsset agentTemplate;
        [SerializeField] VisualTreeAsset weaponTemplate;
        VisualElement root;
        ScrollView roster;
        VisualElement weapons;
        int selectedSlot;
        string error;
        bool ready;
        static bool commandLineAutoRunHandled;
        readonly Dictionary<TextElement, string> staticText = new Dictionary<TextElement, string>();
#if UNITY_EDITOR
        Label debugStatus;
#endif
        readonly Texture2D[] statIcons = new Texture2D[7];
        static readonly string[] StatAssets = { "Bravery", "Accuracy", "Agility", "Distrust", "Greed", "Teamwork", "Luck" };
        static readonly string[] StatLabels = { "Valor", "Puntería", "Agilidad", "Desconfianza", "Avaricia", "Equipo", "Suerte" };
        static readonly string[] StatDescriptions = {
            "Reduce el miedo al estar herido.",
            "Reduce la dispersión, sobre todo a distancia.",
            "Mejora la recarga y reduce la fatiga.",
            "Favorece el uso de compañeros como protección.",
            "Aumenta la prioridad de botín y la pérdida de karma.",
            "Favorece el rescate de compañeros.",
            "Aumenta la probabilidad de impacto crítico hasta un 25%."
        };

        void Start()
        {
            if (!commandLineAutoRunHandled && Array.Exists(Environment.GetCommandLineArgs(),
                argument => argument == "--auto-run"))
            {
                commandLineAutoRunHandled = true;
                if (ExpeditionSession.StartAutoRun(out error))
                {
                    Application.runInBackground = true;
                    SceneManager.LoadScene("SampleScene");
                    return;
                }
                Debug.LogError(error, this);
            }
            UIDocument document = GetComponent<UIDocument>();
            if (layout == null && document != null) layout = document.visualTreeAsset;
            if (panelSettings == null && document != null) panelSettings = document.panelSettings;
            if (layout == null) layout = Resources.Load<VisualTreeAsset>("Planning");
            if (panelSettings == null) panelSettings = Resources.Load<PanelSettings>("PlanningPanel");
            if (agentTemplate == null) agentTemplate = Resources.Load<VisualTreeAsset>("PlanningAgentRow");
            if (weaponTemplate == null) weaponTemplate = Resources.Load<VisualTreeAsset>("PlanningWeaponCard");
            if (layout == null || panelSettings == null || agentTemplate == null || weaponTemplate == null)
            {
                Debug.LogError("La interfaz o la configuración del panel de planificación no están disponibles.", this);
                enabled = false;
                return;
            }
            for (int i = 0; i < statIcons.Length; i++)
            {
                statIcons[i] = Resources.Load<Texture2D>("StatIcons/" + StatAssets[i]);
                if (statIcons[i] == null) { Debug.LogError("Falta el icono de estadística " + StatAssets[i], this); enabled = false; return; }
            }
            if (document == null) document = gameObject.AddComponent<UIDocument>();
            if (document.panelSettings != panelSettings) document.panelSettings = panelSettings;
            if (document.visualTreeAsset != layout) document.visualTreeAsset = layout;
            root = document.rootVisualElement;
            root.Query<TextElement>().ForEach(element => {
                if (!string.IsNullOrEmpty(element.text)) staticText[element] = element.text;
            });
            roster = root.Q<ScrollView>("roster");
            weapons = root.Q<VisualElement>("weapons");
            if (!BindControls())
            {
                Debug.LogError("El UXML de planificación no contiene todos los controles requeridos.", this);
                enabled = false;
                return;
            }
            BuildStatGuide();
            ready = ExpeditionSession.Initialize();
            if (ready) BuildSpecialtyControls();
#if UNITY_EDITOR
            debugStatus = new Label("EDITOR: F1 reiniciar campaña | F2 +$20 | F3 todas las armas | F4 +5 mejoras por agente");
            debugStatus.AddToClassList("hint");
            root.Add(debugStatus);
#endif
            Refresh();
        }

#if UNITY_EDITOR
        void Update()
        {
            if (!ready || Keyboard.current == null) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard.f1Key.wasPressedThisFrame) ApplyEditorDebug(1);
            else if (keyboard.f2Key.wasPressedThisFrame) ApplyEditorDebug(2);
            else if (keyboard.f3Key.wasPressedThisFrame) ApplyEditorDebug(3);
            else if (keyboard.f4Key.wasPressedThisFrame) ApplyEditorDebug(4);
        }

        void ApplyEditorDebug(int command)
        {
            CampaignData data = ExpeditionSession.Data;
            ExpeditionCatalog catalog = ExpeditionSession.Catalog;
            string result;
            switch (command)
            {
                case 1:
                    ExpeditionSession.Restart();
                    selectedSlot = 0;
                    error = null;
                    result = "Campaña reiniciada.";
                    break;
                case 2:
                    if (data.money > int.MaxValue - 20) { Debug.LogError("No se pueden agregar fondos: se alcanzó el límite."); return; }
                    data.money += 20;
                    result = "Fondos +$20.";
                    break;
                case 3:
                    for (int i = 0; i < catalog.weapons.Length; i++)
                        if (catalog.weapons[i] != catalog.basicWeapon && data.inventory[i] == int.MaxValue)
                        { Debug.LogError("No se pueden agregar armas: se alcanzó el límite."); return; }
                    for (int i = 0; i < catalog.weapons.Length; i++)
                        if (catalog.weapons[i] != catalog.basicWeapon) data.inventory[i]++;
                    result = "Se agregó una copia de cada arma; el Mouse Blaster es ilimitado.";
                    break;
                case 4:
                    foreach (AgentProgress progress in data.progress)
                        if (progress.pending > int.MaxValue - 5)
                        { Debug.LogError("No se pueden agregar mejoras: se alcanzó el límite."); return; }
                    foreach (AgentProgress progress in data.progress) progress.pending += 5;
                    result = "Cada agente recibió 5 puntos.";
                    break;
                default: Debug.LogError("Comando de depuración de planificación desconocido."); return;
            }
            ExpeditionSession.Save();
            if (debugStatus != null) debugStatus.text = "EDITOR: " + result + " | F1 reiniciar | F2 +$20 | F3 armas | F4 mejoras";
            Refresh();
        }
#endif

        bool BindControls()
        {
            if (root == null || roster == null || weapons == null ||
                root.Q<VisualElement>("planner") == null || root.Q<VisualElement>("pending-expedition") == null ||
                root.Q<VisualElement>("game-over") == null || root.Q<Label>("funds") == null ||
                root.Q<Label>("lives") == null || root.Q<Label>("last-result") == null ||
                root.Q<Label>("load-error") == null || root.Q<Label>("error") == null ||
                root.Q<Label>("weapon-heading") == null || root.Q<Label>("equipped-weapon") == null ||
                root.Q<Label>("upgrade-heading") == null || root.Q<Label>("weapon-tooltip") == null ||
                root.Q<Label>("detail-name") == null || root.Q<Label>("detail-description") == null ||
                root.Q<Label>("detail-career") == null ||
                root.Q<VisualElement>("detail-portrait") == null || root.Q<VisualElement>("detail-stars") == null ||
                root.Q<VisualElement>("stat-guide") == null || root.Q<VisualElement>("skill-upgrades") == null) return false;
            VisualElement agentExample = agentTemplate.Instantiate();
            VisualElement weaponExample = weaponTemplate.Instantiate();
            if (agentExample.Q<Button>("agent-button") == null || agentExample.Q<VisualElement>("agent-portrait") == null ||
                agentExample.Q<Label>("agent-name") == null || agentExample.Q<Label>("agent-cost") == null ||
                agentExample.Q<Label>("agent-status") == null || agentExample.Q<VisualElement>("agent-stars") == null ||
                agentExample.Q<VisualElement>("agent-stats") == null || weaponExample.Q<Button>("weapon-button") == null ||
                weaponExample.Q<Image>("weapon-icon") == null ||
                weaponExample.Q<Label>("weapon-details") == null || weaponExample.Q<Label>("weapon-selected-mark") == null) return false;
            Button resume = root.Q<Button>("resume-button");
            Button abandon = root.Q<Button>("abandon-button");
            Button random = root.Q<Button>("random-button");
            Button launch = root.Q<Button>("launch-button");
            Button restart = root.Q<Button>("restart-button");
            Button language = root.Q<Button>("language-button");
            Button autoRun = root.Q<Button>("auto-run-button");
            if (resume == null || abandon == null || random == null || launch == null || restart == null || language == null || autoRun == null) return false;
            autoRun.clicked += () => {
                if (ExpeditionSession.StartAutoRun(out error)) SceneManager.LoadScene("SampleScene");
                else Refresh();
            };
            language.clicked += () => {
                GameText.SetEnglish(!GameText.English);
                root.Q<VisualElement>("stat-guide").Clear();
                BuildStatGuide();
                Refresh();
            };
            resume.clicked += () => SceneManager.LoadScene("SampleScene");
            abandon.clicked += () => { ExpeditionSession.Abandon(); error = null; Refresh(); };
            random.clicked += Randomize;
            launch.clicked += () => ShowDifficultyMenu(launch);
            restart.clicked += () => { ExpeditionSession.Restart(); selectedSlot = 0; error = null; Refresh(); };
            for (int i = 0; i < ExpeditionSession.SquadSize; i++)
            {
                int slot = i;
                Button select = root.Q<Button>("select-slot-" + i);
                Button minus = root.Q<Button>("money-minus-" + i);
                Button plus = root.Q<Button>("money-plus-" + i);
                if (select == null || minus == null || plus == null ||
                    root.Q<VisualElement>("slot-" + i) == null || root.Q<VisualElement>("slot-stats-" + i) == null ||
                    root.Q<VisualElement>("slot-portrait-" + i) == null || root.Q<VisualElement>("slot-stars-" + i) == null ||
                    root.Q<Label>("slot-name-" + i) == null || root.Q<Label>("slot-weapon-" + i) == null ||
                    root.Q<Label>("slot-money-" + i) == null) return false;
                select.clicked += () => { selectedSlot = slot; Refresh(); };
                minus.clicked += () => ChangeMoney(slot, -1);
                plus.clicked += () => ChangeMoney(slot, 1);
            }
            return true;
        }

        static int[] Stats(AgentProfile profile) =>
            new[] { profile.bravery, profile.accuracy, profile.agility, profile.distrust, profile.greed, profile.teamwork, Mathf.Clamp(profile.luck, 1, 10) };

        static void SetStars(VisualElement container, AgentProfile profile)
        {
            container.Clear();
            container.tooltip = GameText.Get("Calificación basada en vida, velocidad y ataque.");
            int rating = profile.Rating;
            for (int i = 0; i < 5; i++)
            {
                var star = new Label("★") { pickingMode = PickingMode.Ignore };
                star.AddToClassList("star");
                star.AddToClassList(i < rating ? "star-filled" : "star-empty");
                container.Add(star);
            }
        }

        static void SetPortrait(VisualElement container, AgentDefinition agent)
        {
            container.Clear();
            container.pickingMode = PickingMode.Ignore;
            container.style.backgroundColor = agent.placeholderColor;
            if (agent.portrait != null)
            {
                var image = new Image { sprite = agent.portrait, scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
                image.AddToClassList("portrait-image");
                container.Add(image);
                return;
            }
            var fallback = new VisualElement { pickingMode = PickingMode.Ignore };
            fallback.AddToClassList("portrait-fallback");
            var head = new VisualElement { pickingMode = PickingMode.Ignore };
            head.AddToClassList("portrait-head");
            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("portrait-body");
            var initial = new Label(string.IsNullOrEmpty(agent.profile.name) ? "?" : agent.profile.name.Substring(0, 1)) {
                pickingMode = PickingMode.Ignore
            };
            initial.AddToClassList("portrait-initial");
            fallback.Add(head);
            fallback.Add(body);
            fallback.Add(initial);
            container.Add(fallback);
        }

        static string AgentDescription(AgentDefinition definition, AgentProfile profile)
        {
            if (!string.IsNullOrWhiteSpace(definition.description)) return definition.description;
            int[] values = Stats(profile);
            int best = 0;
            foreach (int candidate in new[] { 1, 2, 5 })
                if (values[candidate] > values[best]) best = candidate;
            return GameText.Format("Especialidad: {0}. Avaricia: {1}/10.",
                GameText.Get(StatLabels[best]).ToLowerInvariant(), profile.greed);
        }

        void Refresh()
        {
            foreach (var entry in staticText) entry.Key.text = GameText.Get(entry.Value);
            root.Q<Button>("language-button").text = GameText.English ? "EN / ES" : "ES / EN";
            root.Q<Label>("weapon-tooltip").style.display = DisplayStyle.None;
            Label loadError = root.Q<Label>("load-error");
            loadError.text = ready ? "" : GameText.Get("No se pudo cargar el catálogo o la campaña. Revisa la consola.");
            loadError.style.display = ready ? DisplayStyle.None : DisplayStyle.Flex;
            bool gameOver = ready && ExpeditionSession.GameOver;
            root.Q<VisualElement>("game-over").style.display = gameOver ? DisplayStyle.Flex : DisplayStyle.None;
            root.Q<VisualElement>("planner").style.display = ready && !ExpeditionSession.Active && !gameOver
                ? DisplayStyle.Flex : DisplayStyle.None;
            root.Q<VisualElement>("pending-expedition").style.display = ready && ExpeditionSession.Active && !gameOver
                ? DisplayStyle.Flex : DisplayStyle.None;
            if (!ready) return;

            CampaignData data = ExpeditionSession.Data;
            ExpeditionCatalog catalog = ExpeditionSession.Catalog;
            int reserved = data.selectedMoney[0] + data.selectedMoney[1] + data.selectedMoney[2];
            root.Q<Label>("funds").text = data.active ? GameText.Format("Fondos: ${0}", data.money) :
                GameText.Format("Fondos: ${0}   |   Asignados: ${1}   |   Contratos: ${2}", data.money, reserved, SelectedHireCost(data, catalog));
            root.Q<Label>("lives").text = GameText.Format("VIDAS RESTANTES: {0}/{1}",
                Mathf.Max(0, ExpeditionSession.LossLimit - ExpeditionSession.Losses(data)), ExpeditionSession.LossLimit);
            Label lastResult = root.Q<Label>("last-result");
            lastResult.text = string.IsNullOrEmpty(data.lastResultKey) ? data.lastResult ?? "" :
                data.lastResultValues == null ? GameText.Get(data.lastResultKey) :
                GameText.Format(data.lastResultKey, Array.ConvertAll(data.lastResultValues, value => (object)value));
            if (data.hasLastScore) lastResult.text += " | " + GameText.Format("Puntaje: {0}", data.lastScore.ToString("0.#"));
            lastResult.style.display = string.IsNullOrEmpty(lastResult.text) ? DisplayStyle.None : DisplayStyle.Flex;
            Label errorLabel = root.Q<Label>("error");
            errorLabel.text = GameText.Get(error ?? "");
            errorLabel.style.display = string.IsNullOrEmpty(error) ? DisplayStyle.None : DisplayStyle.Flex;
            if (data.active || gameOver) return;

            long hiringBudget = ExpeditionSession.HiringBudgetForSlot(data, catalog, selectedSlot);
            Vector2 rosterPosition = roster.scrollOffset;
            roster.Clear();
            for (int i = 0; i < catalog.agents.Length; i++)
            {
                int index = i;
                AgentDefinition definition = catalog.agents[i];
                int cost = ExpeditionSession.HireCost(definition);
                if (Array.IndexOf(data.selectedAgents, i) >= 0 || cost > hiringBudget) continue;
                AgentProfile profile = ExpeditionSession.Profile(data, catalog, i);
                bool canUpgrade = ExpeditionSession.CanUpgrade(data, catalog, i, profile);
                string status = GameText.Get(canUpgrade ? "Mejora disponible" :
                    data.unavailable[i] > 0 ? (definition.freeRecruit ? "Recuperación pendiente" : "Perdido") : "");
                VisualElement row = agentTemplate.Instantiate();
                Button button = row.Q<Button>("agent-button");
                button.clicked += () =>
                {
                    if (!ExpeditionSession.CanHireForSlot(data, catalog, index, selectedSlot)) return;
                    data.selectedAgents[selectedSlot] = index;
                    ExpeditionSession.Save();
                    Refresh();
                };
                SetAgentName(row.Q<Label>("agent-name"), profile, canUpgrade);
                row.Q<Label>("agent-cost").text = cost == 0 ? GameText.Get("Gratis") : $"${cost}";
                row.Q<Label>("agent-status").text = status;
                row.Q<Label>("agent-status").style.display = string.IsNullOrEmpty(status) ? DisplayStyle.None : DisplayStyle.Flex;
                SetPortrait(row.Q<VisualElement>("agent-portrait"), definition);
                SetStars(row.Q<VisualElement>("agent-stars"), profile);
                SetStats(row.Q<VisualElement>("agent-stats"), profile);
                button.SetEnabled(ExpeditionSession.CanHireForSlot(data, catalog, i, selectedSlot));
                roster.Add(row);
            }
            roster.scrollOffset = rosterPosition;
            for (int i = 0; i < ExpeditionSession.SquadSize; i++)
            {
                AgentDefinition agent = catalog.agents[data.selectedAgents[i]];
                AgentProfile profile = ExpeditionSession.Profile(data, catalog, data.selectedAgents[i]);
                root.Q<VisualElement>("slot-" + i).EnableInClassList("slot-selected", selectedSlot == i);
                SetAgentName(root.Q<Label>("slot-name-" + i), profile,
                    ExpeditionSession.CanUpgrade(data, catalog, data.selectedAgents[i], profile));
                SetPortrait(root.Q<VisualElement>("slot-portrait-" + i), agent);
                SetStars(root.Q<VisualElement>("slot-stars-" + i), profile);
                SetStats(root.Q<VisualElement>("slot-stats-" + i), profile);
                root.Q<Label>("slot-weapon-" + i).text = GameText.Get(catalog.weapons[data.selectedWeapons[i]].displayName);
                root.Q<Label>("slot-money-" + i).text = GameText.Format("Dinero asignado: ${0}", data.selectedMoney[i]);
                root.Q<Button>("money-minus-" + i).SetEnabled(data.selectedMoney[i] > 0);
                root.Q<Button>("money-plus-" + i).SetEnabled(reserved + SelectedHireCost(data, catalog) < data.money);
            }
            RefreshAgentDetails(data, catalog);
            RefreshSpecialtyControls();
        }

        static void SetAgentName(Label label, AgentProfile profile, bool canUpgrade, int levelSize = 11)
        {
            label.enableRichText = true;
            label.style.color = profile.LevelColor;
            label.text = profile.NameWithLevel(levelSize) + (canUpgrade ? " <color=#ffcc62>▲</color>" : "");
        }

        void RefreshAgentDetails(CampaignData data, ExpeditionCatalog catalog)
        {
            int selectedAgent = data.selectedAgents[selectedSlot];
            AgentDefinition definition = catalog.agents[selectedAgent];
            AgentProfile profile = ExpeditionSession.Profile(data, catalog, selectedAgent);
            SetAgentName(root.Q<Label>("detail-name"), profile,
                ExpeditionSession.CanUpgrade(data, catalog, selectedAgent, profile), 14);
            AgentProgress progress = data.progress[selectedAgent];
            root.Q<Label>("detail-career").text = GameText.Format("Ratas eliminadas: {0} · Dinero recogido: ${1}",
                progress.ratsKilled, progress.moneyCollected);
            root.Q<Label>("detail-description").text = AgentDescription(definition, profile);
            SetPortrait(root.Q<VisualElement>("detail-portrait"), definition);
            SetStars(root.Q<VisualElement>("detail-stars"), profile);
            root.Q<Label>("weapon-heading").text = GameText.Get("ARMAS DISPONIBLES · DAÑO | ALCANCE");
            root.Q<Label>("equipped-weapon").text = GameText.Format("Equipada: {0}",
                GameText.Get(catalog.weapons[data.selectedWeapons[selectedSlot]].displayName));
            weapons.Clear();
            for (int i = 0; i < catalog.weapons.Length; i++)
            {
                int index = i;
                int available = ExpeditionSession.AvailableForSlot(data, catalog, i, selectedSlot);
                if (available == 0) continue;
                WeaponDefinition weapon = catalog.weapons[i];
                VisualElement card = weaponTemplate.Instantiate();
                card.AddToClassList("weapon-tile");
                Button button = card.Q<Button>("weapon-button");
                button.clicked += () => {
                    data.selectedWeapons[selectedSlot] = index;
                    error = null;
                    ExpeditionSession.Save();
                    Refresh();
                };
                bool selected = data.selectedWeapons[selectedSlot] == i;
                button.EnableInClassList("weapon-selected", selected);
                card.Q<Label>("weapon-selected-mark").style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;
                Image image = card.Q<Image>("weapon-icon");
                image.sprite = weapon.icon;
                image.scaleMode = ScaleMode.ScaleToFit;
                image.style.display = weapon.icon != null ? DisplayStyle.Flex : DisplayStyle.None;
                card.Q<Label>("weapon-details").text = $"{weapon.damage:0} | {weapon.range:0}";
                button.RegisterCallback<PointerEnterEvent>(_ => ShowWeaponTooltip(button, GameText.Get(weapon.displayName)));
                button.RegisterCallback<PointerLeaveEvent>(_ => root.Q<Label>("weapon-tooltip").style.display = DisplayStyle.None);
                weapons.Add(card);
            }
            RefreshUpgrades(data, catalog, selectedAgent, profile);
        }

        void ShowWeaponTooltip(Button button, string name)
        {
            Label tooltip = root.Q<Label>("weapon-tooltip");
            Vector2 position = root.WorldToLocal(new Vector2(button.worldBound.xMin, button.worldBound.yMax));
            tooltip.text = name;
            tooltip.style.left = Mathf.Clamp(position.x, 0f, Mathf.Max(0f, root.resolvedStyle.width - 200f));
            tooltip.style.top = Mathf.Clamp(position.y + 3f, 0f, Mathf.Max(0f, root.resolvedStyle.height - 30f));
            tooltip.style.display = DisplayStyle.Flex;
        }

        static int SelectedHireCost(CampaignData data, ExpeditionCatalog catalog)
        {
            int cost = 0;
            foreach (int index in data.selectedAgents) cost += ExpeditionSession.HireCost(catalog.agents[index]);
            return cost;
        }

        Image StatIcon(int index)
        {
            var icon = new Image { image = statIcons[index], scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            icon.AddToClassList("stat-icon");
            return icon;
        }

        void SetStats(VisualElement container, AgentProfile profile)
        {
            container.Clear();
            int[] values = Stats(profile);
            for (int i = 0; i < values.Length; i++)
            {
                var cell = new VisualElement();
                cell.AddToClassList("stat-cell");
                cell.Add(StatIcon(i));
                var number = new Label(values[i].ToString()) { pickingMode = PickingMode.Ignore };
                number.AddToClassList("stat-value");
                cell.Add(number);
                container.Add(cell);
            }
        }

        void BuildStatGuide()
        {
            VisualElement guide = root.Q<VisualElement>("stat-guide");
            for (int i = 0; i < StatLabels.Length; i++)
            {
                var item = new VisualElement();
                item.AddToClassList("guide-item");
                var heading = new VisualElement();
                heading.AddToClassList("stat-cell");
                heading.Add(StatIcon(i));
                var name = new Label(GameText.Get(StatLabels[i]));
                name.AddToClassList("guide-name");
                heading.Add(name);
                var description = new Label(GameText.Get(StatDescriptions[i]));
                description.AddToClassList("guide-description");
                item.Add(heading);
                item.Add(description);
                guide.Add(item);
            }
        }

        void RefreshUpgrades(CampaignData data, ExpeditionCatalog catalog, int agent, AgentProfile profile)
        {
            int points = data.progress[agent].pending;
            string legacy = data.upgradePoints > 0 ? GameText.Format(" · Compartidos anteriores: {0}", data.upgradePoints) : "";
            root.Q<Label>("upgrade-heading").text = GameText.Format("MEJORAS INDIVIDUALES · Puntos: {0}{1}", points, legacy);
            VisualElement container = root.Q<VisualElement>("skill-upgrades");
            container.Clear();
            int[] values = Stats(profile);
            for (int i = 0; i < values.Length; i++)
            {
                int skill = i;
                var row = new VisualElement();
                row.AddToClassList("upgrade-row");
                row.Add(StatIcon(i));
                var label = new Label(GameText.Get(StatLabels[i]));
                label.AddToClassList("upgrade-name");
                row.Add(label);
                var value = new Label(values[i].ToString());
                value.AddToClassList("upgrade-value");
                row.Add(value);
                var bar = new VisualElement();
                bar.AddToClassList("upgrade-bar");
                for (int segment = 0; segment < 10; segment++)
                {
                    var part = new VisualElement();
                    part.AddToClassList("upgrade-segment");
                    if (segment < values[i]) part.AddToClassList("upgrade-segment-filled");
                    bar.Add(part);
                }
                row.Add(bar);
                var button = new Button(() => {
                    if (ExpeditionSession.Upgrade(data, catalog, agent, skill)) { ExpeditionSession.Save(); Refresh(); }
                }) { text = "+", name = "upgrade-skill-" + i };
                button.AddToClassList("skill-button");
                button.SetEnabled(ExpeditionSession.CanUpgrade(data, catalog, agent, profile) && values[i] < 10);
                row.Add(button);
                container.Add(row);
            }
        }

        void ChangeMoney(int slot, int delta)
        {
            CampaignData data = ExpeditionSession.Data;
            int reserved = data.selectedMoney[0] + data.selectedMoney[1] + data.selectedMoney[2];
            if (delta < 0 && data.selectedMoney[slot] == 0 ||
                delta > 0 && reserved + SelectedHireCost(data, ExpeditionSession.Catalog) >= data.money) return;
            data.selectedMoney[slot] += delta;
            ExpeditionSession.Save();
            Refresh();
        }

        void Randomize()
        {
            if (ExpeditionSession.RandomizePlan(ExpeditionSession.Data, ExpeditionSession.Catalog))
            {
                error = null;
                ExpeditionSession.Save();
            }
            else error = "No hay suficientes agentes disponibles.";
            Refresh();
        }

        void ShowDifficultyMenu(Button button)
        {
            var menu = new GenericDropdownMenu();
            menu.AddItem(GameText.Get("Fácil · 0.5x"), false, () => Launch(ExpeditionDifficulty.Easy));
            menu.AddItem(GameText.Get("Normal · 1.0x"), false, () => Launch(ExpeditionDifficulty.Normal));
            menu.AddItem(GameText.Get("Difícil · 1.5x (+1 sala)"), false, () => Launch(ExpeditionDifficulty.Hard));
            menu.AddItem(GameText.Get("Enjambre · 2.5x enemigos (+2 salas)"), false, () => Launch(ExpeditionDifficulty.Swarm));
            menu.DropDown(button.worldBound, button, DropdownMenuSizeMode.Content);
        }

        void Launch(ExpeditionDifficulty difficulty)
        {
            if (ExpeditionSession.Launch(difficulty, out error)) SceneManager.LoadScene("Start");
            else Refresh();
        }
    }
}
