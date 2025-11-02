using System;
using System.Collections.Generic;
using Terraria;
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

            var player = Main.LocalPlayer;
            if (player is null)
                return;

            var cfg = CLV_DamageConfig.Instance ?? ModContent.GetInstance<CLV_DamageConfig>();

            if (cfg?.EnableArmorDefenseBoost == true)
            {
                float mult = Math.Max(1f, cfg.ArmorDefenseMultiplier);
                int baseDef = Math.Max(0, item.defense);
                int add = (int)Math.Floor(baseDef * (mult - 1f) + 0.0001f);

                if (add > 0)
                {
                    tooltips.Add(new TooltipLine(Mod,
                        "CLV_DefenseBoost",
                        $"[CLV] +{add} Defense (Lunar Veil armor bonus)")
                    {
                        OverrideColor = Microsoft.Xna.Framework.Color.LightSkyBlue
                    });
                    tooltips.Add(new TooltipLine(Mod,
                        "CLV_DefenseBoost_KO",
                        $"[CLV] 루나베일 방어구 보정: 방어력 +{add}")
                    {
                        OverrideColor = Microsoft.Xna.Framework.Color.LightSkyBlue
                    });
                }
            }

            int stealthMax = GetActiveSetStealthMaxIfWearingFullSet(player);
            if (stealthMax > 0 && IsPartOfAnyKnownSet(item))
            {
                tooltips.Add(new TooltipLine(Mod,
                    "CLV_SetBonusActive",
                    $"[CLV] Set Bonus Active: Rogue Stealth Max = {stealthMax}")
                {
                    OverrideColor = Microsoft.Xna.Framework.Color.MediumSeaGreen
                });
                tooltips.Add(new TooltipLine(Mod,
                    "CLV_SetBonusActive_KO",
                    $"[CLV] 세트 보너스 활성: 로그 스텔스 최대치 = {stealthMax}")
                {
                    OverrideColor = Microsoft.Xna.Framework.Color.MediumSeaGreen
                });
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

        private static bool IsPartOfAnyKnownSet(Item item)
        {
            string n = item.ModItem?.Name ?? string.Empty;
            return n is "WindmillionHat" or "WindmillionRobe" or "WindmillionBoots"
                || n is "LunarianVoidHead" or "LunarianVoidBody" or "LunarianVoidLegs"
                || n is "ScissorianMask" or "ScissorianChestplate" or "ScissorianGreaves"
                || n is "EldritchianHood" or "EldritchianCloak" or "EldritchianLegs"
                || n is "GarbageMask" or "GarbageChestplate"
                || GarbageLegsNames.Contains(n);
        }

        private static int GetActiveSetStealthMaxIfWearingFullSet(Player p)
        {
            string H = p.armor[0]?.ModItem?.Name ?? string.Empty;
            string B = p.armor[1]?.ModItem?.Name ?? string.Empty;
            string L = p.armor[2]?.ModItem?.Name ?? string.Empty;

            if (H == "WindmillionHat" && B == "WindmillionRobe" && L == "WindmillionBoots")
                return 50;

            if ((H == "LunarianVoidHead" && B == "LunarianVoidBody" && L == "LunarianVoidLegs") ||
                (H == "ScissorianMask"   && B == "ScissorianChestplate" && L == "ScissorianGreaves") ||
                (H == "EldritchianHood"  && B == "EldritchianCloak"     && L == "EldritchianLegs") ||
                (H == "GarbageMask"      && B == "GarbageChestplate"    && GarbageLegsNames.Contains(L)))
                return 100;

            return 0;
        }
    }
}
