// Systems/CLV_AutoRogueOnSwappedThrow.cs
// 목적: "클래스 스와핑" 무기가 '현재' 투척 상태일 때만 RogueDamageClass 적용.
// 휴리스틱(이름/툴팁 토큰) 제거. 오로지 확정 신호(API or ThrowingDamageClass 카운트)만 사용.

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

        public override bool CanUseItem(Item item, Player player)
        {
            TryAutoRogue(item);
            return base.CanUseItem(item, player);
        }

        public override void UseStyle(Item item, Player player, Microsoft.Xna.Framework.Rectangle heldItemFrame)
        {
            TryAutoRogue(item);
            base.UseStyle(item, player, heldItemFrame);
        }

        private static void TryAutoRogue(Item item)
        {
            if (item == null)
                return;

            // 등록제로 이미 Rogue 지정된 타입은 무시
            if (LVRogueRegistry.IsRegistered(item.type))
                return;

            bool isThrow = false;
            RogueGuards.TryGetCurrentThrowState(item, out isThrow);

            bool shouldForce = ShouldForceRogue(item) || (ProblemWeaponRegistry.IsProblemAnyItem(item) && isThrow);

            if (shouldForce)
            {
                if (!RogueGuards.TryForceRogueDamageClass(item))
                    RogueGuards.RestoreOriginalDamageClass(item);
            }
            else
            {
                RogueGuards.RestoreOriginalDamageClass(item);
            }
        }

        private static bool ShouldForceRogue(Item item)
        {
            if (RogueGuards.TryGetCurrentThrowState(item, out bool isThrow))
                return isThrow;

            if (TryReflectExplicitFlag(item, out bool flag))
                return flag;

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
