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
            if (!RogueStealthBridge.TryGetStealth(player, out var current, out var max) || max <= 0f)
                return 0f;

            float ratio = MathHelper.Clamp(current / max, 0f, 1f);
            return 0.25f * ratio;
        }

        public static float ConsumeRogueStealth(Player player, float amount)
        {
            if (player == null)
                return 0f;

            bool strike = RogueStealthBridge.IsStrikeReady(player);
            float consumed = RogueStealthBridge.ConsumeForAttack(player, strike);

            if (strike)
                RogueStealthBridge.NotifyStealthStrikeFired(player, null);

            RogueStealthBridge.TryGetStealth(player, out var cur, out _);
            CompatDebug.LogStealth(strike, consumed, cur);
            return consumed;
        }
    }
}
