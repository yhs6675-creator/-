using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    public sealed class RogueContext : ModPlayer
    {
        public bool RogueSwapActive { get; private set; }
        internal bool PendingProjectileMark { get; private set; }
        public int LastRogueMarkTick { get; internal set; } = int.MinValue;

        private uint lastConsumeFrame = uint.MaxValue;
        private int lastConsumeAnimation = -1;
        private int lastConsumeItemTime = -1;
        private uint pendingMarkFrame = uint.MaxValue;
        private int pendingMarkItemType = -1;
        private bool pendingMarkConsumed;

        public override void ResetEffects()
        {
            RogueSwapActive = false;
            if (pendingMarkConsumed && Main.GameUpdateCount != pendingMarkFrame)
            {
                ClearPendingMark();
            }
            else if (PendingProjectileMark && Main.GameUpdateCount - pendingMarkFrame > 1u)
            {
                ClearPendingMark();
            }
        }

        public override void PostUpdate()
        {
            RogueSwapActive = DetermineSwapState(Player.HeldItem);
        }

        internal bool EvaluateSwapState(Item item)
        {
            bool swap = DetermineSwapState(item ?? Player.HeldItem);
            RogueSwapActive = swap;
            return swap;
        }

        private static bool DetermineSwapState(Item item)
        {
            if (item == null)
                return false;

            return RogueGuards.TryGetCurrentThrowState(item, out var throwing) && throwing;
        }

        internal bool TryFlagConsume()
        {
            if (lastConsumeFrame == Main.GameUpdateCount)
                return false;

            if (lastConsumeAnimation >= 0 && lastConsumeItemTime >= 0)
            {
                if (Player.itemAnimation == lastConsumeAnimation && Player.itemTime == lastConsumeItemTime)
                    return false;
            }

            lastConsumeFrame = Main.GameUpdateCount;
            lastConsumeAnimation = Math.Max(0, Player.itemAnimation);
            lastConsumeItemTime = Math.Max(0, Player.itemTime);
            return true;
        }

        internal void MarkNextProjectile()
        {
            PendingProjectileMark = true;
            pendingMarkFrame = Main.GameUpdateCount;
            pendingMarkItemType = Player.HeldItem?.type ?? 0;
            pendingMarkConsumed = false;
            LastRogueMarkTick = (int)Main.GameUpdateCount;
        }

        internal bool TryConsumeMark(IEntitySource source)
        {
            if (!PendingProjectileMark)
                return false;

            if (pendingMarkFrame == uint.MaxValue)
            {
                ClearPendingMark();
                return false;
            }

            uint diff = Main.GameUpdateCount - pendingMarkFrame;
            if (diff > 1u)
            {
                ClearPendingMark();
                return false;
            }

            if (source is EntitySource_ItemUse use && use.Entity == Player)
            {
                if (!MatchesPendingItem(use.Item))
                    return false;
                return CompleteConsume(diff);
            }

            if (source is EntitySource_ItemUse_WithAmmo withAmmo && withAmmo.Entity == Player)
            {
                if (!MatchesPendingItem(withAmmo.Item))
                    return false;
                return CompleteConsume(diff);
            }

            if (source is EntitySource_Parent parent)
            {
                if (parent.Entity == Player)
                    return CompleteConsume(diff);

                if (parent.Entity is Projectile proj && proj.owner == Player.whoAmI)
                {
                    var global = proj.GetGlobalProjectile<RogueProjGlobal>();
                    if (global != null && global.FromRogueSwap)
                        return CompleteConsume(diff);
                }

                return false;
            }

            if (diff > 0u)
            {
                ClearPendingMark();
            }

            return false;
        }

        internal bool ForceConsumeMark()
        {
            if (!PendingProjectileMark)
                return false;

            pendingMarkConsumed = true;
            ClearPendingMark();
            return true;
        }

        private bool CompleteConsume(uint age)
        {
            pendingMarkConsumed = true;

            if (age >= 1u)
            {
                ClearPendingMark();
            }

            return true;
        }

        private bool MatchesPendingItem(Item sourceItem)
        {
            if (pendingMarkItemType <= 0)
                return true;

            int type = sourceItem?.type ?? 0;
            if (type == 0)
                return true;

            return type == pendingMarkItemType;
        }

        private void ClearPendingMark()
        {
            PendingProjectileMark = false;
            pendingMarkFrame = uint.MaxValue;
            pendingMarkItemType = -1;
            pendingMarkConsumed = false;
        }
    }
}
