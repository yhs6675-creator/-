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
            if (!IsWeapon(item) || !IsLunarVeilWeapon(item))
                return;

            var cfg = CLV_DamageConfig.Instance ?? ModContent.GetInstance<CLV_DamageConfig>();
            float multiplier = cfg?.LunarVeilDamageMultiplier ?? 2f;

            if (cfg?.EnableMasterScaling == true && Main.masterMode)
                multiplier *= MathF.Max(1f, cfg.MasterModeExtraMultiplier);

            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0f)
                multiplier = 1f;

            damage *= multiplier;
        }

        private static bool IsWeapon(Item item)
        {
            if (item == null || item.IsAir)
                return false;

            if (item.damage <= 0)
                return false;

            if (item.pick > 0 || item.axe > 0 || item.hammer > 0)
                return false;

            return true;
        }

        private static bool IsLunarVeilWeapon(Item item)
        {
            var modItem = item.ModItem;
            if (modItem == null)
                return false;

            return IsLunarVeilFamily(modItem.Mod?.Name ?? string.Empty);
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
