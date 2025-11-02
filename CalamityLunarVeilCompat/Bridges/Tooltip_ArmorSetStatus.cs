using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat
{
    /// <summary>
    /// 루나베일 방어구 착용 시, 세트 보너스(스텔스 최대치)와 방어력 보정(+50% 기본)을
    /// 인벤토리 툴팁에 실시간 안내한다.
    /// </summary>
    public class Tooltip_ArmorSetStatus : GlobalItem
    {
        private static readonly HashSet<string> GarbageLegsNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "GarbageLegs",
            "GarbageGreaves",
            "GarbagePants",
            "GarbageBoots",
        };

        public override bool InstancePerEntity => false;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!IsArmor(item))
                return;

            if (!IsLunarVeilFamily(item))
                return;

            var cfg = CLV_DamageConfig.Instance ?? ModContent.GetInstance<CLV_DamageConfig>();
            if (cfg == null || !cfg.ShowCompatTooltips)
                return;

            var player = Main.LocalPlayer;
            if (player is null)
                return;

            bool useKorean = UseKorean();

            if (cfg.EnableArmorDefenseBoost && IsLunarVeilArmor(item))
            {
                float mult = Math.Max(1f, cfg.ArmorDefenseMultiplier);
                int baseDef = Math.Max(0, item.defense);
                int add = (int)Math.Floor(baseDef * (mult - 1f) + 0.0001f);

                string armorLine = useKorean
                    ? $"[CLV] 루나베일 방어구 보정: 방어력 +{add}"
                    : $"[CLV] Armor Bonus (Lunar Veil): +{add} Defense";
                tooltips.Add(new TooltipLine(Mod,
                    "CLV_ArmorBoost",
                    armorLine)
                {
                    OverrideColor = Microsoft.Xna.Framework.Color.LightSkyBlue
                });

                if (cfg.ShowDebugTooltips)
                {
                    string multTxt = mult % 1f == 0f ? mult.ToString("0") : mult.ToString("0.##");
                    string debugLine = useKorean
                        ? $"[DBG] baseDef={baseDef}, mult={multTxt}, add={add}"
                        : $"[DBG] baseDef={baseDef}, mult={multTxt}, add={add}";
                    tooltips.Add(new TooltipLine(Mod,
                        "CLV_ArmorDebug",
                        debugLine));
                }
            }

            int stealthMax = GetActiveSetStealthMaxIfWearingFullSet(player);
            if (stealthMax > 0 && IsPartOfAnyKnownSet(item))
            {
                string setBonusLine = useKorean
                    ? $"[CLV] 세트 보너스 활성: 로그 스텔스 최대치 = {stealthMax}"
                    : $"[CLV] Set Bonus Active: Rogue Stealth Max = {stealthMax}";
                tooltips.Add(new TooltipLine(Mod,
                    "CLV_SetBonusActive",
                    setBonusLine)
                {
                    OverrideColor = Microsoft.Xna.Framework.Color.MediumSeaGreen
                });

                if (cfg.ShowDebugTooltips)
                {
                    string debugLine = useKorean
                        ? $"[DBG] stealthMax(set)={stealthMax}"
                        : $"[DBG] stealthMax(set)={stealthMax}";
                    tooltips.Add(new TooltipLine(Mod,
                        "CLV_StealthDebug",
                        debugLine));
                }
            }
        }

        private static bool IsArmor(Item item)
            => item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0;

        private static bool IsLunarVeilFamily(Item item)
        {
            var modName = item.ModItem?.Mod?.Name;
            if (string.IsNullOrEmpty(modName))
                return false;

            return modName.StartsWith("LunarVeil", StringComparison.Ordinal)
                || modName == "Stellamod"
                || modName == "LunarVeilLegacy"
                || modName == "LunarVeil"
                || modName == "LunarVeilLegacyMod";
        }

        private static bool MatchesGarbageLeg(string name, Item item)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (GarbageLegsNames.Contains(name))
                return true;

            return name.StartsWith("Garbage", StringComparison.OrdinalIgnoreCase) && (item?.legSlot ?? -1) >= 0;
        }

        private static bool IsLunarVeilArmor(Item item)
            => IsArmor(item) && IsLunarVeilFamily(item);

        private static bool IsPartOfAnyKnownSet(Item item)
        {
            string n = item.ModItem?.Name ?? string.Empty;
            return n is "WindmillionHat" or "WindmillionRobe" or "WindmillionBoots"
                || n is "LunarianVoidHead" or "LunarianVoidBody" or "LunarianVoidLegs"
                || n is "ScissorianMask" or "ScissorianChestplate" or "ScissorianGreaves"
                || n is "EldritchianHood" or "EldritchianCloak" or "EldritchianLegs"
                || n is "GarbageMask" or "GarbageChestplate"
                || MatchesGarbageLeg(n, item);
        }

        private static int GetActiveSetStealthMaxIfWearingFullSet(Player p)
        {
            var headItem = p.armor[0];
            var bodyItem = p.armor[1];
            var legItem = p.armor[2];

            string H = headItem?.ModItem?.Name ?? string.Empty;
            string B = bodyItem?.ModItem?.Name ?? string.Empty;
            string L = legItem?.ModItem?.Name ?? string.Empty;

            if (H == "WindmillionHat" && B == "WindmillionRobe" && L == "WindmillionBoots")
                return 50;

            if ((H == "LunarianVoidHead" && B == "LunarianVoidBody" && L == "LunarianVoidLegs") ||
                (H == "ScissorianMask"   && B == "ScissorianChestplate" && L == "ScissorianGreaves") ||
                (H == "EldritchianHood"  && B == "EldritchianCloak"     && L == "EldritchianLegs") ||
                (H == "GarbageMask"      && B == "GarbageChestplate"    && MatchesGarbageLeg(L, legItem)))
                return 100;

            return 0;
        }

        private static bool UseKorean()
        {
            var cfg = CLV_DamageConfig.Instance ?? ModContent.GetInstance<CLV_DamageConfig>();
            switch (cfg.TooltipLanguage)
            {
                case TooltipLanguageMode.Korean:
                    return true;
                case TooltipLanguageMode.English:
                    return false;
                case TooltipLanguageMode.Auto:
                default:
                {
                    var culture = Language.ActiveCulture?.CultureInfo;
                    if (culture != null)
                    {
                        if (culture.TwoLetterISOLanguageName.Equals("ko", StringComparison.OrdinalIgnoreCase))
                            return true;
                        if (culture.Name.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }

                    return false;
                }
            }
        }
    }
}
