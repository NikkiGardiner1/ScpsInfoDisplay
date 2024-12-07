﻿using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.CustomRoles.API.Features;
using MEC;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp096;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Player = Exiled.API.Features.Player;

namespace ScpsInfoDisplay
{
    internal class EventHandlers
    {
        private CoroutineHandle _displayCoroutine;
        public Config Config = ScpsInfoDisplay.Instance.Config;
        internal void OnRoundStarted()
        {
            if (_displayCoroutine.IsRunning)
                Timing.KillCoroutines(_displayCoroutine);

            _displayCoroutine = Timing.RunCoroutine(ShowDisplay());
        }

        private IEnumerator<float> ShowDisplay()
        {
            while (Round.InProgress)
            {
                yield return Timing.WaitForSeconds(1f);
                try
                {
                    foreach (Player player in Player.List.Where(p => p != null && ShouldDisplayForPlayer(p)))
                    {
                        StringBuilder builder = StringBuilderPool.Shared.Rent($"<align={Config.TextAlignment.ToString().ToLower()}>");

                        // Display SCPs
                        foreach (Player scp in Player.List.Where(p => p?.Role.Team == Team.SCPs && ShouldDisplayForPlayer(p)))
                        {
                            if (Config.DisplayStrings.ContainsKey(scp.Role.Type))
                            {
                                builder.Append((scp == player ? Config.PlayersMarker : string.Empty)
                                               + ProcessStringVariables(Config.DisplayStrings[scp.Role.Type], player, scp)).Append('\n');
                            }
                        }

                        // Display Custom Roles, but only the ones defined in CustomRolesIntegrations
                        foreach (CustomRole customRole in CustomRole.Registered)
                        {
                            if (Config.CustomRolesIntegrations.ContainsKey(customRole.Name))
                            {
                                foreach (Player customPlayer in customRole.TrackedPlayers)
                                {
                                    builder.Append((customPlayer == player ? Config.PlayersMarker : string.Empty)
                                                   + ProcessCustomRoleVariables(customRole, customPlayer)).Append('\n');
                                }
                            }
                        }

                        builder.Append($"<voffset={Config.TextPositionOffset}em> </voffset></align>");
                        player.ShowHint(StringBuilderPool.Shared.ToStringReturn(builder), 1.25f);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
        }
        private bool ShouldDisplayForPlayer(Player player)
        {
            return Config.DisplayStrings.ContainsKey(player.Role.Type) ||
                   CustomRole.Registered.Any(customRole => customRole.TrackedPlayers.Contains(player) && 
                                                           Config.CustomRolesIntegrations.ContainsKey(customRole.Name));
        }

        private string ProcessStringVariables(string raw, Player observer, Player target) => raw
            .Replace("%arhealth%", Math.Floor(target.HumeShield) >= 0 ? Math.Floor(target.HumeShield).ToString() : string.Empty)
            .Replace("%healthpercent%", Math.Floor(target.Health / target.MaxHealth * 100).ToString())
            .Replace("%health%", Math.Floor(target.Health).ToString())
            .Replace("%generators%", Generator.List.Count(gen => gen.IsEngaged).ToString())
            .Replace("%engaging%", Generator.List.Count(gen => gen.IsActivating) > 0 ? $" (+{Generator.List.Count(gen => gen.IsActivating)})" : string.Empty)
            .Replace("%distance%", target != observer ? Math.Floor(Vector3.Distance(observer.Position, target.Position)) + "m" : string.Empty)
            .Replace("%zombies%", Player.List.Count(p => p.Role.Type == RoleTypeId.Scp0492).ToString())
            .Replace("%079level%", target.Role.Is(out Scp079Role scp079) ? scp079.Level.ToString() : string.Empty)
            .Replace("%079energy%", target.Role.Is(out Scp079Role _) ? Math.Floor(scp079.Energy).ToString() : string.Empty)
            .Replace("%079experience%", target.Role.Is(out Scp079Role _) ? Math.Floor((double)scp079.Experience).ToString() : string.Empty)
            .Replace("%106vigor%", target.Role.Is(out Scp106Role scp106) ? Math.Floor(scp106.Vigor * 100).ToString() : string.Empty)
            .Replace("%3114disguisetype%", target.Role.Is(out Scp3114Role scp3114) ? (scp3114.DisguiseStatus.ToString() != "None" ? SkeletonDisguiseNames(scp3114.StolenRole) : "None") : string.Empty)
            .Replace("%096state%", target.Role.Is(out Exiled.API.Features.Roles.Scp096Role scp096) ? (Config.Scp096StateIndicator.TryGetValue(scp096.RageState, out var stateIcon) ? stateIcon : "Unknown") : string.Empty)
            .Replace("%096targets%", target.Role.Is(out Exiled.API.Features.Roles.Scp096Role _) ? scp096.Targets.Count.ToString() : string.Empty)
            .Replace("%173stared%", target.Role.Is(out Scp173Role scp173) ? (Config.Scp173ObservationIndicators.TryGetValue(scp173.IsObserved ? "Observed" : "Unobserved", out var icon) ? icon : "-") : string.Empty)
            .Replace("%playername%", target.Nickname);

        private string ProcessCustomRoleVariables(CustomRole customRole, Player observer) => 
            Config.CustomRolesIntegrations.TryGetValue(customRole.Name, out var value) ? value : string.Empty
            .Replace("%customrole%", customRole.Name)
            .Replace("%playername%", observer.Nickname)
            .Replace("%health%", Math.Floor(observer.Health).ToString())
            .Replace("%healthpercent%", Math.Floor(observer.ArtificialHealth) >= 0 ? Math.Floor(observer.ArtificialHealth).ToString() : string.Empty);
        
        private string SkeletonDisguiseNames(RoleTypeId disguise)
        {
            return Config.Scp3114DisguiseDisplay.TryGetValue(disguise, out var displayName) ? displayName : disguise.ToString();
        }
    }
}
