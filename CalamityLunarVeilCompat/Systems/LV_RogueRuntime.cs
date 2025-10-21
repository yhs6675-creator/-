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
            if (ProblemWeaponRegistry.WhitelistHardForce && ProblemWeaponRegistry.IsProblemAnyItem(item))
            {
                if (!baseDamageScaled && TryGetStealthMultiplier(player, out var hardForceMultiplier))
                    modifiers.FinalDamage *= hardForceMultiplier;

                return;
            }

            if (!ShouldHandleItem(item, player, out _))
                return;

            if (baseDamageScaled)
                return;

            if (TryGetStealthMultiplier(player, out var multiplier))
                modifiers.FinalDamage *= multiplier;
        }

        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (ProblemWeaponRegistry.WhitelistHardForce && ProblemWeaponRegistry.IsProblemAnyItem(item))
            {
                var strikeHardForce = RogueStealthBridge.IsStrikeReady(player);
                RogueStealthBridge.ConsumeForAttack(player, strikeHardForce);

                if (strikeHardForce)
                    RogueStealthBridge.NotifyStealthStrikeFired(player, null);

                return;
            }

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

            if (RogueGuards.TryGetCalamityRogue(out var rogue) && item.DamageType == rogue)
                return true;

            if (RogueGuards.TryGetLVThrowDamageClass(out var lvThrow) && item.CountsAsClass(lvThrow))
                return true;

            return false;
        }

        internal static bool ShouldHandleItem(Item item, Player player, out bool isProblemItem)
        {
            isProblemItem = false;

            if (item == null || player == null)
                return false;

            if (ProblemWeaponRegistry.IsProblemAnyItem(item) && IsSwapActiveOrRogue(item, player))
            {
                isProblemItem = true;
                return true;
            }

            if (!ShouldProcess(item, player))
                return false;

            var modName = item.ModItem?.Mod?.Name;
            return !string.Equals(modName, "CalamityMod", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsSwapActiveOrRogue(Item item, Player player)
        {
            if (item == null || player == null)
                return false;

            if (RogueGuards.TryGetCalamityRogue(out var rogue) && item.DamageType == rogue)
                return true;

            if (RogueGuards.TryGetCurrentThrowState(item, out bool isThrow) && isThrow)
                return true;

            return false;
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

            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                var owner = Main.player[projectile.owner];
                if (owner != null && owner.active)
                {
                    player = owner;
                    item = owner.HeldItem;
                }
            }

            if (TryResolveSource(source, out var resolvedPlayer, out var resolvedItem))
            {
                player = resolvedPlayer ?? player;
                item = resolvedItem ?? item;
            }

            item ??= player?.HeldItem;

            void TagFrom(Item captureItem, bool registerProjectile)
            {
                IsRogueShot = true;

                if (captureItem != null)
                {
                    var globalTagged = captureItem.GetGlobalItem<LV_RogueRuntime>();
                    if (globalTagged != null)
                    {
                        BaseDamageScaledAtFire = globalTagged.HasBaseDamageBeenScaled;

                        if (player != null && globalTagged.TryGetStrikeStateForProjectile(player, out var strikeFromUse))
                            WasStrikeReadyAtFire = strikeFromUse;
                    }
                }

                if (player != null && !WasStrikeReadyAtFire)
                    WasStrikeReadyAtFire = RogueStealthBridge.IsStrikeReady(player);

                if (registerProjectile)
                    ProblemWeaponRegistry.AddProjectileTypeRuntime(projectile.type);
            }

            if (ProblemWeaponRegistry.IsProblemProjectile(projectile))
            {
                TagFrom(item, true);
                return;
            }

            DamageClass rogueDc = null;
            if (RogueGuards.TryGetCalamityRogue(out var rogueClass))
                rogueDc = rogueClass;

            DamageClass lvThrowDc = null;
            if (RogueGuards.TryGetLVThrowDamageClass(out var lvThrowClass))
                lvThrowDc = lvThrowClass;

            if (ProblemWeaponRegistry.WhitelistHardForce && item != null && ProblemWeaponRegistry.IsProblemAnyItem(item))
            {
                TagFrom(item, true);
                return;
            }

            if (item != null && RogueGuards.TryGetCurrentThrowState(item, out bool isThrow) && isThrow)
            {
                TagFrom(item, true);
                return;
            }

            if (item != null && ProblemWeaponRegistry.IsProblemAnyItem(item))
            {
                TagFrom(item, true);
                return;
            }

            if (item != null && ((rogueDc != null && item.DamageType == rogueDc) || (lvThrowDc != null && item.CountsAsClass(lvThrowDc))))
            {
                TagFrom(item, false);
                return;
            }

            if ((rogueDc != null && projectile.DamageType == rogueDc) || (lvThrowDc != null && projectile.CountsAsClass(lvThrowDc)))
            {
                TagFrom(item, false);
                return;
            }

            if (item == null || player == null)
                return;

            if (!LV_RogueRuntime.ShouldHandleItem(item, player, out var isProblemItem))
                return;

            TagFrom(item, isProblemItem);
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            var player = (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
                ? Main.player[projectile.owner]
                : null;

            if (player == null)
                return;

            bool hardForcedProjectile = ProblemWeaponRegistry.WhitelistHardForce && ProblemWeaponRegistry.IsProblemProjectile(projectile);

            if (!hardForcedProjectile)
            {
                bool byDamageClass = false;

                if (RogueGuards.TryGetCalamityRogue(out var rogue) && projectile.DamageType == rogue)
                    byDamageClass = true;
                else if (RogueGuards.TryGetLVThrowDamageClass(out var lvThrow) && projectile.CountsAsClass(lvThrow))
                    byDamageClass = true;

                if (!(IsRogueShot || ProblemWeaponRegistry.IsProblemProjectile(projectile) || byDamageClass))
                    return;
            }

            if (BaseDamageScaledAtFire)
                return;

            if (LV_RogueRuntime.TryGetStealthMultiplier(player, out var multiplier))
                modifiers.FinalDamage *= multiplier;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (ConsumedOnce)
                return;

            var player = (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
                ? Main.player[projectile.owner]
                : null;

            if (player == null)
                return;

            bool hardForcedProjectile = ProblemWeaponRegistry.WhitelistHardForce && ProblemWeaponRegistry.IsProblemProjectile(projectile);

            if (!hardForcedProjectile)
            {
                bool byDamageClass = false;

                if (RogueGuards.TryGetCalamityRogue(out var rogue) && projectile.DamageType == rogue)
                    byDamageClass = true;
                else if (RogueGuards.TryGetLVThrowDamageClass(out var lvThrow) && projectile.CountsAsClass(lvThrow))
                    byDamageClass = true;

                if (!(IsRogueShot || ProblemWeaponRegistry.IsProblemProjectile(projectile) || byDamageClass))
                    return;
            }
            else
            {
                // hard force ensures stealth consume regardless of projectile tagging
            }

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
