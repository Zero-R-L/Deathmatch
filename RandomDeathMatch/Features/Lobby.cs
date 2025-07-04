﻿using AdminToys;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using MapGeneration;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;




using Respawning;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using static TheRiptide.Translation;
using LightSourceToy = AdminToys.LightSourceToy;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;

namespace TheRiptide
{
    public class LobbyConfig
    {
        public int SpawnColorRed{ get; set; } = 67;
        public int SpawnColorGreen { get; set; } = 191;
        public int SpawnColorBlue { get; set; } = 240;
        public float SpawnLightIntensity { get; set; } = 100.00f;
        [Description("max players should be less than SpawnDimX x SpawnDimY")]
        public int SpawnDimX { get; set; } = 8;
        public int SpawnDimY { get; set; } = 8;
        public float SpawnProtection { get; set; } = 3.0f;
    }

    class Lobby : CustomEventsHandler
    {
        public static Lobby Singleton { get; private set; }

        LobbyConfig config;

        public class Spawn
        {
            public RoleTypeId role = RoleTypeId.ClassD;
            public bool in_spawn = true;
            public bool in_spectator_mode = false;
            public CoroutineHandle teleport_handle;
            public int spawn_room = -1;
        }

        public Dictionary<int, Spawn> player_spawns = new Dictionary<int, Spawn>();
        private SortedSet<int> avaliable_spawn_rooms = new SortedSet<int>();
        private List<GameObject> blocks = new List<GameObject>();
        private bool round_started = false;

        public Lobby()
        {
            Singleton = this;
        }

        public void Init(LobbyConfig config)
        {
            this.config = config;
            avaliable_spawn_rooms.Clear();
            for (int i = 0; i < config.SpawnDimX * config.SpawnDimY; i++)
                avaliable_spawn_rooms.Add(i);
        }

        public void MapGenerated()
        {
            Timing.CallDelayed(0.0f, () =>
            {
                BuildSpawn(config.SpawnDimX, config.SpawnDimY);
            });
            round_started = false;

            //foreach (var c in NetworkManager.singleton.spawnPrefabs)
            //    Logger.Info(c.name);
        }

        public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
        {
            OnPlayerJoined(ev.Player);
        }
        void OnPlayerJoined(Player player)
        {
            if (!player_spawns.ContainsKey(player.PlayerId))
            {
                player_spawns.Add(player.PlayerId, new Spawn());
                player_spawns[player.PlayerId].spawn_room = avaliable_spawn_rooms.First();
                avaliable_spawn_rooms.Remove(avaliable_spawn_rooms.First());
            }

            if (!round_started)
            {
                round_started = true;
                MEC.Timing.CallDelayed(1.0f, () => { Round.Start(); });
            }
            else
            {
                MEC.Timing.CallDelayed(1.0f, () => { if (!player.IsAlive) { RespawnPlayer(player); } });
            }

            if (Player.Count == 1)
            {
                DmRound.GameStarted = false;
            }
            else if (Player.Count == 2)
            {
                foreach (var p in Player.ReadyList)
                    if (p.IsAlive)
                        p.ReferenceHub.playerEffectsController.DisableAllEffects();
            }
        }

        public override void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            OnPlayerLeft(ev.Player);
        }
        void OnPlayerLeft(Player player)
        {
            if (player_spawns.ContainsKey(player.PlayerId))
            {
                avaliable_spawn_rooms.Add(player_spawns[player.PlayerId].spawn_room);
                if (!player.DoNotTrack)
                    Database.Singleton.SaveConfigSpawn(player);
                player_spawns.Remove(player.PlayerId);
            }

            if(Player.Count == 2)
            {
                DmRound.GameStarted = false;
                foreach (var p in Player.ReadyList)
                    if (p.IsAlive)
                        ApplyGameNotStartedEffects(player);
            }
        }

        public override void OnPlayerDeath(PlayerDeathEventArgs ev)
        {
            OnPlayerDeath(ev.Player, ev.Attacker, ev.DamageHandler);
        }
        void OnPlayerDeath(Player target, Player killer, DamageHandlerBase damage)
        {
            if (target == null)
                return;

            if (player_spawns.ContainsKey(target.PlayerId))
            {
                Timing.KillCoroutines(player_spawns[target.PlayerId].teleport_handle);
                Killfeeds.PushKill(target, killer, damage);
            }
            Killfeeds.UpdateAllDirty();
            BroadcastOverride.BroadcastLine(target, 1, 300, BroadcastPriority.Low, translation.Respawn);
            BroadcastOverride.BroadcastLine(target, 2, 300, BroadcastPriority.Low, translation.Attachments);
            BroadcastOverride.UpdateAllDirty();
        }

