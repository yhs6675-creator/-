// File: Bridges/LV_RogueArmorConvert_Map.cs
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat {
    public class LV_RogueArmorConvert_Map : GlobalItem {
        public override bool InstancePerEntity => false;

        public override void UpdateEquip(Item item, Player player) {
            if (!RogueCache.CalamityPresent) return;

            // 방어구 판정(armor 속성 대신 슬롯으로)
            if (item.headSlot < 0 && item.bodySlot < 0 && item.legSlot < 0) return;

            var mi = item.ModItem;
            if (mi?.Mod?.Name != "Stellamod") return;

            var rogue = RogueCache.Rogue;

            switch (mi.Name) {
                // ===== LunarianVoid =====
                case "LunarianVoidHead":
                    player.GetCritChance(rogue) += 10;
                    player.GetDamage(rogue)     += 0.25f;
                    break;
                case "LunarianVoidBody":
                    player.GetCritChance(rogue) += 10;
                    break;
                case "LunarianVoidLegs":
                    player.GetDamage(rogue)     += 0.05f;
                    break;

                // ===== Scissorian =====
                case "ScissorianMask":
                    player.GetDamage(rogue)     += 0.20f;
                    break;
                case "ScissorianChestplate":
                    player.GetCritChance(rogue) += 20;
                    player.GetDamage(rogue)     += 0.10f;
                    break;
                case "ScissorianGreaves":
                    player.GetCritChance(rogue) += 15;
                    break;

                // ===== Jianxin =====
                // per-piece 투척 라인 적음 → 세트 스텔스만

                // ===== Windmillion =====
                case "WindmillionHat":
                    player.GetAttackSpeed(rogue) += 0.30f;
                    player.GetKnockback(rogue)   += 0.30f;
                    break;
                case "WindmillionRobe":
                    player.GetCritChance(rogue)  += 2;
                    break;
                case "WindmillionBoots":
                    player.GetDamage(rogue)      += 0.10f;
                    break;

                // ===== Eldritchian =====
                case "EldritchianHood":
                    player.GetAttackSpeed(rogue) += 0.10f;
                    player.GetDamage(rogue)      += 0.20f;
                    break;
                case "EldritchianCloak":
                    player.GetAttackSpeed(rogue) += 0.12f;
                    player.GetDamage(rogue)      += 0.16f;
                    break;
                case "EldritchianLegs":
                    player.GetAttackSpeed(rogue) += 0.08f;
                    player.GetDamage(rogue)      += 0.23f;
                    break;

                // ===== 단품/미완 세트 =====
                case "GarbageMask":
                    player.GetAttackSpeed(rogue) += 0.30f;
                    player.GetDamage(rogue)      += 0.15f;
                    break;
                case "GarbageChestplate":
                    player.GetAttackSpeed(rogue) += 0.10f;
                    player.GetDamage(rogue)      += 0.25f;
                    break;
            }
        }
    }
}
