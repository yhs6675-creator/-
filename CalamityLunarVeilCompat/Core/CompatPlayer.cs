// File: Core/CompatPlayer.cs
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat {
    public class CompatPlayer : ModPlayer {
        public bool  LunarVeilSet_GrantsStealth;

        public float StealthMaxBase = 100f;
        public float Stealth;
        public float StealthRegenPerSecBase = 6f;
        public float StealthConsumeOnUse = 25f;

        public float LV_MaxBonus;
        public float LV_RegenMult = 1f;
        public float LV_ConsumeMult = 1f;
        public float LV_FullStealthDMG;

        public override void ResetEffects() {
            LunarVeilSet_GrantsStealth = false;
            LV_MaxBonus = 0f;
            LV_RegenMult = 1f;
            LV_ConsumeMult = 1f;
            LV_FullStealthDMG = 0f;
        }

        bool IsHoldingRogue() => RogueCache.IsRogue(Player.HeldItem);
        public bool CanUseStealth() =>
            RogueCache.CalamityPresent && (IsHoldingRogue() || LunarVeilSet_GrantsStealth);

        public override void PostUpdate() {
            if (CanUseStealth()) {
                float max = StealthMaxBase + LV_MaxBonus;
                float regenPerTick = (StealthRegenPerSecBase * LV_RegenMult) * (1f / 60f);
                Stealth = Utils.Clamp(Stealth + regenPerTick, 0f, max);
            } else {
                Stealth = System.MathF.Min(Stealth, StealthMaxBase);
            }
        }

        public bool TryConsumeStealth() {
            if (!CanUseStealth()) return true;
            float need = StealthConsumeOnUse * LV_ConsumeMult;
            if (Stealth >= need) { Stealth -= need; return true; }
            return false;
        }
    }
}
