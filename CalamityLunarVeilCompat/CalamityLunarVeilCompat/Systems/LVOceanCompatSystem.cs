// Systems/LVOceanCompatSystem.cs
// namespace: CLVCompat.Systems
// — 던전 스푸핑(유황/심연 방향 유도) + Lumi/Gothivia 폴백 + Illuria 배치 + 액체 정리·기초 보강 강화 —

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;              // Point, Rectangle
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;              // GenVars.structures
using CLVCompat;

namespace CLVCompat.Systems
{
    public class LVOceanCompatSystem : ModSystem
    {
        // ─────────────────────────────────────
        // Sulphur side control by dungeon spoof
        // ─────────────────────────────────────
        private static int _overrideSulphurSide = 0;      // -1=왼쪽, +1=오른쪽, 0=해제
        private static int _savedDungeonX = int.MinValue; // 복구용

        // ─────────────────────────────────────
        // Sky No-Build settings (보관)
        // ─────────────────────────────────────
        private static bool _skyNoBuildEnabled = false;
        private static int  _skyPadX = 96;
        private static int  _skyExtraSpan = 800;
        private static bool _skyCleanup = false;

        // ─────────────────────────────────────
        // Structures
        // ─────────────────────────────────────
        private struct ShoreEntry
        {
            public string Id;
            public System.Drawing.Size Size;
            public Func<Point, bool> PlaceAt;
        }

        private static readonly List<ShoreEntry> _priorityShores = new();
        private static int  _signatureTileId = -1;
        private static bool _illuriaPlaced;

        // ============================================================
        // Public API — Sulphur side control (dungeon spoof)
        // ============================================================
        public static void OverrideSulphurSide(int side)
        {
            if (side < -1 || side > 1) side = 0;
            _overrideSulphurSide = side;
            ModContent.GetInstance<CalamityLunarVeilCompat>().Logger.Info($"[LVCompat] OverrideSulphurSide set to {side}");
        }

        public static void ApplySulphurSideOverride()
        {
            if (_overrideSulphurSide == 0) return;
            if (_savedDungeonX != int.MinValue) return;

            _savedDungeonX = Main.dungeonX;

            if (_overrideSulphurSide > 0) // 오른쪽 고정
                Main.dungeonX = Math.Max(10, Main.maxTilesX - 10);
            else                          // 왼쪽 고정
                Main.dungeonX = 10;

            ModContent.GetInstance<CalamityLunarVeilCompat>()
                .Logger.Info($"[LVCompat] Spoof dungeonX: {_savedDungeonX} -> {Main.dungeonX}");
        }

        public static void AssertSulphurSideOverride()
        {
            if (_savedDungeonX == int.MinValue) return;

            int now = Main.dungeonX;
            Main.dungeonX = _savedDungeonX;
            _savedDungeonX = int.MinValue;

            ModContent.GetInstance<CalamityLunarVeilCompat>()
                .Logger.Info($"[LVCompat] Restore dungeonX: {now} -> {Main.dungeonX}");
        }

        // ============================================================
        // Public API — Sky / Signature / Registration
        // ============================================================
        public static void EnableSulphurSkyNoBuild(bool enable, int padX = 96, int extraSpan = 800, bool enableCleanup = false)
        {
            _skyNoBuildEnabled = enable;
            _skyPadX = padX;
            _skyExtraSpan = extraSpan;
            _skyCleanup = enableCleanup;
        }

        public static void SetSignatureTile(int tileId) => _signatureTileId = tileId;

