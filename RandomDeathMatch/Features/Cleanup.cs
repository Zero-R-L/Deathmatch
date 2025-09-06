using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using MEC;



using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TheRiptide
{
    public class CleanupConfig
    {
        public bool IsEnabled { get; set; } = true;
        [Description("items to not cleanup when the round start e.g. scp207, medkits ect... [see global reference config for types]")]
        public List<ItemType> InitialCleanupWhitelist { get; set; } = [];

        [Description("how often to cleanup items in seconds. -1 = never")]
        public int ItemCleanupPeriod = 1;
        [Description("items to cleanup throughout the round if dropped by player [see global reference config for types]\n# armor, gun, keycards are automaticaly deleted")]
        public List<ItemType> ItemCleanupBlacklist { get; set; } =
        [
            ItemType.Jailbird
        ];

        [Description("how often to cleanup ragdolls in seconds. -1 = never")]
        public int RagdollCleanupPeriod { get; set; } = -1;
    }

    class Cleanup : CustomEventsHandler
    {
        public static Cleanup Singleton { get; private set; }

        private CleanupConfig config;
        private CoroutineHandle item_cleanup;
        private CoroutineHandle ragdoll_cleanup;

        public Cleanup()
        {
            Singleton = this;
        }

        public void Init(CleanupConfig config)
        {
            this.config = config;
        }

        public override void OnServerRoundStarted()
        {
            OnRoundStart();
        }
        void OnRoundStart()
        {
            Timing.CallDelayed(1.0f, () =>
            {
                foreach (var item in Pickup.List)
                {
                    item.Destroy();
                }

                Timing.KillCoroutines(item_cleanup);
                if (config.ItemCleanupPeriod >= 0)
                    item_cleanup = Timing.RunCoroutine(ItemCleanup());

                Timing.KillCoroutines(ragdoll_cleanup);
                if (config.RagdollCleanupPeriod >= 0)
                    ragdoll_cleanup = Timing.RunCoroutine(RagdollCleanup());
            });
        }

        public void RoundRestart()
        {
            Timing.KillCoroutines(item_cleanup, ragdoll_cleanup);
        }

        private IEnumerator<float> ItemCleanup()
        {
            for (; ; )
            {
                try
                {
                    foreach (var item in Pickup.List)
                    {
                        if (item is AmmoPickup || item is BodyArmorPickup || item is FirearmPickup || item is KeycardPickup || config.ItemCleanupBlacklist.Contains(item.Type))
                        {
                            item.Destroy();
                        }
                    }
                }
                catch(Exception ex)
                {
                    Logger.Error("ItemCleaup Error: " + ex.ToString());
                }

                yield return Timing.WaitForSeconds(config.ItemCleanupPeriod);
            }
        }

        private IEnumerator<float> RagdollCleanup()
        {
            while (true)
            {
                try
                {
                    foreach (var item in Ragdoll.List)
                    {
                        if (item.Base.gameObject == null) continue;
                        try
                        {
                            item.Destroy();
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("RagdollCleaup Error: " + ex.ToString());
                }

                yield return Timing.WaitForSeconds(config.RagdollCleanupPeriod);
            }
        }
    }
}
