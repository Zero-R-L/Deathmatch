using InventorySystem.Configs;

namespace TheRiptide
{
    public static class Extensions
    {
        public static ushort GetAmmoLimit(this Player player, ItemType itemType) => InventoryLimits.GetAmmoLimit(itemType, player.ReferenceHub);
    }
}
