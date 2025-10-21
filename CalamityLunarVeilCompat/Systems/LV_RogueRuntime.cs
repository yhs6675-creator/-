using CalamityLunarVeilCompat.Bridges;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    /// <summary>
    /// 루나베일 Rogue 무기에 칼라미티 스텔스 보정을 적용하고 스왑 상태를 감시한다.
    /// </summary>
    public class LV_RogueRuntime : GlobalItem
    {
        public override bool InstancePerEntity => false;

        public override bool? UseItem(Item item, Player player)
        {
            HandleUse(item, player);
            return base.UseItem(item, player);
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            HandleUse(item, player);
            return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
        }

        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (!TryPrepare(item, player, out float stealthBonus, out bool swapped))
                return;

            if (stealthBonus > 0f)
                damage *= 1f + stealthBonus;
        }

        public override void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!TryPrepare(item, player, out float stealthBonus, out _))
                return;

            if (stealthBonus > 0f)
                modifiers.SourceDamage *= 1f + stealthBonus;
        }

        private static void HandleUse(Item item, Player player)
        {
            if (!TryPrepare(item, player, out _, out _))
                return;

            var ctx = player.GetModPlayer<RogueContext>();
            if (!ctx.TryFlagConsume())
                return;

            ProjectileSnapshot.MarkNextAsRogue(player);
            float consumed = CalamityBridge.ConsumeRogueStealth(player, 1f);
            float stealthBonus = CalamityBridge.GetRogueStealthScalar(player);
            CompatDebug.LogRogueEntry(item, true, stealthBonus, consumed);
        }

        private static bool TryPrepare(Item item, Player player, out float stealthBonus, out bool swapped)
        {
            stealthBonus = 0f;
            swapped = false;

            if (!IsSwapThrowReady(item, player, out bool whitelisted))
                return false;

            swapped = true;

            var rogue = CalamityBridge.GetRogueDamageClass();
            if (rogue != null)
                player.GetDamage(rogue) += 0f;

            stealthBonus = CalamityBridge.GetRogueStealthScalar(player);
            return true;
        }

        private static bool IsSwapThrowReady(Item item, Player player, out bool whitelisted)
        {
            whitelisted = false;

            if (item == null || player == null)
                return false;

            whitelisted = WhitelistIndex.WhitelistTypes.Contains(item.type);
            bool swap = player.GetModPlayer<RogueContext>().EvaluateSwapState(item);
            bool throwState = RogueGuards.TryGetCurrentThrowState(item, out var throwing) && throwing;
            bool problem = ProblemWeaponRegistry.IsProblemAnyItem(item);

            bool eligible = whitelisted || problem;
            if (!eligible && RogueGuards.TryGetLVThrowDamageClass(out var lvThrow) && item.CountsAsClass(lvThrow))
                eligible = true;

            bool enterRogue = swap && throwState && eligible;

            CompatDebug.LogSwapGate(item, whitelisted, swap, throwState, enterRogue);

            if (!enterRogue)
            {
                RogueGuards.RestoreOriginalDamageClass(item);
                return false;
            }

            return true;
        }
    }
}
