using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat
{
    public class CLV_LunarVeilGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => false;

        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (!TryGetConfig(out var config))
                return;

            if (IsWeapon(item) && IsLunarVeilWeapon(item))
            {
                float multiplier = config.LunarVeilDamageMultiplier;
                if (config.EnableMasterScaling && Main.masterMode)
                    multiplier *= config.MasterModeExtraMultiplier;

                damage *= multiplier;
            }
        }

        public override void UpdateEquip(Item item, Player player)
        {
            if (!TryGetConfig(out var config))
                return;

            if (!config.EnableArmorDefenseBoost)
                return;

            if (!IsLunarVeilArmor(item))
                return;

            float mult = Math.Max(1f, config.ArmorDefenseMultiplier);
            int baseDefense = Math.Max(0, item.defense);
            int bonus = (int)Math.Floor(baseDefense * (mult - 1f) + 0.0001f);
            if (bonus > 0)
                player.statDefense += bonus;
        }

        private static bool TryGetConfig(out CLV_DamageConfig config)
        {
            config = CLV_DamageConfig.Instance ?? ModContent.GetInstance<CLV_DamageConfig>();
            return config != null;
        }

        private static bool IsWeapon(Item item)
        {
            if (item?.ModItem == null)
                return false;
            if (item.damage <= 0)
                return false;
            if (item.pick > 0 || item.axe > 0 || item.hammer > 0)
                return false;
            return item.DamageType != DamageClass.Default;
        }

        private static bool IsLunarVeilWeapon(Item item)
        {
            if (item?.ModItem?.Mod == null)
                return false;

            string modName = item.ModItem.Mod.Name;
            if (string.IsNullOrEmpty(modName))
                return false;

            return IsLunarVeilFamily(modName);
        }

        private static bool IsLunarVeilArmor(Item item)
        {
            if (item?.ModItem?.Mod == null)
                return false;

            bool isArmorSlot = item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0;
            if (!isArmorSlot)
                return false;

            string modName = item.ModItem.Mod.Name;
            return !string.IsNullOrEmpty(modName) && IsLunarVeilFamily(modName);
        }

        private static bool IsLunarVeilFamily(string modName)
        {
            if (string.IsNullOrEmpty(modName))
                return false;

            return modName == "Stellamod"
                || modName == "LunarVeilLegacy"
                || modName == "LunarVeil"
                || modName == "LunarVeilLegacyMod"
                || modName.StartsWith("LunarVeil", StringComparison.Ordinal);
        }
    }
}
