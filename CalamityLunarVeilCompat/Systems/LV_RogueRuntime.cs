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

        public override bool InstancePerEntity => true;

        public override bool CanUseItem(Item item, Player player)
        {
            lastStealthStrike = false;
            lastConsumeFrame = uint.MaxValue;
            lastConsumePlayer = -1;
            lastConsumeAnimation = -1;
            lastConsumeItemTime = -1;
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
            if (!ShouldProcess(item, player))
                return;

            if (RogueStealthBridge.TryGetStealth(player, out var cur, out var max) && max > 0f)
            {
                var ratio = MathHelper.Clamp(cur / max, 0f, 1f);
                damage *= 1f + 0.25f * ratio;
            }
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
            if (!ShouldProcess(item, player))
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

        private static bool ShouldProcess(Item item, Player player)
        {
            if (item == null || player == null)
                return false;

            if (!RogueGuards.IsFromLunarVeil(item))
                return false;

            if (!RogueGuards.TryGetCalamityRogue(out var rogue))
                return false;

            if (item.DamageType != rogue)
                return false;

            if (RogueGuards.TryGetCurrentThrowState(item, out var isThrow))
                return isThrow;

            return LVRogueRegistry.IsRegistered(item.type);
        }
    }

    internal sealed class LVRogueRuntimeProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool StealthStrike { get; private set; }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (!TryResolveSource(source, out var player, out var item))
                return;

            var global = item.GetGlobalItem<LV_RogueRuntime>();
            if (global == null)
                return;

            if (!global.TryGetStrikeStateForProjectile(player, out var strike))
                return;

            StealthStrike = strike;
            if (strike)
                RogueStealthBridge.NotifyStealthStrikeFired(player, projectile);
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
                default:
                    player = null;
                    item = null;
                    return false;
            }
        }
    }
}