        public override void OnPlayerChangedSpectator(PlayerChangedSpectatorEventArgs ev)
        {
            OnPlayerChangeSpectator(ev.Player, ev.OldTarget, ev.NewTarget);
        }
        void OnPlayerChangeSpectator(Player player, Player old_target, Player new_target)
        {
            if (player == null || !player_spawns.ContainsKey(player.PlayerId))
                return;

            Spawn spawn = player_spawns[player.PlayerId];

            if (!spawn.in_spectator_mode)
            {
                BroadcastOverride.ClearLine(player, 1, BroadcastPriority.VeryLow);
                BroadcastOverride.ClearLine(player, 2, BroadcastPriority.VeryLow);
                BroadcastOverride.UpdateIfDirty(player);
                RespawnPlayer(player);
            }
            else
            {
                if (new_target == null || new_target.PlayerId == Server.Host.PlayerId)
                {
                    spawn.in_spectator_mode = false;
                    RespawnPlayer(player);
                }
            }
        }

        public override void OnPlayerChangingRole(PlayerChangingRoleEventArgs ev)
        {
            if (!OnPlayerChangeRole(ev.Player, ev.OldRole, ev.NewRole, ev.ChangeReason))
            {
                ev.IsAllowed = false;
            }
        }
        bool OnPlayerChangeRole(Player player, PlayerRoleBase old_role, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !player_spawns.ContainsKey(player.PlayerId))
                return true;

