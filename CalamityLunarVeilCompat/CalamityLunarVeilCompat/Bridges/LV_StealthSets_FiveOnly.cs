// File: Bridges/LV_StealthSets_FiveOnly.cs  (reflection-based, no compile-time Calamity reference)
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat
{
    public class LV_StealthSets_FiveOnly : ModPlayer
    {
        // 루나베일 5세트(Head, Body, Legs 내부명)
        // 필요시 세트 추가/수정 가능
        static readonly (string H, string B, string L)[] StealthSets = new[] {
            ("LunarianVoidHead",    "LunarianVoidBody",     "LunarianVoidLegs"),
            ("ScissorianMask",      "ScissorianChestplate", "ScissorianGreaves"),
            ("JianxinMask",         "JianxinChestplate",    "JianxinLeggings"),
            // ... (기존에 쓰시던 세트들 그대로 이어서 넣으세요)
        };

        // 루나베일 방어구인지 간단 판별 (모드명=Stellamod, 내부 아이템명 매칭)
        private static bool IsLVArmor(Item item, string internalName)
        {
            if (item == null || item.IsAir || item.ModItem == null) return false;
            return item.ModItem.Mod?.Name == "Stellamod"
                   && (item.ModItem.Name?.Equals(internalName, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        // 현재 플레이어가 위 목록 중 어떤 세트를 전부 착용했는지
        private static bool WearingAnyLVStealthSet(Player p)
        {
            var h = p.armor[0];
            var b = p.armor[1];
            var l = p.armor[2];

            foreach (var set in StealthSets)
            {
                if (IsLVArmor(h, set.H) && IsLVArmor(b, set.B) && IsLVArmor(l, set.L))
                    return true;
            }
            return false;
        }

        // ───────────────── CalamityPlayer 리플렉션 보조 ─────────────────

        static bool triedCala, okCala;
        static Assembly calAsm;
        static Type tCalamityPlayer;
        static FieldInfo fWearingRogueArmor;
        static FieldInfo fRogueStealthMax;
        static PropertyInfo pWearingRogueArmor;
        static PropertyInfo pRogueStealthMax;
        static MethodInfo mGetModPlayerGeneric;

        private static void EnsureCalamityReflection()
        {
            if (triedCala) return;
            triedCala = true;
            try
            {
                if (!ModLoader.TryGetMod("CalamityMod", out var cal)) return;

                // 어셈블리 & 타입
                calAsm = cal.Code;
                if (calAsm == null) return;

                // 네임스페이스는 버전에 따라 변동 가능 → 이름으로 탐색
                tCalamityPlayer = calAsm.GetTypes().FirstOrDefault(t => t.Name == "CalamityPlayer");
                if (tCalamityPlayer == null) return;

                // Player.GetModPlayer<T>() 제네릭 메서드 취득
                mGetModPlayerGeneric = typeof(Player).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "GetModPlayer" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1);

                // 필드/프로퍼티(버전에 따라 필드 또는 프로퍼티일 수 있으므로 둘 다 시도)
                fWearingRogueArmor = tCalamityPlayer.GetField("wearingRogueArmor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                fRogueStealthMax   = tCalamityPlayer.GetField("rogueStealthMax",   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                pWearingRogueArmor = tCalamityPlayer.GetProperty("wearingRogueArmor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                pRogueStealthMax   = tCalamityPlayer.GetProperty("rogueStealthMax",   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                okCala = mGetModPlayerGeneric != null && tCalamityPlayer != null;
            }
            catch { okCala = false; }
        }

        private static object GetCalamityPlayer(Player p)
        {
            if (!okCala) return null;
            try
            {
                var m = mGetModPlayerGeneric.MakeGenericMethod(tCalamityPlayer);
                return m.Invoke(p, null);
            }
            catch { return null; }
        }

        private static void SetWearingRogue(object calPlayerObj, bool value)
        {
            if (calPlayerObj == null) return;
            try
            {
                if (fWearingRogueArmor != null) { fWearingRogueArmor.SetValue(calPlayerObj, value); return; }
                if (pWearingRogueArmor != null && pWearingRogueArmor.CanWrite) { pWearingRogueArmor.SetValue(calPlayerObj, value); return; }
            }
            catch { /* ignore */ }
        }

        private static void AddStealthMaxAtLeast(object calPlayerObj, float minAdd)
        {
            if (calPlayerObj == null) return;
            try
            {
                // 현재 값 읽기
                float current = 0f;
                if (fRogueStealthMax != null)
                {
                    current = Convert.ToSingle(fRogueStealthMax.GetValue(calPlayerObj));
                    if (current < minAdd) fRogueStealthMax.SetValue(calPlayerObj, minAdd);
                    return;
                }
                if (pRogueStealthMax != null && pRogueStealthMax.CanRead)
                {
                    current = Convert.ToSingle(pRogueStealthMax.GetValue(calPlayerObj));
                    if (pRogueStealthMax.CanWrite && current < minAdd)
                        pRogueStealthMax.SetValue(calPlayerObj, minAdd);
                    return;
                }
            }
            catch { /* ignore */ }
        }

        // ───────────────── 실제 적용 지점 ─────────────────

        public override void UpdateEquips()
        {
            // 칼라미티 리플렉션 준비
            EnsureCalamityReflection();
            if (!okCala) return; // Calamity 없으면 아무 것도 안 함

            if (!WearingAnyLVStealthSet(Player))
                return;

            // CalamityPlayer 인스턴스 확보
            var cal = GetCalamityPlayer(Player);
            if (cal == null) return;

            // 칼라미티 Rogue 세트 착용 신호
            SetWearingRogue(cal, true);

            // 스텔스 최대치 확보 (기존 코드: 최소 +0.5f 보장)
            AddStealthMaxAtLeast(cal, 0.5f);
        }
    }
}
