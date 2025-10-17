using System.Collections.Generic;
using Microsoft.Xna.Framework; // Rectangle
using Terraria;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    /// <summary>
    /// Lightweight scanner that builds rectangular masks for Sulphurous Sea and Abyss
    /// by checking tile/wall owners (CalamityMod). Designed to be robust across versions.
    /// </summary>
    public static class LV_CalamityMasks
    {
        public static readonly List<Rectangle> SulphSeaRects = new();
        public static readonly List<Rectangle> AbyssRects = new();

        private const string Calamity = "CalamityMod";

        public static void BuildMasks(int sample = 8, int minCluster = 120)
        {
            SulphSeaRects.Clear();
            AbyssRects.Clear();

            int leftBandStart = 20;
            int rightBandStart = Main.maxTilesX - 20;

            ScanBand(0, leftBandStart, 150, Main.maxTilesY - 200, sample, minCluster, SulphSeaRects);
            ScanBand(rightBandStart, Main.maxTilesX, 150, Main.maxTilesY - 200, sample, minCluster, SulphSeaRects);

            // Deep band for Abyss.
            ScanBand(0, Main.maxTilesX, Main.maxTilesY - 400, Main.maxTilesY - 50, sample, minCluster, AbyssRects);
        }

        private static void ScanBand(int x0, int x1, int y0, int y1, int step, int minCount, List<Rectangle> outRects)
        {
            int w = x1 - x0;
            int h = y1 - y0;
            if (w <= 0 || h <= 0) return;

            int cell = 24; // coarse grid cell size
            for (int gx = x0; gx < x1; gx += cell)
            {
                for (int gy = y0; gy < y1; gy += cell)
                {
                    int cnt = 0;
                    for (int x = gx; x < gx + cell && x < x1; x += step)
                    {
                        for (int y = gy; y < gy + cell && y < y1; y += step)
                        {
                            if (!WorldGen.InWorld(x, y, 10)) continue;

                            Tile t = Main.tile[x, y];
                            var mt = TileLoader.GetTile(t.TileType);
                            var mw = t.WallType > 0 ? WallLoader.GetWall(t.WallType) : null;
                            if ((mt?.Mod?.Name == Calamity) || (mw?.Mod?.Name == Calamity))
                                cnt++;
                        }
                    }
                    if (cnt >= minCount)
                        outRects.Add(new Rectangle(gx, gy, cell, cell));
                }
            }
            // NOTE: optional: merge adjacent rectangles if needed.
        }

        public static bool IntersectsSulphOrAbyss(Rectangle r, int pad = 6)
        {
            var pr = new Rectangle(r.X - pad, r.Y - pad, r.Width + pad * 2, r.Height + pad * 2);
            foreach (var s in SulphSeaRects)
                if (pr.Intersects(s)) return true;
            foreach (var s in AbyssRects)
                if (pr.Intersects(s)) return true;
            return false;
        }
    }
}