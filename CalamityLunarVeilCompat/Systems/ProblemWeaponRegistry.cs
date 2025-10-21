using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    internal static class ProblemWeaponRegistry
    {
        internal static bool WhitelistHardForce = true;

        private static readonly HashSet<int> ThrowItemTypeIds = new();
        private static readonly HashSet<int> SwappedItemTypeIds = new();
        private static readonly HashSet<int> ProjectileTypeIds = new();

        private static readonly string[] ProjectileNamespaceHints = new[]
        {
            ".Projectiles.Thrown",
            ".Projectiles.Throw",
            ".Projectiles.Throwing",
            ".Projectiles.Cards",
            ".Projectiles.Jar",
            ".Projectiles.Ball",
            ".Projectiles.Spear",
            ".Projectiles.Tomahawk",
            ".Projectiles.Orion",
            ".Projectiles.Ivythorn",
            ".Projectiles.Stinger",
        };

        private static readonly string[] ItemNamespaceHints = new[]
        {
            ".Items.Thrown",
            ".Items.Throw",
            ".Items.Cards",
            ".Items.Jar",
            ".Items.Ball",
            ".Items.Spear",
            ".Items.Tomahawk",
            ".Items.Rogue",
        };

        private static bool initialized;

        internal static void Initialize()
        {
            if (initialized)
                return;

            TryResolveItemsByNamespaceHints(ItemNamespaceHints);
            TryResolveProjectilesByNamespaceHints(ProjectileNamespaceHints);
            initialized = true;
        }

        private static void TryResolveItemsByNamespaceHints(IEnumerable<string> hints)
        {
            for (int type = 1; type < ItemLoader.ItemCount; type++)
            {
                try
                {
                    var item = new Item();
                    item.SetDefaults(type);

                    if (item.ModItem == null)
                        continue;

                    if (!FullNameMatches(item.ModItem, hints))
                        continue;

                    ThrowItemTypeIds.Add(type);
                    SwappedItemTypeIds.Add(type);
                    RegisterResolvedItem(item);
                }
                catch
                {
                }
            }
        }

        private static void TryResolveProjectilesByNamespaceHints(IEnumerable<string> hints)
        {
            for (int type = 0; type < ProjectileLoader.ProjectileCount; type++)
            {
                try
                {
                    var sample = ContentSamples.ProjectilesByType[type];
                    if (sample == null)
                        continue;

                    if (FullNameMatches(sample.ModProjectile, hints))
                        ProjectileTypeIds.Add(type);
                }
                catch
                {
                }
            }
        }

        private static bool FullNameMatches(object modThing, IEnumerable<string> hints)
        {
            if (modThing == null)
                return false;

            string fullName = null;
            string typeFullName = modThing.GetType().FullName;

            if (modThing is ModItem modItem)
                fullName = modItem.FullName;
            else if (modThing is ModProjectile modProjectile)
                fullName = modProjectile.FullName;

            bool Matches(string candidate)
            {
                if (string.IsNullOrEmpty(candidate))
                    return false;

                foreach (var hint in hints)
                {
                    if (!string.IsNullOrWhiteSpace(hint) &&
                        candidate.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }

                return false;
            }

            return Matches(fullName) || Matches(typeFullName);
        }

        private static bool TryMatchItemNamespaces(Item item)
        {
            if (item?.ModItem == null)
                return false;

            if (!FullNameMatches(item.ModItem, ItemNamespaceHints))
                return false;

            ThrowItemTypeIds.Add(item.type);
            SwappedItemTypeIds.Add(item.type);
            RegisterResolvedItem(item);
            return true;
        }

        internal static bool IsProblemThrowItem(Item item)
            => item != null && (ThrowItemTypeIds.Contains(item.type) || TryMatchItemNamespaces(item));

        internal static bool IsProblemSwappedItem(Item item)
            => item != null && (SwappedItemTypeIds.Contains(item.type) || TryMatchItemNamespaces(item));

        internal static bool IsProblemAnyItem(Item item)
            => IsProblemThrowItem(item) || IsProblemSwappedItem(item);

        internal static bool IsProblemProjectile(Projectile projectile)
        {
            if (projectile == null)
                return false;

            if (ProjectileTypeIds.Contains(projectile.type))
                return true;

            if (FullNameMatches(projectile.ModProjectile, ProjectileNamespaceHints))
            {
                RegisterResolvedProjectile(projectile);
                return true;
            }

            return false;
        }

        internal static void AddProjectileTypeRuntime(int projType)
        {
            if (projType <= ProjectileID.None || projType >= ProjectileLoader.ProjectileCount)
                return;

            if (!ProjectileTypeIds.Contains(projType))
                ProjectileTypeIds.Add(projType);
        }

        private static void RegisterResolvedItem(Item item)
        {
            if (item == null)
                return;

            AddProjectileTypeRuntime(item.shoot);
        }

        private static void RegisterResolvedProjectile(Projectile projectile)
        {
            if (projectile == null)
                return;

            AddProjectileTypeRuntime(projectile.type);
        }
    }
}
