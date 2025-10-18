using Terraria;

namespace CalamityLunarVeilCompat {
    public static class EquipHelpers {
        public static bool HasItemEquipped(this Player p, string modName, string internalName) {
            for (int i = 3; i <= 9 && i < p.armor.Length; i++) {
                var mi = p.armor[i]?.ModItem;
                if (mi != null && mi.Mod?.Name == modName && mi.Name == internalName) return true;
            }
            return false;
        }
    }
}
