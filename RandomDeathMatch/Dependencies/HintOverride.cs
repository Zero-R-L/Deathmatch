﻿using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.CustomHandlers;
using MEC;



using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TheRiptide
{
    class HintOverride : CustomEventsHandler
    {
        class HintInfo
        {
            class Hint
            {
                public string msg;
                public float duration = -1.0f;
            }

            readonly SortedDictionary<int, Hint> active_hints = [];
            readonly Stopwatch stop_watch = new();
            CoroutineHandle handle = new();

            public HintInfo()
            {
                stop_watch.Start();
            }

            public void Add(int id, string msg, float duration)
            {
                UpdateDuration();

                if (!active_hints.ContainsKey(id))
                    active_hints.Add(id, new Hint { msg = msg, duration = duration });
                else
                    active_hints[id] = new Hint { msg = msg, duration = duration };
            }

            public void Remove(int id)
            {
                active_hints.Remove(id);
            }

            public void Clear()
            {
                active_hints.Clear();
            }

            public void Refresh(Player player)
            {
                if (handle.IsValid)
                    Timing.KillCoroutines(handle);
                stop_watch.Restart();
                handle = Timing.RunCoroutine(_Update(player));
            }

            private void UpdateDuration()
            {
                float delta = (float)stop_watch.Elapsed.TotalSeconds;
                stop_watch.Restart();

                foreach (int id in active_hints.Keys.ToList())
                    active_hints[id].duration -= delta;
            }

            private float Update(Player player)
            {
                UpdateDuration();

                string msg = "";
                foreach (Hint hint in active_hints.Values)
                    if (hint.duration > 0.0f)
                        msg += hint.msg;

                player.SendHint(msg, 300);

                float min = 300.0f;
                bool any_active = false;
                foreach(var id in active_hints.Keys.ToList())
                {
                    if (active_hints[id].duration > 0.0f)
                    {
                        min = Math.Min(min, active_hints[id].duration);
                        any_active = true;
                    }
                    else
                        active_hints.Remove(id);

                }
                if (any_active)
                    return min;
                else
                    return -1.0f;
            }

            private IEnumerator<float> _Update(Player player)
            {
                float delta = 1.0f;
                while (delta > 0.0f)
                {
                    delta = Update(player);
                    yield return Timing.WaitForSeconds(delta);
                }
                yield break;
            }
        }

        private static readonly Dictionary<int, HintInfo> hint_info = [];

        public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
        {
            OnPlayerJoined(ev.Player);
        }
        void OnPlayerJoined(Player player)
        {
            if (!hint_info.ContainsKey(player.PlayerId))
                hint_info.Add(player.PlayerId, new HintInfo());
        }

        public override void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            OnPlayerLeft(ev.Player);
        }
        void OnPlayerLeft(Player player)
        {
            if (hint_info.ContainsKey(player.PlayerId))
                hint_info.Remove(player.PlayerId);
        }

        public static void Add(Player player, int id, string msg, float duration)
        {
            if (hint_info.ContainsKey(player.PlayerId))
                hint_info[player.PlayerId].Add(id, msg, duration);
        }

        public static void Remove(Player player, int id)
        {
            if (hint_info.ContainsKey(player.PlayerId))
                hint_info[player.PlayerId].Remove(id);
        }

        public static void Clear(Player player)
        {
            if (hint_info.ContainsKey(player.PlayerId))
                hint_info[player.PlayerId].Clear();
        }

        public static void Refresh(Player player)
        {
            if (hint_info.ContainsKey(player.PlayerId))
                hint_info[player.PlayerId].Refresh(player);
        }

        public static void Refresh()
        {
            foreach (var p in Player.ReadyList)
                if (p.IsReady && hint_info.ContainsKey(p.PlayerId))
                    hint_info[p.PlayerId].Refresh(p);
        }
    }
}
