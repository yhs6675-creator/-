using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat
{
    public class CLV_ArmorDefensePlayer : ModPlayer
    {
        public override void PostUpdateEquips()
        {
            var cfg = CLV_DamageConfig.Instance ?? ModContent.GetInstance<CLV_DamageConfig>();
            if (cfg == null || !cfg.EnableArmorDefenseBoost)
                return;

            float multiplier = Math.Max(1f, cfg.ArmorDefenseMultiplier);
            int totalBonus = 0;

            for (int slot = 0; slot < 3; slot++)
            {
                var item = Player.armor[slot];
                if (item?.ModItem?.Mod == null)
                    continue;

                bool isArmorSlot = item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0;
                if (!isArmorSlot)
                    continue;

                if (!CLV_LunarVeilGlobalItem.IsLunarVeilFamily(item.ModItem.Mod.Name))
                    continue;

                int baseDefense = Math.Max(0, item.defense);
                int add = (int)Math.Floor(baseDefense * (multiplier - 1f) + 0.0001f);
                if (add > 0)
                    totalBonus += add;
            }

            if (totalBonus > 0)
                Player.statDefense += totalBonus;
        }
    }
}
