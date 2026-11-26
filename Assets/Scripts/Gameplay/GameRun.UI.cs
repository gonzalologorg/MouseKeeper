using UnityEngine;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        bool returnToPlanningAfterDefeat;

        void ChangeScore(double amount)
        {
            score = System.Math.Max(int.MinValue, System.Math.Min(int.MaxValue, score + amount));
        }

        public void RecordPickupScore(PickupKind kind, int price)
        {
            if (Finished) return;
            ChangeScore((long)Mathf.Max(0, price) * 10 +
                (kind == PickupKind.Passive ? 30 : kind == PickupKind.Weapon ? 50 : 0));
        }

        int ClearedRoomCount()
        {
            int count = 0;
            foreach (Room room in rooms)
                if (room.kind != RoomKind.Corridor && room.visited && room.cleared && !room.spawning) count++;
            return count;
        }

        int ChamberCount()
        {
            int count = 0;
            foreach (Room room in rooms) if (room.kind != RoomKind.Corridor) count++;
            return count;
        }

        bool BossRevealed() => bossRoom != null && ClearedRoomCount() * 2 >= ChamberCount();

        public void EndRun()
        {
            if (Finished) return;
            Finished = true;
            returnToPlanningAfterDefeat = ExpeditionSession.Active;
            FinishBalanceTelemetry("defeat");
            if (ExpeditionSession.Active)
                ExpeditionSession.Settle(completedFloorCredit + (FloorCleared() ? 1f : 0f), agents.ToArray(), false, score);
        }

        public void ShowMessage(string text)
        {
            message = GameText.Get(text);
            messageUntil = Time.time + 2f;
        }

        static Rect FormationButtonsRect() => new Rect(Screen.width - 255f, Screen.height - 75f, 240f, 64f);

        static Rect FormationButtonRect(int index)
        {
            Rect panel = FormationButtonsRect();
            return new Rect(panel.x + index * 80f, panel.y, 78f, panel.height);
        }

        void DrawAtlasIcon(Rect rect, int index)
        {
            if (hudIcons != null)
                GUI.DrawTextureWithTexCoords(rect, hudIcons, new Rect(index / 8f, 0f, 1f / 8f, 1f));
        }

        static void DrawSpriteIcon(Rect rect, Sprite icon)
        {
            Rect source = icon.textureRect;
            Texture2D texture = icon.texture;
            GUI.DrawTextureWithTexCoords(rect, texture,
                new Rect(source.x / texture.width, source.y / texture.height,
                    source.width / texture.width, source.height / texture.height));
        }

        void DrawPassiveIcons(PlayerController agent, Rect area)
        {
            int capacity = Mathf.Max(0, Mathf.FloorToInt(area.width / 24f));
            int shown = Mathf.Min(agent.Passives.Count, capacity);
            if (agent.Passives.Count > capacity && capacity > 0) shown--;
            for (int i = 0; i < shown; i++)
            {
                PassiveItemDefinition passive = agent.Passives[i];
                if (passive == null) continue;
                Rect rect = new Rect(area.x + i * 24f, area.y, 22f, 22f);
                Sprite icon = passive.DisplayIcon;
                if (icon != null)
                {
                    DrawSpriteIcon(rect, icon);
                }
                else
                {
                    int index = passive is RoomDamagePassiveItemDefinition ? 1 :
                        passive is RevivePassiveItemDefinition ? 2 :
                        passive is RetaliationPassiveItemDefinition ? 3 :
                        passive is StatPassiveItemDefinition ? 0 : 4;
                    DrawAtlasIcon(rect, index);
                }
                GUI.Label(rect, new GUIContent("", GameText.Get(passive.displayName)));
            }
            if (agent.Passives.Count > shown && capacity > 0)
                GUI.Label(new Rect(area.x + shown * 24f, area.y, 36f, 22f), "+" + (agent.Passives.Count - shown), agentHudLabel);
        }

        void DrawFormationButtons()
        {
            string[] labels = { "Objetivo", "Distancia", "Cercano" };
            string[] descriptions = { "Concentrar fuego", "Mantener distancia", "Enemigo más cercano" };
            Color previous = GUI.backgroundColor;
            for (int i = 0; i < 3; i++)
            {
                Rect button = FormationButtonRect(i);
                GUI.backgroundColor = (int)Formation == i ? new Color(1f, 0.78f, 0.32f) : previous;
                GUI.Box(button, new GUIContent("", GameText.Get(descriptions[i])), GUI.skin.button);
                DrawAtlasIcon(new Rect(button.x + 23f, button.y + 3f, 32f, 32f), 5 + i);
                GUI.Label(new Rect(button.x + 5f, button.y + 37f, 72f, 22f), GameText.Get(labels[i]), agentHudLabel);
            }
            GUI.backgroundColor = previous;
        }

        GUIStyle agentHudLabel;
        GUIStyle agentHudName;
        GUIStyle healthHudLabel;

        static void HudRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        void DrawAgentHud(PlayerController agent, Rect panel)
        {
            if (agent == null || agent.Profile == null) return;
            float x = panel.x + 9f;
            float y = panel.y + 3f;
            string name = agent.Profile.name;
            float nameWidth = Mathf.Min(125f, agentHudName.CalcSize(new GUIContent(name)).x + 3f);
            GUI.Label(new Rect(x, y, nameWidth, 21f), name, agentHudName);
            x += nameWidth;
            string details = $" | ${agent.Money} | Lvl {agent.Profile.Level} |";
            float detailsWidth = agentHudLabel.CalcSize(new GUIContent(details)).x + 3f;
            GUI.Label(new Rect(x, y, detailsWidth, 21f), details, agentHudLabel);
            x += detailsWidth + 2f;
            Texture2D specialty = squadIndicators != null ? squadIndicators.SpecialtyIcon(agent.Specialty) : null;
            if (specialty != null && x + 19f <= panel.xMax)
                GUI.DrawTexture(new Rect(x, y, 19f, 19f), specialty);

            Rect health = new Rect(panel.x + 9f, panel.y + 31.5f, 142f, 11f);
            HudRect(health, new Color(0.5f, 0.5f, 0.5f, 0.4f));
            float fraction = agent.Dead || agent.MaxHealth <= 0f ? 0f : Mathf.Clamp01(agent.Health / agent.MaxHealth);
            Color fill = Color.Lerp(new Color(0.88f, 0.24f, 0.19f), new Color(0.28f, 0.76f, 0.44f), fraction);
            fill = Color.Lerp(fill, Color.white, 0.5f);
            HudRect(new Rect(health.x + 1f, health.y + 1f, (health.width - 2f) * fraction, health.height - 2f), fill);
            GUI.Label(new Rect(health.x, panel.y + 26f, health.width, 22f),
                agent.Dead ? GameText.Get("Caído") : $"{agent.Health:0} / {agent.MaxHealth:0}", healthHudLabel);

            Rect weaponRect = new Rect(health.xMax + 7f, panel.y + 26f, 22f, 22f);
            WeaponInstance weapon = agent.Weapons[agent.SelectedWeapon];
            if (weapon != null && weapon.Definition != null)
            {
                if (weapon.Definition.icon != null) DrawSpriteIcon(weaponRect, weapon.Definition.icon);
                GUI.Label(weaponRect, new GUIContent("", GameText.Get(weapon.Definition.displayName)));
            }
            DrawPassiveIcons(agent, new Rect(weaponRect.xMax + 7f, weaponRect.y,
                Mathf.Max(0f, panel.xMax - weaponRect.xMax - 14f), 22f));
        }

        void DrawBossHealthBar()
        {
            if (viewCamera == null || currentRoom == null || currentRoom != bossRoom ||
                currentRoom.boss == null || currentRoom.boss.IsDefeated || currentRoom.boss.maxHealth <= 0f) return;
            MouseEnemy boss = currentRoom.boss;
            CharacterController shape = boss.GetComponent<CharacterController>();
            Vector3 anchor = shape != null ? new Vector3(shape.bounds.center.x, shape.bounds.max.y + 0.5f, shape.bounds.center.z) : boss.transform.position + Vector3.up * 2f;
            Vector3 screen = viewCamera.WorldToScreenPoint(anchor);
            if (screen.z <= 0f) return;
            Rect bar = new Rect(screen.x - 80f, Screen.height - screen.y - 6f, 160f, 12f);
            HudRect(bar, new Color(0.08f, 0.08f, 0.08f, 0.85f));
            HudRect(new Rect(bar.x + 1f, bar.y + 1f, (bar.width - 2f) * Mathf.Clamp01(boss.Health / boss.maxHealth), bar.height - 2f),
                new Color(0.85f, 0.18f, 0.2f));
        }

        void OnGUI()
        {
            if (player == null) return;
            if (ExpeditionSession.AutoRun && GUI.Button(new Rect(15f, 45f, 190f, 30f), "DETENER AUTO-RUN (ESC)"))
            {
                StopAutoRun();
                return;
            }
            if (ExpeditionSession.AutoRun)
                GUI.Label(new Rect(15f, 78f, 300f, 22f), "AUTO-RUN #" + ExpeditionSession.AutoRunNumber + " · x" + ExpeditionSession.AutoRunSpeed + " · " + ExpeditionSession.Difficulty);
            DrawBossHealthBar();
            agentHudLabel ??= new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
            agentHudLabel.normal.textColor = Color.white;
            agentHudName ??= new GUIStyle(agentHudLabel) { fontStyle = FontStyle.Bold, clipping = TextClipping.Clip };
            healthHudLabel ??= new GUIStyle(agentHudLabel) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            agentHudName.normal.textColor = Color.white;
            healthHudLabel.normal.textColor = Color.white;
            float panelWidth = Mathf.Max(225f, Mathf.Min(380f, FormationButtonsRect().x - 25f));
            float firstY = Screen.height - 10f - agents.Count * 56f;
            for (int i = 0; i < agents.Count; i++)
                DrawAgentHud(agents[i], new Rect(15f, firstY + i * 56f, panelWidth, 52f));
            DrawFormationButtons();
            var floorPanel = new Rect(Screen.width - 165f, 15f, 150f, 52f);
            GUI.Label(new Rect(floorPanel.x + 9f, floorPanel.y + 4f, 135f, 21f), GameText.Format("Piso: {0}", levelIndex + 1), agentHudLabel);
            GUI.Label(new Rect(floorPanel.x + 9f, floorPanel.y + 26f, 135f, 21f), GameText.Format("Ratones: {0}", miceDefeated), agentHudLabel);
            GUI.Label(new Rect(floorPanel.x + 9f, floorPanel.y + 48f, 150f, 21f), GameText.Format("Puntaje: {0}", score.ToString("0.#")), agentHudLabel);
            if (bossRoom != null && bossRoom.bossDefeated && !FloorCleared() && ChamberCount() > 0)
                GUI.Label(new Rect(15f, firstY - 24f, 600f, 22f),
                    GameText.Format("Salida anticipada: -{0}% del contrato",
                        (100f * ClearedRoomCount() / ChamberCount() / ExpeditionSession.FloorCount).ToString("0.#")));
            foreach (PlayerController agent in agents)
            {
                if (agent.Dead) continue;
                ItemPickup nearby = agent.NearbyPickup();
                if (nearby == null || string.IsNullOrEmpty(nearby.DisplayName)) continue;
                Vector3 screen = viewCamera.WorldToScreenPoint(nearby.transform.position + Vector3.up);
                if (screen.z > 0f)
                {
                    Sprite icon = nearby.DisplayIcon;
                    if (icon != null) DrawSpriteIcon(new Rect(screen.x - 100f, Screen.height - screen.y, 22f, 22f), icon);
                    GUI.Label(new Rect(screen.x - (icon != null ? 74f : 100f), Screen.height - screen.y, 200f, 22f), nearby.DisplayName);
                }
            }
            if (!string.IsNullOrEmpty(message) && Time.time < messageUntil)
                GUI.Label(new Rect(Screen.width / 2f - 140f, Screen.height - 95f, 280f, 25f), message);
            if (Finished)
            {
                var style = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                style.normal.textColor = Color.white;
                GUI.Box(new Rect(Screen.width / 2f - 300f, Screen.height / 2f - 55f, 600f, 110f),
                    GameText.Get("Todos los agentes han padecido,\npresiona Enter para continuar"), style);
            }
            if (!string.IsNullOrEmpty(GUI.tooltip))
                GUI.Box(new Rect(Event.current.mousePosition.x + 12f, Event.current.mousePosition.y - 28f, 180f, 24f), GUI.tooltip);
        }
    }
}
