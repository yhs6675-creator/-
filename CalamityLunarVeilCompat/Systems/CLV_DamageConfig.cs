using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace CalamityLunarVeilCompat
{
    public class CLV_DamageConfig : ModConfig
    {
        public static CLV_DamageConfig Instance;

        public override ConfigScope Mode => ConfigScope.ServerSide;

        public override void OnLoaded() => Instance = this;

        public override void OnChanged()
        {
            if (Instance != this)
            {
                Instance = this;
            }
        }

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

        [Label("CLV: 무기/방어구 적용 툴팁 표시")]
        [Tooltip("루나베일 무기/방어구에 적용된 배율/증가치를 툴팁으로 보여줍니다.")]
        public bool ShowCompatTooltips { get; set; } = true;

        [Label("CLV: 디버그 툴팁(계산근거) 표시")]
        [Tooltip("계산값(배율, baseDef, add 등)을 디버그용으로 함께 표시합니다.")]
        public bool ShowDebugTooltips { get; set; } = false;
    }
}
