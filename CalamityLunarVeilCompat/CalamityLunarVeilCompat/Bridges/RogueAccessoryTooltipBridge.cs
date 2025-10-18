using System.Collections.Generic;
using System.Text.RegularExpressions;
using Terraria.ModLoader;
using Terraria;

namespace CalamityLunarVeilCompat.Bridges
{
    // Throwing / Thrown / 투척 -> Rogue / 로그
    public class RogueTooltipBridge : GlobalItem
    {
        private static readonly HashSet<string> AllowedMods = new()
        {
            "LunarVielmod",
            "Stellamod",
        };

        private static readonly Regex EnThrowing = new(@"\bThrowing\b", RegexOptions.IgnoreCase);
        private static readonly Regex EnThrown   = new(@"\bThrown\b",   RegexOptions.IgnoreCase);
        private static readonly Regex KrThrow    = new(@"투척",          RegexOptions.None);

        public override bool InstancePerEntity => false;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            var mi = item.ModItem;
            if (mi is null) return;
            if (!AllowedMods.Contains(mi.Mod?.Name ?? string.Empty)) return;

            for (int i = 0; i < tooltips.Count; i++)
            {
                var line = tooltips[i];
                if (string.IsNullOrEmpty(line?.Text)) continue;

                var text = EnThrowing.Replace(line.Text, "Rogue");
                text = EnThrown.Replace(text, "Rogue");
                text = KrThrow.Replace(text, "로그");

                line.Text = text;
            }
        }
    }
}
