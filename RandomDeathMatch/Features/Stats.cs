﻿using PlayerStatsSystem;



using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using CustomPlayerEffects;
using static TheRiptide.Translation;
using LabApi.Events.CustomHandlers;
using LabApi.Events.Arguments.PlayerEvents;

namespace TheRiptide
{
    class Statistics : CustomEventsHandler
    {
        public class LifeStats
        {
            public float damage = 0.0f;
            public int head_shots = 0;
            public int body_shots = 0;
            public int limb_shots = 0;
            public int other_hits = 0;
        }

        public class Stats
        {
            public int killstreak = 0;
            public int highest_killstreak = 0;
            public int kills = 0;
            public int headshot_kills = 0;
            public int deaths = 0;
            public int shots = 0;
            public int shots_hit = 0;
            public int headshots = 0;
            public int time_alive = 0;
            public int damage_delt = 0;
            public int damage_recieved = 0;

            public float start_time = 0;
        }

        public static Dictionary<int, Dictionary<int, LifeStats>> attacker_stats = [];

        public static Dictionary<int, Stats> player_stats = [];
        private static Action<ReferenceHub, DamageHandlerBase> OnPlayerDamaged;

        public static void Init()
        {
            OnPlayerDamaged = new Action<ReferenceHub, DamageHandlerBase>((hub, damage) =>
            {
                try
                {
                    if (hub != null)
                    {
                        if (damage is AttackerDamageHandler attacker)
                        {
                            if (attacker_stats.ContainsKey(attacker.Attacker.PlayerId))
                            {
                                var attacks = attacker_stats[attacker.Attacker.PlayerId];
                                if (attacks.ContainsKey(hub.PlayerId))
                                    attacks[hub.PlayerId].damage += attacker.DealtHealthDamage;
                            }
                            if (player_stats.ContainsKey(attacker.Attacker.PlayerId))
                                player_stats[hub.PlayerId].damage_delt += Mathf.RoundToInt(attacker.DealtHealthDamage);
                        }

                        if (damage is StandardDamageHandler standard && player_stats.ContainsKey(hub.PlayerId))
                            player_stats[hub.PlayerId].damage_recieved += Mathf.RoundToInt(standard.DealtHealthDamage);
                    }
                }
                catch(Exception ex)
                {
                    Logger.Error("on damage error: " + ex.ToString());
                }
            });
            PlayerStats.OnAnyPlayerDamaged += OnPlayerDamaged;
        }

        public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
        {
            OnPlayerJoined(ev.Player);
        }
        void OnPlayerJoined(Player player)
        {
            int id = player.PlayerId;
            if (!player_stats.ContainsKey(id))
                player_stats.Add(id, new Stats());

            if (!attacker_stats.ContainsKey(id))
                attacker_stats.Add(id, []);
        }

        public override void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            OnPlayerLeft(ev.Player);
        }
        void OnPlayerLeft(Player player)
        {
            int id = player.PlayerId;
            if (player_stats.ContainsKey(id))
                player_stats.Remove(id);

            if (attacker_stats.ContainsKey(id))
                attacker_stats.Remove(id);
        }

