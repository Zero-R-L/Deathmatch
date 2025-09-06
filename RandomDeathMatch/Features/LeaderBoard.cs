﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LiteDB;
using MEC;



using UnityEngine;
using System.Diagnostics;
using PlayerRoles.FirstPersonControl;
using static TheRiptide.Translation;
using CommandSystem;
using RemoteAdmin;
using LabApi.Events.CustomHandlers;
using LabApi.Loader;
using LabApi.Events.Arguments.PlayerEvents;

namespace TheRiptide
{
    public class LeaderBoardConfig
    {
        public bool IsEnabled { get; set; } = true;

        [Description("Sessions before this date are ignored in the leader board, applies to total kills, highest killstreak and total time. leaderboard needs to be rebuilt with dmrblb in RA console to apply the changes")]
        public DateTime BeginEpoch { get; set; } = new DateTime(DateTime.Now.Year, 1, 1);
        [Description("Sessions after this date are ignored in the leader board, applies to total kills, highest killstreak and total time. leaderboard needs to be rebuilt with dmrblb in RA console to apply the changes")]
        public DateTime EndEpoch { get; set; } = new DateTime(DateTime.Now.Year, 4, 1);
        [Description("How often to advance the Epoch in months. Triggered when the current date is beyond the EndEpoch")]
        public int AutoIncrementPeriod { get; set; } = 3;

        public int LinesPerPage { get; set; } = 26;

        [Description("the delay in seconds after the round ends before showing all the players the leaderboard")]
        public float DisplayEndRoundDelay { get; set; } = 5.0f;

        [Description("which leader board to display at round end -1 = random, 0 = Rank, 1 = Experience, 2 = Killstreak, 3 = Kills, 4 = Time")]
        public int LeaderBoardType { get; set; } = -1;


        [Description("table")]
        public string VOffset { get; set; } = "<voffset=7em>";
        public string Format { get; set; } = "<size=29><line-height=75%><mspace=0.55em>";

        public string LedgendColor { get; set; } = "<color=#43BFF0>";
        public string LedgendHighlightColor { get; set; } = "<color=#FF0000>";

        public string RecordColor { get; set; } = "<color=#43BFF0>";
        public string RecordHighlightColor { get; set; } = "<color=#FF0000>";

        public string TableColor { get; set; } = "<color=#5d318c>";

        public int PositionWidth { get; set; } = 3;
        public int NameWidth { get; set; } = 18;
        public int KillstreakValueWidth { get; set; } = 3;
        public int KillsWidth { get; set; } = 5;
        public int TimeWidth { get; set; } = 4;

        [Description("table characters")]
        public string TopLeftCorner { get; set; } = "╻";
        public string TopJunction { get; set; } = "┯";
        public string TopRightCorner { get; set; } = "╻";
        public string LeftJunction { get; set; } = "┃";
        public string Vertical { get; set; } = "┃";
        public string Horizontal { get; set; } = "━";
        public string LedgendVertical { get; set; } = "│";
        public string RightJunction { get; set; } = "┃";
        public string LedgendHorizontal { get; set; } = "─";
        public string LedgendIntersection { get; set; } = "┼";
        public string RecordSeparator { get; set; } = "│";
        public string BottomLeftCorner { get; set; } = "╹";
        public string BottomJunction { get; set; } = "┷";
        public string BottomRightCorner { get; set; } = "╹";

        [Description("use this after changing the start/end epoch. rebuilding might be very slow")]
        public List<PlayerPermissions> lbCmdPermissions { get; set; } =
        [
            PlayerPermissions.ServerConsoleCommands
        ];
    }

    enum LeaderBoardType { Rank, Experience, Killstreak, Kills, Time }

    class LeaderBoard : CustomEventsHandler
    {
        public static LeaderBoard Singleton { get; private set; }

        public LeaderBoardConfig config;

