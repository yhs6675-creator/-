using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat
{
    public class CLV_DebugCanary : GlobalItem
    {
        public override bool InstancePerEntity => false;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            var cfg = CLV_DamageConfig.Instance ?? ModContent.GetInstance<CLV_DamageConfig>();
            if (cfg == null || !cfg.ShowCompatTooltips)
            {
                return;
            }

            tooltips.Add(new TooltipLine(Mod, "CLV_CANARY", "[CLV] CANARY: hooks alive"));
        }
    }
}
