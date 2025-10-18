// Systems/LV_BuildingLocalGuard.cs
// namespace: CLVCompat.Systems
// — 구조물 배치 보조: 겹침 방지, 유황/어비스 마스크 교차 차단, 최소 이격(클리어런스) 보장 —

using System;
using Microsoft.Xna.Framework;            // Point, Rectangle
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;            // GenVars.structures, StructureMap
// 참고: tModLoader 1.4.4 기준 StructureMap에는 ContainsAny가 없고 CanPlace(Rectangle,int) 사용.

namespace CLVCompat.Systems
{
    public static class LV_BuildingLocalGuard
    {
        /// <summary>
        /// 요청한 직사각형 영역을, 월드 경계/기존 구조물/마스크와 겹치지 않게 적절히 밀어 넣어 배치합니다.
        /// - padding: 보호 여유폭
        /// - maxTries: 시도 횟수
        /// - allowSulphurTouch: 유황 타일과의 경미한 접촉 허용 (액체/마스크와의 교차는 여전히 차단)
        /// 성공 시 실제 배치된 영역을 out으로 돌려줍니다.
        /// </summary>
        public static bool TryPlaceNoOverlap(Rectangle request, out Rectangle placed, int padding = 8, int maxTries = 60, bool allowSulphurTouch = false)
        {
            placed = request;

            // 0) 기본 클램프
            placed = ClampToWorld(placed);

            for (int i = 0; i < Math.Max(4, maxTries); i++)
            {
                // 1) 패딩 확대 사각형
                Rectangle padded = Inflate(placed, padding);

                // 2) 월드 경계
                if (!InWorldSafe(padded))
                {
                    placed = Nudge(placed);
                    continue;
                }

                // 3) 기존 보호 구조물과 겹침 금지
                // StructureMap.ContainsAny 대신 CanPlace 사용: 배치 가능하지 않으면 겹침이 있는 것
                if (GenVars.structures != null && !GenVars.structures.CanPlace(padded, 0))
                {
                    placed = Nudge(placed);
                    continue;
                }

                // 4) 유황/어비스 금지 마스크와 교차 금지
                if (LV_CalamityMasks.IntersectsSulphOrAbyss(padded))
                {
                    placed = Nudge(placed);
                    continue;
                }

                // 4.5) 유황 가장자리와의 최소 이격(12타일) 보장
                if (!HasClearanceFromSulphOrAbyss(placed, 12))
                {
                    placed = Nudge(placed);
                    continue;
                }

                // 5) 지형 기초 확인(필요 최소 타일 기반)
                if (!HasBasicFooting(placed, requireRows: 1))
                {
                    placed = Nudge(placed);
                    continue;
                }

                // 6) (선택) 유황 타일 직접 접촉 허용/차단
                if (!allowSulphurTouch && TouchesSulphurTiles(placed))
                {
                    placed = Nudge(placed);
                    continue;
                }

                // 통과
                return true;
            }

            return false;
        }

        // ───────────────────── 내부 유틸 ─────────────────────

        private static bool InWorldSafe(Rectangle r)
        {
            return r.Left >= 2 && r.Top >= 2 && r.Right < Main.maxTilesX - 2 && r.Bottom < Main.maxTilesY - 2;
        }

        public static Rectangle Inflate(Rectangle r, int pad)
        {
            return new Rectangle(r.Left - pad, r.Top - pad, r.Width + pad * 2, r.Height + pad * 2);
        }

        public static Rectangle Inflate(Rectangle r, int padX, int padY)
        {
            return new Rectangle(r.Left - padX, r.Top - padY, r.Width + padX * 2, r.Height + padY * 2);
        }

        public static Rectangle ClampToWorld(Rectangle r)
        {
            int l = Math.Max(2, r.Left);
            int t = Math.Max(2, r.Top);
            int rr = Math.Min(Main.maxTilesX - 3, r.Right);
            int bb = Math.Min(Main.maxTilesY - 3, r.Bottom);
            if (rr <= l) rr = l + 1;
            if (bb <= t) bb = t + 1;
            return new Rectangle(l, t, rr - l, bb - t);
        }

        /// <summary>간단한 랜덤 이동으로 새로운 위치를 탐색</summary>
        public static Rectangle Nudge(Rectangle r)
        {
            int dx = WorldGen.genRand.Next(-4, 5);
            int dy = WorldGen.genRand.Next(-2, 3);
            r.Offset(dx, dy);
            return ClampToWorld(r);
        }

        /// <summary>유황/어비스 마스크 경계에서 최소 이격 보장</summary>
        private static bool HasClearanceFromSulphOrAbyss(Rectangle area, int clearance)
        {
            var pad = Inflate(area, clearance);
            return !LV_CalamityMasks.IntersectsSulphOrAbyss(pad);
        }

        /// <summary>하부 지지 타일이 최소한 있는지(간단 검사)</summary>
        private static bool HasBasicFooting(Rectangle area, int requireRows)
        {
            int y = area.Bottom - 1;
            int count = 0;
            for (int x = area.Left; x < area.Right; x++)
            {
                var t = Framing.GetTileSafely(x, y);
                if (t.HasTile && Main.tileSolid[t.TileType])
                {
                    count++;
                    if (count >= Math.Max(1, area.Width / 3)) return true;
                }
            }
            return false;
        }

        /// <summary>영역 안에 유황 지형(타일)이 직접 닿는지</summary>
        private static bool TouchesSulphurTiles(Rectangle area)
        {
            // 간단 판정: 내부 타일 중 칼라미티 유황 계열이 있는지 검사
            // (정확한 타일 ID 매핑은 Calamity 설치 환경에서 확장 가능)
            for (int x = area.Left; x < area.Right; x++)
            {
                for (int y = area.Top; y < area.Bottom; y++)
                {
                    if (!WorldGen.InWorld(x, y, 10)) continue;
                    var t = Framing.GetTileSafely(x, y);
                    if (!t.HasTile) continue;

                    // 필요 시 Calamity 유황 타일 ID 매핑 추가 지점
                    // if (t.TileType == CalamityTileId.SulphurousSand || ...)
                }
            }
            return false;
        }
    }
}
