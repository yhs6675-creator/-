using Terraria.ModLoader;

namespace CLVCompat.Systems
{
    public sealed class WhitelistBootstrap : ModSystem
    {
        public override void PostSetupContent()
        {
            WhitelistIndex.BuildIndex();
            WhitelistIndex.ApplyWhitelist(WhitelistSource.GetDisplayNames());
            ProblemWeaponRegistry.Initialize();
        }
    }
}
