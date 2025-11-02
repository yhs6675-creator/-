// CalamityLunarVeilCompat.cs
// namespace: CLVCompat

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CLVCompat.Systems;
using XnaPoint = Microsoft.Xna.Framework.Point;
using SysSize  = System.Drawing.Size;
using LVOceanSys = CLVCompat.Systems.LVOceanCompatSystem;

namespace CLVCompat
{
    public class CalamityLunarVeilCompat : Mod
    {
        public override void Load()
        {
            // ── 유황바다 방향 고정 (오른쪽) ──
            LVOceanSys.OverrideSulphurSide(+1);

            // ── 상공 금지/사후 청소 ──
            LVOceanSys.EnableSulphurSkyNoBuild(enable: true, padX: 96, extraSpan: 800, enableCleanup: true);

            // ── 바다 타워 2종 등록 ──
            LVOceanSys.RegisterSulphurPriorityShore("TowerLumi",     new SysSize(36, 110), TowerLumiPlaceAt);
            LVOceanSys.RegisterSulphurPriorityShore("TowerGothivia", new SysSize(32, 120), TowerGothiviaPlaceAt);

            // (선택) 사막 숭배의 탑은 별개 생성로직이므로 필요 시 유지
            // LVOceanSys.RegisterSulphurPriorityShore("WorshippingTowers", new SysSize(26, 92), WorshippingTowersPlaceAt);

            // ── 검증용 시그니처 타일 ──
            LVOceanSys.SetSignatureTile(TileID.ObsidianBrick);
        }

        public override void Unload()
        {
            global::CalamityLunarVeilCompat.CLV_DamageConfig.Instance = null;
        }

        private static bool TowerLumiPlaceAt(XnaPoint anchor)
        {
            try
            {
                var lv = ModLoader.GetMod("LunarVeilMod");
                if (lv != null)
                {
                    object result = lv.Call("PlaceTowerLumi", anchor.X, anchor.Y);
                    return result is bool b && b;
                }
            }
            catch (System.Exception ex)
            {
                ModContent.GetInstance<CalamityLunarVeilCompat>()
                    .Logger.Warn($"[LVCompat] TowerLumi place exception: {ex}");
            }
            return false;
        }

        private static bool TowerGothiviaPlaceAt(XnaPoint anchor)
        {
            try
            {
                var lv = ModLoader.GetMod("LunarVeilMod");
                if (lv != null)
                {
                    object result = lv.Call("PlaceTowerGothivia", anchor.X, anchor.Y);
                    return result is bool b && b;
                }
            }
            catch (System.Exception ex)
            {
                ModContent.GetInstance<CalamityLunarVeilCompat>()
                    .Logger.Warn($"[LVCompat] TowerGothivia place exception: {ex}");
            }
            return false;
        }

        // (선택) 사막 숭배의 탑
        private static bool WorshippingTowersPlaceAt(XnaPoint anchor)
        {
            try
            {
                var lv = ModLoader.GetMod("LunarVeilMod");
                if (lv != null)
                {
                    object result = lv.Call("PlaceWorshippingTowers", anchor.X, anchor.Y);
                    return result is bool b && b;
                }
            }
            catch (System.Exception ex)
            {
                ModContent.GetInstance<CalamityLunarVeilCompat>()
                    .Logger.Warn($"[LVCompat] Worshipping Towers place exception: {ex}");
            }
            return false;
        }
    }
}
