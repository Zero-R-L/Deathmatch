using InventorySystem.Items;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.CustomHandlers;
using MEC;
using PlayerStatsSystem;



using System;
using System.Collections.Generic;
using System.Linq;
using UsableItem = InventorySystem.Items.Usables.UsableItem;

namespace TheRiptide
{
    public class MenuItem(ItemType item, string description, Func<Player, bool> on_click)
    {
        public ItemType item = item;
        public string description = description;
        public Func<Player, bool> on_click = on_click;
    }

    public class Menu(string description, List<MenuItem> items)
    {
        public string description = description;
        public List<MenuItem> items = items;
    }

    public struct MenuInfo(int total_items, int broadcast_lines)
    {
        public int total_items = total_items;
        public int broadcast_lines = broadcast_lines;
    }

    public class InventoryMenu : CustomEventsHandler
    {
        public static InventoryMenu Singleton { get; private set; }

        static readonly Dictionary<int, int> player_menu = [];
        static readonly Dictionary<int, Menu> menus = [];

        public InventoryMenu()
        {
            Singleton = this;
        }

        public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
        {
            OnPlayerJoined(ev.Player);
        }
        void OnPlayerJoined(Player player)
        {
            if (!player_menu.ContainsKey(player.PlayerId))
                player_menu.Add(player.PlayerId, 0);
        }

        public override void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            OnPlayerLeft(ev.Player);
        }
        void OnPlayerLeft(Player player)
        {
            if (player_menu.ContainsKey(player.PlayerId))
                player_menu.Remove(player.PlayerId);
        }

        public override void OnPlayerDroppingItem(PlayerDroppingItemEventArgs ev)
        {
            if (!OnPlayerDropitem(ev.Player, ev.Item.Base))
            {
                ev.IsAllowed = false;
            }
        }
        bool OnPlayerDropitem(Player player, ItemBase item)
        {
            if (!player_menu.ContainsKey(player.PlayerId))
                return true;
            bool allow_drop = true;
            Menu menu = menus[player_menu[player.PlayerId]];
            Predicate<MenuItem> has_item = (menu_item) => { return menu_item.item == item.ItemTypeId; };
            if (menu.items.Exists(has_item))
                allow_drop = menu.items.Find(has_item).on_click(player);
            BroadcastOverride.UpdateIfDirty(player);
            return allow_drop;
        }

        public override void OnPlayerUsingItem(PlayerUsingItemEventArgs ev)
        {
            if (!OnPlayerUseItem(ev.Player, ev.UsableItem.Base))
            {
                ev.IsAllowed = false;
            }
        }
        bool OnPlayerUseItem(Player player, UsableItem item)
        {
            if (!player_menu.ContainsKey(player.PlayerId))
                return true;
            if (player_menu[player.PlayerId] != 0)
                return false;
            return true;
        }

        public override void OnPlayerThrowingItem(PlayerThrowingItemEventArgs ev)
        {
            if (!player_menu.ContainsKey(ev.Player.PlayerId))
                return;
            if (player_menu[ev.Player.PlayerId] != 0)
                ev.IsAllowed = false;
        }

        public override void OnPlayerThrowingProjectile(PlayerThrowingProjectileEventArgs ev)
        {
            //todo fix
            if (!player_menu.ContainsKey(ev.Player.PlayerId))
            if (player_menu[ev.Player.PlayerId] != 0)
                ev.IsAllowed = false;
        }

        public override void OnPlayerDeath(PlayerDeathEventArgs ev)
        {
            OnPlayerDeath(ev.Player, ev.Attacker, ev.DamageHandler);
        }
        void OnPlayerDeath(Player target, Player killer, DamageHandlerBase damage)
        {
            if (!player_menu.ContainsKey(target.PlayerId))
                return;

            if(player_menu[target.PlayerId] != 0)
            {
                BroadcastOverride.ClearLines(target, BroadcastPriority.High);
                player_menu[target.PlayerId] = 0;
            }
        }

        public static void CreateMenu(int id, string description, List<MenuItem> items)
        {
            menus.Add(id, new Menu(description, items));
        }

        public static void ShowMenu(Player player, int menu_id)
        {
            SetMenu(player, menu_id);
            Menu menu = menus[menu_id];

            player.ClearInventory();
            player.ReferenceHub.inventory.SendAmmoNextFrame = true;
            BroadcastOverride.ClearLines(player, BroadcastPriority.High);
            List<string> broadcast = [];
            List<ItemType> items = [];
            if (menu.description != "")
                broadcast.Add(menu.description);
            for (int i = 0; i < menu.items.Count(); i++)
            {
                if (menu.items[i].description != "")
                    broadcast.Add(menu.items[i].description);
                items.Add(menu.items[i].item);
            }
            if (!broadcast.IsEmpty())
            {
                if (broadcast.Count >= 7)
                    BroadcastOverride.SetEvenLineSizes(player, broadcast.Count() + 1);
                else
                    BroadcastOverride.SetEvenLineSizes(player, 7);
                BroadcastOverride.BroadcastLines(player, 1, 1500.0f, BroadcastPriority.High, broadcast);
            }

            int index = 0;
            Action add_items_inorder = null;
            add_items_inorder = () =>
            {
                player.AddItem(items[index]);
                index++;
                if (index < items.Count)
                {
                    Timing.CallDelayed(0.0f, add_items_inorder);
                }
            };
            Timing.CallDelayed(0.0f, add_items_inorder);
        }

        public static void SetMenu(Player player, int menu_id)
        {
            player_menu[player.PlayerId] = menu_id;
        }

        public static MenuInfo GetInfo(int menu_id)
        {
            Menu menu = menus[menu_id];
            int broadcast_lines = 0;
            if (menu.description != "")
                broadcast_lines++;
            foreach (MenuItem item in menu.items)
                if (item.description != "")
                    broadcast_lines++;
            return new MenuInfo(menu.items.Count, broadcast_lines);
        }

        public static int GetPlayerMenuID(Player player)
        {
            return player_menu[player.PlayerId];
        }

        public static void Clear()
        {
            menus.Clear();
            foreach (var id in player_menu.Keys.ToList())
                player_menu[id] = 0;
        }
    }
}