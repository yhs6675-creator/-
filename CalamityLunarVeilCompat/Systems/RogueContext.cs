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

        private uint lastConsumeFrame = uint.MaxValue;
        private int lastConsumeAnimation = -1;
        private int lastConsumeItemTime = -1;
        private uint pendingMarkFrame = uint.MaxValue;

        public override void ResetEffects()
        {
            RogueSwapActive = false;
            if (Main.GameUpdateCount - pendingMarkFrame > 1u)
            {
                PendingProjectileMark = false;
                pendingMarkFrame = uint.MaxValue;
            }
        }

        public override void PostUpdate()
        {
            RogueSwapActive = DetermineSwapState(Player.HeldItem);
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
        }

        internal bool TryConsumeMark(IEntitySource source)
        {
            if (!PendingProjectileMark)
                return false;

            if (pendingMarkFrame == uint.MaxValue)
            {
                PendingProjectileMark = false;
                return false;
            }

            uint diff = Main.GameUpdateCount - pendingMarkFrame;
            if (diff > 1u)
            {
                PendingProjectileMark = false;
                pendingMarkFrame = uint.MaxValue;
                return false;
            }

            if (source is EntitySource_ItemUse use && use.Entity == Player)
            {
                PendingProjectileMark = false;
                pendingMarkFrame = uint.MaxValue;
                return true;
            }

            if (source is EntitySource_ItemUse_WithAmmo withAmmo && withAmmo.Entity == Player)
            {
                PendingProjectileMark = false;
                pendingMarkFrame = uint.MaxValue;
                return true;
            }

            if (source is EntitySource_Parent parent && parent.Entity == Player)
            {
                PendingProjectileMark = false;
                pendingMarkFrame = uint.MaxValue;
                return true;
            }

            if (diff > 0u)
            {
                PendingProjectileMark = false;
                pendingMarkFrame = uint.MaxValue;
            }

            return false;
        }
    }
}
