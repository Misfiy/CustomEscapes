using Exiled.Events.EventArgs.Player;
using Exiled.API.Enums;
using PlayerRoles;
using Respawning;
using Exiled.API.Features;
using MEC;
using UnityEngine;

namespace CustomEscapes.Events
{
    public sealed class PlayerHandler
    {
        private readonly Config config = Escaping.Instance.Config;
        public void OnEscaping(EscapingEventArgs ev)
        {
            if (config.EscapeScenarios.TryGetValue(ev.Player.Role.Type, out Escape escapeScenario))
            {
                if (!escapeScenario.AllowDefaultEscape && ev.Player.IsCuffed == false)
                {
                    ev.IsAllowed = false;
                    return;
                }

                if (ev.Player.IsCuffed && escapeScenario.CuffedTickets != null)
                {
                    ev.NewRole = escapeScenario.CuffedRole;
                   if(escapeScenario.CuffedTickets != null) ev.RespawnTickets = new KeyValuePair<SpawnableTeamType, float>(escapeScenario.CuffedTickets.Team, escapeScenario.CuffedTickets.Number);
                }
                else if (!ev.Player.IsCuffed)
                {
                    ev.NewRole = escapeScenario.NormalRole;
                    if(escapeScenario.NormalTickets != null) ev.RespawnTickets = new KeyValuePair<SpawnableTeamType, float>(escapeScenario.NormalTickets.Team, escapeScenario.NormalTickets.Number);
                }

                if (ev.NewRole == RoleTypeId.None)
                {
                    Log.Debug($"{ev.Player.Nickname} attempted to escape, but NewRole is set to None!");
                    ev.IsAllowed = false;
                    return;
                }

                ev.EscapeScenario = EscapeScenario.CustomEscape;
                ev.IsAllowed = true;
                if (escapeScenario.NormalEscapeMessage != null && !ev.Player.IsCuffed)
                {
                    if (escapeScenario.NormalEscapeMessage.UseHints) ev.Player.ShowHint(escapeScenario.NormalEscapeMessage.Message, escapeScenario.NormalEscapeMessage.Duration);
                    else ev.Player.Broadcast(escapeScenario.NormalEscapeMessage.Duration, escapeScenario.NormalEscapeMessage.Message);
                }
                else if (escapeScenario.CuffedEscapeMessage != null && ev.Player.IsCuffed)
                {
                    if (escapeScenario.CuffedEscapeMessage.UseHints) ev.Player.ShowHint(escapeScenario.CuffedEscapeMessage.Message, escapeScenario.CuffedEscapeMessage.Duration);
                    else ev.Player.Broadcast(escapeScenario.CuffedEscapeMessage.Duration, escapeScenario.CuffedEscapeMessage.Message);
                }
                Log.Debug($"{ev.Player.Nickname} has escaped as a {ev.Player.Role.Type}! They became {ev.NewRole}");
            }
        }
        public void OnSpawned(SpawnedEventArgs ev)
        {
            if (!config.EscapeScenarios.TryGetValue(ev.Player.Role.Type, out Escape escapeScenario)) return;
            if ((escapeScenario?.NewEscapeNormal?[0] == null) && (escapeScenario?.NewEscapeCuffed?[0] == null)) return; // if no new escapes for normal nor cuffed, return (nothing to do)
            Timing.RunCoroutine(DoCustomEscape(ev.Player, ev.Player.Role.Type, escapeScenario).CancelWith(ev.Player.GameObject));
        }
        private IEnumerator<float> DoCustomEscape(Player player, RoleTypeId oldRole, Escape scenario)
        {
            while (player.Role.Type == oldRole)
            {
                // yield return Timing.WaitForSeconds(1f);
                if (player.IsCuffed && scenario.NewEscapeCuffed?[0] != null)
                {
                    foreach (NewEscapePosition newEscPos in scenario.NewEscapeCuffed)
                    {
                        if (Vector3.Distance(player.Position, newEscPos.Position) <= newEscPos.Distance)
                        {
                            if (scenario.CuffedEscapeMessage != null)
                            {
                                if (scenario.CuffedEscapeMessage.UseHints) player.ShowHint(scenario.CuffedEscapeMessage.Message, scenario.CuffedEscapeMessage.Duration);
                                else player.Broadcast(scenario.CuffedEscapeMessage.Duration, scenario.CuffedEscapeMessage.Message);
                            }
                            player.Role.Set(scenario.CuffedRole, SpawnReason.Escaped, RoleSpawnFlags.All);
                            if(scenario.CuffedTickets != null) Respawn.GrantTickets(scenario.CuffedTickets.Team, scenario.CuffedTickets.Number);
                        }
                    }
                }
                else if (!player.IsCuffed && scenario.NewEscapeNormal?[0] != null)
                {
                    foreach (NewEscapePosition newEscPos in scenario.NewEscapeNormal)
                    {
                        if (Vector3.Distance(player.Position, newEscPos.Position) <= newEscPos.Distance)
                        {
                            if (scenario.NormalEscapeMessage != null)
                            {
                                if (scenario.NormalEscapeMessage.UseHints) player.ShowHint(scenario.NormalEscapeMessage.Message, scenario.NormalEscapeMessage.Duration);
                                else player.Broadcast(scenario.NormalEscapeMessage.Duration, scenario.NormalEscapeMessage.Message);
                            }
                            player.Role.Set(scenario.NormalRole, SpawnReason.Escaped, RoleSpawnFlags.All);
                            if(scenario.NormalTickets != null) Respawn.GrantTickets(scenario.NormalTickets.Team, scenario.NormalTickets.Number);
                        }

                    }
                }
                else
                {
                    yield break; // something horrific has happened, stop whatever we're doing
                }
                yield return Timing.WaitForSeconds(1f);
            }
            // yield break;
        }
    }

}
