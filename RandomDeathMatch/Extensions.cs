using InventorySystem;
using InventorySystem.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    public static class Extensions
    {
        public static ushort GetAmmoLimit(this Player player, ItemType itemType) => InventoryLimits.GetAmmoLimit(itemType, player.ReferenceHub);
    }
}
