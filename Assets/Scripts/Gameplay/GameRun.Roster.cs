using System;
using UnityEngine;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        const string RosterKey = "MouseKeeper.AgentRoster";
        AgentProfile[] profiles;

        bool LoadAgentProfiles()
        {
            if (squad != null && squad.Length > 0)
            {
                if (squad.Length != 3)
                {
                    Debug.LogError("El escuadrón debe contener exactamente tres agentes.", this);
                    return false;
                }
                profiles = new AgentProfile[squad.Length];
                for (int i = 0; i < squad.Length; i++)
                {
                    if (squad[i] == null || squad[i].profile == null)
                    {
                        Debug.LogError("Falta la definición o el perfil del agente " + (i + 1) + ".", this);
                        return false;
                    }
                    profiles[i] = squad[i].profile.Copy();
                }
            }
            else
            {
                AgentRoster roster = null;
                if (PlayerPrefs.HasKey(RosterKey))
                {
                    try
                    {
                        roster = JsonUtility.FromJson<AgentRoster>(PlayerPrefs.GetString(RosterKey));
                    }
                    catch (ArgumentException error)
                    {
                        Debug.LogError("No se pudo leer la configuración anterior de agentes: " + error.Message);
                    }
                    if (roster == null || roster.agents == null || roster.agents.Length != 3)
                        Debug.LogError("La configuración anterior de agentes no es válida; se usarán los valores iniciales.");
                }
                if (roster == null || roster.agents == null || roster.agents.Length != 3)
                    roster = new AgentRoster { agents = new[] {
                        new AgentProfile { name = "Agente 1", model = 0 },
                        new AgentProfile { name = "Agente 2", model = 1 },
                        new AgentProfile { name = "Agente 3", model = 2 } } };
                profiles = roster.agents;
            }

            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] == null) profiles[i] = new AgentProfile { name = "Agente " + (i + 1) };
                AgentProfile profile = profiles[i];
                if (string.IsNullOrWhiteSpace(profile.name)) profile.name = "Agente " + (i + 1);
                if (squad == null || squad.Length == 0)
                {
                    int modelCount = 3 + (agentModels != null ? agentModels.Length : 0);
                    if (profile.model < 0 || profile.model >= modelCount ||
                        (profile.model >= 3 && agentModels[profile.model - 3] == null))
                    {
                        Debug.LogWarning("El modelo anterior de " + profile.name + " no está disponible; se usará una apariencia provisional.", this);
                        profile.model = i % 3;
                    }
                    if (profile.weapon < 0 || profile.weapon >= weapons.Length || weapons[profile.weapon] == null)
                    {
                        Debug.LogWarning("El arma anterior de " + profile.name + " no está disponible; se usará la primera configurada.", this);
                        for (int j = 0; j < weapons.Length; j++)
                            if (weapons[j] != null) { profile.weapon = j; break; }
                    }
                }
                profile.health = Mathf.Clamp(profile.health, 20f, 200f);
                profile.speed = Mathf.Clamp(profile.speed, 2f, 8f);
                profile.attackSpeed = Mathf.Clamp(profile.attackSpeed, 0.5f, 2f);
                profile.bravery = Mathf.Clamp(profile.bravery, 1, 10);
                profile.accuracy = Mathf.Clamp(profile.accuracy, 1, 10);
                profile.agility = Mathf.Clamp(profile.agility, 1, 10);
                profile.distrust = Mathf.Clamp(profile.distrust, 1, 10);
                profile.greed = Mathf.Clamp(profile.greed, 1, 10);
                profile.teamwork = Mathf.Clamp(profile.teamwork, 1, 10);
                profile.luck = Mathf.Clamp(profile.luck, 1, 10);
                if (ExpeditionSession.Active && squad != null && squad.Length == ExpeditionSession.SquadSize)
                    profiles[i] = ExpeditionSession.Profile(ExpeditionSession.Data, ExpeditionSession.Catalog,
                        ExpeditionSession.Data.selectedAgents[i]);
            }
            return true;
        }
    }
}
