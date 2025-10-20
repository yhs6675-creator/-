using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    internal static class ProblemWeaponRegistry
    {
        private static readonly string[] DisplayNamesThrow = new[]
        {
            "Scatterbombs",
            "Zenovias Pikpik Jar",
            "Rogue Lgniter Cards MKII",
            "Rogue lgniter Cards",
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

        private static readonly string[] DisplayNamesSwapped = new[]
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
        };

        private static readonly HashSet<int> ThrowItemTypeIds = new();
        private static readonly HashSet<int> SwappedItemTypeIds = new();
        private static bool initialized;

        internal static void Initialize()
        {
            if (initialized)
                return;

            TryResolveByDisplayNames(DisplayNamesThrow, ThrowItemTypeIds);
            TryResolveByDisplayNames(DisplayNamesSwapped, SwappedItemTypeIds);
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
                        bucket.Add(type);
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

        private static bool MatchesDisplayNameRuntime(Item item, IEnumerable<string> names)
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
                    return true;

                var corrected = normalizedTarget
                    .Replace("lgniter", "igniter")
                    .Replace("larve", "larva")
                    .Replace("starring", "star");

                if (normalizedDisplay.Contains(corrected))
                    return true;
            }

            return false;
        }

        internal static bool IsProblemThrowItem(Item item)
            => item != null && (ThrowItemTypeIds.Contains(item.type) || MatchesDisplayNameRuntime(item, DisplayNamesThrow));

        internal static bool IsProblemSwappedItem(Item item)
            => item != null && (SwappedItemTypeIds.Contains(item.type) || MatchesDisplayNameRuntime(item, DisplayNamesSwapped));

        internal static bool IsProblemAnyItem(Item item)
            => IsProblemThrowItem(item) || IsProblemSwappedItem(item);
    }
}
