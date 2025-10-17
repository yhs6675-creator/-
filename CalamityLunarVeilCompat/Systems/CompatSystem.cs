// Systems/CompatSystem.cs
// namespace: CLVCompat.Systems
// — 순서 제어: (Calamity 직전) 던전X 스푸핑 → (Calamity 실행) → (Calamity 직후) 던전X 복구 —

using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.WorldBuilding;           // GenPass, PassLegacy
using Terraria.IO;                      // GameConfiguration
using Terraria.GameContent.Generation;  // GenerationProgress

namespace CLVCompat.Systems
{
    public class CompatSystem : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            // 1) 칼라미티 관련 패스 근처 인덱스 탐색
            int calamityIdx = tasks.FindIndex(p =>
            {
                string n = p?.Name ?? string.Empty;
                return n.IndexOf("Abyss", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Sulphurous", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Sulphur", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Calamity Sea", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Calamity", StringComparison.OrdinalIgnoreCase) >= 0;
            });

            if (calamityIdx < 0)
                calamityIdx = tasks.FindIndex(p => string.Equals(p?.Name, "Micro Biomes", StringComparison.OrdinalIgnoreCase));
            if (calamityIdx < 0)
                calamityIdx = 0;

            // 2) Calamity ‘직전’: 던전 좌표 스푸핑 시작
            tasks.Insert(calamityIdx, new PassLegacy("LV: Force SeaSide (Pre-Calamity)",
                (GenerationProgress progress, GameConfiguration config) =>
                {
                    progress.Message = "Forcing Calamity sea side (pre)…";
                    LVOceanCompatSystem.ApplySulphurSideOverride();
                }));

            // 3) Calamity ‘직후’: 던전 좌표 복구
            int postIdx = calamityIdx + 1;
            tasks.Insert(postIdx, new PassLegacy("LV: Assert SeaSide (Post-Calamity)",
                (GenerationProgress progress, GameConfiguration config) =>
                {
                    progress.Message = "Asserting Calamity sea side (post)…";
                    LVOceanCompatSystem.AssertSulphurSideOverride();
                }));

            // (선택) 이후에 마스크/예약 등 추가 패스를 연결하려면 이 아래에 삽입
        }
    }
}
