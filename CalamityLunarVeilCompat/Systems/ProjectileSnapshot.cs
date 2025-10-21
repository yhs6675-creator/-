using Terraria;
using Terraria.DataStructures;

namespace CLVCompat.Systems
{
    internal static class ProjectileSnapshot
    {
        public static void MarkNextAsRogue(Player player)
        {
            player?.GetModPlayer<RogueContext>()?.MarkNextProjectile();
        }

        public static bool TryConsumeMark(IEntitySource source)
        {
            if (source == null)
                return false;

            Player player = source switch
            {
                EntitySource_ItemUse_WithAmmo withAmmo => withAmmo.Entity as Player,
                EntitySource_ItemUse use => use.Entity as Player,
                EntitySource_Parent parent => parent.Entity as Player,
                _ => null
            };

            return player != null && player.GetModPlayer<RogueContext>().TryConsumeMark(source);
        }
    }
}
