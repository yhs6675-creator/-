// Systems/CLV_AutoRogueOnSwappedThrow.cs
// 목적: "클래스 스와핑" 무기가 '현재' 투척 상태일 때만 RogueDamageClass 적용.
// 확정 신호(루나베일 Mod.Call 또는 ThrowingDamageClass 카운트)만 사용.

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
            if (item?.ModItem is null)
                return;

            if (!RogueGuards.IsFromLunarVeil(item))
                return;

            // 등록제 대상은 별도 시스템에서 처리
            if (LVRogueRegistry.IsRegistered(item.type))
                return;

            if (!RogueGuards.TryGetCalamityRogue(out var rogue))
                return;

            if (!RogueGuards.TryGetCurrentThrowState(item, out var isThrow) || !isThrow)
                return;

            if (item.DamageType != rogue)
                item.DamageType = rogue;
        }
    }
}
