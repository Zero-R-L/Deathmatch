using System;
using System.Collections.Generic;

using MEC;




using PlayerRoles;
using UnityEngine;
using System.ComponentModel;
using CustomPlayerEffects;
using InventorySystem.Items.Firearms.Attachments;
using LightContainmentZoneDecontamination;
using PlayerStatsSystem;
using static TheRiptide.Translation;
using static RoundSummary;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader;

//todo voice and spectate cmd
namespace TheRiptide
{
    public class MainConfig
    {
        public bool IsEnabled { get; set; } = true;

        [Description("round time in minutes")]
        public float RoundTime { get; set; } = 20.0f;
        [Description("round end in seconds")]
        public float RoundEndTime { get; set; } = 30.0f;

        public string DummyPlayerName { get; set; } = "[THE RIPTIDE]";
    }

    public class GlobalReferenceConfig
    {
        [Description("[AUTO GENERATED FILE] may contain types which no longer work. A reference list of types to be used in other configs (do not edit)")]
        public List<ItemType> AllItems { get; set; } = new List<ItemType>();
        public List<string> AllEffects { get; set; } = new List<string>();
        public List<AttachmentName> AllAttachments { get; set; } = new List<AttachmentName>();
    }

    public class Deathmatch : Plugin<MainConfig>
    {
        public Deathmatch()
        {
            Singleton = this;
        }
        public static Deathmatch Singleton { get; private set; }
        public override string Name => "Deathmatch";
        public override string Description => null;
        public override string Author => "The Riptide & ZeroRL";
        public override Version Version => new Version(1, 0, 0, 0);
        public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);

        public GlobalReferenceConfig global_reference_config;
        public RoomsConfig rooms_config;
        public KillstreakConfig killstreak_config;
        public LoadoutConfig loadout_config;
        public LobbyConfig lobby_config;
        public ExperienceConfig experience_config;
        public RankConfig rank_config;
        public TrackingConfig tracking_config;
        public TranslationConfig translation_config;
        public AttachmentBlacklistConfig attachment_blacklist_config;
        public VoiceChatConfig voice_chat_config;
        public CleanupConfig cleanup_config;
        public LeaderBoardConfig leader_board_config;

        public override void LoadConfigs()
        {
            T LoadConfigOrDefault<T>(string configPath) where T : class, new()
            {
                if (this.TryLoadConfig(configPath, out T config))
                {
                    return config;
                }

                Logger.Warn($"Failed to load the {configPath} file, using default values.");
                return default;
            }
            base.LoadConfigs();
            global_reference_config = LoadConfigOrDefault<GlobalReferenceConfig>("global_reference_config.yml");
            rooms_config = LoadConfigOrDefault<RoomsConfig>("rooms_config.yml");
            killstreak_config = LoadConfigOrDefault<KillstreakConfig>("killstreak_config.yml");
            loadout_config = LoadConfigOrDefault<LoadoutConfig>("loadout_config.yml");
            lobby_config = LoadConfigOrDefault<LobbyConfig>("lobby_config.yml");
            experience_config = LoadConfigOrDefault<ExperienceConfig>("experience_config.yml");
            rank_config = LoadConfigOrDefault<RankConfig>("rank_config.yml");
            tracking_config = LoadConfigOrDefault<TrackingConfig>("tracking_config.yml");
            translation_config = LoadConfigOrDefault<TranslationConfig>("translation_config.yml");
            attachment_blacklist_config = LoadConfigOrDefault<AttachmentBlacklistConfig>("attachment_blacklist_config.yml");
            voice_chat_config = LoadConfigOrDefault<VoiceChatConfig>("voice_chat_config.yml");
            cleanup_config = LoadConfigOrDefault<CleanupConfig>("cleanup_config.yml");
            leader_board_config = LoadConfigOrDefault<LeaderBoardConfig>("leader_board_config.yml");
        }
        public override void Enable()
        {
            if (!Config.IsEnabled)
            {
                return;
            }

            Start();
        }
        public override void Disable()
        {
            if (!Config.IsEnabled)
            {
                return;
            }

            Stop();
        }
        DmRound DmRound { get; } = new DmRound();
        InventoryMenu InventoryMenu { get; } = new InventoryMenu();
        BroadcastOverride BroadcastOverride { get; } = new BroadcastOverride();
        //FacilityManager FacilityManager { get; } = new FacilityManager();
        BadgeOverride BadgeOverride { get; } = new BadgeOverride();
        HintOverride HintOverride { get; } = new HintOverride();
        Statistics Statistics { get; } = new Statistics();
        Killfeeds Killfeeds { get; } = new Killfeeds();
        Killstreaks Killstreaks { get; } = new Killstreaks();
        Loadouts Loadouts { get; } = new Loadouts();
        Lobby Lobby { get; } = new Lobby();
        Rooms Rooms { get; } = new Rooms();
        Ranks Ranks { get; } = new Ranks();
        Experiences Experiences { get; } = new Experiences();
        Tracking Tracking { get; } = new Tracking();
        AttachmentBlacklist AttachmentBlacklist { get; } = new AttachmentBlacklist();
        VoiceChat VoiceChat { get; } = new VoiceChat();
        Cleanup Cleanup { get; } = new Cleanup();
        LeaderBoard LeaderBoard { get; } = new LeaderBoard();

