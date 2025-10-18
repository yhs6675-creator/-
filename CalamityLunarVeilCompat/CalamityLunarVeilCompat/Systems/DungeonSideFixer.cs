// Systems/DungeonSideFixer.cs
// namespace: CLVCompat.Systems
// — 바닐라 Dungeon 패스 '직전'에 던전 방향을 오른쪽(+1)으로 강제.
// — WorldGen.dungeonSide 를 리플렉션으로 고정하고, 보조로 Main.dungeonX 힌트 제공.
// — Dungeon 완료 직후 실제 결과 로그 출력.

using System;
using System.Reflection;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;           // GenPass, PassLegacy
using Terraria.IO;                      // GameConfiguration
using Terraria.GameContent.Generation;  // GenerationProgress

namespace CLVCompat.Systems
{
    public class DungeonSideFixer : ModSystem
    {
        // 원하는 방향: +1 = 오른쪽, -1 = 왼쪽
        // 필요 시 외부에서 바꿀 수 있게 public 필드로 둠.
        public static int DesiredDungeonSide = +1;

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            // 1) 던전 패스 인덱스 탐색
            int dungeonIdx = tasks.FindIndex(p =>
            {
                string n = p?.Name ?? string.Empty;
                return n.IndexOf("Dungeon", StringComparison.OrdinalIgnoreCase) >= 0;
            });

            if (dungeonIdx < 0)
            {
                // 혹시 못 찾으면 Micro Biomes 이전으로라도 고정
                dungeonIdx = tasks.FindIndex(p => string.Equals(p?.Name, "Micro Biomes", StringComparison.OrdinalIgnoreCase));
                if (dungeonIdx < 0) dungeonIdx = 0;
            }

            // 2) 던전 직전: 방향 강제 + 던전X 힌트
            tasks.Insert(dungeonIdx, new PassLegacy("LV: Fix Dungeon Side (Pre-Dungeon)",
                (GenerationProgress progress, GameConfiguration config) =>
                {
                    progress.Message = "Fixing Dungeon side (pre)…";

                    // 힌트: 던전 예상 위치를 오른쪽 끝 근처로 미리 잡아줌
                    int hintX = Math.Max(250, Main.maxTilesX - 350);
                    int oldX = Main.dungeonX;
                    Main.dungeonX = hintX;

                    // WorldGen.dungeonSide 고정: +1=오른쪽, -1=왼쪽
                    ForceDungeonSide(DesiredDungeonSide);

                    ModContent.GetInstance<CalamityLunarVeilCompat>()
                        .Logger.Info($"[LVCompat] Dungeon side pre: set side={DesiredDungeonSide}, hint dungeonX {oldX} -> {Main.dungeonX}");
                }));

            // 3) 던전 직후: 결과 로깅
            int postIdx = dungeonIdx + 1;
            tasks.Insert(postIdx, new PassLegacy("LV: Fix Dungeon Side (Post-Dungeon)",
                (GenerationProgress progress, GameConfiguration config) =>
                {
                    progress.Message = "Checking Dungeon side (post)…";

                    // 바닐라 계산 결과 확인 (던전이 오른쪽이면 dungeonX > 중간값)
                    bool right = Main.dungeonX >= Main.maxTilesX / 2;
                    ModContent.GetInstance<CalamityLunarVeilCompat>()
                        .Logger.Info($"[LVCompat] Dungeon result: dungeonX={Main.dungeonX}, maxX={Main.maxTilesX} => right={right}");

                    // 칼라미티가 Abyss 기준을 읽도록 LVOceanCompatSystem 스푸핑 루틴과 병행 사용해도 무방.
                }));
        }

        private static void ForceDungeonSide(int side)
        {
            try
            {
                var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                // WorldGen.dungeonSide (int, -1/ +1)
                var f = typeof(WorldGen).GetField("dungeonSide", flags);
                if (f != null && f.FieldType == typeof(int))
                {
                    f.SetValue(null, side >= 0 ? +1 : -1);
                    ModContent.GetInstance<CalamityLunarVeilCompat>()
                        .Logger.Info($"[LVCompat] Set WorldGen.dungeonSide = {(side >= 0 ? +1 : -1)}");
                }

                // 보조: GenVars 내 유사 필드가 있으면 같이 세팅(버전 차이 대비)
                var genVarsType = typeof(GenVars);
                var f2 = genVarsType.GetField("dungeonSide", flags);
                if (f2 != null && f2.FieldType == typeof(int))
                {
                    f2.SetValue(null, side >= 0 ? +1 : -1);
                    ModContent.GetInstance<CalamityLunarVeilCompat>()
                        .Logger.Info($"[LVCompat] Set GenVars.dungeonSide = {(side >= 0 ? +1 : -1)}");
                }
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<CalamityLunarVeilCompat>()
                    .Logger.Warn($"[LVCompat] ForceDungeonSide failed: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
