using System.Collections.Generic;

namespace CLVCompat.Systems
{
    internal static class WhitelistSource
    {
        private static readonly string[] ThrowDisplayNames =
        {
            "Scatterbombs",
            "Zenovias Pikpik Jar",
            "Rogue Igniter Cards MKII",
            "Rogue Igniter Cards",
            "Rogue Cards",
            "Rogue Lgniter Cards",
            "Hyus",
            "Hyus (후야)",
            "Hyus(후야)",
            "Ivythorn Shuriken",
            "Larvein Spear",
            "Dirt Glove",
            "Plate",
            "Life Seeking Vial",
            "Lil' stinger",
            "Lil' Stinger",
            "Orion",
        };

        private static readonly string[] SwappedDisplayNames =
        {
            "Starring Balls",
            "Hookarama",
            "Molted Crust Balls",
            "Gladiator Spear",
            "Frost Monger",
            "Heartspire",
            "Sirius",
            "Voyager",
            "Holmberg Scythe",
            "Palm Tomahawks",
            "No Longer Bridget",
            "Bridget",
            "Pearlescent Ice Balls",
            "Kilvier",
            "The Irradiaspear",
            "Burning Angel",
            "Prismatic Cryadia Balls",
            "Poisoned Angel",
            "Hit me",
            "Vulcan Breaker",
        };

        internal static IEnumerable<string> GetDisplayNames()
        {
            foreach (var name in ThrowDisplayNames)
                yield return name;

            foreach (var name in SwappedDisplayNames)
                yield return name;
        }

        internal static IEnumerable<string> GetThrowDisplayNames() => ThrowDisplayNames;
        internal static IEnumerable<string> GetSwappedDisplayNames() => SwappedDisplayNames;
    }
}
