// Systems/LVThrowToRogueCompat.cs
// 등록된 루나베일 투척/저글러 무기에만 RogueDamageClass 적용.
// 적용 타이밍: HoldItem/UpdateInventory (루나베일 SetDefaults 이후).

using Terraria;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    public class LVThrowToRogueCompat : GlobalItem
    {
        public override bool InstancePerEntity => false;

        public override void HoldItem(Item item, Player player) => TryApplyRogue(item);
        public override void UpdateInventory(Item item, Player player) => TryApplyRogue(item);

        private static void TryApplyRogue(Item item)
        {
            if (item?.ModItem is null)
                return;

            if (!LVRogueRegistry.IsRegistered(item.type))
                return;

            if (!RogueGuards.IsFromLunarVeil(item))
                return;

            if (!RogueGuards.TryGetCalamityRogue(out var rogue))
                return;

            // 확정 신호로 '현재 투척'이 false임이 확인되면 적용하지 않음
            if (RogueGuards.TryGetCurrentThrowState(item, out var isThrow) && !isThrow)
                return;

            if (item.DamageType != rogue)
                item.DamageType = rogue;
        }
    }
}
