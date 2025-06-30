﻿using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Armor;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using PlayerRoles;
using PlayerStatsSystem;

using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace TheRiptide
{
    public static class Utility
    {
        public static void AddArmor(Player player, ItemType armor_type, bool dont_remove_excess_on_drop)
        {
            player.AddItem(armor_type);
        }

        public static bool IsArmor(ItemType item)
        {
            return item == ItemType.ArmorLight || item == ItemType.ArmorCombat || item == ItemType.ArmorHeavy;
        }

        public static ItemType GetItemFromDamageHandler(DamageHandlerBase damage)
        {
            if (damage is FirearmDamageHandler firearm)
                return firearm.WeaponType;
            else if (damage is DisruptorDamageHandler)
                return ItemType.ParticleDisruptor;
            else if (damage is ExplosionDamageHandler)
                return ItemType.GrenadeHE;
            else if (damage is JailbirdDamageHandler)
                return ItemType.Jailbird;
            else if (damage is MicroHidDamageHandler)
                return ItemType.MicroHID;
            else if (damage is Scp018DamageHandler)
                return ItemType.SCP018;
            else
                return ItemType.None;
        }

        public static bool IsHumanRole(RoleTypeId role)
        {
            return role == RoleTypeId.ChaosConscript || role == RoleTypeId.ChaosMarauder || role == RoleTypeId.ChaosRepressor || role == RoleTypeId.ChaosRifleman ||
                role == RoleTypeId.ClassD || role == RoleTypeId.FacilityGuard || role == RoleTypeId.NtfCaptain || role == RoleTypeId.NtfPrivate ||
                role == RoleTypeId.NtfSergeant || role == RoleTypeId.NtfSpecialist || role == RoleTypeId.Scientist;
        }

        public static void AddItems(Player player, List<ItemType> items)
        {
            foreach (ItemType i in items)
                if (!player.IsInventoryFull || i == ItemType.SCP330)
                    player.AddItem(i);
        }

        public static bool RemoveItem(Player player, ItemType type)
        {
            var matches = player.Items.Where((i) => i.Type == type);
            if (matches.Count() >= 1)
            {
                player.ReferenceHub.inventory.ServerRemoveItem(matches.First().Serial, null);
                //player.RemoveItem(new Item(matches.First()));
                return true;
            }
            return false;
        }

        public static void AddFirearm(Player player, ItemType type, bool grant_ammo)
        {
            int ammo_reserve = 0;
            int load_ammo = 0;
            Firearm firearm = player.ReferenceHub.inventory.ServerAddItem(type, ItemAddReason.AdminCommand) as Firearm;
            if (firearm != null)
            {
                firearm.TryGetModule(out IPrimaryAmmoContainerModule ammo);

                if (grant_ammo)
                    ammo_reserve = player.GetAmmoLimit(ammo.AmmoType);
                else
                    ammo_reserve = player.GetAmmo(ammo.AmmoType);

                uint attachment_code = AttachmentsServerHandler.PlayerPreferences[player.ReferenceHub][type];
                AttachmentsUtils.ApplyAttachmentsCode(firearm, attachment_code, true);
                load_ammo = math.min(ammo_reserve, ammo.AmmoMax);
                firearm.ApplyAttachmentsCode(attachment_code, true);
                //firearm.Status = new FirearmStatus((byte)load_ammo, FirearmStatusFlags.MagazineInserted, attachment_code);
                ammo_reserve -= load_ammo;
                player.SetAmmo(ammo.AmmoType, (ushort)ammo_reserve);
            }
        }

        public static bool IsGun(ItemType type)
        {
            bool result = false;
            switch (type)
            {
                case ItemType.GunCOM15:
                    result = true;
                    break;
                case ItemType.GunCOM18:
                    result = true;
                    break;
                case ItemType.GunCom45:
                    result = true;
                    break;
                case ItemType.GunFSP9:
                    result = true;
                    break;
                case ItemType.GunCrossvec:
                    result = true;
                    break;
                case ItemType.GunE11SR:
                    result = true;
                    break;
                case ItemType.GunFRMG0:
                    result = true;
                    break;
                case ItemType.GunA7:
                    result = true;
                    break;
                case ItemType.GunAK:
                    result = true;
                    break;
                case ItemType.GunRevolver:
                    result = true;
                    break;
                case ItemType.GunShotgun:
                    result = true;
                    break;
                case ItemType.GunLogicer:
                    result = true;
                    break;
                case ItemType.ParticleDisruptor:
                    result = true;
                    break;
            }
            return result;
        }

        public static ItemType GunAmmoType(ItemType type)
        {
            ItemType ammo = ItemType.None;
            switch (type)
            {
                case ItemType.GunCOM15:
                case ItemType.GunCOM18:
                case ItemType.GunCom45:
                case ItemType.GunFSP9:
                case ItemType.GunCrossvec:
                    ammo = ItemType.Ammo9x19;
                    break;
                case ItemType.GunE11SR:
                case ItemType.GunFRMG0:
                    ammo = ItemType.Ammo556x45;
                    break;
                case ItemType.GunA7:
                case ItemType.GunAK:
                case ItemType.GunLogicer:
                    ammo = ItemType.Ammo762x39;
                    break;
                case ItemType.GunRevolver:
                    ammo = ItemType.Ammo44cal;
                    break;
                case ItemType.GunShotgun:
                    ammo = ItemType.Ammo12gauge;
                    break;
            }
            return ammo;
        }

    }
}