using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat
{
    public class Tooltip_WeaponStatus : GlobalItem
    {
        public override bool InstancePerEntity => false;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            var cfg = CLV_DamageConfig.Instance ?? ModContent.GetInstance<CLV_DamageConfig>();
            if (cfg == null || !cfg.ShowCompatTooltips)
                return;

            if (!IsWeapon(item))
                return;

            if (!IsLunarVeilWeapon(item))
                return;

            float mult = cfg.LunarVeilDamageMultiplier;
            if (cfg.EnableMasterScaling && Main.masterMode)
                mult *= Math.Max(1f, cfg.MasterModeExtraMultiplier);

            if (!float.IsFinite(mult) || mult <= 0f)
                mult = 1f;

            string multTxt = mult % 1f == 0f ? mult.ToString("0") : mult.ToString("0.##");

            tooltips.Add(new TooltipLine(Mod, "CLV_WeaponMult_EN",
                $"[CLV] Damage Multiplier (Lunar Veil): x{multTxt}"));

            tooltips.Add(new TooltipLine(Mod, "CLV_WeaponMult_KO",
                $"[CLV] 루나베일 무기 데미지 배율: x{multTxt}"));

            if (cfg.ShowDebugTooltips)
            {
                tooltips.Add(new TooltipLine(Mod, "CLV_WeaponDebug",
                    $"[CLV/DBG] baseDamage={item.damage}, master={Main.masterMode}"));
            }
        }

        private static bool IsWeapon(Item item)
        {
            if (item == null)
                return false;

            if (item.damage <= 0)
                return false;

            if (item.pick > 0 || item.axe > 0 || item.hammer > 0)
                return false;

            return true;
        }

        private static bool IsLunarVeilWeapon(Item item)
        {
            var modName = item.ModItem?.Mod?.Name;
            if (string.IsNullOrEmpty(modName))
                return false;

            if (modName.StartsWith("LunarVeil", StringComparison.Ordinal))
                return true;

            return modName == "Stellamod"
                || modName == "LunarVeilLegacy"
                || modName == "LunarVeil"
                || modName == "LunarVeilLegacyMod";
        }
    }
}
