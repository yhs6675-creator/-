using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat.Bridges
{
    // 루나베일 "투척 무기"를 칼라미티 RogueDamageClass로 강제 전환
    public sealed class RogueWeaponForceConvert : GlobalItem
    {
        // ✅ 루나베일 내부명 2종 모두 허용
        private static readonly HashSet<string> AllowedMods = new()
        {
            "LunarVielmod",
            "Stellamod",
        };

        // ✅ 확실히 "투척 무기"로 판정되는 내부명(점진 확장 권장)
        //  - 초기엔 비워도 됨. 아래의 휴리스틱이 기본 동작을 커버.
        private static readonly HashSet<string> ExplicitThrowWeapons = new()
        {
            // "StoneDagger", "BoneShuriken", ...
        };

        // ✅ 휴리스틱: 네임스페이스/클래스명에 Throw/Thrown이 들어간 무기를 Rogue로 전환
        private static bool LooksLikeThrowing(ModItem mi)
        {
            // 클래스명/네임스페이스에 Throw/Thrown가 들어가면 투척 계열로 추정
            var t = mi.GetType();
            string fullname = t.FullName ?? t.Name;
            return fullname.Contains("Throw") || fullname.Contains("Thrown");
        }

        public override bool InstancePerEntity => false;

        public override void SetDefaults(Item item)
        {
            var mi = item.ModItem;
            if (mi is null) return;

            string modName = mi.Mod?.Name ?? string.Empty;
            if (!AllowedMods.Contains(modName)) return;

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
                if (ExplicitThrowWeapons.Contains(mi.Name))
                {
                    item.DamageType = rogueDC;
                    return;
                }

                // 2) 휴리스틱: Throw/Thrown 패턴을 가진 무기 → Rogue로 전환
                if (LooksLikeThrowing(mi))
                {
                    item.DamageType = rogueDC;
                    return;
                }

                // 3) 추가 휴리스틱: 투척형 사용 모션(던지기 계열) 추정
                //  - 과도한 오탐을 막기 위해 마지막 단계로만 사용
                if (item.noMelee && item.useStyle == 1 /*SwingThrow 기본*/ && item.shoot > 0)
                {
                    item.DamageType = rogueDC;
                }
            }
        }
    }
}
