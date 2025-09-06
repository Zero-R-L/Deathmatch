﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.CustomHandlers;
using MEC;
using PlayerStatsSystem;



using static TheRiptide.Translation;

namespace TheRiptide
{
    class Killfeeds : CustomEventsHandler
    {
        class Killfeed
        {
            class Item
            {
                public bool is_special = false;
                public string msg = "";
                public float duration = 0.0f;

                public Item()
                {
                }

                public Item(bool is_special, string msg, float duration)
                {
                    this.is_special = is_special;
                    this.msg = msg;
                    this.duration = duration;
                }
            }

            readonly List<Item> killfeed = [];
            CoroutineHandle handle;
            readonly Stopwatch stop_watch = new();
            bool dirty = false;

            public void PushKill(bool is_special, string msg, float duration)
            {
                UpdateDuration();

                if (killfeed.Count >= killfeed_size)
                {
                    IEnumerable<Item> items = killfeed.Where((Item item) => { return !item.is_special; });
                    if (items.IsEmpty())
                    {
                        killfeed.RemoveAt(0);
                    }
                    else
                    {
                        killfeed.Remove(items.First());
                    }
                }
                killfeed.Add(new Item(is_special, msg, duration));
                dirty = true;
            }

            public void UpdateDuration()
            {
                float delta = (float)stop_watch.Elapsed.TotalSeconds;
                stop_watch.Restart();
                for (int i = killfeed.Count - 1; i >= 0; i--)
                {
                    killfeed[i].duration -= delta;
                    if (killfeed[i].duration < 0.0f)
                        killfeed.RemoveAt(i);
                }
            }

            public float Update(Player player, float delta)
            {
                UpdateDuration();

                if (killfeed.IsEmpty())
                    return -1.0f;

                float max_duration = Math.Max(standard_duration, special_duration);
                for (int i = 0; i < killfeed.Count; i++)
                    BroadcastOverride.BroadcastLine(aux_size + i + 1, max_duration, BroadcastPriority.VeryLow, killfeed[i].msg);
                for (int i = killfeed.Count; i < killfeed_size; i++)
                    BroadcastOverride.ClearLine(aux_size + i + 1, BroadcastPriority.VeryLow);

                return killfeed.Min((item) => { return item.duration; });
            }

            public void UpdateIfDirty(Player player)
            {
                if (dirty)
                {
                    dirty = false;
                    if (handle.IsValid)
                        Timing.KillCoroutines(handle);
                    stop_watch.Restart();
                    handle = Timing.RunCoroutine(_Update(player));
                }
            }

            public IEnumerator<float> _Update(Player player)
            {
                float delta = 1.0f;
                while (delta > 0.0f)
                {
                    delta = Update(player, (float)stop_watch.Elapsed.TotalSeconds);
                    yield return Timing.WaitForSeconds(delta);
                }
                yield break;
            }
        }

        static readonly Dictionary<int, Killfeed> player_killfeed = [];

        static readonly float standard_duration = 5.0f;
        static readonly float special_duration = 10.0f;
        static readonly List<int> broadcast_layout = [];
        public static int killfeed_size = 0;
        static int aux_size = 0;

        public static void Init(int aux_lines, int killfeed_lines, int killfeed_line_size)
        {
            aux_size = aux_lines;
            killfeed_size = killfeed_lines;
            int used_space = killfeed_lines * killfeed_line_size;
            int space_left = 178 - used_space;
            int aux_lines_size = space_left / aux_lines;

            for (int i = 0; i < aux_lines; i++)
                broadcast_layout.Add(aux_lines_size);
            for (int i = 0; i < killfeed_lines; i++)
                broadcast_layout.Add(killfeed_line_size);
        }

        public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
        {
            OnPlayerJoined(ev.Player);
        }
        void OnPlayerJoined(Player player)
        {
            if (!player_killfeed.ContainsKey(player.PlayerId))
                player_killfeed.Add(player.PlayerId, new Killfeed());
            SetBroadcastKillfeedLayout(player);
        }

        public override void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            OnPlayerLeft(ev.Player);
        }
        void OnPlayerLeft(Player player)
        {
            if (player_killfeed.ContainsKey(player.PlayerId))
                player_killfeed.Remove(player.PlayerId);
        }

        public static void SetBroadcastKillfeedLayout(Player player)
        {
            BroadcastOverride.SetCustomLineSizes(player, broadcast_layout);
        }

        private static string HitboxToString(HitboxType hitbox)
        {
            string hitbox_string = "";
            switch (hitbox)
            {
                case HitboxType.Body: hitbox_string = "<b><color=#36a832>" + translation.Body + "</color></b>"; break;
                case HitboxType.Limb: hitbox_string = "<b><color=#43BFF0>" + translation.Limb + "</color></b>"; break;
                case HitboxType.Headshot: hitbox_string = "<b><color=#FF0000>" + translation.Head + "</color></b>"; break;
            }
            return hitbox_string;
        }

