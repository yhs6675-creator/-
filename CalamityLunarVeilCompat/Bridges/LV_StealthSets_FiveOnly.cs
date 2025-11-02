// File: Bridges/LV_StealthSets_FiveOnly.cs  (reflection-based, no compile-time Calamity reference)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat
{
    public class LV_StealthSets_FiveOnly : ModPlayer
    {
        static readonly HashSet<string> GarbageLegsNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "GarbageLegs",
            "GarbageGreaves",
            "GarbagePants",
            "GarbageBoots",
        };

        // 루나베일 방어구인지 간단 판별 (모드명=Stellamod, 내부 아이템명 매칭)
        private static bool IsLVArmor(Item item, string internalName)
        {
            if (item == null || item.IsAir || item.ModItem == null) return false;
            var modName = item.ModItem.Mod?.Name;
            bool isLunarVeilFamily = modName == "Stellamod"
                                  || modName == "LunarVeilLegacy"
                                  || modName == "LunarVeil"
                                  || modName == "LunarVeilLegacyMod"
                                  || (modName != null && modName.StartsWith("LunarVeil", StringComparison.Ordinal));

            return isLunarVeilFamily
                && (item.ModItem.Name?.Equals(internalName, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private static bool TryGetStealthMaxForSet(Player p, out float stealthMax)
        {
            stealthMax = 0f;

            var head = p.armor[0];
            var body = p.armor[1];
            var legs = p.armor[2];

            if (head?.ModItem == null || body?.ModItem == null || legs?.ModItem == null)
                return false;

            bool IsGarbageLegs(Item item)
            {
                foreach (var name in GarbageLegsNames)
                {
                    if (IsLVArmor(item, name))
                        return true;
                }
                return false;
            }

            if (IsLVArmor(head, "WindmillionHat") && IsLVArmor(body, "WindmillionRobe") && IsLVArmor(legs, "WindmillionBoots"))
            {
                stealthMax = 0.5f;
            }
            else if ((IsLVArmor(head, "LunarianVoidHead") && IsLVArmor(body, "LunarianVoidBody") && IsLVArmor(legs, "LunarianVoidLegs"))
                  || (IsLVArmor(head, "ScissorianMask")   && IsLVArmor(body, "ScissorianChestplate") && IsLVArmor(legs, "ScissorianGreaves"))
                  || (IsLVArmor(head, "EldritchianHood")  && IsLVArmor(body, "EldritchianCloak")     && IsLVArmor(legs, "EldritchianLegs"))
                  || (IsLVArmor(head, "GarbageMask")      && IsLVArmor(body, "GarbageChestplate")    && IsGarbageLegs(legs)))
            {
                stealthMax = 1.0f;
            }
            else
            {
                return false;
            }

            stealthMax = Math.Clamp(stealthMax, 0f, 1f);
            return true;
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

        // ───────────────── 실제 적용 지점 ─────────────────

        public override void UpdateEquips()
        {
            // 칼라미티 리플렉션 준비
            EnsureCalamityReflection();
            if (!okCala) return; // Calamity 없으면 아무 것도 안 함

            if (!TryGetStealthMaxForSet(Player, out float stealthMax))
                return;

            // CalamityPlayer 인스턴스 확보
            var cal = GetCalamityPlayer(Player);
            if (cal == null) return;

            // 칼라미티 Rogue 세트 착용 신호
            SetWearingRogue(cal, true);

            try
            {
                if (fRogueStealthMax != null)
                {
                    fRogueStealthMax.SetValue(cal, stealthMax);
                }
                else if (pRogueStealthMax != null && pRogueStealthMax.CanWrite)
                {
                    pRogueStealthMax.SetValue(cal, stealthMax);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
