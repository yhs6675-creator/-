// LV_LatePlacement.cs
// namespace: CLVCompat.Systems

using Microsoft.Xna.Framework;
using Terraria;
using XnaPoint = Microsoft.Xna.Framework.Point;
using XnaRect  = Microsoft.Xna.Framework.Rectangle;

namespace CLVCompat.Systems
{
    /// <summary>
    /// 월드젠 최후단에서 실행되어
    /// - Illuria 우선 배치
    /// - 바다 타워(TowerLumi / TowerGothivia) 분산 배치
    /// 를 처리합니다. Calamity 유황바다/심연과의 겹침은 LocalGuard가 회피합니다.
    /// </summary>
    public static class LV_LatePlacement
    {
        public static void RunOceanLatePlacement()
        {
            int leftShoreX  = FindShoreX(left: true);
            int rightShoreX = FindShoreX(left: false);
            int surfaceY    = FindSurfaceY((leftShoreX + rightShoreX) / 2);
            if (leftShoreX <= 0 || rightShoreX <= 0 || surfaceY <= 0 || rightShoreX - leftShoreX < 400)
                return;

            // 1) Illuria (예: 오른쪽 해안 우선)
            int illuriaW = 240, illuriaH = 160;
            int illuriaX = rightShoreX - illuriaW / 2;
            int illuriaY = surfaceY - illuriaH + 30;
            bool illuriaOk = LVOceanCompatSystem.PlaceIlluriaAt(illuriaX, illuriaY, illuriaW, illuriaH);

            // 2) 바다 타워 2종 — 좌/우 분산
            //    일루리아가 오른쪽이면 Lumi는 좌쪽 우선, Gothivia는 우측 우선
            bool lumiOk = LVOceanCompatSystem.PlaceRegisteredOceanTower("TowerLumi", leftShoreX, surfaceY)
                       || LVOceanCompatSystem.PlaceRegisteredOceanTower("TowerLumi", rightShoreX, surfaceY);

            bool gothOk = LVOceanCompatSystem.PlaceRegisteredOceanTower("TowerGothivia", rightShoreX, surfaceY)
                       || LVOceanCompatSystem.PlaceRegisteredOceanTower("TowerGothivia", leftShoreX, surfaceY);

            // (필요하면 디버그 로그 활성화)
            // ModContent.GetInstance<CalamityLunarVeilCompat>().Logger.Debug($"Illuria:{illuriaOk} Lumi:{lumiOk} Goth:{gothOk}");
        }

        // ── 좌/우 해안 X 근사 ──
        private static int FindShoreX(bool left)
        {
            int x = left ? 20 : Main.maxTilesX - 20;
            int step = left ? +1 : -1;
            int y0 = 100, y1 = 300;

            for (; x > 10 && x < Main.maxTilesX - 10; x += step)
            {
                int waterCount = 0;
                for (int y = y0; y < y1; y += 3)
                {
                    if (!WorldGen.InWorld(x, y, 10)) continue;
                    Tile t = Main.tile[x, y];
                    if (t.LiquidAmount > 0 && t.LiquidType == 0) waterCount++;
                }
                if (waterCount < 10) break; // 물 밀도 하락 변곡점 ≒ 해안
            }
            return x;
        }

        // ── 표면 Y 근사 ──
        private static int FindSurfaceY(int x)
        {
            for (int y = 50; y < Main.maxTilesY - 200; y++)
            {
                if (!WorldGen.InWorld(x, y, 10)) continue;
                if (Main.tile[x, y].HasTile) return y;
            }
            return 200;
        }
    }
}