        class PlayerDetails
        {
            public string name = "";
            public float rank_rating = 0.0f;
            public string rank_tag = "";
            public string rank_color = "";
            public ulong xp_total = 0;
            public string xp_tag = "";
            public int total_kills = 0;
            public int highest_killstreak = 0;
            public string killstreak_tag = "";
            public int total_play_time = 0;
            //public float kill_to_death_ratio;
            //public float hit_head_shot_percentage;
            //public float hit_accuracy_percentage;
            public string record_cache = "";
            public Dictionary<LeaderBoardType, int> position_cache = [];
            public bool connected = false;
        }

        private readonly Dictionary<string, int> user_index = [];
        private readonly List<PlayerDetails> player_details = [];
        private readonly Dictionary<LeaderBoardType, List<int>> type_order = new()
        {
            { LeaderBoardType.Rank,          new List<int>{} },
            { LeaderBoardType.Experience,    new List<int>{} },
            { LeaderBoardType.Killstreak,    new List<int>{} },
            { LeaderBoardType.Kills,         new List<int>{} },
            { LeaderBoardType.Time,          new List<int>{} },
        };

        class State
        {
            public LeaderBoardType type;
            public int page = 0;
            public float cooldown = 0;
        }

        private readonly Dictionary<int, State> player_leader_board_state = [];
        private CoroutineHandle controller;
        private int rank_width;
        private int xp_width;
        private int ks_type_width;
        private int total_width;
        private bool reloading_leader_board = false;

        public bool EnableTitle = true;


        public LeaderBoard()
        {
            Singleton = this;
        }

        public void Init(LeaderBoardConfig config)
        {
            this.config = config;

            rank_width = Ranks.Singleton.config.Ranks.Max(x => x.Tag.Length);
            xp_width = Experiences.Singleton.config.LeaderBoardFormat.Replace("{tier}", "").Replace("{stage}", "").Replace("{level}", "").Length + Experiences.Singleton.config.LeaderBoardTierTags.Max(x => x.Length) + Experiences.Singleton.config.LeaderBoardStageTags.Max(x => x.Length) + Experiences.Singleton.config.LeaderBoardLevelTags.Max(x => x.Length);
            ks_type_width = Killstreaks.Singleton.config.KillstreakTables.Keys.Max(x => x.Length);
            total_width = 1 + config.PositionWidth + 1 + config.NameWidth + 1 + rank_width + 1 + xp_width + 1 + ks_type_width + 1 + config.KillstreakValueWidth + 1 + config.KillsWidth + 1 + config.TimeWidth + 1;

            controller = Timing.RunCoroutine(_Controller());
        }

        public void MapGenerated()
        {
            EnableTitle = true;
            bool dirty = false;
            while (DateTime.Now > config.EndEpoch)
            {
                dirty = true;
                config.BeginEpoch = config.BeginEpoch.AddMonths(config.AutoIncrementPeriod);
                config.EndEpoch = config.EndEpoch.AddMonths(config.AutoIncrementPeriod);
            }
            if (dirty)
                RebuildLeaderBoard();

            Deathmatch.Singleton.SaveConfig(config, "leader_board_config.yml");
        }

        public override void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            OnPlayerLeft(ev.Player);
        }
        void OnPlayerLeft(Player player)
        {
            if (player_leader_board_state.ContainsKey(player.PlayerId))
                player_leader_board_state.Remove(player.PlayerId);
        }

        private int MaxPages(LeaderBoardType type)
        {
            return Mathf.CeilToInt((type_order[type].Count + 1) / (config.LinesPerPage - 1));
        }

        public void EnableLeaderBoardMode(Player player, LeaderBoardType type)
        {
            if (!player_leader_board_state.ContainsKey(player.PlayerId))
                player_leader_board_state.Add(player.PlayerId, new State { type = type });
            HintOverride.Clear(player);
            DisplayLeaderBoard(player, type, 0);
        }

        public void DisableLeaderBoardMode(Player player)
        {
            if (player_leader_board_state.ContainsKey(player.PlayerId))
                player_leader_board_state.Remove(player.PlayerId);
            HintOverride.Clear(player);
            HintOverride.Refresh(player);
        }

