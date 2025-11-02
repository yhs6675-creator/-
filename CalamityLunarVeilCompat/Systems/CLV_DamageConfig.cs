using System.ComponentModel;
using Terraria.ModLoader.Config;
using Terraria.ModLoader;

namespace CalamityLunarVeilCompat
{
    public enum TooltipLanguageMode
    {
        Auto,
        Korean,
        English,
    }

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

        [Label("CLV: 툴팁 언어")]
        [Tooltip("Auto: 게임 언어에 맞춤 / Korean: 한글만 / English: 영어만")]
        [DefaultValue(TooltipLanguageMode.Auto)]
        public TooltipLanguageMode TooltipLanguage { get; set; } = TooltipLanguageMode.Auto;

        [Label("CLV: 적용 툴팁 표시")]
        [Tooltip("루나베일 무기/방어구에 적용된 배율/증가치를 툴팁으로 보여줍니다.")]
        [DefaultValue(true)]
        public bool ShowCompatTooltips { get; set; } = true;

        [Label("CLV: 디버그 툴팁 표시")]
        [DefaultValue(false)]
        public bool ShowDebugTooltips { get; set; } = false;

        [Label("Lunar Veil 무기 데미지 배율")]
        [Tooltip("예: 2.0 = 2배. 숫자를 직접 입력할 수 있습니다.")]
        [Range(0.1f, 10f)]
        [DefaultValue(2.0f)]
        public float LunarVeilDamageMultiplier { get; set; } = 2.0f;

        [Label("마스터 난이도 추가 배율 활성화")]
        [DefaultValue(false)]
        public bool EnableMasterScaling { get; set; } = false;

        [Label("마스터 난이도 추가 배율")]
        [Range(1.0f, 3.0f)]
        [DefaultValue(1.1f)]
        public float MasterModeExtraMultiplier { get; set; } = 1.1f;

        [Label("Lunar Veil 방어구 방어력 배율")]
        [Tooltip("예: 1.5 = +50%. 숫자를 직접 입력할 수 있습니다.")]
        [Range(1.0f, 3.0f)]
        [DefaultValue(1.5f)]
        public float ArmorDefenseMultiplier { get; set; } = 1.5f;

        [Label("Lunar Veil 방어구 방어력 보정 활성화")]
        [DefaultValue(true)]
        public bool EnableArmorDefenseBoost { get; set; } = true;
    }
}