            Spawn spawn = player_spawns[player.PlayerId];
            if (new_role != spawn.role && new_role != RoleTypeId.Spectator && new_role != RoleTypeId.Overwatch && new_role != RoleTypeId.Tutorial)
            {
                Timing.CallDelayed(0.0f, () => { player.SetRole(spawn.role, RoleChangeReason.RemoteAdmin); });
                return false;
            }
            else
            {
                return true;
            }
        }

        public override void OnPlayerSpawned(PlayerSpawnedEventArgs ev)
        {
            OnPlayerSpawn(ev.Player, ev.Role.RoleTypeId);
        }
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if (!player_spawns.ContainsKey(player.PlayerId))
                return;

            Spawn spawn = player_spawns[player.PlayerId];

            if (role == spawn.role)
            {
                spawn.in_spawn = true;

                if (!Loadouts.ValidateLoadout(player))
                {
                    BroadcastOverride.BroadcastLine(player, 2, 300, BroadcastPriority.High, translation.Teleport);
                }
                else
                {
                    BroadcastOverride.BroadcastLine(player, 1, 7, BroadcastPriority.Low, translation.Teleporting);
                    BroadcastOverride.BroadcastLine(player, 2, 7, BroadcastPriority.Low, translation.TeleportCancel);
                    Timing.KillCoroutines(spawn.teleport_handle);
                    spawn.teleport_handle = Timing.CallDelayed(7.0f, () =>
                    {
                        if (player == null || !player_spawns.ContainsKey(player.PlayerId))
                            return;
                        if (Player.ReadyList.Count() == 1)
                        {
                            BroadcastOverride.BroadcastLines(player, 1, 1500.0f, BroadcastPriority.Low, translation.WaitingForPlayers);
                            BroadcastOverride.UpdateIfDirty(player);
                        }
                        else if (Player.ReadyList.Count() >= 2 && !DmRound.GameStarted)
                        {
                            DmRound.GameStarted = true;
                            BroadcastOverride.ClearLines(BroadcastPriority.Low);
                            BroadcastOverride.UpdateAllDirty();
                        }
                        TeleportRandom(player);
                    });
                }
                BroadcastOverride.UpdateIfDirty(player);
                Timing.CallDelayed(0.0f, () =>
                {
                    int x = spawn.spawn_room % config.SpawnDimX;
                    int y = spawn.spawn_room / config.SpawnDimY;
                    player.Position = offset + new Vector3(1.0f + x * 2.0f, 0.5f, 1.0f + y * 2.0f);
                });
            }
        }

        public override void OnServerWaveTeamSelecting(WaveTeamSelectingEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public override void OnServerWaveRespawning(WaveRespawningEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public override void OnPlayerEscaping(PlayerEscapingEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public Spawn GetSpawn(Player player)
        {
            return player_spawns[player.PlayerId];
        }

        public void CancelTeleport(Player player)
        {
            Timing.KillCoroutines(player_spawns[player.PlayerId].teleport_handle);
        }

        public void TeleportOutOfSpawn(Player player)
        {
            Spawn spawn = player_spawns[player.PlayerId];

            if (!Loadouts.ValidateLoadout(player))
            {
                if (spawn.in_spawn)
                    BroadcastOverride.BroadcastLine(player, 2, 300, BroadcastPriority.High, translation.Teleport);
            }
            else
            {
                if (spawn.in_spawn)
                {
                    if (player.Role == spawn.role)
                    {
                        BroadcastOverride.BroadcastLine(player, 1, 3, BroadcastPriority.VeryLow, translation.FastTeleport);
                        Timing.KillCoroutines(spawn.teleport_handle);
                        spawn.teleport_handle = Timing.CallDelayed(3.0f, () =>
                        {
                            try
                            {
                                if (Player.ReadyList.Count() == 1)
                                {
                                    BroadcastOverride.BroadcastLines(player, 1, 1500.0f, BroadcastPriority.Low, translation.WaitingForPlayers);
                                    BroadcastOverride.UpdateIfDirty(player);
                                }
                                else if (Player.ReadyList.Count() >= 2 && !DmRound.GameStarted)
                                {
                                    DmRound.GameStarted = true;
                                    BroadcastOverride.ClearLines(BroadcastPriority.Low);
                                    BroadcastOverride.UpdateAllDirty();
                                }
                                TeleportRandom(player);
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e);
                            }
                        });
                    }
                    else
                        player.SetRole(spawn.role, RoleChangeReason.RemoteAdmin);
                }
                else
                {
                    if (Player.ReadyList.Count() == 1)
                    {
                        BroadcastOverride.BroadcastLines(player, 1, 1500.0f, BroadcastPriority.Low, translation.WaitingForPlayers);
                        BroadcastOverride.UpdateIfDirty(player);
                    }
                    else if (Player.ReadyList.Count() >= 2 && !DmRound.GameStarted)
                    {
                        DmRound.GameStarted = true;
                        BroadcastOverride.ClearLines(BroadcastPriority.Low);
                        BroadcastOverride.UpdateAllDirty();
                    }
                }
            }
        }

        public bool InSpawn(Player player)
        {
            return player_spawns[player.PlayerId].in_spawn;
        }

        public void SetRole(Player player, RoleTypeId role)
        {
            player_spawns[player.PlayerId].role = role;
        }

        public void SetSpectatorMode(Player player, bool is_spectator)
        {
            player_spawns[player.PlayerId].in_spectator_mode = is_spectator;
            if(is_spectator)
            {
                BroadcastOverride.ClearLines(player, BroadcastPriority.High);
                Killfeeds.SetBroadcastKillfeedLayout(player);
                BroadcastOverride.BroadcastLine(player, 1, 10, BroadcastPriority.High, translation.SpectatorMode);
                player.SetRole(RoleTypeId.Spectator);
            }
        }

        private void BuildBlock(GameObject prefab, Vector3 offset, Vector3 size)
        {
            Vector3 mid = (offset + (offset + size)) / 2.0f;
            GameObject obj = NetworkManager.Instantiate(prefab, mid, Quaternion.Euler(Vector3.zero));
            PrimitiveObjectToy toy = obj.GetComponent<PrimitiveObjectToy>();
            toy.NetworkScale = size;
            toy.NetworkPrimitiveType = PrimitiveType.Cube;
            Transform t = obj.GetComponent<Transform>();
            t.localScale = size;
            NetworkServer.Spawn(obj);
            blocks.Add(obj);
        }

        private void AddLight(Vector3 position, Color color, float intensity, float range)
        {
            GameObject obj = NetworkManager.Instantiate(NetworkManager.singleton.spawnPrefabs.Where((x) => x.name == "LightSourceToy").First(), position, Quaternion.Euler(Vector3.zero));
            LightSourceToy toy = obj.GetComponent<LightSourceToy>();
            toy.LightColor = color;
            toy.LightIntensity = intensity;
            toy.LightRange = range;
            toy.ShadowStrength = 0.0f;
            NetworkServer.Spawn(obj);
        }

        static Vector3 offset = new Vector3(42.656f, 400f, -47.25f);

        private void BuildSpawn(int x, int y)
        {
            GameObject pot_prefab = NetworkClient.prefabs.Values.First(p => p.name == "PrimitiveObjectToy");

            BuildBlock(pot_prefab, offset, new Vector3(2.0f * x, -0.1f, 2.0f * y));
            //BuildBlock(offset + new Vector3(0.0f, 3.0f, 0.0f), new Vector3(16.0f, 0.1f, 16.0f));
            for (int i = 0; i < x + 1; i++)
            {
                BuildBlock(pot_prefab, offset + new Vector3(i * 2.0f, 0.0f, 0.0f), new Vector3(0.1f, 2.25f, 2.0f * y));
            }
            for (int i = 0; i < y + 1; i++)
            {
                BuildBlock(pot_prefab, offset + new Vector3(0.0f, 0.0f, i * 2.0f), new Vector3(2.0f * x, 2.25f, 0.1f));
            }
            AddLight(offset + new Vector3(x, x + y, y), new Color((float)config.SpawnColorRed / 255.0f, (float)config.SpawnColorGreen / 255.0f, (float)config.SpawnColorBlue / 255.0f), config.SpawnLightIntensity, (x + y) * 2.0f);
        }

        private void RespawnPlayer(Player player)
        {
            if (player.Role == RoleTypeId.Spectator)
            {
                player.SetRole(player_spawns[player.PlayerId].role, RoleChangeReason.Respawn);
            }
        }

        private void TeleportRandom(Player player)
        {
            try
            {
                if (player == null)
                {
                    Logger.Error("could not teleport player because player was null");
                    return;
                }

                if (!player_spawns.ContainsKey(player.PlayerId))
                {
                    Logger.Error("could not teleport player: " + player.Nickname + " because they where never added to players");
                    return;
                }

                Spawn spawn = player_spawns[player.PlayerId];

                if (spawn.in_spawn)
                {
                    RoomIdentifier surface = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
                    List<Vector3> positions = Teleport.RoomPositions(surface);
                    List<bool> occupied_positions = new List<bool>(positions.Count);
                    foreach (var p in positions)
                        occupied_positions.Add(false);

                    HashSet<RoomIdentifier> occupied_rooms = new HashSet<RoomIdentifier>();
                    foreach (Player p in Player.ReadyList)
                    {
                        if (Rooms.ValidPlayerInRoom(p))
                        {
                            if (p.Room.Zone != FacilityZone.Surface)
                                occupied_rooms.Add(p.Room.Base);
                            else
                            {
                                int i = 1;
                                int closest_index = 0;
                                float distance = Vector3.Distance(positions.First(), p.Position);
                                for(; i < positions.Count; i++)
                                {
                                    if(Vector3.Distance(positions[i], p.Position) < distance)
                                    {
                                        closest_index = i;
                                        distance = Vector3.Distance(positions[i], p.Position);
                                    }
                                }
                                occupied_positions[closest_index] = true;
                            }
                        }
                    }
                    if (occupied_positions.All((occupied) => occupied))
                        occupied_positions.Add(surface);

                    HashSet<RoomIdentifier> occupied_adjacent_rooms = new HashSet<RoomIdentifier>();
                    foreach(var o in occupied_rooms)
                    {
                        occupied_adjacent_rooms.Add(o);
                        foreach (var adj in FacilityManager.GetAdjacent(o).Keys)
                            occupied_adjacent_rooms.Add(adj);
                    }

                    IEnumerable<RoomIdentifier> opened_rooms = Rooms.OpenedRooms;
                    IEnumerable<RoomIdentifier> available_rooms = opened_rooms.Except(occupied_adjacent_rooms);
                    if (available_rooms.IsEmpty())
                        available_rooms = opened_rooms.Except(occupied_rooms);
                    if (available_rooms.IsEmpty())
                        available_rooms = opened_rooms;

                    System.Random random = new System.Random();
                    RoomIdentifier room = null;
                    if (!available_rooms.IsEmpty())
                        room = available_rooms.ElementAt(random.Next(available_rooms.Count()));
                    if(room != null)
                    {
                        if (room.Zone != FacilityZone.Surface)
                            Teleport.Room(player, room);
                        else
                        {
                            List<int> indexes = new List<int>();
                            for(int i = 0; i < occupied_positions.Count; i++)
                                if (!occupied_positions[i])
                                    indexes.Add(i);
                            Teleport.RoomAt(player, room, indexes.RandomItem());
                        }
                        if (DmRound.GameStarted)
                        {
                            Statistics.SetPlayerStartTime(player, Time.time);
                            Killstreaks.Singleton.AddKillstreakStartEffects(player);
                        }
                        else
                            ApplyGameNotStartedEffects(player);
                        spawn.in_spawn = false;
                        Tracking.Singleton.PlayerSpawn(player);
                        player.ReferenceHub.playerEffectsController.ChangeState<CustomPlayerEffects.SpawnProtected>(1, config.SpawnProtection);
                    }
                    else
                    {
                        Logger.Error("could not teleport player: " + player.Nickname + " because there was no opened rooms");
                    }
                }
                else
                {
                    Logger.Error("could not teleport player: " + player.Nickname + " because they are not in spawn");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("teleport error: " + ex.ToString());
            }
        }

        public static void ApplyGameNotStartedEffects(Player player)
        {
            player.ReferenceHub.playerEffectsController.ChangeState<CustomPlayerEffects.Scp207>(255, 0);
            player.ReferenceHub.playerEffectsController.ChangeState<CustomPlayerEffects.MovementBoost>(255, 0);
            player.ReferenceHub.playerEffectsController.ChangeState<CustomPlayerEffects.DamageReduction>(255, 0);
        }
    }
}