        public override void OnPlayerDeath(PlayerDeathEventArgs ev)
        {
            OnPlayerDeath(ev.Player, ev.Attacker, ev.DamageHandler);
        }
        void OnPlayerDeath(Player victim, Player killer, DamageHandlerBase damage)
        {
            if (killer != null && killer.IsAlive && player_stats.ContainsKey(killer.PlayerId))
            {
                Stats stats = player_stats[killer.PlayerId];
                stats.kills++;
                if (damage is StandardDamageHandler standard)
                    if (standard.Hitbox == HitboxType.Headshot)
                        stats.headshot_kills++;
                stats.killstreak++;
                if (stats.killstreak > stats.highest_killstreak)
                    stats.highest_killstreak = stats.killstreak;
            }
            if (player_stats.ContainsKey(victim.PlayerId))
            {
                Stats stats = player_stats[victim.PlayerId];

                stats.time_alive += (int)Math.Round(Time.time - stats.start_time);
                stats.killstreak = 0;
                stats.deaths++;

                if(killer != null && player_stats.ContainsKey(killer.PlayerId))
                {
                    string hint = translation.DeathMsgKiller.Replace("{killer}", "<b><color=" + Killstreaks.Singleton.KillstreakColorCode(killer) + ">"+ killer.Nickname + "</color></b>").Replace("{health}", killer.Health.ToString("0"));
                    try
                    {
                        if (killer.ReferenceHub.playerStats.TryGetModule(out AhpStat ahp))
                            hint += translation.DeathMsgAhp.Replace("{ahp}", ahp.CurValue.ToString("0"));
                    }
                    catch(Exception ex)
                    {
                        Logger.Error("Error could not get Ahp of player: " + ex.ToString());
                    }

                    if (killer.ReferenceHub.playerEffectsController.TryGetEffect(out DamageReduction damage_reduction))
                    {
                        if (damage_reduction.IsEnabled && damage_reduction.Intensity != 0)
                            hint += translation.DeathMsgDamageReduction.Replace("{reduction}", (damage_reduction.Intensity / 2.0f).ToString("0.0"));
                    }

                    if (killer.ReferenceHub.playerEffectsController.TryGetEffect(out BodyshotReduction bodyshot_reduction))
                    {
                        if (bodyshot_reduction.IsEnabled && bodyshot_reduction.Intensity != 0)
                        {
                            float percentage = 0.0f;
                            if (bodyshot_reduction.Intensity == 1)
                                percentage = 5.0f;
                            else if (bodyshot_reduction.Intensity == 2)
                                percentage = 10.0f;
                            else if (bodyshot_reduction.Intensity == 3)
                                percentage = 12.5f;
                            else if (bodyshot_reduction.Intensity >= 4)
                                percentage = 15.0f;
                            hint += translation.DeathMsgBodyshotReduction.Replace("{reduction}", percentage.ToString("0.0"));
                        }
                    }

                    if (attacker_stats[victim.PlayerId].ContainsKey(killer.PlayerId))
                    {
                        LifeStats victim_life_stats = attacker_stats[victim.PlayerId][killer.PlayerId];
                        hint += translation.DeathMsgDamageDelt.
                            Replace("{damage}", victim_life_stats.damage.ToString("0")).
                            Replace("{head_shots}", victim_life_stats.head_shots.ToString()).
                            Replace("{body_shots}", victim_life_stats.body_shots.ToString()).
                            Replace("{limb_shots}", victim_life_stats.limb_shots.ToString());
                        if (victim_life_stats.other_hits != 0)
                            hint += translation.DeathMsgDamageOther.
                                Replace("{other_hits}", victim_life_stats.other_hits.ToString());
                    }

                    if(attacker_stats[killer.PlayerId].ContainsKey(victim.PlayerId))
                    {
                        LifeStats attacker_life_stats = attacker_stats[killer.PlayerId][victim.PlayerId];
                        hint += translation.DeathMsgDamageTaken.
                            Replace("{damage}", attacker_life_stats.damage.ToString("0")).
                            Replace("{head_shots}", attacker_life_stats.head_shots.ToString()).
                            Replace("{body_shots}", attacker_life_stats.body_shots.ToString()).
                            Replace("{limb_shots}", attacker_life_stats.limb_shots.ToString());
                        if (attacker_life_stats.other_hits != 0)
                            hint += translation.DeathMsgDamageOther.
                                Replace("{other_hits}", attacker_life_stats.other_hits.ToString());
                    }

                    victim.SendHint(hint, 7.0f);
                }

                attacker_stats[victim.PlayerId].Clear();
                foreach (var p in Player.ReadyList)
                    if (attacker_stats.ContainsKey(p.PlayerId))
                        attacker_stats[p.PlayerId].Remove(victim.PlayerId);
            }
        }

        public override void OnPlayerHurt(PlayerHurtEventArgs ev)
        {
            OnPlayerDamage(ev.Player, ev.Attacker, ev.DamageHandler);
        }
        void OnPlayerDamage(Player victim, Player attacker, DamageHandlerBase damage)
        {
            bool valid_attacker = attacker != null && player_stats.ContainsKey(attacker.PlayerId);
            if (valid_attacker)
            {
                Stats stats = player_stats[attacker.PlayerId];
                stats.shots_hit++;
                if (damage is StandardDamageHandler standard)
                {
                    if (standard.Hitbox == HitboxType.Headshot)
                        stats.headshots++;
                    //stats.damage_delt += (int)math.round(standard.Damage);
                }
            }

            bool valid_victim = victim != null && player_stats.ContainsKey(victim.PlayerId);
            if (valid_victim)
            {
                Stats stats = player_stats[victim.PlayerId];
                if (damage is StandardDamageHandler standard)
                {
                    //stats.damage_recieved += (int)math.round(standard.Damage);
                }
            }

            if(valid_attacker && valid_victim)
            {
                int vid = victim.PlayerId;
                int aid = attacker.PlayerId;

                LifeStats life_stats = new();
                if (!attacker_stats[aid].ContainsKey(vid))
                    attacker_stats[aid].Add(vid, life_stats);
                else
                    life_stats = attacker_stats[aid][vid];

                if (damage is FirearmDamageHandler firearm)
                {
                    //life_stats.damage += firearm.Damage;
                    if (firearm.Hitbox == HitboxType.Headshot)
                        life_stats.head_shots++;
                    else if (firearm.Hitbox == HitboxType.Body)
                        life_stats.body_shots++;
                    else if (firearm.Hitbox == HitboxType.Limb)
                        life_stats.limb_shots++;
                    else
                        life_stats.other_hits++;
                }
                else if(damage is StandardDamageHandler standard)
                {
                    //life_stats.damage += standard.Damage;
                    life_stats.other_hits++;
                }
            }
        }