        public void DisplayLeaderBoard(Player player, LeaderBoardType type, int page)
        {
            if (player_details.IsEmpty() || reloading_leader_board)
            {
                HintOverride.Add(player, 0, "<color=#FF0000><size=128><b>Loading...</b></size></color>", 1500);
                HintOverride.Refresh(player);
                if (!reloading_leader_board)
                    ReloadLeaderBoard();
                return;
            }

            Func<string, bool, string> highlight = new((s, b) => { return b ? "<b>" + config.LedgendHighlightColor + s + "</color></b>" : config.LedgendColor + s + "</color>"; });
            Func<string, string> tc = new((s) => config.TableColor + s + "</color>");

            string ledgend =
                tc(config.Vertical) + highlight(translation.LedgendPos.PadRight(config.PositionWidth), false) +
                tc(config.LedgendVertical) + highlight(translation.LedgendName.PadRight(config.NameWidth), false) +
                tc(config.LedgendVertical) + highlight(translation.LedgendRank.PadRight(rank_width), type == LeaderBoardType.Rank) +
                tc(config.LedgendVertical) + highlight(translation.LedgendExperience.PadRight(xp_width), type == LeaderBoardType.Experience) +
                tc(config.LedgendVertical) + highlight(translation.LedgendKillstreak.PadRight(config.KillstreakValueWidth + ks_type_width + 1), type == LeaderBoardType.Killstreak) +
                tc(config.LedgendVertical) + highlight(translation.LedgendKills.PadRight(config.KillsWidth), type == LeaderBoardType.Kills) +
                tc(config.LedgendVertical) + highlight(translation.LedgendTime.PadRight(config.TimeWidth), type == LeaderBoardType.Time) +
                tc(config.Vertical) + "\n";

            string lb = config.VOffset + "<line-height=100%>" + (EnableTitle ? translation.LeaderBoardTitle : "\n\n\n") + translation.LeaderBoardControl + config.Format + '\n';
            Func<string, string, string, string, string> build_row = new((l, m, i, r) =>
            {
                return tc(
                l + new string(m.First(), config.PositionWidth) +
                i + new string(m.First(), config.NameWidth) +
                i + new string(m.First(), rank_width) +
                i + new string(m.First(), xp_width) +
                i + new string(m.First(), config.KillstreakValueWidth + ks_type_width + 1) +
                i + new string(m.First(), config.KillsWidth) +
                i + new string(m.First(), config.TimeWidth) +
                r) + "\n";
            });
            lb += build_row(config.TopLeftCorner, config.Horizontal, config.TopJunction, config.TopRightCorner);
            lb += ledgend;
            lb += build_row(config.LeftJunction, config.LedgendHorizontal, config.LedgendIntersection, config.RightJunction);

            if (!user_index.ContainsKey(player.UserId) || !player_details.TryGet(user_index[player.UserId], out PlayerDetails pd))
                pd = new PlayerDetails { connected = true, name = player.Nickname, position_cache = new Dictionary<LeaderBoardType, int> { { LeaderBoardType.Experience, 99999 }, { LeaderBoardType.Kills, 99999 }, { LeaderBoardType.Killstreak, 99999 }, { LeaderBoardType.Rank, 99999 }, { LeaderBoardType.Time, 99999 } } };
            if (!pd.position_cache.ContainsKey(type))
                pd.position_cache.Add(type, type_order[type].FindIndex((p) => user_index[player.UserId] == p));

            int line = page * (config.LinesPerPage - 1);
            int max = Mathf.Min(line + config.LinesPerPage, type_order[type].Count);
            bool drawn_player = false;
            for (int i = line; i < max; i++)
            {
                int position = 0;
                PlayerDetails rd;
                if (!drawn_player && ((i == line && pd.position_cache[type] < i) || (i + 1 == max && pd.position_cache[type] > i)))
                {
                    drawn_player = true;
                    if ((i == line && pd.position_cache[type] < i))
                        i--;
                    max--;

                    rd = pd;
                    position = rd.position_cache[type] + 1;
                }
                else
                {
                    rd = player_details[type_order[type].ElementAt(i)];
                    position = i + 1;
                }

                Func<string, bool, string> record_highlight = new((s, b) => b ? config.RecordHighlightColor + s + "</color>" : config.RecordColor + s + "</color>");
                if(rd.record_cache == "")
                {
                    try
                    {
                        foreach (var p in Player.ReadyList)
                        {
                            if (p != null && p.UserId != null && user_index.ContainsKey(p.UserId) && player_details[user_index[p.UserId]] == rd)
                            {
                                rd.connected = true;
                                break;
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.Error("connection highlight error: " + ex.ToString());
                    }
                    string ks_color = config.RecordColor;
                    if (Killstreaks.Singleton.config.KillstreakTables.ContainsKey(rd.killstreak_tag))
                        ks_color = "<color=" + Killstreaks.Singleton.config.KillstreakTables[rd.killstreak_tag].ColorHex + ">";

                    rd.rank_color = rd.rank_color == "" ? BadgeOverride.ColorNameToHex[Ranks.Singleton.config.UnrankedColor] : rd.rank_color;

                    string name = record_highlight("<noparse>" + (rd.name.Length > config.NameWidth ? rd.name.Substring(0, config.NameWidth) : rd.name).PadRight(config.NameWidth).Replace("{", "｛").Replace("}", "｝") + "</noparse>", rd.connected);
                    string rank = "<color=" + rd.rank_color + ">" + rd.rank_tag.PadRight(rank_width) + "</color>";
                    string xp = rd.xp_tag.PadRight(xp_width);
                    string ks = ks_color + rd.killstreak_tag.PadRight(ks_type_width) + "</color> " + record_highlight(rd.highest_killstreak.ToString().PadRight(config.KillstreakValueWidth), rd.connected);
                    string kills = record_highlight(rd.total_kills.ToString().PadRight(config.KillsWidth), rd.connected);
                    string time = record_highlight((rd.total_play_time/(60.0f * 60.0f)).ToString("0").PadRight(config.TimeWidth), rd.connected);
                    string l = tc(config.RecordSeparator);
                    rd.record_cache = l + name + l + rank + l + xp + l + ks + l + kills + l + time + tc(config.Vertical) + "\n";
                }
                string pos;
                if (position < 1000) pos = record_highlight(position.ToString().PadLeft(config.PositionWidth), rd.connected);
                else pos = record_highlight((Mathf.FloorToInt(position / 1000).ToString() + "K").PadLeft(config.PositionWidth), rd.connected);
                lb += tc(config.Vertical) + pos + rd.record_cache;
            }
            lb += build_row(config.BottomLeftCorner, config.Horizontal, config.BottomJunction, config.BottomRightCorner);
            lb += translation.PageAndLine.
                Replace("{page}", (page + 1).ToString()).
                Replace("{page_max}", (MaxPages(type) + 1).ToString()).
                Replace("{line_start}", (line + 1).ToString()).
                Replace("{line_end}", max.ToString()).
                Replace("{line_max}", type_order[type].Count.ToString());
            HintOverride.Add(player, 0, lb, 1500);
            HintOverride.Refresh(player);
        }

        public void ReloadLeaderBoard()
        {
            reloading_leader_board = true;

            user_index.Clear();
            player_details.Clear();
            foreach (var key in type_order.Keys.ToList())
                type_order[key].Clear();

            Database.Singleton.Async((db) =>
            {
                //user
                var user_collection = db.GetCollection<Database.User>("users");
                user_collection.EnsureIndex(x => x.UserId);
                var users = user_collection.Include(x => x.tracking).Include(x => x.tracking.sessions).FindAll();

                //rank
                var rank_collection = db.GetCollection<Database.Rank>("ranks");
                rank_collection.EnsureIndex(x => x.UserId);
                var db_ranks = rank_collection.FindAll();

                //xp
                var xp_collection =db.GetCollection<Database.Experience>("experiences");
                xp_collection.EnsureIndex(x => x.UserId);
                var db_xps = xp_collection.FindAll();

                //other
                var leader_board_collection = db.GetCollection<Database.LeaderBoard>("leader_board");
                leader_board_collection.EnsureIndex(x => x.UserId);
                var db_leaderboard = leader_board_collection.FindAll();

                Timing.CallDelayed(0.0f,()=>
                {
                    //user
                    foreach (var user in users)
                    {
                        user_index.Add(user.UserId, player_details.Count);
                        player_details.Add(new PlayerDetails());
                        player_details.Last().name = user.tracking.sessions.Last().nickname;
                    }

                    //rank
                    foreach (var rank in db_ranks)
                    {
                        if (user_index.TryGetValue(rank.UserId, out int index) && player_details.TryGet(index, out PlayerDetails details))
                        {
                            RankInfo info = Ranks.Singleton.GetInfo(rank);
                            details.rank_rating = rank.rating;
                            details.rank_tag = info.Tag;
                            details.rank_color = BadgeOverride.ColorNameToHex[info.Color];
                        }
                    }
                    //xp
                    ulong level_stride = (ulong)Experiences.Singleton.MaxLevelXp();
                    ulong stage_stride = level_stride * (ulong)Experiences.Singleton.config.LevelTags.Count;
                    ulong tier_stride = stage_stride * (ulong)Experiences.Singleton.config.StageTags.Count;
                    foreach (var xp in db_xps)
                    {
                        if (user_index.TryGetValue(xp.UserId, out int index) && player_details.TryGet(index, out PlayerDetails details))
                        {
                            details.xp_total = (ulong)xp.tier * tier_stride + (ulong)xp.stage * stage_stride + (ulong)xp.level * level_stride + (ulong)xp.value;
                            details.xp_tag = Experiences.Singleton.LeaderBoardString(new Experiences.XP { tier = xp.tier, stage = xp.stage, level = xp.level, value = xp.value });
                        }
                    }

                    //other
                    foreach (var record in db_leaderboard)
                    {
                        if (user_index.TryGetValue(record.UserId, out int index) && player_details.TryGet(index, out PlayerDetails details))
                        {
                            details.total_kills = record.total_kills;
                            details.highest_killstreak = record.highest_killstreak;
                            details.killstreak_tag = record.killstreak_tag;
                            details.total_play_time = record.total_play_time;
                        }
                    }

                    type_order[LeaderBoardType.Rank] = SortIndex(x => x.rank_rating);
                    type_order[LeaderBoardType.Experience] = SortIndex(x => x.xp_total);
                    type_order[LeaderBoardType.Kills] = SortIndex(x => x.total_kills);
                    type_order[LeaderBoardType.Killstreak] = SortIndex(x => x.highest_killstreak);
                    type_order[LeaderBoardType.Time] = SortIndex(x => x.total_play_time);

                    reloading_leader_board = false;

                    foreach (int id in player_leader_board_state.Keys.ToList())
                    {
                        try
                        {
                            if (Player.TryGet(id, out Player player))
                                DisplayLeaderBoard(player, player_leader_board_state[id].type, player_leader_board_state[id].page);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("update leaderboard error: " + ex.ToString());
                        }
                    }
                });
            });
        }

        public void RebuildLeaderBoard()
        {
            Logger.Info("Rebuilding leader board");
            Database.Singleton.Async((db) =>
            {
                Stopwatch stopwatch = new();
                stopwatch.Start();
                var leader_boards = Database.Singleton.DB.GetCollection<Database.LeaderBoard>("leader_board");
                var users = Database.Singleton.DB.GetCollection<Database.User>("users");
                var tracking = Database.Singleton.DB.GetCollection<Database.Tracking>("tracking");
                var sessions = Database.Singleton.DB.GetCollection<Database.Session>("sessions");
                var lives = Database.Singleton.DB.GetCollection<Database.Life>("lives");

                leader_boards.DeleteAll();
                leader_boards.EnsureIndex(x => x.UserId);
                users.EnsureIndex(x => x.UserId);
                tracking.EnsureIndex(x => x.TrackingId);
                sessions.EnsureIndex(x => x.SessionId);
                lives.EnsureIndex(x => x.LifeId);

                var all_users = users.Include(x => x.tracking).FindAll();
                foreach (var user in all_users)
                {
                    Database.LeaderBoard lb = new() { UserId = user.UserId };
                    var tracker = tracking.Include(x => x.sessions).FindById(user.tracking.TrackingId);
                    if (tracker != null && tracker.sessions != null)
                    {
                        foreach (var session in tracker.sessions)
                        {
                            if (config.BeginEpoch < session.connect && session.connect < config.EndEpoch)
                            {
                                lb.total_play_time += Mathf.CeilToInt((float)(session.disconnect - session.connect).TotalSeconds);
                                foreach (var life_id in session.lives)
                                {
                                    var life = lives.Include(x => x.loadout).FindById(life_id.LifeId);
                                    if (life != null && life.kills != null)
                                    {
                                        int killstreak = 0;
                                        foreach (var kill in life.kills)
                                        {
                                            if (life.death == null || kill.KillId != life.death.KillId)
                                            {
                                                lb.total_kills++;
                                                killstreak++;
                                            }
                                        }
                                        if (killstreak > lb.highest_killstreak)
                                        {
                                            lb.highest_killstreak = killstreak;
                                            lb.killstreak_tag = life.loadout.killstreak_mode;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    leader_boards.Insert(lb);
                }
                stopwatch.Stop();
                Timing.CallDelayed(0.0f,()=>
                {
                    Logger.Info("Finished rebuilding leader board. Time: " + stopwatch.Elapsed.ToString());
                });
            });
        }

        private List<int> SortIndex<T>(Func<PlayerDetails,T> key_selector)
        {
            return [.. player_details.Select((x, i) => new KeyValuePair<PlayerDetails, int>(x, i)).OrderByDescending(x => key_selector(x.Key)).Select(x => x.Value)];
        }

        private IEnumerator<float> _Controller()
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            while (true)
            {
                float delta = (float)stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();
                foreach (var id in player_leader_board_state.Keys.ToList())
                {
                    try
                    {
                        if (Player.TryGet(id, out Player player))
                        {
                            State state = player_leader_board_state[id];
                            state.cooldown -= delta;
                            var mm = player.GameObject.GetComponentInChildren<FirstPersonMovementModule>();
                            if (mm == null)
                            {
                                player_leader_board_state.Remove(id);
                                HintOverride.Clear(player);
                                HintOverride.Refresh(player);
                                continue;
                            }
                            if (mm.Motor.GetFrameMove() == Vector3.zero)
                                state.cooldown = 0.0f;
                            if (state.cooldown <= 0.0f)
                            {
                                bool updated = false;
                                var dir = Quaternion.Inverse(player.GameObject.transform.rotation) * mm.Motor.GetFrameMove();
                                //foward
                                if (dir.z > 0.01f)
                                {
                                    state.page = Mathf.Max(state.page - 1, 0);
                                    updated = true;
                                }
                                //backward
                                if (dir.z < -0.01f)
                                {
                                    state.page = Mathf.Min(state.page + 1, MaxPages(state.type));
                                    updated = true;
                                }
                                //right
                                if (dir.x > 0.01f)
                                {
                                    state.type = (LeaderBoardType)Mathf.Min((int)state.type + 1, Enum.GetValues(typeof(LeaderBoardType)).Length - 1);
                                    state.page = 0;
                                    updated = true;
                                }
                                //left
                                if (dir.x < -0.01f)
                                {
                                    state.type = (LeaderBoardType)Mathf.Max((int)state.type - 1, 0);
                                    state.page = 0;
                                    updated = true;
                                }
                                if (updated)
                                {
                                    state.cooldown = 0.5f;
                                    DisplayLeaderBoard(player, state.type, state.page);
                                }
                            }
                        }
                        else
                            player_leader_board_state.Remove(id);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("LeaderBoard _Controller error: " + ex.ToString());
                        player_leader_board_state.Remove(id);
                    }
                }
                yield return Timing.WaitForOneFrame;
            }
        }

        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class DmRebuildLeaderBoard : ICommand
        {
            public bool SanitizeResponse => false;

            public string Command { get; } = "dm_rebuild_lb";

            public string[] Aliases { get; } = ["dmrblb"];

            public string Description { get; } = "rebuilds leader board using the updated config. warning this command might be very slow";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (sender is PlayerCommandSender sender1 && !sender1.CheckPermission([.. Singleton.config.lbCmdPermissions], out response))
                    return false;

                Singleton.RebuildLeaderBoard();

                response = "rebuilding... check server console for results";
                return true;
            }
        }
    }
}
