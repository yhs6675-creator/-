// Systems/LVRogueRegistrationList.cs
// 루나베일 오픈소스(Thrown.zip)의 투척/저글러 무기만 Rogue 대상으로 "등록".
// 안전판: ModLoader.GetMod(...)/Find(...) 사용 금지 → ModContent.TryFind(...)만 사용.
// 루나베일이 로드되지 않은 경우, 조용히 스킵.

using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    public class LVRogueRegistrationList : ModSystem
    {
        // 1) 오픈소스 투척/저글러 목록 (Thrown.zip 기준)
        private static readonly string[] ThrownClassNames =
        {
            "AlcadThrowingCards","ArtistsHeart","AssassinsKnife","AssassinsShuriken","BasicBaseball",
            "BurningFlask","CinderBomber","CleanestCleaver","ComicalTrident","DaggerDagger",
            "DarkButcher","DogmaBalls","FableKnives","FlinchMachine","GintzlSpear",
            "GreyBricks","PunkedUpChops","Ragsaw","RazorBragett","Scatterbomb",
            "SpikedLobber","SpiritCapsule","StickyCards","Stonen","TheButcher",
            "ThePenetrator","ThrowingCard","ThrowingCardsMKII","WindmillShuriken",
            "YourFired","ZenoviasPikpikglove",
            // ── 저글러 타입이 더 있으면 여기에 추가 ──
            // "JugglerKnife","JugglerOrbs", ...
        };

        // 2) 강제 Rogue 변환(원래 비투척이지만 예외로 Rogue 지정)
        private static readonly string[] ForceConvertClassNames =
        {
            // 필요 시 클래스명 추가
        };

        // 3) 제외 목록(어떤 이유로든 등록 제외)
        private static readonly string[] ExcludeClassNames =
        {
            // 필요 시 클래스명 추가
        };

        public override void PostSetupContent()
        {
            // 칼라미티 Rogue가 없으면 아무것도 안 함
            if (!ModContent.TryFind<DamageClass>("CalamityMod/RogueDamageClass", out _))
                return;

            // 루나베일이 하나도 로드되지 않았다면 조용히 스킵
            bool lunarLoaded = RogueGuards.EnumerateLunarVeilModIds().Any(ModLoader.HasMod);
            if (!lunarLoaded)
            {
                Mod.Logger.Warn("[LVCompat] Lunar Veil not detected — skipping Rogue registration.");
                return;
            }

            var excludeSet = new System.Collections.Generic.HashSet<int>();
            foreach (var cls in ExcludeClassNames)
            {
                int t0 = ResolveExternalItemType(cls);
                if (t0 > 0) excludeSet.Add(t0);
            }

            int ok = 0, fail = 0;

            // 1) 오픈소스 투척/저글러 등록
            foreach (var cls in ThrownClassNames)
            {
                int t = ResolveExternalItemType(cls);
                if (t > 0 && !excludeSet.Contains(t))
                {
                    LVRogueRegistry.Register(t);
                    ok++;
                }
                else
                {
                    fail++;
                    Mod.Logger.Info($"[LVCompat] Rogue 등록 실패/제외: {cls}");
                }
            }

            // 2) 강제 Rogue 등록(선택)
            foreach (var cls in ForceConvertClassNames)
            {
                int t = ResolveExternalItemType(cls);
                if (t > 0 && !excludeSet.Contains(t))
                {
                    LVRogueRegistry.Register(t);
                    ok++;
                }
                else
                {
                    fail++;
                    Mod.Logger.Info($"[LVCompat] Rogue 강제 등록 실패/제외: {cls}");
                }
            }

            Mod.Logger.Info($"[LVCompat] Rogue 등록 완료 — 성공 {ok} / 실패 {fail}");
        }

        private static int ResolveExternalItemType(string className)
        {
            // 안전판: TryFind는 대상 모드가 없으면 false를 반환할 뿐, 절대 예외를 던지지 않음.
            foreach (var modName in RogueGuards.EnumerateLunarVeilModIds())
            {
                if (ModContent.TryFind<ModItem>($"{modName}/{className}", out var mi))
                    return mi.Type;
            }
            return -1;
        }
    }
}