        public override void OnPlayerShotWeapon(PlayerShotWeaponEventArgs ev)
        {
            player_stats[ev.Player.PlayerId].shots++;
        }

        public static Stats GetStats(Player player)
        {
            return player_stats[player.PlayerId];
        }

        public static void SetPlayerStartTime(Player player, float time)
        {
            player_stats[player.PlayerId].start_time = time;
        }

        public static void DisplayStats(Player player, int line)
        {
            Stats stats = player_stats[player.PlayerId];
            float mins_alive = 60.0f / math.max(stats.time_alive, 300);
            float kd = (float)stats.kills / stats.deaths;
            float HsK = (float)stats.headshot_kills / stats.kills;
            float accuracy = (float)stats.shots_hit / stats.shots;
            float score = kd * HsK * accuracy / mins_alive;

            string stats_msg_1 = translation.PlayerStatsLine1.
                Replace("{kills}", stats.kills.ToString()).
                Replace("{deaths}", stats.deaths.ToString()).
                Replace("{kd}", kd.ToString("0.0")).
                Replace("{top_ks}", stats.highest_killstreak.ToString()).
                Replace("{score}", score.ToString("0.0"));
            string stats_msg_2 = translation.PlayerStatsLine2.
                Replace("{hsk}", (HsK * 100.0f).ToString("0")).
                Replace("{hs}", (((float)stats.headshots / stats.shots_hit) * 100.0f).ToString("0.0")).
                Replace("{accuracy}", (accuracy * 100.0f).ToString("0")).
                Replace("{dmg_delt}", stats.damage_delt.ToString()).
                Replace("{dmg_taken}", stats.damage_recieved.ToString());
            BroadcastOverride.BroadcastLine(player, line, 300, BroadcastPriority.Highest, stats_msg_1);
            BroadcastOverride.BroadcastLine(player, line + 1, 300, BroadcastPriority.Highest, stats_msg_2);
        }

        public static void DisplayRoundStats()
        {
            int highest_killstreak = 0;
            string highest_killstreak_name = "N/A";

            int most_kills = 0;
            string most_kills_name = "N/A";

            float most_score = 0.0f;
            string best_player_name = "N/A";

            foreach (Player player in Player.ReadyList)
            {
                Stats stats = player_stats[player.PlayerId];
                if (stats.highest_killstreak > highest_killstreak)
                {
                    highest_killstreak = stats.highest_killstreak;
                    highest_killstreak_name = player.Nickname;
                }

                if (stats.kills > most_kills)
                {
                    most_kills = stats.kills;
                    most_kills_name = player.Nickname;
                }

                float mins_alive = 60.0f / math.max(stats.time_alive, 300);
                float akd = (stats.kills + 1.0f) / (stats.deaths + 1.0f);
                float HsK = (float)stats.headshot_kills / stats.kills;
                float accuracy = (float)stats.shots_hit / stats.shots;
                float score = akd * HsK * accuracy / mins_alive;

                if (score > most_score)
                {
                    most_score = score;
                    best_player_name = player.Nickname;
                }
            }


            string highest_killstreak_msg = translation.HighestKillstreak.Replace("{name}", highest_killstreak_name).Replace("{streak}", highest_killstreak.ToString());
            string most_kills_msg = translation.HighestKills.Replace("{name}", most_kills_name).Replace("{kills}", most_kills.ToString()); 
            string highest_score_msg = translation.HighestScore.Replace("{name}", best_player_name).Replace("{score}", most_score.ToString());

            foreach (Player player in Player.ReadyList)
                BroadcastOverride.SetEvenLineSizes(player, 5);

            BroadcastOverride.BroadcastLine(1, 300, BroadcastPriority.Highest, highest_killstreak_msg);
            BroadcastOverride.BroadcastLine(2, 300, BroadcastPriority.Highest, most_kills_msg);
            BroadcastOverride.BroadcastLine(3, 300, BroadcastPriority.Highest, highest_score_msg);

            foreach (Player player in Player.ReadyList)
                DisplayStats(player, 4);
            BroadcastOverride.UpdateAllDirty();
        }
    }
}