        public void Start()
        {
            Database.Singleton.Load(this.GetConfigDirectory().FullName);
            translation = translation_config;
            ServerEvents.MapGenerated += OnMapGenerated;
            ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
            ServerEvents.RoundEnded += OnRoundEnd;
            ServerEvents.RoundRestarted += OnRoundRestart;
            DmRound.Singleton.Init(Config);
            Statistics.Init();
            Rooms.Singleton.Init(rooms_config);
            Killstreaks.Singleton.Init(killstreak_config);
            Loadouts.Singleton.Init(loadout_config);
            Lobby.Singleton.Init(lobby_config);
            CustomHandlersManager.RegisterEventsHandler(DmRound);
            //dependencies
            CustomHandlersManager.RegisterEventsHandler(InventoryMenu);
            CustomHandlersManager.RegisterEventsHandler(BroadcastOverride);
            //CustomHandlersManager.RegisterEventsHandler(FacilityManager);
            CustomHandlersManager.RegisterEventsHandler(BadgeOverride);
            CustomHandlersManager.RegisterEventsHandler(HintOverride);
            BadgeOverride.Singleton.Init(2);

            //features

            CustomHandlersManager.RegisterEventsHandler(Statistics);
            CustomHandlersManager.RegisterEventsHandler(Killfeeds);
            CustomHandlersManager.RegisterEventsHandler(Killstreaks);
            CustomHandlersManager.RegisterEventsHandler(Loadouts);
            CustomHandlersManager.RegisterEventsHandler(Lobby);
            CustomHandlersManager.RegisterEventsHandler(Rooms);
            if (rank_config.IsEnabled)
            {
                Ranks.Singleton.Init(rank_config);
                CustomHandlersManager.RegisterEventsHandler(Ranks);
            }

            if (experience_config.IsEnabled)
            {
                Experiences.Singleton.Init(experience_config);
                CustomHandlersManager.RegisterEventsHandler(Experiences);
            }

            if (tracking_config.IsEnabled)
            {
                Tracking.Singleton.Init(tracking_config);
                CustomHandlersManager.RegisterEventsHandler(Tracking);
            }

            if (attachment_blacklist_config.IsEnabled)
            {
                AttachmentBlacklist.Singleton.Init(attachment_blacklist_config);
                CustomHandlersManager.RegisterEventsHandler(AttachmentBlacklist);
            }

            if (voice_chat_config.IsEnabled)
            {
                VoiceChat.Singleton.Init(voice_chat_config);
                CustomHandlersManager.RegisterEventsHandler(VoiceChat);
            }

            if (cleanup_config.IsEnabled)
            {
                Cleanup.Singleton.Init(cleanup_config);
                CustomHandlersManager.RegisterEventsHandler(Cleanup);
            }

            if (leader_board_config.IsEnabled)
            {
                LeaderBoard.Singleton.Init(leader_board_config);
                CustomHandlersManager.RegisterEventsHandler(LeaderBoard);
            }

            DeathmatchMenu.Singleton.SetupMenus();

            GameCore.ConfigFile.OnConfigReloaded += DmRound.Singleton.OnConfigReloaded;
        }
        public void Stop()
        {
            Database.Singleton.UnLoad();
            //features
            CustomHandlersManager.UnregisterEventsHandler(LeaderBoard);
            CustomHandlersManager.UnregisterEventsHandler(Cleanup);
            CustomHandlersManager.UnregisterEventsHandler(VoiceChat);
            CustomHandlersManager.UnregisterEventsHandler(AttachmentBlacklist);
            CustomHandlersManager.UnregisterEventsHandler(Tracking);
            CustomHandlersManager.UnregisterEventsHandler(Experiences);
            CustomHandlersManager.UnregisterEventsHandler(Ranks);
            CustomHandlersManager.UnregisterEventsHandler(Rooms);
            CustomHandlersManager.UnregisterEventsHandler(Lobby);
            CustomHandlersManager.UnregisterEventsHandler(Loadouts);
            CustomHandlersManager.UnregisterEventsHandler(Killstreaks);
            CustomHandlersManager.UnregisterEventsHandler(Killfeeds);
            CustomHandlersManager.UnregisterEventsHandler(Statistics);

            //dependencies
            CustomHandlersManager.UnregisterEventsHandler(HintOverride);
            CustomHandlersManager.UnregisterEventsHandler(BadgeOverride);
            //EventManager.UnregisterEvents<FacilityManager>(this);
            CustomHandlersManager.UnregisterEventsHandler(BroadcastOverride);
            CustomHandlersManager.UnregisterEventsHandler(InventoryMenu);

            CustomHandlersManager.UnregisterEventsHandler(DmRound);
            ServerEvents.MapGenerated -= OnMapGenerated;
            ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
            ServerEvents.RoundEnded -= OnRoundEnd;
            ServerEvents.RoundRestarted -= OnRoundRestart;

            DeathmatchMenu.Singleton.ClearMenus();

            GameCore.ConfigFile.OnConfigReloaded -= DmRound.Singleton.OnConfigReloaded;
        }
        public void OnMapGenerated(MapGeneratedEventArgs ev)
        {
            FacilityManager.MapGenerated();
            Lobby.Singleton.MapGenerated();
            if (rank_config.IsEnabled)
            {
                Ranks.Singleton.MapGenerated();
            }

            if (leader_board_config.IsEnabled)
            {
                LeaderBoard.Singleton.OnServerMapGenerated(null);
            }
        }
        public void OnWaitingForPlayers()
        {
            GenerateGlobalReferenceConfig();
            DmRound.Singleton.WaitingForPlayers();
            if (tracking_config.IsEnabled)
            {
                Tracking.Singleton.WaitingForPlayers();
            }

            if (voice_chat_config.IsEnabled)
            {
                VoiceChat.Singleton.WaitingForPlayers();
            }
        }
        public void OnRoundEnd(RoundEndedEventArgs ev)
        {
            DmRound.Singleton.RoundEnd();
        }
        public void OnRoundRestart()
        {
            DmRound.Singleton.RoundRestart();
            FacilityManager.RoundRestart();
            Rooms.Singleton.RoundRestart();
            if (cleanup_config.IsEnabled)
            {
                Cleanup.Singleton.RoundRestart();
            }
        }

        public static bool IsPlayerValid(Player player)
        {
            return DmRound.players.Contains(player.PlayerId);
        }

        private void GenerateGlobalReferenceConfig()
        {
            try
            {
                global_reference_config.AllItems.Clear();
                foreach (ItemType item in Enum.GetValues(typeof(ItemType)))
                {
                    global_reference_config.AllItems.Add(item);
                }

                global_reference_config.AllEffects.Clear();
                foreach (StatusEffectBase effect in Server.Host.GameObject.GetComponentsInChildren<StatusEffectBase>(true))
                {
                    global_reference_config.AllEffects.Add(effect.name);
                }

                global_reference_config.AllAttachments.Clear();
                foreach (AttachmentName name in Enum.GetValues(typeof(AttachmentName)))
                {
                    global_reference_config.AllAttachments.Add(name);
                }

                this.SaveConfig(global_reference_config, "global_reference_config.yml");
            }
            catch (Exception e)
            {
                Logger.Error("Global reference config error delete config if this error is common\n " + e.ToString());
            }
        }
    }
}
