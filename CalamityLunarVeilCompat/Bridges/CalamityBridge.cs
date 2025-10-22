using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using CLVCompat.Systems;

namespace CalamityLunarVeilCompat.Bridges
{
    internal static class CalamityBridge
    {
        public static DamageClass GetRogueDamageClass()
        {
            return RogueGuards.TryGetCalamityRogue(out var rogue) ? rogue : null;
        }

        public static float GetRogueStealthScalar(Player player)
        {
            if (player == null)
                return 0f;

            if (!RogueStealthBridge.TryGetStealth(player, out var current, out var max) || max <= 0f)
                return 0f;

            float ratio = MathHelper.Clamp(current / max, 0f, 1f);
            const float DamageScale = 0.25f;
            return DamageScale * ratio;
        }

        public static float ConsumeRogueStealth(Player player, float amountRatio = 1f)
        {
            if (player == null)
                return 0f;

            amountRatio = MathHelper.Clamp(amountRatio, 0f, 1f);
            if (amountRatio <= 0f)
                return 0f;

            bool strike = RogueStealthBridge.IsStrikeReady(player);
            float consumed = RogueStealthBridge.ConsumeForAttack(player, strike);

            if (amountRatio < 1f && consumed > 0f &&
                RogueStealthBridge.TryGetStealth(player, out var curAfter, out var maxAfter))
            {
                float refund = consumed * (1f - amountRatio);
                if (refund > 0f)
                {
                    float restored = MathHelper.Clamp(curAfter + refund, 0f, maxAfter);
                    if (RogueStealthBridge.TrySetStealth(player, restored))
                    {
                        consumed -= refund;
                        curAfter = restored;
                    }
                }
                RogueStealthBridge.TryGetStealth(player, out curAfter, out _);
            }

            if (strike)
                RogueStealthBridge.NotifyStealthStrikeFired(player, null);

            RogueStealthBridge.TryGetStealth(player, out var current, out _);
            CompatDebug.LogStealth(strike, consumed, current);
            return consumed;
        }
    }
}
