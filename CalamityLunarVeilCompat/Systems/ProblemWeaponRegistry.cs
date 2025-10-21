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

        private static readonly string[] DisplayNamesThrow =
        {
            "Scatterbombs",
            "Zenovias Pikpik Jar",
            "Rogue Igniter Cards MKII",
            "Rogue Igniter Cards",
            "Rogue Cards",
            "Rogue Lgniter Cards",
            "Hyus",
            "Hyus (후야)",
            "Hyus(후야)",
            "Ivythorn Shuriken",
            "Larvein Spear",
            "Dirt Glove",
            "Plate",
            "Life Seeking Vial",
            "Lil' stinger",
            "Lil' Stinger",
            "Orion",
        };

        private static readonly string[] DisplayNamesSwapped =
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

            TryResolveItemsByDisplayNames(DisplayNamesThrow, ThrowItemTypeIds);
            TryResolveItemsByDisplayNames(DisplayNamesSwapped, SwappedItemTypeIds);
            TryResolveItemsByNamespaceHints(ItemNamespaceHints);
            TryResolveProjectilesByNamespaceHints(ProjectileNamespaceHints);
            initialized = true;
        }

        private static void TryResolveItemsByDisplayNames(IEnumerable<string> names, HashSet<int> bucket)
        {
            if (names == null)
                return;

            for (int type = 1; type < ItemLoader.ItemCount; type++)
            {
                try
                {
                    var item = new Item();
                    item.SetDefaults(type);

                    if (item.ModItem == null)
                        continue;

                    if (!MatchesDisplayName(item, names))
                        continue;

                    if (bucket.Add(type))
                        RegisterResolvedItem(item);
                }
                catch
                {
                }
            }
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

        private static bool TryMatchItemDisplayName(Item item, IEnumerable<string> names, HashSet<int> bucket)
        {
            if (item?.ModItem == null)
                return false;

            if (!MatchesDisplayName(item, names))
                return false;

            if (!bucket.Add(item.type))
                return true;

            RegisterResolvedItem(item);
            return true;
        }

        private static bool MatchesDisplayName(Item item, IEnumerable<string> names)
        {
            if (item == null)
                return false;

            var display = item.ModItem?.DisplayName?.Value ?? item.Name ?? string.Empty;

            if (MatchesDisplayName(display, names))
                return true;

            var internalName = item.ModItem?.Name;

            if (!string.IsNullOrEmpty(internalName) && !string.Equals(internalName, display, StringComparison.OrdinalIgnoreCase))
                return MatchesDisplayName(internalName, names);

            return false;
        }

        private static bool MatchesDisplayName(string value, IEnumerable<string> names)
        {
            if (names == null)
                return false;

            var normalizedValue = Normalize(value);

            if (string.IsNullOrEmpty(normalizedValue))
                return false;

            foreach (var raw in names)
            {
                var normalizedName = Normalize(raw);

                if (string.IsNullOrEmpty(normalizedName))
                    continue;

                if (normalizedValue.Equals(normalizedName, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (normalizedValue.Contains(normalizedName))
                    return true;

                if (normalizedName.Contains(normalizedValue))
                    return true;

                var corrected = normalizedName.Replace("lgniter", "igniter");

                if (!normalizedName.Equals(corrected, StringComparison.Ordinal))
                {
                    if (normalizedValue.Contains(corrected))
                        return true;
                }
            }

            return false;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value
                .Replace("’", "'")
                .Replace("‘", "'")
                .Replace("`", "'")
                .Replace("\"", "");

            value = string.Join(" ", value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            return value.ToLowerInvariant();
        }

        internal static bool IsProblemThrowItem(Item item)
        {
            if (item == null)
                return false;

            if (ThrowItemTypeIds.Contains(item.type))
                return true;

            if (TryMatchItemDisplayName(item, DisplayNamesThrow, ThrowItemTypeIds))
                return true;

            return TryMatchItemNamespaces(item) && ThrowItemTypeIds.Contains(item.type);
        }

        internal static bool IsProblemSwappedItem(Item item)
        {
            if (item == null)
                return false;

            if (SwappedItemTypeIds.Contains(item.type))
                return true;

            if (TryMatchItemDisplayName(item, DisplayNamesSwapped, SwappedItemTypeIds))
                return true;

            return TryMatchItemNamespaces(item) && SwappedItemTypeIds.Contains(item.type);
        }

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
