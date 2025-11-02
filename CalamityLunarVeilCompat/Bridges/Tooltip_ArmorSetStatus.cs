using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
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

            if (TryGetConfig(out var config) && config.EnableArmorDefenseBoost)
            {
                float mult = Math.Max(1f, config.ArmorDefenseMultiplier);
                int baseDefense = Math.Max(0, item.defense);
                int add = (int)Math.Floor(baseDefense * (mult - 1f) + 0.0001f);

                if (add > 0)
                {
                    tooltips.Add(new TooltipLine(Mod,
                        "CLV_DefenseBoost",
                        $"[CLV] +{add} Defense (Lunar Veil armor bonus)")
                    {
                        OverrideColor = Color.LightSkyBlue
                    });
                    tooltips.Add(new TooltipLine(Mod,
                        "CLV_DefenseBoost_KO",
                        $"[CLV] 루나베일 방어구 보정: 방어력 +{add}")
                    {
                        OverrideColor = Color.LightSkyBlue
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
                    OverrideColor = Color.MediumSeaGreen
                });
                tooltips.Add(new TooltipLine(Mod,
                    "CLV_SetBonusActive_KO",
                    $"[CLV] 세트 보너스 활성: 로그 스텔스 최대치 = {stealthMax}")
                {
                    OverrideColor = Color.MediumSeaGreen
                });
            }
        }

        private static bool TryGetConfig(out CLV_DamageConfig config)
        {
            config = CLV_DamageConfig.Instance ?? ModContent.GetInstance<CLV_DamageConfig>();
            return config != null;
        }

        private static bool IsArmor(Item item)
        {
            if (item == null)
                return false;

            return item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0;
        }

        private static bool IsLunarVeilFamily(Item item)
        {
            var modName = item?.ModItem?.Mod?.Name;
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
            string name = item?.ModItem?.Name ?? string.Empty;
            return name is "WindmillionHat" or "WindmillionRobe" or "WindmillionBoots"
                or "LunarianVoidHead" or "LunarianVoidBody" or "LunarianVoidLegs"
                or "ScissorianMask" or "ScissorianChestplate" or "ScissorianGreaves"
                or "EldritchianHood" or "EldritchianCloak" or "EldritchianLegs";
        }

        private static int GetActiveSetStealthMaxIfWearingFullSet(Player player)
        {
            string head = player?.armor[0]?.ModItem?.Name ?? string.Empty;
            string body = player?.armor[1]?.ModItem?.Name ?? string.Empty;
            string legs = player?.armor[2]?.ModItem?.Name ?? string.Empty;

            if (head.Length == 0 || body.Length == 0 || legs.Length == 0)
                return 0;

            if (!(IsLunarVeilFamily(player.armor[0]) && IsLunarVeilFamily(player.armor[1]) && IsLunarVeilFamily(player.armor[2])))
                return 0;

            if (head == "WindmillionHat" && body == "WindmillionRobe" && legs == "WindmillionBoots")
                return 50;

            if ((head == "LunarianVoidHead" && body == "LunarianVoidBody" && legs == "LunarianVoidLegs") ||
                (head == "ScissorianMask"   && body == "ScissorianChestplate" && legs == "ScissorianGreaves") ||
                (head == "EldritchianHood"  && body == "EldritchianCloak"     && legs == "EldritchianLegs"))
                return 100;

            return 0;
        }
    }
}
