using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat
{
    /// <summary>
    /// Calamity Rogue Stealth 연동 브리지.
    /// 공격 트리거 시 최종적으로 스텔스를 0으로 강제 세팅.
    /// </summary>
    internal static class CalamityStealthBridge
    {
        private static Mod calamity;
        static CalamityStealthBridge() => ModLoader.TryGetMod("CalamityMod", out calamity);

        public static bool Available => calamity != null;

        /// <summary>
        /// 공격 "첫 프레임"에서 호출. Calamity 호출 후 결과와 무관하게 0으로 고정.
        /// </summary>
        public static bool ConsumeOnceByAttack(Player player)
        {
            // (선호) Calamity 공식 로직 먼저 호출해 부가 플래그 갱신 시도
            if (Available)
            {
                try { calamity.Call("ConsumeStealth", player); }
                catch { /* 무시하고 강제 처리 진행 */ }
            }
            return ForceDrainToMinimum(player);
        }

        /// <summary>
        /// (참고) 채널 무기용 틱 소모가 필요하면 호출. 현재 설계에선 첫 프레임에 이미 0이라 불필요.
        /// </summary>
        public static bool ConsumePerTick(Player player, float _ignored) => ForceDrainToMinimum(player);

        // ───────── 내부 유틸 ─────────

        private static bool ForceDrainToMinimum(Player player)
        {
            try
            {
                var (t, mp) = GetCalPlayer(player);
                if (mp == null) return false;

                float max = GetField(t, mp, "rogueStealthMax", "maxStealth", "stealthMax");
                if (max <= 0f) return false;

                // 🔥 최소치 = 0
                SetStealth(t, mp, 0f);
                return true;
            }
            catch { return false; }
        }

        private static (Type, object) GetCalPlayer(Player p)
        {
            var t = Type.GetType("CalamityMod.CalamityPlayer, CalamityMod")
                ?? Type.GetType("CalamityMod.CalamityPlayer, CalamityModPublic");
            if (t == null) return (null, null);

            var getModPlayer = typeof(Player).GetMethod("GetModPlayer", new[] { typeof(Type) });
            var mp = getModPlayer?.Invoke(p, new object[] { t });
            return (t, mp);
        }

        private static float GetField(Type t, object o, params string[] names)
        {
            foreach (var n in names)
            {
                var f = t.GetField(n);
                if (f != null) return Convert.ToSingle(f.GetValue(o));
            }
            return 0f;
        }

        private static void SetStealth(Type t, object o, float v)
        {
            var f = t.GetField("rogueStealth") ?? t.GetField("stealth") ?? t.GetField("currentStealth");
            f?.SetValue(o, v);
        }
    }
}
