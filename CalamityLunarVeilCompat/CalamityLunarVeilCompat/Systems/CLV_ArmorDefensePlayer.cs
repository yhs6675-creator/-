using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat
{
    public class CLV_ArmorDefensePlayer : ModPlayer
    {
        public override void PostUpdateEquips()
        {
            var config = CLV_DamageConfig.Instance ?? ModContent.GetInstance<CLV_DamageConfig>();
            if (config == null || !config.EnableArmorDefenseBoost)
                return;

            float multiplier = Math.Max(1f, config.ArmorDefenseMultiplier);
            int totalBonus = 0;

            for (int slot = 0; slot < 3; slot++)
            {
                Item item = Player.armor[slot];
                if (item?.ModItem?.Mod == null)
                    continue;

                bool isArmorSlot = item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0;
                if (!isArmorSlot)
                    continue;

                string modName = item.ModItem.Mod.Name;
                if (string.IsNullOrEmpty(modName))
                    continue;

                if (!IsLunarVeilFamily(modName))
                    continue;

                int baseDefense = Math.Max(0, item.defense);
                int bonus = (int)Math.Floor(baseDefense * (multiplier - 1f) + 0.0001f);
                if (bonus > 0)
                    totalBonus += bonus;
            }

            if (totalBonus > 0)
                Player.statDefense += totalBonus;
        }

        private static bool IsLunarVeilFamily(string modName)
        {
            return modName == "Stellamod"
                || modName == "LunarVeilLegacy"
                || modName == "LunarVeil"
                || modName == "LunarVeilLegacyMod"
                || modName.StartsWith("LunarVeil", StringComparison.Ordinal);
        }
    }
}
