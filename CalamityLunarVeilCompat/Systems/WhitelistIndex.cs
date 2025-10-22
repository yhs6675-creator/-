using System;
using System.Collections.Generic;
using CLVCompat.Utils;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    public static class WhitelistIndex
    {
        public static readonly Dictionary<string, List<int>> DisplayNameIndex = new();
        public static readonly HashSet<int> WhitelistTypes = new();
        public static readonly HashSet<string> DisplayNameSet = new(StringComparer.OrdinalIgnoreCase);

        public static void BuildIndex()
        {
            DisplayNameIndex.Clear();

            for (int type = 1; type < ItemLoader.ItemCount; type++)
            {
                string display = Lang.GetItemNameValue(type);
                string key = DisplayNameNormalizer.Normalize(display);

                if (!DisplayNameIndex.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    DisplayNameIndex[key] = list;
                }

                list.Add(type);
            }
        }

        public static void ApplyWhitelist(IEnumerable<string> displayNames)
        {
            WhitelistTypes.Clear();
            DisplayNameSet.Clear();

            var mod = ModContent.GetInstance<CalamityLunarVeilCompat>();

            foreach (var raw in displayNames)
            {
                if (!string.IsNullOrWhiteSpace(raw))
                    DisplayNameSet.Add(raw.Trim());

                string key = DisplayNameNormalizer.Normalize(raw);

                if (DisplayNameIndex.TryGetValue(key, out var types))
                {
                    foreach (var type in types)
                    {
                        WhitelistTypes.Add(type);

                        string display = Lang.GetItemNameValue(type);
                        if (!string.IsNullOrWhiteSpace(display))
                            DisplayNameSet.Add(display.Trim());
                    }
                }
                else
                {
                    mod.Logger.Warn($"[Whitelist] No match: '{raw}' (norm='{key}')");
                }
            }

            mod.Logger.Info($"[Whitelist] Bound types: {WhitelistTypes.Count}");
        }
    }
}
