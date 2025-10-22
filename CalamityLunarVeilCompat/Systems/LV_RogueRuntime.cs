using System;
using CalamityLunarVeilCompat.Bridges;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
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
            // UseItem만 호출되는 무기에서도 스텔스 소비/스냅샷이 일관되게 적용되도록 공통 처리 호출.
            HandleUse(item, player);
            return base.UseItem(item, player);
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 다수의 루나베일 투척 무기는 Shoot 훅만 실행되므로 여기서도 동일한 소비 로직을 호출한다.
            HandleUse(item, player);
            return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
        }

        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (!TryPrepare(item, player, out float stealthBonus, out bool swapThrowNow))
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
            if (!TryPrepare(item, player, out float stealthBonus, out bool swapThrowNow))
                return;

            var ctx = player.GetModPlayer<RogueContext>();
            if (!ctx.TryFlagConsume())
                return;

            ProjectileSnapshot.MarkNextAsRogue(player);
            ctx.LastRogueMarkTick = Main.GameUpdateCount;
            CompatDebug.LogInfo($"[DIAG] HandleUse MarkNext completed for item={item?.Name ?? "<null>"}");
            float consumed = CalamityBridge.ConsumeRogueStealth(player, 1f);
            CompatDebug.LogRogueEntry(item, swapThrowNow, stealthBonus, consumed);
            CompatDebug.LogInfo($"[DIAG] HandleUse consumed={consumed:0.###}");
        }

        private static bool TryPrepare(Item item, Player player, out float stealthBonus, out bool swapThrowNow)
        {
            stealthBonus = 0f;
            swapThrowNow = false;

            if (!IsSwapThrowReady(item, player, out bool whitelisted, out bool pureLVThrow, out bool swapNow))
                return false;

            swapThrowNow = swapNow;

            var rogue = CalamityBridge.GetRogueDamageClass();
            if (rogue != null)
                player.GetDamage(rogue) += 0f;

            stealthBonus = CalamityBridge.GetRogueStealthScalar(player);
            return true;
        }

        private static bool IsSwapThrowReady(Item item, Player player, out bool whitelisted, out bool pureLVThrow, out bool swapThrowNow)
        {
            whitelisted = false;
            pureLVThrow = false;
            swapThrowNow = false;

            if (item == null || player == null)
                return false;

            bool whitelistHit = WhitelistIndex.WhitelistTypes.Contains(item.type);
            bool problem = ProblemWeaponRegistry.IsProblemAnyItem(item);
            whitelisted = whitelistHit;
            bool swap = player.GetModPlayer<RogueContext>().EvaluateSwapState(item);
            bool throwState = RogueGuards.TryGetCurrentThrowState(item, out var throwing) && throwing;

            // ① 확정 투척이면 무조건 통과
            bool haveLVThrow = RogueGuards.TryGetLVThrowDamageClass(out var lvThrow);
            bool pureByLVClass = haveLVThrow && item.CountsAsClass(lvThrow);
            bool isTMLThrow = item.DamageType is Terraria.ModLoader.ThrowingDamageClass;
            string modName = item.ModItem?.Mod?.Name ?? string.Empty;
            bool pureByFallbackA = isTMLThrow && modName.Equals("LunarVeil", StringComparison.OrdinalIgnoreCase);
            bool pureByWhitelist = whitelistHit;
            pureLVThrow = pureByLVClass || pureByFallbackA || pureByWhitelist;
            string lvThrowName = haveLVThrow ? lvThrow.DisplayName?.Value ?? "<null>" : "<null>";
            string itemDamageType = item?.DamageType?.GetType().FullName ?? "<null>";
            CompatDebug.LogInfo($"[DIAG] pureLVThrow={pureLVThrow}, byLVClass={pureByLVClass}, byFallbackA={pureByFallbackA}, byWhitelist={pureByWhitelist}, lvThrow={lvThrowName}, itemDC={itemDamageType}, modName={modName}");

            // ② 스왑핑 무기는 "스왑 + 투척 상태"일 때만 통과
            swapThrowNow = swap && throwState;

            // ③ 화이트리스트/문제 목록은 보조 수단이지만, 스왑 전엔 금지
            bool WL_or_Problem = (whitelistHit || problem) && swapThrowNow;

            // ✅ 최종 진입: 확정 투척은 무조건 통과, 스왑핑은 스왑+투척일 때만 통과
            bool enterRogue = pureLVThrow || swapThrowNow || WL_or_Problem;

            CompatDebug.LogSwapGate(item,
                whitelisted: whitelistHit,
                swap: swap,
                throwState: throwState,
                enterRogue: enterRogue,
                pure: pureLVThrow,
                swapThrowNow: swapThrowNow);

            if (!enterRogue)
            {
                RogueGuards.RestoreOriginalDamageClass(item);
                return false;
            }

            return true;
        }
    }
}
