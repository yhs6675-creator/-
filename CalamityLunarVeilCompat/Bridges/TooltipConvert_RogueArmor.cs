// File: Bridges/TooltipConvert_RogueArmor.cs
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat {
    public class TooltipConvert_RogueArmor : GlobalItem {
        public override bool InstancePerEntity => false;

        // 투척 관련으로 분류한 갑옷(세트 5 + 단품)
        static readonly HashSet<string> ThrowArmorNames = new() {
            // LunarianVoid
            "LunarianVoidHead","LunarianVoidBody","LunarianVoidLegs",
            // Scissorian
            "ScissorianMask","ScissorianChestplate","ScissorianGreaves",
            // Windmillion
            "WindmillionHat","WindmillionRobe","WindmillionBoots",
            // Eldritchian
            "EldritchianHood","EldritchianCloak","EldritchianLegs",
            // 단품
            "GarbageMask","GarbageChestplate",
        };

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
            if (!RogueCache.CalamityPresent) return;

            bool isArmor = !(item.headSlot < 0 && item.bodySlot < 0 && item.legSlot < 0);
            if (!isArmor) return;

            var mi = item.ModItem;
            if (mi?.Mod?.Name != "Stellamod") return;
            if (!ThrowArmorNames.Contains(mi.Name)) return;

            foreach (var line in tooltips) {
                if (string.IsNullOrEmpty(line.Text)) continue;

                // 영어/한국어 동시 대응
                line.Text = line.Text
                    .Replace("Throwing", "Rogue")
                    .Replace("Thrown",   "Rogue")
                    .Replace("Throw ",   "Rogue ")
                    .Replace("투척",     "로그");
            }
        }
    }
}
