﻿using MapGeneration;


using Interactables.Interobjects.DoorUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using MEC;
using UnityEngine;

using CustomPlayerEffects;
using PlayerStatsSystem;
using System.ComponentModel;
using static TheRiptide.Translation;
using LabApi.Events.CustomHandlers;
using LabApi.Events.Arguments.PlayerEvents;

namespace TheRiptide
{
    public class RoomsConfig
    {
        public float RoomsPerPlayer { get; set; } = 2.5f;
        [Description("how resistant surface is to changes in its open/close state. higher numbers will keep surface open/closed for longer")]
        public int SurfaceWeight { get; set; } = 5;
        [Description("decon delay. broadcast msg is not sent until time left is less than DecontaminationCaution. we want to buffer this timer to prevent frequent changes in room decon state being seen by the player i.e. lights flickering from yellow to normal")]
        public float DecontaminationTime { get; set; } = 25.0f;
        [Description("broadcast with low priority a caution message to the player. see BroadcastOverride.cs for details about priority")]
        public float DecontaminationCaution { get; set; } = 20.0f;
        [Description("broadcast with medium priority a warning message to the player")]
        public float DecontaminationWarning { get; set; } = 14.0f;
        [Description("broadcast with high priority a danger message to the player")]
        public float DecontaminationDanger { get; set; } = 7.0f;
        public float SurfaceDecontaminationTimeMultiplier { get; set; } = 2.0f;
    }

    public class Rooms : CustomEventsHandler
    {
        public static Rooms Singleton { get; private set; }

        public RoomsConfig config;

        //private static bool open_facility = false;
        private static readonly Dictionary<RoomIdentifier, int> opened_rooms = [];
        private static readonly Dictionary<RoomIdentifier, float> closing_rooms = [];
        private static readonly Dictionary<RoomIdentifier, int> closed_rooms = [];

        private static CoroutineHandle update_handle = new();
        private static CoroutineHandle light_update_handle = new();
        private static CoroutineHandle decontamination_update_handle = new();

        public static IEnumerable<RoomIdentifier> OpenedRooms { get { return opened_rooms.Keys; } }

        public Rooms()
        {
            Singleton = this;
        }

        public void Init(RoomsConfig config)
        {
            this.config = config;
        }

        public override void OnServerRoundStarted()
        {
            OnRoundStart();
        }
        void OnRoundStart()
        {
            update_handle = Timing.RunCoroutine(_Update());
            light_update_handle = Timing.RunCoroutine(_UpdateDecontaminationLights());
            decontamination_update_handle = Timing.RunCoroutine(_UpdateDecontaminator());
        }

        public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
        {
            OnPlayerJoined(ev.Player);
        }
        void OnPlayerJoined(Player player)
        {
            if(Player.Count == 1)
            {
                //UnlockFacility();
                Timing.CallDelayed(7.0f, () =>
                 {
                     if (Player.Count == 1)
                     {
                         UnlockFacility();
                         //open_facility = true;
                     }
                 });
            }
            else if(Player.Count == 2)
            {
                LockdownFacility();
                //if (open_facility)
                //{
                //    LockdownFacility();
                //    open_facility = false;
                //}
                foreach (Player p in Player.ReadyList)
                {
                    if(p != player)
                    {
                        if(SearchForStartRoom(p))
                        {
                            BroadcastOverride.BroadcastLine(p, 1, 300.0f, BroadcastPriority.Low, translation.SecondPlayerJoined.Replace("{name}", player.Nickname));
                            BroadcastOverride.UpdateIfDirty(p);
                            Timing.CallDelayed(10.0f, () =>
                            {
                                if (ValidPlayerInRoom(p) && Deathmatch.IsPlayerValid(player) && !DmRound.GameStarted)
                                    BroadcastOverride.BroadcastLine(p, 2, 290.0f, BroadcastPriority.Low, translation.SecondPlayerHelp.Replace("{name}", player.Nickname));
                                BroadcastOverride.UpdateIfDirty(p);
                            });
                        }
                        else
                        {
                            OpenRoom(ValidRooms.ElementAt(new System.Random().Next(ValidRooms.Count())));
                        }
                        break;
                    }
                }
                ResizeFacility((int)Math.Round(Player.Count * config.RoomsPerPlayer));
            }
            else
            {
                ResizeFacility((int)Math.Round(Player.Count * config.RoomsPerPlayer));
            }
        }

