// Systems/OceanCompatIntegration.cs
// CLVCompat의 LVOceanCompatSystem에 구조물(바다/해안) 등록을 연결하는 통합 클래스
// 컴파일 오류(CS0234) 원인: LVOceanCompatSystem를 네임스페이스처럼 사용 -> 클래스 별칭으로 고정

using Terraria.ModLoader;
using Terraria.ID;
using Terraria; // WorldGen 등
using XnaPoint = Microsoft.Xna.Framework.Point; // Point 충돌 방지
using SysSize  = System.Drawing.Size;           // Size 충돌 방지
using LVOceanSys = CLVCompat.Systems.LVOceanCompatSystem; // ★ 시스템 클래스 별칭

namespace CLVCompat.Systems
{
    public class OceanCompatIntegration : ModSystem
    {
        public override void Load()
        {
            // 여기에서 LVOceanCompatSystem에 필요한 구조물들을 등록합니다.
            // 이미 메인 모드 클래스(CalamityLunarVeilCompat.cs)에서
            // Worshipping Towers를 우선 등록했다면, 중복 등록은 피하세요.

            // 예시) 루나베일 쪽 '바다 구조물'을 등록하고 싶을 때:
            // LVOceanSys.RegisterOceanStructure(
            //     id: "LunarSeaSanctum",
            //     size: new SysSize(40, 28),
            //     placeAt: PlaceLunarSeaSanctum
            // );

            // 예시) 루나베일 쪽 '해안 구조물'을 등록하고 싶을 때:
            // LVOceanSys.RegisterShoreStructure(
            //     id: "LunarShoreOutpost",
            //     size: new SysSize(34, 24),
            //     placeAt: PlaceLunarShoreOutpost
            // );
        }

        // ─────────────────────────────────────────────
        // 아래 Place* 메서드들은 "왼쪽-위 모서리(anchor)" 기준으로 배치하세요.
        // 실제 구조물 생성 로직으로 교체하면 됩니다.
        // 현재는 컴파일 안전한 템플릿만 제공(더미 타일 1개 찍고 true).
        // ─────────────────────────────────────────────

        private static bool PlaceLunarSeaSanctum(XnaPoint anchor)
        {
            // TODO: 실제 구조물 타일 배치 코드를 넣으세요.
            WorldGen.PlaceTile(anchor.X, anchor.Y + 1, TileID.ObsidianBrick, mute: true, forced: true);
            return true;
        }

        private static bool PlaceLunarShoreOutpost(XnaPoint anchor)
        {
            // TODO: 실제 구조물 타일 배치 코드를 넣으세요.
            WorldGen.PlaceTile(anchor.X, anchor.Y + 1, TileID.GrayBrick, mute: true, forced: true);
            return true;
        }
    }
}
