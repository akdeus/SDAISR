// Copyright (C) 2026 Alonso Proschle Vargas Dev (AKDEUS) - Todos los derechos reservados.

using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins

{

    [Info("TeamSupervisor", "Alonso Proschle V Dev (AKDEUS)", "1.0.0")]
    [Description("Supervisa que el tamaño maximo de integrantes del equipo no sea excedido.")]
    public class TeamSupervisor : RustPlugin

    {

        private const int MaxTeamSize = 3;
        private const float ScanInterval = 60f;
        private const int AlertCooldownMinutes = 5;
        private const string ChatCommandName = "teamsupervisor";
        private const string SystemPrefix = "[SDAI - TeamSupervisor]";
        private const string MsgPluginLoaded = "TeamSupervisor: Activo y vigilando (Maximo 3 jugadores por equipo).";
        private const string MsgNoPermission = "No tienes permisos para usar este comando.";
        private const string MsgScanning = "Escaneando equipos...";
        private const string MsgTeamAlert = "Team {0} | {1}/{2} | Lider: {3} | {4}";
        private const string StatusOnline = "Conectado";
        private const string StatusSleeping = "Dormido";
        private const string StatusDisconnected = "Desconectado";

        private readonly Dictionary<ulong, DateTime> _cooldowns = new Dictionary<ulong, DateTime>();

        private void Init()

        {

            cmd.AddChatCommand(ChatCommandName, this, nameof(CmdCheckTeams));
            Puts($"{SystemPrefix} {string.Format(MsgPluginLoaded, MaxTeamSize)}");
            timer.Every(ScanInterval, () => ScanTeams(false));

        }

        private void OnTeamCreated(BasePlayer player, RelationshipManager.PlayerTeam team)

        {

            if (team != null)
                CheckTeam(team, false);

        }

        private void OnTeamAcceptInvite(RelationshipManager.PlayerTeam team, BasePlayer player)

        {

            if (team != null)
                CheckTeam(team, false);

        }

        private void CmdCheckTeams(BasePlayer player, string command, string[] args)

        {

            if (player == null) return;

            if (!player.IsAdmin)

            {

                SendReply(player, $"{SystemPrefix} {MsgNoPermission}");
                return;

            }

            SendReply(player, $"{SystemPrefix} {MsgScanning}");
            ScanTeams(true);

        }

        private void ScanTeams(bool force)

        {

            var manager = RelationshipManager.ServerInstance;
            if (manager == null || manager.playerTeams == null) return;

            foreach (var entry in manager.playerTeams)

            {

                if (entry.Value != null)
                    CheckTeam(entry.Value, force);

            }

        }

        private void CheckTeam(RelationshipManager.PlayerTeam team, bool force)

        {

            if (team.members == null || team.members.Count <= MaxTeamSize) return;

            ulong teamId = team.teamID;

            if (!force && _cooldowns.TryGetValue(teamId, out DateTime lastAlert))

            {

                if ((DateTime.UtcNow - lastAlert).TotalMinutes < AlertCooldownMinutes)
                    return;

            }

            _cooldowns[teamId] = DateTime.UtcNow;

            string leader = ResolveName(team.teamLeader);
            var membersInfo = new List<string>(team.members.Count);

            foreach (ulong memberId in team.members)

            {

                BasePlayer target = BasePlayer.FindByID(memberId) ?? BasePlayer.FindSleeping(memberId);
                string name = ResolveName(memberId);

                if (target != null)

                {

                    string state = target.IsConnected ? StatusOnline : StatusSleeping;
                    Vector3 pos = target.transform.position;
                    membersInfo.Add($"{name} ({state}) [{Mathf.RoundToInt(pos.x)}, {Mathf.RoundToInt(pos.y)}, {Mathf.RoundToInt(pos.z)}]");

                }

                else

                {

                    membersInfo.Add($"{name} ({StatusDisconnected})");

                }

            }

            string body = string.Format(MsgTeamAlert, teamId, team.members.Count, MaxTeamSize, leader, string.Join(", ", membersInfo));
            string alert = $"{SystemPrefix} {body}";

            Puts(alert);

            foreach (BasePlayer admin in BasePlayer.activePlayerList)

            {

                if (admin != null && admin.IsAdmin)
                    admin.ChatMessage(alert);

            }

        }

        private string ResolveName(ulong userId)

        {

            BasePlayer player = BasePlayer.FindByID(userId) ?? BasePlayer.FindSleeping(userId);
            if (player != null && !string.IsNullOrEmpty(player.displayName))
                return player.displayName;

            IPlayer iPlayer = covalence.Players.FindPlayerById(userId.ToString());
            if (iPlayer != null && !string.IsNullOrEmpty(iPlayer.Name))
                return iPlayer.Name;

            return userId.ToString();

        }

    }

}

// Copyright (C) 2026 Alonso Proschle Vargas Dev (AKDEUS) - Todos los derechos reservados.