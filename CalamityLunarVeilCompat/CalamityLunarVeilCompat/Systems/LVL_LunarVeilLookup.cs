using Terraria.ModLoader;

namespace CalamityLunarVeilCompat.Systems
{
    public static class LVL_LunarVeilLookup
    {
        public static int Type_AssassinsShuriken = -1;
        public static int Type_CleanestCleaver   = -1;

        // 루나베일 레거시 모드 이름 후보
        private static readonly string[] ModCandidates = new[]
        {
            "Stellamod", "LunarVeilLegacy", "LunarVeil", "LunarVeilLegacyMod"
        };

        public static void Init()
        {
            // 이미 초기화됨
            if (Type_AssassinsShuriken != -1 && Type_CleanestCleaver != -1) return;

            foreach (var modName in ModCandidates)
            {
                if (!ModLoader.TryGetMod(modName, out var m)) continue;

                if (Type_AssassinsShuriken == -1 && m.TryFind<ModItem>("AssassinsShuriken", out var assn))
                    Type_AssassinsShuriken = assn.Type;

                if (Type_CleanestCleaver == -1 && m.TryFind<ModItem>("CleanestCleaver", out var cleaver))
                    Type_CleanestCleaver = cleaver.Type;
            }
        }

        public static bool IsAssassinsShuriken(int itemType)
            => itemType == Type_AssassinsShuriken && itemType != -1;

        public static bool IsCleanestCleaver(int itemType)
            => itemType == Type_CleanestCleaver && itemType != -1;
    }
}
