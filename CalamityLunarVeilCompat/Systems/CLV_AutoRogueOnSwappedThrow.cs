// Systems/CLV_AutoRogueOnSwappedThrow.cs
// 목적: "클래스 스와핑" 무기가 '현재' 투척 상태일 때만 RogueDamageClass 적용.
// 휴리스틱(이름/툴팁 토큰) 제거. 오로지 확정 신호(API or ThrowingDamageClass 카운트)만 사용.

using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    public class CLV_AutoRogueOnSwappedThrow : GlobalItem
    {
        public override bool InstancePerEntity => false;

        public override void HoldItem(Item item, Player player) => TryAutoRogue(item);
        public override void UpdateInventory(Item item, Player player) => TryAutoRogue(item);

        private static void TryAutoRogue(Item item)
        {
            // 등록제로 이미 Rogue 지정된 타입은 무시
            if (LVRogueRegistry.IsRegistered(item.type))
                return;

            // 루나베일 소속 아이템만 대상
            if (!RogueGuards.IsFromLunarVeil(item))
                return;

            // 지금 '투척' 상태인지 확정 신호로만 판정
            if (!IsCurrentlyThrown_ByDefiniteSignals(item))
                return;

            if (ModContent.TryFind<DamageClass>("CalamityMod/RogueDamageClass", out var rogue))
            {
                item.DamageType = rogue;
                ModContent.GetInstance<CalamityLunarVeilCompat>().Logger.Info(
                    $"[LVCompat] Auto-Rogue (SWAP): {item.ModItem?.GetType().FullName}"
                );
            }
        }

        private static bool IsCurrentlyThrown_ByDefiniteSignals(Item item)
        {
            // 1) 루나베일이 API를 제공하는 경우(최우선)
            if (TryAskLunarVeilByCall(item, out bool isThrow))
                return isThrow;

            // 2) 루나베일 고유 ThrowingDamageClass를 실제로 '카운트'하는지 확인
            if (TryCountsAsLunarVeilThrow(item))
                return true;

            // 3) (선택적) 내부 표식/프로퍼티가 명확히 제공되는 경우만 true
            if (TryReflectExplicitFlag(item, out bool flag)) // 예: bool IsClassSwappedToThrow
                return flag;

            return false;
        }

        private static bool TryAskLunarVeilByCall(Item item, out bool isThrow)
        {
            isThrow = false;
            foreach (var name in RogueGuards.EnumerateLunarVeilModIds())
            {
                var lv = ModLoader.GetMod(name);
                if (lv == null) continue;

                object ret = null;

                // 선호 시그니처 1: (itemType)
                try { ret = lv.Call("IsClassSwappedToThrow", item.type); } catch { }
                if (ret is bool b1) { isThrow = b1; return true; }

                // 선호 시그니처 2: (item)
                try { ret = lv.Call("IsClassSwappedToThrow", item); } catch { }
                if (ret is bool b2) { isThrow = b2; return true; }
            }
            return false;
        }

        private static bool TryCountsAsLunarVeilThrow(Item item)
        {
            // 루나베일이 자체 ThrowingDamageClass를 노출한다면 그것만 신뢰
            // 후보 경로를 시도: "<Mod>/ThrowingDamageClass", "<Mod>/LVThrowingDamageClass" 등
            foreach (var name in RogueGuards.EnumerateLunarVeilModIds())
            {
                if (ModContent.TryFind<DamageClass>($"{name}/ThrowingDamageClass", out var lvThrow))
                    if (item.CountsAsClass(lvThrow)) return true;

                if (ModContent.TryFind<DamageClass>($"{name}/LVThrowingDamageClass", out var lvThrow2))
                    if (item.CountsAsClass(lvThrow2)) return true;
            }
            return false;
        }

        private static bool TryReflectExplicitFlag(Item item, out bool isThrow)
        {
            isThrow = false;
            var mi = item.ModItem;
            if (mi == null) return false;

            var t = mi.GetType();
            // "명시적" 플래그만 인정. 모호한 문자열 비교는 하지 않음.
            var prop = t.GetProperty("IsClassSwappedToThrow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                isThrow = (bool)(prop.GetValue(mi) ?? false);
                return true;
            }

            var field = t.GetField("IsClassSwappedToThrow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(bool))
            {
                isThrow = (bool)(field.GetValue(mi) ?? false);
                return true;
            }

            return false;
        }
    }
}
