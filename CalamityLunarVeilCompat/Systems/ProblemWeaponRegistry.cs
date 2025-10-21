using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    internal static class ProblemWeaponRegistry
    {
        internal static bool WhitelistHardForce = true;

        private static readonly string[] DisplayNames_Throw = new[]
        {
            "Scatterbombs",
            "Zenovias Pikpik Jar",
            "Rogue Igniter Cards MKII",
            "Rogue Igniter Cards",
            "Rogue Cards",
            "Hyus",
            "Ivythorn Shuriken",
            "Larvein Spear",
            "Dirt Glove",
            "Plate",
            "Life Seeking Vial",
            "Lil' stinger",
            "Orion",
        };

        private static readonly string[] DisplayNames_Swapped = new[]
        {
            "Starring Balls",
            "Hookarama",
            "Molted Crust Balls",
            "Gladiator Spear",
            "Frost Monger",
            "Heartspire",
            "Sirius",
            "Voyager",
            "Holmberg Scythe",
            "Palm Tomahawks",
            "No Longer Bridget",
            "Bridget",
            "Pearlescent Ice Balls",
            "Kilvier",
            "The Irradiaspear",
            "Burning Angel",
            "Prismatic Cryadia Balls",
            "Poisoned Angel",
            "Hit me",
            "Vulcan Breaker",
        };

        private static readonly HashSet<int> ThrowItemTypeIds = new();
        private static readonly HashSet<int> SwappedItemTypeIds = new();
        private static readonly HashSet<int> ProjectileTypeIds = new();
        private static readonly HashSet<string> ProjectileFullNames = new(StringComparer.OrdinalIgnoreCase);

        private static bool initialized;

        internal static void Initialize()
        {
            if (initialized)
                return;

            TryResolveByDisplayNames(DisplayNames_Throw, ThrowItemTypeIds);
            TryResolveByDisplayNames(DisplayNames_Swapped, SwappedItemTypeIds);
            initialized = true;
        }

        private static void TryResolveByDisplayNames(IEnumerable<string> names, HashSet<int> bucket)
        {
            var normalizedNames = names
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(Normalize)
                .ToHashSet(StringComparer.Ordinal);

            for (int type = 1; type < ItemLoader.ItemCount; type++)
            {
                try
                {
                    var item = new Item();
                    item.SetDefaults(type);

                    if (item.ModItem == null)
                        continue;

                    var display = item.ModItem.DisplayName?.Value ?? item.Name ?? string.Empty;
                    if (normalizedNames.Contains(Normalize(display)))
                    {
                        bucket.Add(type);
                        RegisterResolvedItem(item);
                    }
                }
                catch
                {
                }
            }
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .Replace("’", "'")
                .Replace("‘", "'")
                .Replace("`", "'")
                .Replace("\"", string.Empty)
                .Replace("  ", " ")
                .ToLowerInvariant();
        }

        private static bool MatchesDisplayNameRuntime(Item item, IEnumerable<string> names, HashSet<int> bucket)
        {
            if (item == null)
                return false;

            var display = item.ModItem?.DisplayName?.Value ?? item.Name ?? string.Empty;
            var normalizedDisplay = Normalize(display);

            if (string.IsNullOrEmpty(normalizedDisplay))
                return false;

            foreach (var raw in names)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var normalizedTarget = Normalize(raw);

                if (string.IsNullOrEmpty(normalizedTarget))
                    continue;

                if (normalizedDisplay.Contains(normalizedTarget) || normalizedTarget.Contains(normalizedDisplay))
                {
                    bucket.Add(item.type);
                    RegisterResolvedItem(item);
                    return true;
                }

                var corrected = normalizedTarget
                    .Replace("lgniter", "igniter")
                    .Replace("larve", "larva")
                    .Replace("starring", "star");

                if (normalizedDisplay.Contains(corrected))
                {
                    bucket.Add(item.type);
                    RegisterResolvedItem(item);
                    return true;
                }
            }

            return false;
        }

        internal static bool IsProblemThrowItem(Item item)
            => item != null && (ThrowItemTypeIds.Contains(item.type) || MatchesDisplayNameRuntime(item, DisplayNames_Throw, ThrowItemTypeIds));

        internal static bool IsProblemSwappedItem(Item item)
            => item != null && (SwappedItemTypeIds.Contains(item.type) || MatchesDisplayNameRuntime(item, DisplayNames_Swapped, SwappedItemTypeIds));

        internal static bool IsProblemAnyItem(Item item)
            => IsProblemThrowItem(item) || IsProblemSwappedItem(item);

        internal static bool IsProblemProjectile(Projectile projectile)
        {
            if (projectile == null)
                return false;

            if (ProjectileTypeIds.Contains(projectile.type))
                return true;

            var modProj = projectile.ModProjectile;
            if (modProj?.Mod != null)
            {
                var fullName = $"{modProj.Mod.Name}/{modProj.Name}";
                if (ProjectileFullNames.Contains(fullName))
                {
                    RegisterResolvedProjectile(projectile);
                    return true;
                }
            }

            return false;
        }

        internal static void AddProjectileTypeRuntime(int projType)
        {
            if (projType <= ProjectileID.None || projType >= ProjectileLoader.ProjectileCount)
                return;

            if (ProjectileTypeIds.Add(projType))
            {
                var sample = ContentSamples.ProjectilesByType[projType];
                var modProj = sample.ModProjectile;
                if (modProj?.Mod != null)
                    ProjectileFullNames.Add($"{modProj.Mod.Name}/{modProj.Name}");
            }
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
