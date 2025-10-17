// Systems/CalamitySidePatcher.cs
// namespace: CLVCompat.Systems

using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding; // GenVars 타입만 참조(직접 필드 접근은 리플렉션으로)

namespace CLVCompat.Systems
{
    /// <summary>
    /// Calamity 내부의 유황바다/심연 방향 결정을 리플렉션으로 강제합니다.
    /// side: -1=왼쪽, +1=오른쪽, 0=자동(패치 안 함)
    /// </summary>
    public static class CalamitySidePatcher
    {
        public static bool TryForceSulphurSide(int side)
        {
            if (side == 0) return false;

            try
            {
                // CalamityWorld 유사 타입들 후보
                string[] typeNames =
                {
                    "CalamityMod.World.CalamityWorld",
                    "CalamityMod.CalamityWorld"
                };

                var calamity = ModLoader.GetMod("CalamityMod");
                if (calamity == null) return false;

                var asm = calamity.Code;
                var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

                foreach (var tn in typeNames)
                {
                    var t = asm?.GetType(tn);
                    if (t == null) continue;

                    // 후보 필드/프로퍼티 이름들(버전별 케이스 커버)
                    string[] names =
                    {
                        "sulphurousSeaSide", "SulphurousSeaSide",
                        "sulphurOceanSide",  "SulphurOceanSide",
                        "sulphSeaSide",      "SulphSeaSide"
                    };

                    foreach (var name in names)
                    {
                        var f = t.GetField(name, flags);
                        if (f != null && f.FieldType == typeof(int)) { f.SetValue(null, side); return true; }

                        var p = t.GetProperty(name, flags);
                        if (p != null && p.CanWrite && p.PropertyType == typeof(int)) { p.SetValue(null, side); return true; }
                    }
                }
            }
            catch (Exception)
            {
                // 조용히 실패
            }

            return false;
        }

        /// <summary>
        /// 바닐라 해변/바다 힌트를 '오른쪽 선호'로 유도.
        /// 빌드마다 GenVars 필드명이 달라질 수 있으므로 리플렉션으로만 접근합니다.
        /// (없으면 그냥 건너뜀)
        /// </summary>
        public static void NudgeVanillaOceansToRight()
        {
            try
            {
                int w = Main.maxTilesX;
                var gvType = typeof(GenVars);
                var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

                // 필드 후보들: 빌드에 따라 존재하는 것만 처리
                string[] leftNames  = { "leftBeach", "leftBeachEnd", "LeftBeach", "LeftBeachEnd" };
                string[] rightNames = { "rightBeach", "rightBeachStart", "RightBeach", "RightBeachStart" };

                // leftBeach ~= 최소값 보정
                foreach (var name in leftNames)
                {
                    var f = gvType.GetField(name, flags);
                    if (f != null && f.FieldType == typeof(int))
                    {
                        int cur = (int)f.GetValue(null);
                        int next = Math.Max(100, cur);
                        if (next != cur) f.SetValue(null, next);
                        break;
                    }
                }

                // rightBeach ~= 우측 선호 보정
                foreach (var name in rightNames)
                {
                    var f = gvType.GetField(name, flags);
                    if (f != null && f.FieldType == typeof(int))
                    {
                        int cur = (int)f.GetValue(null);
                        int clamp = Math.Min(w - 100, cur);
                        int preferRight = w - w / 8; // 맵 오른쪽 12.5% 지점
                        int next = Math.Max(clamp, preferRight);
                        if (next != cur) f.SetValue(null, next);
                        break;
                    }
                }
            }
            catch
            {
                // no-op (필드 없으면 그냥 넘어감)
            }
        }
    }
}
