using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat.Bridges
{
    public class RogueAccessoryBridge : GlobalItem
    {
        private static readonly HashSet<string> AllowedMods = new()
        {
            "LunarVielmod",
            "Stellamod",
        };

        private static readonly HashSet<string> RogueTargets = new()
        {
            "LeatherGlove",
            "BonedThrowBroochA",
            "HeatedVerliaBroochA",
        };

        // 조정된 효과 수치
        // LeatherGlove: +50% 속도 (1.5배)
        // BonedThrowBroochA: +200% 속도 (3배)
        // HeatedVerliaBroochA: 투사체 속도 변화 없음
        private static readonly Dictionary<string, Effect> Effects = new()
        {
            ["LeatherGlove"]        = new Effect{ dmgAdd = 0.03f, projMult = 1.5f },
            ["BonedThrowBroochA"]   = new Effect{ dmgMult = 0.20f, projMult = 3.0f },
            ["HeatedVerliaBroochA"] = new Effect{ dmgMult = 0.15f, projMult = 1.0f },
        };

        public override bool InstancePerEntity => false;

        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (!item.accessory) return;

            // Calamity 필수 의존
            if (!ModContent.TryFind<DamageClass>("CalamityMod", "RogueDamageClass", out var rogueDC))
                return;

            var mi = item.ModItem;
            if (mi is null) return;
            if (!AllowedMods.Contains(mi.Mod?.Name ?? string.Empty)) return;
            if (!RogueTargets.Contains(mi.Name)) return;

            if (Effects.TryGetValue(mi.Name, out var e))
            {
                if (e.dmgMult != 0f) player.GetDamage(rogueDC) *= (1f + e.dmgMult);
                if (e.dmgAdd  != 0f) player.GetDamage(rogueDC) += e.dmgAdd;

                // RogueProjectileVelocity 누적
                player.GetModPlayer<RogueVelocityPlayer>().rogueProjMult *= e.projMult;
            }
        }

        private struct Effect
        {
            public float dmgMult;   // 데미지 승수형
            public float dmgAdd;    // 데미지 가산형
            public float projMult;  // 투사체 속도 배율
        }
    }

    // 투사체 속도 적용 전용
    public class RogueVelocityPlayer : ModPlayer
    {
        public float rogueProjMult;

        public override void ResetEffects()
        {
            rogueProjMult = 1f;
        }

        public override void ModifyShootStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (!ModContent.TryFind<DamageClass>("CalamityMod", "RogueDamageClass", out var rogueDC))
                return;

            if (item.DamageType == rogueDC && rogueProjMult != 1f)
                velocity *= rogueProjMult;
        }
    }
}
