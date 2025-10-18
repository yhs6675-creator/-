// File: Core/RogueCache.cs
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat {
    internal static class RogueCache {
        private static bool _checked;
        private static bool _calamityPresent;
        private static DamageClass _rogue; // CalamityMod.DamageClasses.RogueDamageClass

        public static bool CalamityPresent {
            get { if (!_checked) Check(); return _calamityPresent; }
        }

        public static DamageClass Rogue {
            get { if (!_checked) Check(); return _rogue ?? DamageClass.Generic; }
        }

        private static void Check() {
            _checked = true;
            _rogue = null;
            _calamityPresent = ModLoader.TryGetMod("CalamityMod", out _);
            if (!_calamityPresent)
                return;

            if (ModContent.TryFind("CalamityMod/RogueDamageClass", out DamageClass found))
                _rogue = found;
        }

        public static bool IsRogue(Item item) {
            if (!CalamityPresent || item is null)
                return false;

            var rogue = Rogue;
            if (rogue == null || rogue == DamageClass.Generic)
                return false;

            if (item.DamageType == rogue)
                return true;

            return item.CountsAsClass(rogue);
        }
    }
}
