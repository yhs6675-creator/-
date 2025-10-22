using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using CalamityLunarVeilCompat.Bridges;

namespace CLVCompat.Systems
{
    public sealed class RogueProjGlobal : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool FromRogueSwap { get; private set; }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            FromRogueSwap = ProjectileSnapshot.TryConsumeMark(source);
            if (!FromRogueSwap && source is EntitySource_Parent parent && parent.Entity is Projectile parentProj)
            {
                FromRogueSwap = parentProj.GetGlobalProjectile<RogueProjGlobal>()?.FromRogueSwap == true;
            }
            CompatDebug.LogSnapshot(projectile, FromRogueSwap);
            string sourceName = source switch
            {
                EntitySource_ItemUse_WithAmmo ammo => ammo.Item?.Name ?? "<null>",
                EntitySource_ItemUse use => use.Item?.Name ?? "<null>",
                _ => source?.ToString() ?? "<null>"
            };
            CompatDebug.LogInfo($"[DIAG] OnSpawn fromRogueSwap={FromRogueSwap}, source={sourceName}");
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            bool diagBypass = true;
            if (!FromRogueSwap && !diagBypass)
                return;

            Player owner = (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
                ? Main.player[projectile.owner]
                : null;

            float stealth = CalamityBridge.GetRogueStealthScalar(owner);
            CompatDebug.LogInfo($"[DIAG] Hit fromSwap={FromRogueSwap}, stealth={stealth:0.###}");
            if (stealth > 0f)
                modifiers.SourceDamage *= 1f + stealth;
        }
    }
}
