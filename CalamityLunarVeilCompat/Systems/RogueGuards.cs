using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    /// <summary>
    /// 공용 가드/헬퍼: 루나베일 투척 여부 판정과 RogueDamageClass 조회.
    /// </summary>
    internal static class RogueGuards
    {
        private static readonly string[] LunarVeilModIds =
        {
            "LunarVeilMod",
            "LunarVeilLegacy",
            "LunarVielmod",
            "LunarViel"
        };

        private static readonly HashSet<string> LunarVeilModIdSet = new(StringComparer.OrdinalIgnoreCase);

        static RogueGuards()
        {
            foreach (var id in LunarVeilModIds)
                LunarVeilModIdSet.Add(id);
        }

        internal static bool IsFromLunarVeil(Item item)
        {
            var modName = item?.ModItem?.Mod?.Name;
            return modName != null && LunarVeilModIdSet.Contains(modName);
        }

        internal static IEnumerable<string> EnumerateLunarVeilModIds() => LunarVeilModIds;

        internal static bool AnyLunarVeilLoaded()
        {
            foreach (var id in LunarVeilModIds)
            {
                if (ModLoader.HasMod(id))
                    return true;
            }

            return false;
        }

        internal static bool TryGetCalamityRogue(out DamageClass rogue)
        {
            return ModContent.TryFind("CalamityMod/RogueDamageClass", out rogue);
        }

        internal static bool TryGetLVThrowDamageClass(out DamageClass lvThrow)
        {
            foreach (var id in LunarVeilModIds)
            {
                if (ModContent.TryFind($"{id}/ThrowingDamageClass", out lvThrow))
                    return true;

                if (ModContent.TryFind($"{id}/LVThrowingDamageClass", out lvThrow))
                    return true;
            }

            lvThrow = null;
            return false;
        }

        /// <summary>
        /// 현재 아이템이 루나베일 기준 '투척' 상태인지 확인.
        /// 확정 신호(스왑 API 또는 ThrowingDamageClass 카운트)만 사용.
        /// </summary>
        internal static bool TryGetCurrentThrowState(Item item, out bool isThrow)
        {
            if (TryAskIsClassSwappedToThrow(item, out var callResult))
            {
                isThrow = callResult;
                return true;
            }

            if (TryGetLVThrowDamageClass(out var lvThrow) && item.CountsAsClass(lvThrow))
            {
                isThrow = true;
                return true;
            }

            isThrow = false;
            return false;
        }

        /// <summary>
        /// 스왑 확정 신호만으로 '투척' 판정을 얻는다.
        /// </summary>
        private static bool TryAskIsClassSwappedToThrow(Item item, out bool isThrow)
        {
            isThrow = false;

            foreach (var id in LunarVeilModIds)
            {
                if (!ModLoader.TryGetMod(id, out var lv))
                    continue;

                object ret = null;

                try
                {
                    ret = lv.Call("IsClassSwappedToThrow", item?.type ?? 0);
                }
                catch
                {
                }

                if (ret is bool b1)
                {
                    isThrow = b1;
                    return true;
                }

                try
                {
                    ret = lv.Call("IsClassSwappedToThrow", item);
                }
                catch
                {
                }

                if (ret is bool b2)
                {
                    isThrow = b2;
                    return true;
                }
            }

            return false;
        }
    }
}
