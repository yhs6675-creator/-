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

        // 읽기 전용: 현재 비율 × 데미지 스케일만 반환. 절대 소모하지 않음.
        public static float GetRogueStealthScalar(Player player)
        {
            if (player == null)
                return 0f;
            if (!RogueStealthBridge.TryGetStealth(player, out var current, out var max))
                return 0f;
            if (current <= 0f || max <= 0f)
                return 0f;
            const float DamageScale = 0.25f; // 필요 시 0.20~0.30 범위 조정
            return (current / max) * DamageScale;
        }

        // 소모 전용: 발사 순간(HandleUse)에서만 호출
        public static float ConsumeRogueStealth(Player player, float amountRatio = 1f)
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
