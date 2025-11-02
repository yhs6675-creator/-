using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat
{
    public class Tooltip_WeaponStatus : GlobalItem
    {
        public override bool InstancePerEntity => false;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item == null)
            {
                return;
            }

            var cfg = CLV_DamageConfig.Instance ?? ModContent.GetInstance<CLV_DamageConfig>();
            if (cfg == null || !cfg.ShowCompatTooltips)
            {
                return;
            }

            if (!IsLunarVeilWeapon(item))
            {
                return;
            }

            bool isTool = item.pick > 0 || item.axe > 0 || item.hammer > 0;
            if (isTool)
            {
                return;
            }

            if (item.damage <= 0 && !cfg.ShowDebugTooltips)
            {
                return;
            }

            float mult = cfg.LunarVeilDamageMultiplier;
            if (cfg.EnableMasterScaling && Main.masterMode)
                mult *= Math.Max(1f, cfg.MasterModeExtraMultiplier);

            if (!float.IsFinite(mult) || mult <= 0f)
                mult = 1f;

            string multTxt = mult % 1f == 0f ? mult.ToString("0") : mult.ToString("0.##");
            bool useKorean = UseKorean();
            string mainLine = useKorean
                ? $"[CLV] 루나베일 무기 데미지 배율: x{multTxt}"
                : $"[CLV] Damage Multiplier (Lunar Veil): x{multTxt}";

            tooltips.Add(new TooltipLine(Mod, "CLV_WeaponMult", mainLine));

            if (cfg.ShowDebugTooltips)
            {
                string debugLine = useKorean
                    ? $"[DBG] baseDamage={item.damage}, master={Main.masterMode}"
                    : $"[DBG] baseDamage={item.damage}, master={Main.masterMode}";
                tooltips.Add(new TooltipLine(Mod, "CLV_WeaponDebug", debugLine));
            }
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

        private static bool UseKorean()
        {
            var cfg = CLV_DamageConfig.Instance ?? ModContent.GetInstance<CLV_DamageConfig>();
            return cfg.TooltipLanguage switch
            {
                TooltipLanguageMode.Korean => true,
                TooltipLanguageMode.English => false,
                _ => IsGameCultureKorean(),
            };
        }

        private static bool IsGameCultureKorean()
        {
            string name = Language.ActiveCulture?.Name ?? Language.ActiveCultureName ?? string.Empty;
            name = name.ToLowerInvariant();
            return name.Contains("ko");
        }
    }
}
