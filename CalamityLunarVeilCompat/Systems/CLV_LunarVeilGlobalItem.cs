using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat
{
    public class CLV_LunarVeilGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => false;

        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (item == null || item.IsAir)
                return;

            if (item.pick > 0 || item.axe > 0 || item.hammer > 0)
                return;

            var modItem = item.ModItem;
            if (modItem == null)
                return;

            if (!IsLunarVeilFamily(modItem.Mod?.Name ?? string.Empty))
                return;

            var cfg = CLV_DamageConfig.Instance ?? ModContent.GetInstance<CLV_DamageConfig>();
            float multiplier = cfg?.LunarVeilDamageMultiplier ?? 2f;

            if (cfg?.EnableMasterScaling == true && Main.masterMode)
                multiplier *= MathF.Max(1f, cfg.MasterModeExtraMultiplier);

            if (!float.IsFinite(multiplier) || multiplier <= 0f)
                multiplier = 1f;

            damage *= multiplier;
        }

        internal static bool IsLunarVeilFamily(string modName)
        {
            if (string.IsNullOrEmpty(modName))
                return false;

            return modName == "Stellamod"
                || modName == "LunarVeilLegacy"
                || modName == "LunarVeil"
                || modName == "LunarVeilLegacyMod"
                || modName.StartsWith("LunarVeil", StringComparison.Ordinal);
        }
    }
}
