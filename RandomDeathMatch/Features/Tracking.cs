using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms;
using PlayerStatsSystem;



using System;
using System.Collections.Generic;
using System.Linq;
using static TheRiptide.Utility;
using UnityEngine;
using CommandSystem;
using RemoteAdmin;
using LabApi.Events.CustomHandlers;
using LabApi.Events.Arguments.PlayerEvents;

namespace TheRiptide
{
    public class TrackingConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool TrackHits { get; set; } = true;
        public bool TrackLoadouts { get; set; } = true;
        public bool TrackRounds { get; set; } = true;

        public List<PlayerPermissions> TrackingCmdPermissions { get; set; } =
        [
            PlayerPermissions.ServerConsoleCommands
        ];
    }

    public class Tracking : CustomEventsHandler
    {
        public static Tracking Singleton { get; private set; }
        private TrackingConfig config;

        private Database.Round current_round = new();
        private readonly Dictionary<int, Database.Session> player_sessions = [];
        private readonly Dictionary<int, Database.Life> player_life = [];
        private Action<ReferenceHub, DamageHandlerBase> OnPlayerDamaged;

        public Tracking()
        {
            Singleton = this;
        }

        public void Init(TrackingConfig config)
        {
            this.config = config;
            if (config.TrackHits)
            {
                OnPlayerDamaged = new Action<ReferenceHub, DamageHandlerBase>((hub, damage) =>
                {
                    try
                    {
                        if (hub != null)
                        {
                            if (damage is AttackerDamageHandler attacker && player_life.ContainsKey(hub.PlayerId) && player_life.ContainsKey(attacker.Attacker.PlayerId) && !player_life[hub.PlayerId].received.IsEmpty())
                                player_life[hub.PlayerId].received.Last().damage = (byte)Mathf.Clamp(Mathf.RoundToInt(attacker.DealtHealthDamage), 0, 255);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("on damage error: " + ex.ToString());
                    }
                });
                PlayerStats.OnAnyPlayerDamaged += OnPlayerDamaged;
            }
        }

        public void WaitingForPlayers()
        {
            current_round = null;
        }

        public override void OnServerRoundStarted()
        {
            OnRoundStart();
        }
        void OnRoundStart()
        {
            if (config.TrackRounds)
            {
                current_round = new Database.Round();
                foreach (var ids in player_sessions.Keys.ToList())
                    player_sessions[ids].round = current_round;
            }
        }

        public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
        {
            OnPlayerJoined(ev.Player);
        }
        void OnPlayerJoined(Player player)
        {
            int id = player.PlayerId;
            if (!player_sessions.ContainsKey(id))
                player_sessions.Add(id, new Database.Session());
            else
                player_sessions[player.PlayerId] = new Database.Session();

            Database.Session session = player_sessions[player.PlayerId];
            session.nickname = player.Nickname;
            session.round = current_round;

            if (current_round != null)
                if (Player.Count > current_round.max_players)
                    current_round.max_players = Player.Count;
        }

        public override void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            OnPlayerLeft(ev.Player);
        }
        void OnPlayerLeft(Player player)
        {
            int id = player.PlayerId;
            if (player_sessions.ContainsKey(id))
            {
                player_sessions[player.PlayerId].disconnect = System.DateTime.Now;
                Database.Singleton.SaveTrackingSession(player);
                if (!DmRound.game_ended && !player.DoNotTrack)
                    Database.Singleton.UpdateLeaderBoard(player);
                player_sessions.Remove(id);
            }

            if (player_life.ContainsKey(id))
                player_life.Remove(id);
        }

        public override void OnPlayerDeath(PlayerDeathEventArgs ev)
        {
            OnPlayerDeath(ev.Player, ev.Attacker, ev.DamageHandler);
        }
        void OnPlayerDeath(Player victim, Player killer, DamageHandlerBase damage)
        {
            if(victim != null && killer != null && player_life.ContainsKey(victim.PlayerId) && player_life.ContainsKey(killer.PlayerId))
            {
                Database.Life victim_life = player_life[victim.PlayerId];
                Database.Life killer_life = player_life[killer.PlayerId];
                Database.Kill kill = new();
                victim_life.death = kill;
                killer_life.kills.Add(kill);
                if(damage is StandardDamageHandler standard)
                    kill.hitbox = standard.Hitbox;
                kill.weapon = GetItemFromDamageHandler(damage);
                if (AttachmentsServerHandler.PlayerPreferences[killer.ReferenceHub].ContainsKey(kill.weapon))
                    kill.attachment_code = AttachmentsServerHandler.PlayerPreferences[killer.ReferenceHub][kill.weapon];
            }
        }

        public override void OnPlayerHurt(PlayerHurtEventArgs ev)
        {
            OnPlayerDamage(ev.Attacker, ev.Player, ev.DamageHandler);
        }
        void OnPlayerDamage(Player attacker, Player victim, DamageHandlerBase damage)
        {
            if (config.TrackHits && victim != null && attacker != null && player_life.ContainsKey(victim.PlayerId) && player_life.ContainsKey(attacker.PlayerId))
            {
                if (damage is StandardDamageHandler standard)
                {
                    Database.Life victim_life = player_life[victim.PlayerId];
                    Database.Life attacker_life = player_life[attacker.PlayerId];
                    Database.Hit hit = new();
                    victim_life.received.Add(hit);
                    attacker_life.delt.Add(hit);
                    hit.health = (byte)victim.Health;
                    hit.hitbox = (byte)standard.Hitbox;
                    hit.weapon = (byte)GetItemFromDamageHandler(damage);
                }
            }
        }

        public override void OnPlayerShotWeapon(PlayerShotWeaponEventArgs ev)
        {
            OnPlayerShotWeapon(ev.Player, ev.FirearmItem.Base);
        }
        void OnPlayerShotWeapon(Player player, Firearm firearm)
        {
            if(player != null)
            {
                if (player_life.ContainsKey(player.PlayerId))
                {
                    Database.Life life = player_life[player.PlayerId];
                    if (life != null)
                        life.shots++;
                }
            }
        }

        public void UpdateLeaderBoard()
        {
            foreach(var id in player_sessions.Keys.ToList())
            {
                try
                {
                    if (Player.TryGet(id, out Player player))
                        Database.Singleton.UpdateLeaderBoard(player);
                }
                catch(Exception ex)
                {
                    Logger.Error(ex.ToString());
                }
            }
        }

        public void PlayerSpawn(Player player)
        {
            if(player != null && player_sessions.ContainsKey(player.PlayerId))
            {
                Database.Session session = player_sessions[player.PlayerId];
                Database.Life life = new();
                Database.Loadout loadout = null;
                session.lives.Add(life);
                if (player_life.ContainsKey(player.PlayerId))
                {
                    loadout = player_life[player.PlayerId].loadout;
                    player_life[player.PlayerId] = life;
                }
                else
                    player_life.Add(player.PlayerId, life);
                life.role = Lobby.Singleton.GetSpawn(player).role;
                if (config.TrackLoadouts)
                {
                    loadout ??= new Database.Loadout();

                    var weapon_attachments = AttachmentsServerHandler.PlayerPreferences[player.ReferenceHub];
                    Loadouts.Loadout player_loadout = Loadouts.GetLoadout(player);
                    Database.Loadout new_loadout = new();

                    new_loadout.killstreak_mode = Killstreaks.GetKillstreak(player).name;
                    new_loadout.primary = player_loadout.primary;
                    if (weapon_attachments.ContainsKey(player_loadout.primary))
                        new_loadout.primary_attachment_code = weapon_attachments[player_loadout.primary];
                    new_loadout.secondary = player_loadout.secondary;
                    if (weapon_attachments.ContainsKey(player_loadout.secondary))
                        new_loadout.secondary_attachment_code = weapon_attachments[player_loadout.secondary];
                    new_loadout.tertiary = player_loadout.tertiary;
                    if (weapon_attachments.ContainsKey(player_loadout.tertiary))
                        new_loadout.tertiary_attachment_code = weapon_attachments[player_loadout.tertiary];

                    if (loadout == null || !new_loadout.Equals(loadout))
                        life.loadout = new_loadout;
                    else
                        life.loadout = loadout;
                }
            }
        }

        public Database.Session GetSession(Player player)
        {
            return player_sessions[player.PlayerId];
        }


        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class DmDeleteUser : ICommand
        {
            public bool SanitizeResponse => false;

            public string Command { get; } = "dm_delete_user";

            public string[] Aliases { get; } = [];

            public string Description { get; } = "delete a player from the database using the players id e.g. 762394880234@steam. usage: dm_delete_user <user_id>";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (sender is PlayerCommandSender sender1 && !sender1.CheckPermission([.. Singleton.config.TrackingCmdPermissions], out response))
                    return false;

                if (arguments.Count == 0)
                {
                    response = "usage: dm_delete_user <user_id>";
                    return false;
                }

                string user_id = arguments.At(0);
                Database.Singleton.DeleteData(user_id);

                response = "Deleting data for " + user_id + " if it exists";
                return false;
            }
        }

    }
}
