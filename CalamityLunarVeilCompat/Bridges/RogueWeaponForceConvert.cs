using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using CLVCompat.Systems;

namespace CalamityLunarVeilCompat.Bridges
{
    // 루나베일 "투척 무기"를 칼라미티 RogueDamageClass로 강제 전환
    public sealed class RogueWeaponForceConvert : GlobalItem
    {
        // ✅ 확실히 "투척 무기"로 판정되는 내부명(점진 확장 권장)
        //  - 기본적으로는 루나베일이 제공하는 스왑 신호/ThrowingDamageClass만 신뢰한다.
        private static readonly HashSet<string> ExplicitThrowWeapons = new()
        {
            // "StoneDagger", "BoneShuriken", ...
        };

        public override bool InstancePerEntity => false;

        public override void SetDefaults(Item item)
        {
            if (!RogueGuards.IsFromLunarVeil(item))
                return;

            var mi = item.ModItem;
            if (mi is null) return;

            // 무기만 대상(소모품 표창 등은 isWeapon 판단이 애매할 수 있으니, shoot/UseStyle도 추가 확인)
            if (!item.CountsAsClass(DamageClass.Melee) &&
                !item.CountsAsClass(DamageClass.Ranged) &&
                !item.CountsAsClass(DamageClass.Magic) &&
                !item.CountsAsClass(DamageClass.Summon) &&
                !item.CountsAsClass(DamageClass.Throwing) && // 일부 모드가 정의했을 수 있음
                item.damage <= 0)
                return;

            // 이미 Rogue라면 스킵
            if (ModContent.TryFind<DamageClass>("CalamityMod", "RogueDamageClass", out var rogueDC))
            {
                if (item.DamageType == rogueDC) return;

                // 1) 내부명 화이트리스트
                if (!ShouldConvert(mi, item))
                    return;

                item.DamageType = rogueDC;
            }
        }

        private static bool ShouldConvert(ModItem modItem, Item item)
        {
            if (modItem == null)
                return false;

            if (ExplicitThrowWeapons.Contains(modItem.Name))
                return true;

            if (RogueGuards.TryGetCurrentThrowState(item, out var isThrow))
                return isThrow;

            return false;
        }
    }
}
