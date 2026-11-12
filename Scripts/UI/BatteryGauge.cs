using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기기 배터리 잔량 표시(프레임 + 슬라이더 + 퍼센트 + 충전 중 표시). 어느 화면에든 붙여 쓸 수 있는 독립 뷰다.
/// 프레임, 슬라이더 Fill, 퍼센트 텍스트는 잔량 단계(levelColors)에 따라 같은 색으로 바뀐다.
///
/// 배터리 값은 엔진 API(SystemInfo)로 기기 안에서만 읽어 화면에 그린다. 저장하거나 서버/분석 도구로 보내지 않는다.
/// 이 값을 기기 밖으로 내보내면 스토어 개인정보 신고(Google Play 데이터 보안, App Store 개인정보 라벨) 대상이 되므로
/// 이 뷰 밖으로 값을 노출하지 않는다.
///
/// 배터리를 읽을 수 없는 환경(에디터/PC 등, 값이 음수)에서는 표시 영역 전체를 숨긴다.
/// 잔량은 천천히 변하므로 일정 간격으로만 다시 읽는다(매 프레임 읽지 않는다).
/// </summary>
public sealed class BatteryGauge : MonoBehaviour {
    [Tooltip("잔량 슬라이더. 0~1 범위로 채운다(상호작용은 꺼 둘 것).")]
    [SerializeField] private Slider slider;
    [Tooltip("잔량 퍼센트 표시(선택).")]
    [SerializeField] private TMP_Text percentText;
    [Tooltip("충전 중일 때만 켜지는 표시(선택). 번개 아이콘 등.")]
    [SerializeField] private GameObject chargingIndicator;
    [Tooltip("배터리를 읽을 수 없을 때 숨길 루트(이 오브젝트의 자식). 비워 두면 슬라이더/퍼센트만 숨긴다.")]
    [SerializeField] private GameObject root;
    [Tooltip("다시 읽는 간격(초).")]
    [SerializeField] private float refreshSeconds = 30f;

    [Header("Level Colors")]
    [Tooltip("잔량 단계에 따라 색이 바뀌는 배터리 프레임 이미지(선택).")]
    [SerializeField] private Graphic frame;
    [Tooltip("잔량 단계별 색. 잔량이 '최소 잔량' 이상인 단계 중 가장 높은 단계의 색을 쓴다. " +
             "프레임, 슬라이더 Fill, 퍼센트 텍스트에 함께 적용된다. 순서는 상관없다.")]
    [SerializeField] private LevelColor[] levelColors = {
        new() { minLevel = 0.5f, color = new Color(0.40f, 0.85f, 0.45f, 1f) },
        new() { minLevel = 0.2f, color = new Color(1.00f, 0.80f, 0.25f, 1f) },
        new() { minLevel = 0f,   color = new Color(1.00f, 0.35f, 0.30f, 1f) },
    };

    [Serializable]
    private struct LevelColor {
        [Range(0f, 1f)] public float minLevel;
        public Color color;
    }

#if UNITY_EDITOR
    // 에디터 테스트용 가짜 배터리. 데스크톱에는 배터리가 없어(값 -1) 표시가 숨겨지므로, 켜면 아래 값으로 대신 그린다.
    // 플레이 중 값을 바꾸면 바로 반영된다. 빌드에는 이 코드가 들어가지 않는다.
    [Header("Editor Simulation")]
    [Tooltip("에디터에서 아래 값을 실제 배터리 대신 사용한다.")]
    [SerializeField] private bool simulate;
    [SerializeField, Range(0f, 1f)] private float simulatedLevel = 0.5f;
    [SerializeField] private bool simulatedCharging;

    private void OnValidate() {
        if (Application.isPlaying && isActiveAndEnabled) Refresh();
    }
#endif

    private IDisposable _timer;
    private Graphic _fill;

    private void OnEnable() {
        Refresh();
        // 타임스케일과 무관하게 흘러야 하므로 실시간 스케줄러를 쓴다.
        _timer = Observable.Interval(TimeSpan.FromSeconds(Mathf.Max(1f, refreshSeconds)), Scheduler.MainThreadIgnoreTimeScale)
            .Subscribe(_ => Refresh());
    }

    private void OnDisable() {
        _timer?.Dispose();
        _timer = null;
    }

    private void Refresh() {
        float level = ReadLevel();
        bool available = level >= 0f;

        SetVisible(available);
        if (!available) return;

        if (slider != null) slider.SetValueWithoutNotify(level);
        if (percentText != null) percentText.text = $"{Mathf.RoundToInt(level * 100f)}%";
        ApplyLevelColor(level);

        if (chargingIndicator != null) chargingIndicator.SetActive(ReadCharging());
    }

    private float ReadLevel() {
#if UNITY_EDITOR
        if (simulate) return simulatedLevel;
#endif
        return SystemInfo.batteryLevel;
    }

    private bool ReadCharging() {
#if UNITY_EDITOR
        if (simulate) return simulatedCharging;
#endif
        var status = SystemInfo.batteryStatus;
        return status == BatteryStatus.Charging || status == BatteryStatus.Full;
    }

    // 잔량이 걸치는 단계의 색을 프레임/Fill/퍼센트에 함께 입힌다. 단계가 하나도 맞지 않으면 색을 건드리지 않는다.
    private void ApplyLevelColor(float level) {
        if (!TryGetLevelColor(level, out var color)) return;

        // Fill 은 슬라이더가 이미 알고 있으므로 따로 연결받지 않는다.
        if (_fill == null && slider != null && slider.fillRect != null) _fill = slider.fillRect.GetComponent<Graphic>();

        if (frame != null) frame.color = color;
        if (_fill != null) _fill.color = color;
        if (percentText != null) percentText.color = color;
    }

    private bool TryGetLevelColor(float level, out Color color) {
        color = default;
        float best = float.MinValue;
        bool found = false;
        if (levelColors == null) return false;

        foreach (var step in levelColors) {
            if (level < step.minLevel || step.minLevel <= best) continue;
            best = step.minLevel;
            color = step.color;
            found = true;
        }
        return found;
    }

    // 이 오브젝트 자체는 끄지 않는다(OnEnable 도중 자신을 끄면 엔진이 막는다). 루트가 없으면 표시 요소만 끈다.
    private void SetVisible(bool visible) {
        if (root != null && root != gameObject) {
            if (root.activeSelf != visible) root.SetActive(visible);
            return;
        }
        if (frame != null && frame.gameObject != gameObject) frame.gameObject.SetActive(visible);
        if (slider != null) slider.gameObject.SetActive(visible);
        if (percentText != null) percentText.gameObject.SetActive(visible);
        if (!visible && chargingIndicator != null) chargingIndicator.SetActive(false);
    }
}