        public static void PushKill(Player victim, Player killer, DamageHandlerBase damage)
        {
            string kill_msg = "[plugin error] unknown kill pushed: " + damage.CassieDeathAnnouncement.Announcement;
            string killer_name = "";
            if (killer != null)
                killer_name = "<b><color=" + Killstreaks.Singleton.KillstreakColorCode(killer) + ">" + killer.Nickname + "</color></b>";

            string victim_name = "";
            if (victim != null)
                victim_name = "<b><color=" + Killstreaks.Singleton.KillstreakColorCode(victim) + ">" + victim.Nickname + "</color></b>";

            if (damage is FirearmDamageHandler firearm)
            {
                string hit_box = HitboxToString(firearm.Hitbox);
                string team_color = "<color=#eb0d47>";
                switch(firearm.WeaponType)
                {
                    case ItemType.GunCOM15:
                    case ItemType.GunCOM18:
                        team_color = "<color=#ff8e00>";
                        break;
                    case ItemType.GunCom45:
                        team_color = "<color=#ffff7C>";
                        break;
                    case ItemType.GunFSP9:
                        team_color = "<color=#5b6370>";
                        break;
                    case ItemType.GunCrossvec:
                        team_color = "<color=#70c3ff>";
                        break;
                    case ItemType.GunE11SR:
                        team_color = "<color=#0096ff>";
                        break;
                    case ItemType.GunFRMG0:
                        team_color = "<color=#1b43cb>";
                        break;
                    case ItemType.GunA7:
                    case ItemType.GunAK:
                    case ItemType.GunRevolver:
                    case ItemType.GunShotgun:
                    case ItemType.GunLogicer:
                        team_color = "<color=#008f1c>";
                        break;
                }
                string untagged_gun_name = Enum.GetName(typeof(ItemType), firearm.WeaponType).Substring(3);
                string gun_name = "<b>" + team_color + untagged_gun_name + "</color></b>";
                kill_msg = translation.FirearmKill.Replace("{killer}", killer_name).Replace("{victim}", victim_name).Replace("{hitbox}", hit_box).Replace("{gun}", gun_name);
            }
            else if (damage is ExplosionDamageHandler explosion)
            {
                if (victim == killer)
                    kill_msg = translation.ExplosionSelfKill.Replace("{victim}", victim_name);
                else
                    kill_msg = translation.ExplosionKill.Replace("{killer}", killer_name).Replace("{victim}", victim_name);
            }
            else if(damage is JailbirdDamageHandler jailbird)
            {
                string hit_box = HitboxToString(jailbird.Hitbox);
                if (jailbird.Hitbox == HitboxType.Headshot)
                    kill_msg = translation.JailbirdHeadKill.Replace("{killer}", killer_name).Replace("{victim}", victim_name).Replace("{hitbox}", hit_box);
                else
                    kill_msg = translation.JailbirdNormalKill.Replace("{killer}", killer_name).Replace("{victim}", victim_name).Replace("{hitbox}", hit_box);
            }
            else if(damage is Scp018DamageHandler scp018)
            {
                if (victim == killer)
                    kill_msg = translation.Scp018SelfKill.Replace("{victim}", victim_name);
                else
                    kill_msg = translation.Scp018Kill.Replace("{killer}", killer_name).Replace("{victim}", victim_name);
            }
            else if(damage is DisruptorDamageHandler disruptor)
            {
                if (victim == killer)
                    kill_msg = translation.DistruptorSelfKill.Replace("{victim}", victim_name);
                else
                    kill_msg = translation.DistruptorKill.Replace("{killer}", killer_name).Replace("{victim}", victim_name).Replace("{hitbox}", HitboxToString(disruptor.Hitbox));
            }
            else if (damage is CustomReasonDamageHandler custom)
            {
                kill_msg = translation.CustomReasonKill.Replace("{victim}", victim_name).Replace("{reason}", custom.CassieDeathAnnouncement.Announcement);
            }
            else if(killer == null && victim != null)
            {
                Statistics.Stats stats = Statistics.GetStats(victim);
                Loadouts.Loadout loadout = Loadouts.GetLoadout(victim);
                if (Lobby.Singleton.InSpawn(victim) && Loadouts.IsLoadoutEmpty(victim) && stats.kills == 0 && stats.deaths == 0 && !loadout.customising)
                    kill_msg = translation.FailedFirstGrade.Replace("{victim}", victim_name);
                else
                    kill_msg = translation.SelfKill.Replace("{victim}", victim_name);
            }

            foreach (Player player in Player.ReadyList)
            {
                if (player_killfeed.ContainsKey(player.PlayerId))
                {
                    int id = player.PlayerId;
                    Killfeed killfeed = player_killfeed[id];
                    if (id == victim.PlayerId || (killer != null && id == killer.PlayerId))
                        killfeed.PushKill(true, kill_msg, special_duration);
                    else
                        killfeed.PushKill(false, kill_msg, standard_duration);
                }
            }
        }

        public static void UpdateIfDirty(Player player)
        {
            if (player_killfeed.ContainsKey(player.PlayerId))
                player_killfeed[player.PlayerId].UpdateIfDirty(player);
        }

        public static void UpdateAllDirty()
        {
            foreach (Player player in Player.ReadyList)
                UpdateIfDirty(player);
        }
    }
}