        public override void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            OnPlayerLeft(ev.Player);
        }
        void OnPlayerLeft(Player player)
        {
            if (Player.Count == 2)
            {
                UnlockFacility();
            }
            else if (Player.Count > 2)
            {
                ResizeFacility((int)Math.Round(Player.Count * config.RoomsPerPlayer));
            }
            ServerConsole.AddLog("player: " + player.Nickname + " left. player count: " + Player.Count);
        }

        public override void OnPlayerDeath(PlayerDeathEventArgs ev)
        {
            OnPlayerDeath(ev.Player, ev.Attacker, ev.DamageHandler);
        }
        void OnPlayerDeath(Player target, Player killer, DamageHandlerBase damage)
        {
            if (Player.Count != 1 && target != null && Deathmatch.IsPlayerValid(target))
            {
                ExpandFacility(1);
                ShrinkFacility(1);
            }
        }

        public void RoundRestart()
        {
            Timing.KillCoroutines(update_handle, light_update_handle, decontamination_update_handle);
        }

        private static readonly IEnumerable<RoomIdentifier> ValidRooms = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone != FacilityZone.Other && r.Zone != FacilityZone.None);

        public void UnlockFacility()
        {
            FacilityManager.UnlockAllRooms(DoorLockReason.AdminCommand);
            FacilityManager.OpenAllRooms();
            FacilityManager.ResetAllRoomLights();
            opened_rooms.Clear();
            foreach (var room in ValidRooms)
                    opened_rooms.Add(room, RoomWeight(room));
            closing_rooms.Clear();
            closed_rooms.Clear();
        }

        public void LockdownFacility()
        {
            FacilityManager.LockAllRooms(DoorLockReason.AdminCommand);
            FacilityManager.CloseAllRooms();
            FacilityManager.SetAllRoomLightColors(new Color(1.0f, 0.0f, 0.0f));
            opened_rooms.Clear();
            closing_rooms.Clear();
            closed_rooms.Clear();
            foreach (var room in ValidRooms)
                closed_rooms.Add(room, RoomWeight(room));
        }

        private bool SearchForStartRoom(Player player)
        {
            if (ValidPlayerInRoom(player))
            {
                OpenRoom(player.Room.Base);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ResizeFacility(int count)
        {
            if (count < opened_rooms.Count())
                ShrinkFacility(opened_rooms.Count() - count);
            else if (count > opened_rooms.Count())
                ExpandFacility(count - opened_rooms.Count());
        }

        public void ExpandFacility(int count)
        {
            if(opened_rooms.IsEmpty())
            {
                foreach (var player in Player.ReadyList)
                    if (SearchForStartRoom(player))
                        break;
                if (opened_rooms.IsEmpty())
                    OpenRoom(ValidRooms.ElementAt(new System.Random().Next(ValidRooms.Count())));
                count--;
            }

            for (int i = 0; i < count; i++)
            {
                HashSet<RoomIdentifier> candidate_set = [];
                HashSet<RoomIdentifier> backup_set = [];
                foreach (var room in opened_rooms.Keys)
                {
                    foreach (var adj in FacilityManager.GetAdjacent(room).Keys)
                    {
                        if (closed_rooms.ContainsKey(adj))
                            candidate_set.Add(adj);
                        else if (closing_rooms.ContainsKey(adj))
                            backup_set.Add(adj);
                    }
                }

                if (!candidate_set.IsEmpty())
                {
                    System.Random random = new();
                    while(true)
                    {
                        var room = candidate_set.ElementAt(random.Next(candidate_set.Count()));
                        closed_rooms[room]--;
                        if (closed_rooms[room] <= 0)
                        {
                            OpenRoom(room);
                            break;
                        }
                    }
                }
                else if (!backup_set.IsEmpty())
                {
                    RoomIdentifier max = backup_set.First();
                    backup_set.Remove(max);
                    foreach (var room in backup_set)
                        if (closing_rooms[room] > closing_rooms[max])
                            max = room;
                    OpenRoom(max);
                }
                else
                    break;
            }  
        }

        public void ShrinkFacility(int count)
        {
            System.Random random = new();
            for (int i = 0; i < count; i++)
            {
                if (!opened_rooms.IsEmpty())
                {
                    Dictionary<RoomIdentifier, bool> visited = [];
                    foreach (var room in opened_rooms.Keys)
                        visited.Add(room, false);

                    RoomIdentifier dsf_room = null;
                    Action<RoomIdentifier> DFS = null;
                    DFS = (node) =>
                    {
                        visited[node] = true;
                        dsf_room = node;
                        foreach (var adj in FacilityManager.GetAdjacent(node).Keys)
                            if (opened_rooms.ContainsKey(adj) && !visited[adj])
                                DFS(adj);
                    };

                    while (true)
                    {
                        DFS(opened_rooms.ElementAt(random.Next(opened_rooms.Count())).Key);
                        foreach (var room in opened_rooms.Keys)
                            visited[room] = false;
                        opened_rooms[dsf_room]--;
                        if (opened_rooms[dsf_room] <= 0)
                        {
                            ClosingRoom(dsf_room);
                            break;
                        }
                    }
                }
                else
                    break;
            }
        }

        public static bool ValidPlayerInRoom(Player player)
        {
            return player.IsAlive && player.Room != null && Deathmatch.IsPlayerValid(player) && !Lobby.Singleton.InSpawn(player);
        }

        private static IEnumerator<float> _UpdateDecontaminator()
        {
            const float delta = 1.0f;
            while (true)
            {
                try
                {
                    foreach (Player player in Player.ReadyList)
                    {
                        if (ValidPlayerInRoom(player) && closed_rooms.Keys.Contains(player.Room.Base))
                        {
                            BroadcastOverride.BroadcastLine(player, 1, delta, BroadcastPriority.High, translation.Decontaminating);
                            BroadcastOverride.UpdateIfDirty(player);
                            player.ReferenceHub.playerEffectsController.EnableEffect<Decontaminating>(1);
                        }
                    }
                }
                catch(Exception ex)
                {
                    ServerConsole.AddLog("Rooms._UpdateDecontaminator() error: " + ex.ToString(), ConsoleColor.White);
                }
                yield return Timing.WaitForSeconds(delta);
            }
        }

        private IEnumerator<float> _UpdateDecontaminationLights()
        {
            const float delta = 0.2f;
            while (true)
            {
                try
                {
                    foreach (var pair in closing_rooms)
                    {
                        bool is_surface = IsSurface(pair.Key);
                        float caution = (is_surface ? config.DecontaminationCaution * config.SurfaceDecontaminationTimeMultiplier : config.DecontaminationCaution) + 1.0f;
                        if (caution >= pair.Value)
                        {
                            float x = pair.Value / caution;
                            FacilityManager.SetRoomLightColor(pair.Key, new Color(1.0f, x, 0.0f));
                        }
                    }
                }
                catch(Exception ex)
                {
                    ServerConsole.AddLog("Rooms._UpdateDecontaminationLights() error: " + ex.ToString(), ConsoleColor.White);
                }
                yield return Timing.WaitForSeconds(delta);
            }
        }

        private IEnumerator<float> _Update()
        {
            const float delta = 1.0f/7.0f;
            while (true)
            {
                try
                {
                    //warn players inside rooms marked for closing
                    foreach (Player player in Player.ReadyList)
                    {
                        if (ValidPlayerInRoom(player) && closing_rooms.ContainsKey(player.Room.Base))
                        {
                            bool is_surface = IsSurface(player.Room.Base);
                            float caution = (is_surface ? config.DecontaminationCaution * config.SurfaceDecontaminationTimeMultiplier : config.DecontaminationCaution) + 1.0f;
                            float warning = (is_surface ? config.DecontaminationWarning * config.SurfaceDecontaminationTimeMultiplier : config.DecontaminationWarning) + 1.0f;
                            float danger = (is_surface ? config.DecontaminationDanger * config.SurfaceDecontaminationTimeMultiplier : config.DecontaminationDanger) + 1.0f;
                            float time = closing_rooms[player.Room.Base];
                            if (Math.Abs(time - Math.Round(time)) <= (delta / 2.0f) || danger >= time)
                            {
                                if (caution >= time && time > warning)
                                    BroadcastOverride.BroadcastLine(player, 1, 1.0f + delta, BroadcastPriority.Low, translation.Caution.Replace("{time}", Math.Floor(time - 1.0f).ToString("0")));
                                else if (warning >= time && time > danger)
                                    BroadcastOverride.BroadcastLine(player, 1, 1.0f + delta, BroadcastPriority.Medium, translation.Warning.Replace("{time}", Math.Floor(time - 1.0f).ToString("0")));
                                else if (danger >= time)
                                    BroadcastOverride.BroadcastLine(player, 1, delta, BroadcastPriority.High, translation.Danger.Replace("{time}", time.ToString("0.000")));
                                BroadcastOverride.UpdateIfDirty(player);
                            }
                        }
                    }

                    List<RoomIdentifier> close = [];
                    foreach (RoomIdentifier key in closing_rooms.Keys.ToList())
                    {
                        closing_rooms[key] -= delta;
                        if (closing_rooms[key] < 0.0f)
                            close.Add(key);
                    }

                    foreach (RoomIdentifier room in close)
                        CloseRoom(room);
                }
                catch (Exception ex)
                {
                    ServerConsole.AddLog("Rooms._Update() Error: " + ex.Message + " in " + ex.StackTrace, ConsoleColor.White);
                }

                yield return Timing.WaitForSeconds(delta);
            }
        }

        private void OpenRoom(RoomIdentifier room)
        {
            FacilityManager.ResetRoomLight(room);
            HashSet<RoomIdentifier> joined_rooms = [room];
            foreach (var adj in FacilityManager.GetAdjacent(room).Keys)
                if (opened_rooms.ContainsKey(adj) || closing_rooms.ContainsKey(adj))
                    joined_rooms.Add(adj);
            FacilityManager.UnlockJoinedRooms(joined_rooms, DoorLockReason.AdminCommand);
            if (!opened_rooms.ContainsKey(room))
                opened_rooms.Add(room, RoomWeight(room));
            closing_rooms.Remove(room);
            closed_rooms.Remove(room);
        }

        private void ClosingRoom(RoomIdentifier room)
        {
            if (!closing_rooms.ContainsKey(room))
                closing_rooms.Add(room, RoomDecontaminationTime(room));
            opened_rooms.Remove(room);
            closed_rooms.Remove(room);
        }

        private void CloseRoom(RoomIdentifier room)
        {
            FacilityManager.CloseRoom(room);
            FacilityManager.LockRoom(room, DoorLockReason.AdminCommand);
            FacilityManager.SetRoomLightColor(room, new Color(1.0f, 0.0f, 0.0f));
            if (!closed_rooms.ContainsKey(room))
                closed_rooms.Add(room, RoomWeight(room));
            opened_rooms.Remove(room);
            closing_rooms.Remove(room);
        }

        private static bool IsSurface(RoomIdentifier room)
        {
            return room.Zone == FacilityZone.Surface;
        }

        private float RoomDecontaminationTime(RoomIdentifier room)
        {
            if (room.Zone == FacilityZone.Surface)
                return config.DecontaminationTime * config.SurfaceDecontaminationTimeMultiplier;
            else
            {
                float extened_decontamination_time = config.DecontaminationTime;
                foreach (var adj in FacilityManager.GetAdjacent(room).Keys)
                    if (closing_rooms.ContainsKey(adj) && closing_rooms[adj] > extened_decontamination_time)
                        extened_decontamination_time = closing_rooms[adj];
                return extened_decontamination_time;
            }
        }
        
        private int RoomWeight(RoomIdentifier room)
        {
            if (room.Zone == FacilityZone.Surface)
                return config.SurfaceWeight;
            else
                return 1;
        }

    }
}
