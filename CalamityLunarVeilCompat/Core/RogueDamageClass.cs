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
            _calamityPresent = ModLoader.TryGetMod("CalamityMod", out var cal);
            if (!_calamityPresent || cal?.Code == null) return;

            var t = cal.Code.GetType("CalamityMod.DamageClasses.RogueDamageClass");
            if (t != null) {
                try { _rogue = (DamageClass)System.Activator.CreateInstance(t); }
                catch { _rogue = null; }
            }
        }

        public static bool IsRogue(Item item) =>
            CalamityPresent && item?.DamageType != null &&
            item.DamageType.GetType().Name.Contains("RogueDamageClass");
    }
}
