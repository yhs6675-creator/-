using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    internal static class CompatDebug
    {
        public static bool LogWhitelist { get; set; }
        public static bool LogSwap { get; set; }
        public static bool LogRogue { get; set; }
        public static bool LogStealthEvents { get; set; }
        public static bool LogSnapshot { get; set; }

        private static CalamityLunarVeilCompat ModInstance => ModContent.GetInstance<CalamityLunarVeilCompat>();

        public static void LogWhitelistEntry(Item item, string normalized, bool whitelisted)
        {
            if (!LogWhitelist)
                return;

            string itemName = item?.Name ?? "<null>";
            string baseName = item != null ? Lang.GetItemNameValue(item.type) : string.Empty;
            ModInstance.Logger.Info($"[WL] item={itemName}/{baseName}, norm={normalized}, whitelisted={(whitelisted ? "yes" : "no")}");
        }

        public static void LogSwapGate(Item item, bool whitelisted, bool swap, bool throwState, bool enterRogue)
        {
            if (!LogSwap)
                return;

            string name = item?.Name ?? "<null>";
            ModInstance.Logger.Info($"[SwapGate] item={name}, whitelisted={whitelisted}, swap={swap}, throwState={throwState}, enterRogue={enterRogue}");
        }

        public static void LogRogueEntry(Item item, bool swapped, float stealth, float consumed)
        {
            if (!LogRogue)
                return;

            string name = item?.Name ?? "<null>";
            ModInstance.Logger.Info($"[Rogue] item={name}, swapped={swapped}, stealth={stealth:0.###}, consumed={consumed:0.###}");
        }

        public static void LogStealth(bool fired, float consumed, float stealthNow)
        {
            if (!LogStealthEvents)
                return;

            ModInstance.Logger.Info($"[Stealth] fired={fired}, consumed={consumed:0.###}, stealthNow={stealthNow:0.###}");
        }

        public static void LogSnapshot(Projectile projectile, bool fromRogueSwap)
        {
            if (!LogSnapshot)
                return;

            int type = projectile != null ? projectile.type : -1;
            string name = projectile?.Name ?? "<null>";
            ModInstance.Logger.Info($"[Snapshot] proj={type}/{name}, fromRogueSwap={fromRogueSwap}");
        }
    }
}