        public static void RegisterSulphurPriorityShore(string id, System.Drawing.Size size, Func<Point, bool> placeAt)
        {
            if (string.IsNullOrWhiteSpace(id) || placeAt == null) return;
            int idx = _priorityShores.FindIndex(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            var entry = new ShoreEntry { Id = id, Size = size, PlaceAt = placeAt };
            if (idx >= 0) _priorityShores[idx] = entry; else _priorityShores.Add(entry);
        }

        // 조회용
        public static int  OverrideSulphur   => _overrideSulphurSide;
        public static bool SkyNoBuildEnabled => _skyNoBuildEnabled;
        public static int  SkyPadX           => _skyPadX;
        public static int  SkyExtraSpan      => _skyExtraSpan;
        public static bool SkyCleanup        => _skyCleanup;

        // ============================================================
        // Terrain helpers — 액체 정리·기초 보강
        // ============================================================
        private static void CarveAndStabilize(Rectangle area, int baseTileId, int baseRows = 3, bool clearLiquids = true)
        {
            int left   = Math.Max(10, area.Left);
            int right  = Math.Min(Main.maxTilesX - 10, area.Right);
            int top    = Math.Max(10, area.Top);
            int bottom = Math.Min(Main.maxTilesY - 10, area.Bottom);

            if (clearLiquids)
            {
                for (int x = left; x < right; x++)
                {
                    for (int y = top; y < bottom; y++)
                    {
                        var t = Framing.GetTileSafely(x, y);
                        if (t.LiquidAmount > 0)
                        {
                            t.LiquidAmount = 0;
                            t.LiquidType = 0;
                        }
                    }
                }
            }

            // 하단 보강(기본 3줄)
            for (int x = left; x < right; x++)
            {
                for (int r = 1; r <= baseRows; r++)
                {
                    int y = bottom - r;
                    var t = Framing.GetTileSafely(x, y);

                    if (!t.HasTile || t.LiquidAmount > 0)
                    {
                        t.HasTile = true;
                        t.TileType = (ushort)baseTileId;
                        t.Slope = 0;
                        t.IsHalfBlock = false;
                        t.LiquidAmount = 0;
                        t.LiquidType = 0;
                    }
                }
            }

            // 프레임 갱신
            for (int x = left; x < right; x++)
                for (int y = bottom - baseRows; y < bottom; y++)
                    WorldGen.SquareTileFrame(x, y, true);
        }

        private static void ClearLiquidBand(Rectangle area, int pad)
        {
            var a = new Rectangle(
                Math.Max(2, area.Left - pad),
                Math.Max(2, area.Top - pad),
                Math.Min(Main.maxTilesX - 4, area.Width + pad * 2),
                Math.Min(Main.maxTilesY - 4, area.Height + pad * 2)
            );

            for (int x = a.Left; x < a.Right; x++)
            for (int y = a.Top; y < a.Bottom; y++)
            {
                if (!WorldGen.InWorld(x, y, 10)) continue;
                var t = Framing.GetTileSafely(x, y);
                if (t.LiquidAmount > 0) { t.LiquidAmount = 0; t.LiquidType = 0; }
            }
        }

        // ============================================================
        // Structure placement
        // ============================================================
        /// <summary>
        /// 우선 등록된 바다 타워 배치. Lumi/Gothivia는 유황 경계 폴백을 적용.
        /// </summary>
        public static bool PlaceRegisteredOceanTower(string id, int shoreX, int surfaceY)
        {
            var entry = _priorityShores.Find(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (entry.PlaceAt == null && entry.Size.IsEmpty) return false;

            int w = entry.Size.Width  > 0 ? entry.Size.Width  : 48;
            int h = entry.Size.Height > 0 ? entry.Size.Height : 160;
            int y = surfaceY - h + 20;

            // Lumi 또는 Gothivia면 폴백 대상
            bool needsOceanFallback =
                id.Equals("TowerLumi", StringComparison.OrdinalIgnoreCase) ||
                id.Equals("TowerGothivia", StringComparison.OrdinalIgnoreCase);

            // 기본 시도
            Func<Point, bool> defaultPlace = entry.PlaceAt ?? (anchor =>
            {
                var r = new Rectangle(anchor.X - w / 2, y, w, h);
                return LV_BuildingLocalGuard.TryPlaceNoOverlap(
                    r, out _, padding: 8, maxTries: 60, allowSulphurTouch: needsOceanFallback);
            });

            bool ok = false;
            try { ok = defaultPlace(new Point(shoreX, y)); }
            catch (Exception ex)
            {
                ModContent.GetInstance<CalamityLunarVeilCompat>().Logger.Warn($"[LVCompat] {id} default place exception: {ex}");
            }

            // 폴백: 오른쪽 바다 기준 안쪽으로 이동 + 소규모 정리·보강(증강)
            if (!ok && needsOceanFallback)
            {
                int[] inwardOffsets = { 12, 18, 24, 30, 36, 48, 60, 72, 84, 96 };
                foreach (int dx in inwardOffsets)
                {
                    int anchorX = Math.Max(20, Math.Min(Main.maxTilesX - 20, shoreX - dx)); // right ocean → x 감소
                    var area = new Rectangle(anchorX - w / 2, y, w, h);

                    ClearLiquidBand(area, 10);
                    CarveAndStabilize(area,
                        baseTileId: Terraria.ID.TileID.HardenedSand, // 모래 지형에 자연스럽게
                        baseRows: 3,
                        clearLiquids: true);

                    try
                    {
                        if (defaultPlace(new Point(anchorX, y)))
                        {
                            ok = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        ModContent.GetInstance<CalamityLunarVeilCompat>()
                            .Logger.Warn($"[LVCompat] {id} fallback dx={dx} place exception: {ex}");
                    }
                }
            }

            if (ok)
            {
                var placedArea = new Rectangle(shoreX - w / 2, y, w, h);
                GenVars.structures?.AddProtectedStructure(placedArea);

                if (_signatureTileId > 0)
                {
                    var p = new Point(placedArea.Left + placedArea.Width / 2, placedArea.Top + placedArea.Height / 2);
                    WorldGen.PlaceTile(p.X, p.Y, _signatureTileId, mute: true, forced: true);
                }
            }

            return ok;
        }

        /// <summary>
        /// Illuria 사전 확정 배치. 요청 영역(rect)에 겹치지 않도록 시도하고,
        /// 성공 시 구조물 보호 및 시그니처 타일을 심습니다.
        /// </summary>
        public static bool PlaceIlluriaAt(int originX, int originY, int width, int height)
        {
            var rect = new Rectangle(originX, originY, width, height);

            ClearLiquidBand(rect, 10);
            CarveAndStabilize(rect,
                baseTileId: Terraria.ID.TileID.HardenedSand,
                baseRows: 3,
                clearLiquids: true);

            if (LV_BuildingLocalGuard.TryPlaceNoOverlap(rect, out var placed, padding: 10, maxTries: 64, allowSulphurTouch: true))
            {
                _illuriaPlaced = true;
                GenVars.structures?.AddProtectedStructure(placed);

                if (_signatureTileId > 0)
                {
                    var p = new Point(placed.Left + placed.Width / 2, placed.Top + placed.Height / 2);
                    WorldGen.PlaceTile(p.X, p.Y, _signatureTileId, mute: true, forced: true);
                }
                return true;
            }

            _illuriaPlaced = false;
            return false;
        }

        public static bool PlaceWorshippingTowers(int leftShoreX, int rightShoreX, int surfaceY)
        {
            var towers = _priorityShores.Find(e => e.Id.Equals("WorshippingTowers", StringComparison.OrdinalIgnoreCase));
            if (towers.PlaceAt == null && towers.Size.IsEmpty) return false;

            int w = towers.Size.Width  > 0 ? towers.Size.Width  : 48;
            int h = towers.Size.Height > 0 ? towers.Size.Height : 160;
            int y = surfaceY - h + 20;

            Func<Point, bool> placeAt = towers.PlaceAt ?? (anchor =>
            {
                var r = new Rectangle(anchor.X - w / 2, y, w, h);
                return LV_BuildingLocalGuard.TryPlaceNoOverlap(r, out _, padding: 8, maxTries: 40);
            });

            // Illuria를 먼저 놓았으면 반대편부터 시도
            if (_illuriaPlaced)
            {
                if (placeAt(new Point(rightShoreX, y))) return true;
                if (placeAt(new Point(leftShoreX,  y))) return true;
            }
            else
            {
                if (placeAt(new Point(leftShoreX,  y))) return true;
                if (placeAt(new Point(rightShoreX, y))) return true;
            }
            return false;
        }
    }
}
