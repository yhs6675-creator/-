using Terraria.ModLoader.Config;

namespace CalamityLunarVeilCompat
{
    public class CLV_DamageConfig : ModConfig
    {
        public static CLV_DamageConfig Instance;

        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Label("Lunar Veil 무기 데미지 배율")]
        [Range(0.1f, 10f)]
        public float LunarVeilDamageMultiplier { get; set; } = 2.0f;

        [Label("마스터 난이도 추가 배율 활성화")]
        public bool EnableMasterScaling { get; set; } = false;

        [Label("마스터 난이도 추가 배율")]
        [Range(1.0f, 3.0f)]
        public float MasterModeExtraMultiplier { get; set; } = 1.1f;

        [Label("Lunar Veil 방어구 방어력 배율")]
        [Tooltip("루나베일 방어구 장착 시 추가되는 방어력 배율. 예: 1.5 = 50% 증가")]
        [Range(1.0f, 3.0f)]
        public float ArmorDefenseMultiplier { get; set; } = 1.5f;

        [Label("Lunar Veil 방어구 방어력 보정 활성화")]
        public bool EnableArmorDefenseBoost { get; set; } = true;

        public override void OnLoaded()
        {
            Instance = this;
        }

        public override void OnUnloaded()
        {
            Instance = null;
        }
    }
}
