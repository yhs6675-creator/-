using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    /// <summary>
    /// 루나베일 Rogue 무기에 칼라미티 스텔스 보정과 소비를 적용.
    /// </summary>
    public class LV_RogueRuntime : GlobalItem
    {
        private bool lastStealthStrike;
        private uint lastConsumeFrame = uint.MaxValue;
        private int lastConsumePlayer = -1;
        private int lastConsumeAnimation = -1;
        private int lastConsumeItemTime = -1;
        private bool baseDamageScaled;

        public override bool InstancePerEntity => true;

        public override bool CanUseItem(Item item, Player player)
        {
            lastStealthStrike = false;
            lastConsumeFrame = uint.MaxValue;
            lastConsumePlayer = -1;
            lastConsumeAnimation = -1;
            lastConsumeItemTime = -1;
            baseDamageScaled = false;
            return base.CanUseItem(item, player);
        }

        public override bool? UseItem(Item item, Player player)
        {
            TryConsumeStealth(item, player);
            return base.UseItem(item, player);
        }

        public override void UseAnimation(Item item, Player player)
        {
            TryConsumeStealth(item, player);
            base.UseAnimation(item, player);
        }

        public override void UseStyle(Item item, Player player, Rectangle heldItemFrame)
        {
            TryConsumeStealth(item, player);
            base.UseStyle(item, player, heldItemFrame);
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            TryConsumeStealth(item, player);
            return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
        }

        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (!ShouldHandleItem(item, player, out _))
                return;

            if (TryGetStealthMultiplier(player, out var multiplier))
            {
                damage *= multiplier;
                baseDamageScaled = true;
            }
        }

        public override void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!ShouldHandleItem(item, player, out _))
                return;

            if (baseDamageScaled)
                return;

            if (TryGetStealthMultiplier(player, out var multiplier))
                modifiers.FinalDamage *= multiplier;
        }

        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!ShouldHandleItem(item, player, out _))
                return;

            var strike = RogueStealthBridge.IsStrikeReady(player);
            RogueStealthBridge.ConsumeForAttack(player, strike);

            if (strike)
                RogueStealthBridge.NotifyStealthStrikeFired(player, null);
        }

        internal bool TryGetStrikeStateForProjectile(Player player, out bool strike)
        {
            if (player == null || lastConsumePlayer != player.whoAmI)
            {
                strike = false;
                return false;
            }

            if (lastConsumeFrame == uint.MaxValue || Main.GameUpdateCount - lastConsumeFrame > 1u)
            {
                strike = false;
                return false;
            }

            strike = lastStealthStrike;
            return true;
        }

        private void TryConsumeStealth(Item item, Player player)
        {
            if (!ShouldHandleItem(item, player, out _))
                return;

            if (lastConsumePlayer == player.whoAmI)
            {
                if (lastConsumeFrame == Main.GameUpdateCount)
                    return;

                if (lastConsumeItemTime >= 0 && lastConsumeAnimation >= 0 &&
                    player.itemTime == lastConsumeItemTime &&
                    player.itemAnimation == lastConsumeAnimation)
                {
                    return;
                }

                if (lastConsumeItemTime >= 0 && player.itemTime >= 0 && player.itemTime < lastConsumeItemTime)
                    return;

                if (lastConsumeAnimation >= 0 && player.itemAnimation > 0 && player.itemAnimation < lastConsumeAnimation)
                    return;
            }

            bool strike = RogueStealthBridge.IsStrikeReady(player);
            RogueStealthBridge.ConsumeForAttack(player, strike);

            lastStealthStrike = strike;
            lastConsumeFrame = Main.GameUpdateCount;
            lastConsumePlayer = player.whoAmI;
            lastConsumeAnimation = Math.Max(0, player.itemAnimation);
            lastConsumeItemTime = Math.Max(0, player.itemTime);

            if (strike && item.shoot <= 0)
                RogueStealthBridge.NotifyStealthStrikeFired(player, null);
        }

        internal static bool ShouldProcess(Item item, Player player)
        {
            if (item == null || player == null)
                return false;

            if (!RogueGuards.TryGetCalamityRogue(out var rogue))
                return false;

            return item.DamageType == rogue;
        }

        internal static bool ShouldHandleItem(Item item, Player player, out bool isProblemItem)
        {
            isProblemItem = ProblemWeaponRegistry.IsProblemAnyItem(item);
            if (isProblemItem)
                return true;

            if (!ShouldProcess(item, player))
                return false;

            var modName = item.ModItem?.Mod?.Name;
            return !string.Equals(modName, "CalamityMod", StringComparison.OrdinalIgnoreCase);
        }

        internal bool HasBaseDamageBeenScaled => baseDamageScaled;

        internal static bool TryGetStealthMultiplier(Player player, out float multiplier)
        {
            multiplier = 1f;

            if (!RogueStealthBridge.TryGetStealth(player, out var cur, out var max) || max <= 0f)
                return false;

            var ratio = MathHelper.Clamp(cur / max, 0f, 1f);
            multiplier = 1f + 0.25f * ratio;
            return true;
        }
    }

    internal sealed class LVRogueRuntimeProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool IsRogueShot { get; private set; }
        public bool WasStrikeReadyAtFire { get; private set; }
        public bool ConsumedOnce { get; private set; }
        public bool BaseDamageScaledAtFire { get; private set; }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            ConsumedOnce = false;
            IsRogueShot = false;
            WasStrikeReadyAtFire = false;
            BaseDamageScaledAtFire = false;

            Player player = null;
            Item item = null;

            if (!TryResolveSource(source, out player, out item))
            {
                if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
                {
                    player = Main.player[projectile.owner];
                    item = player?.HeldItem;
                }
            }

            if (player == null || item == null)
                return;

            if (!LV_RogueRuntime.ShouldHandleItem(item, player, out _))
                return;

            IsRogueShot = true;

            var global = item.GetGlobalItem<LV_RogueRuntime>();
            if (global != null)
            {
                BaseDamageScaledAtFire = global.HasBaseDamageBeenScaled;

                if (global.TryGetStrikeStateForProjectile(player, out var strikeFromUse))
                    WasStrikeReadyAtFire = strikeFromUse;
            }

            if (!WasStrikeReadyAtFire)
                WasStrikeReadyAtFire = RogueStealthBridge.IsStrikeReady(player);
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!IsRogueShot)
                return;

            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;

            var player = Main.player[projectile.owner];
            if (player == null)
                return;

            if (BaseDamageScaledAtFire)
                return;

            if (LV_RogueRuntime.TryGetStealthMultiplier(player, out var multiplier))
                modifiers.FinalDamage *= multiplier;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!IsRogueShot || ConsumedOnce)
                return;

            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;

            var player = Main.player[projectile.owner];
            if (player == null)
                return;

            var strike = WasStrikeReadyAtFire || RogueStealthBridge.IsStrikeReady(player);
            RogueStealthBridge.ConsumeForAttack(player, strike);

            if (strike)
                RogueStealthBridge.NotifyStealthStrikeFired(player, projectile);

            ConsumedOnce = true;
        }

        private static bool TryResolveSource(IEntitySource source, out Player player, out Item item)
        {
            switch (source)
            {
                case EntitySource_ItemUse_WithAmmo withAmmo:
                    player = withAmmo.Entity as Player;
                    item = withAmmo.Item;
                    return player != null && item != null;
                case EntitySource_ItemUse itemUse:
                    player = itemUse.Entity as Player;
                    item = itemUse.Item;
                    return player != null && item != null;
                case EntitySource_Parent parent:
                    player = parent.Entity as Player;
                    item = player?.HeldItem;
                    return player != null && item != null;
                default:
                    player = null;
                    item = null;
                    return false;
            }
        }
    }
}
