using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using CLVCompat.Systems;
using CLVCompat.Utils;

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

        public override void HoldItem(Item item, Player player) => Process(item);

        public override void UpdateInventory(Item item, Player player) => Process(item);

        public override void SetDefaults(Item item) => Process(item);

        private static void Process(Item item)
        {
            if (item == null)
                return;

            bool whitelisted = WhitelistIndex.WhitelistTypes.Contains(item.type);
            string normalized = DisplayNameNormalizer.Normalize(Lang.GetItemNameValue(item.type));
            CompatDebug.LogWhitelistEntry(item, normalized, whitelisted);

            if (!whitelisted && !RogueGuards.IsFromLunarVeil(item))
            {
                RogueGuards.RestoreOriginalDamageClass(item);
                return;
            }

            var modItem = item.ModItem;
            if (modItem == null)
            {
                RogueGuards.RestoreOriginalDamageClass(item);
                return;
            }

            bool shouldConvert = whitelisted || ExplicitThrowWeapons.Contains(modItem.Name);

            if (!shouldConvert && RogueGuards.TryGetCurrentThrowState(item, out var isThrow))
                shouldConvert = isThrow;

            if (!shouldConvert && RogueGuards.TryGetLVThrowDamageClass(out var lvThrow) && item.CountsAsClass(lvThrow))
                shouldConvert = true;

            if (shouldConvert)
            {
                if (!RogueGuards.TryForceRogueDamageClass(item))
                    RogueGuards.RestoreOriginalDamageClass(item);
            }
            else
            {
                RogueGuards.RestoreOriginalDamageClass(item);
            }
        }
    }
}
