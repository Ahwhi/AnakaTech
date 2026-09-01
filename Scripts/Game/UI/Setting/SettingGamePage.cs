using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임(표시/연출) 분류 페이지. 전투 화면을 얼마나 요란하게 볼지 정하는 on/off 항목을 모은다.
/// 각 값은 표시하는 쪽(BattleStage/TouchVfxLayer)이 매번 읽으므로 저장만 하면 즉시 반영된다.
/// </summary>
public sealed class SettingGamePage : SettingPage {
    [Tooltip("전투 중 스킬 사용 말풍선(스킬명 + !) 표시 토글.")]
    [SerializeField] private Toggle skillBubbleToggle;
    [Tooltip("전투 중 피해량 숫자 표시 토글.")]
    [SerializeField] private Toggle damageNumberToggle;
    [Tooltip("대표 캐릭터 타격 시 카메라 흔들림 토글.")]
    [SerializeField] private Toggle cameraShakeToggle;
    [Tooltip("화면을 터치한 자리에 뜨는 피드백 이펙트 표시 토글.")]
    [SerializeField] private Toggle touchVfxToggle;

    protected override void OnInitialize() {
        var data = UserDataManager.Data;
        if (data == null) return;

        BindToggle(skillBubbleToggle, data.ShowSkillBubble, value => data.ShowSkillBubble = value);
        BindToggle(damageNumberToggle, data.ShowDamageNumber, value => data.ShowDamageNumber = value);
        BindToggle(cameraShakeToggle, data.UseCameraShake, value => data.UseCameraShake = value);
        BindToggle(touchVfxToggle, data.ShowTouchVfx, value => data.ShowTouchVfx = value);
    }
}
