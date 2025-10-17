// Systems/LVThrowToRogueCompat.cs
// 등록된 타입(오픈소스 투척/저글러/강제 예외)에만 RogueDamageClass 적용.
// 사용 흐름(우클릭/채널/발사)은 건드리지 않음.

using Terraria;
using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    public class LVThrowToRogueCompat : GlobalItem
    {
        public override bool InstancePerEntity => false;

        public override void SetDefaults(Item item)
        {
            if (!LVRogueRegistry.IsRegistered(item.type))
                return;

            if (ModContent.TryFind<DamageClass>("CalamityMod/RogueDamageClass", out var rogue))
            {
                item.DamageType = rogue;
            }
        }
    }
}
