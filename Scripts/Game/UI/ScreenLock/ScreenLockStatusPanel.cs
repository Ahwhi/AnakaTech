using System;
using System.Text;
using TMPro;
using UniRx;
using UnityEngine;

/// <summary>
/// 절전(잠금) 화면의 방치 진행 현황. 현재 스테이지, 남은 기력, 기력 부스트 사용 여부, 남은 부스트권,
/// 이번 절전 동안 쓴 기력, 절전 지속 시간을 텍스트 하나에 한 줄씩 보여 준다.
/// 각 줄은 "라벨: 값" 형태이며 라벨과 값의 색을 따로 정한다(TMP 리치 텍스트).
/// 모든 값은 클라가 이미 들고 있는 상태를 구독할 뿐 서버에 따로 묻지 않는다.
///
/// 집계 기준은 "이 패널이 초기화된 순간(= 절전 시작)"이다. 패널은 잠금 화면과 함께 만들어지고 사라지므로
/// 절전을 다시 켜면 처음부터 다시 센다.
/// </summary>
public sealed class ScreenLockStatusPanel : MonoBehaviour {
    [Tooltip("현황 전체를 표시할 텍스트. Rich Text 가 켜져 있어야 한다.")]
    [SerializeField] private TMP_Text statusText;

    [Header("Colors")]
    [SerializeField] private Color labelColor = new(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color valueColor = Color.white;
    [Tooltip("기력 부스트가 켜져 있을 때 값 색. 꺼져 있으면 기본 값 색을 쓴다.")]
    [SerializeField] private Color boostOnColor = new(0.40f, 0.85f, 0.45f, 1f);
    [Tooltip("남은 기력이 아래 어느 단계에도 걸리지 않을 때(넉넉할 때) 값 색.")]
    [SerializeField] private Color staminaHighColor = new(0.40f, 0.85f, 0.45f, 1f);
    [Tooltip("남은 기력 단계별 색. 기력이 '이하 기준' 이하인 단계 중 기준이 가장 낮은 단계의 색을 쓴다. 순서는 상관없다.")]
    [SerializeField] private StaminaColor[] staminaColors = {
        new() { atOrBelow = 180, color = new Color(1.00f, 0.80f, 0.25f, 1f) },
        new() { atOrBelow = 60,  color = new Color(1.00f, 0.35f, 0.30f, 1f) },
    };

    [Serializable]
    private struct StaminaColor {
        public int atOrBelow;
        public Color color;
    }

    [Header("Labels")]
    [SerializeField] private string stageLabel = "현재 스테이지";
    [SerializeField] private string staminaLabel = "남은 기력";
    [SerializeField] private string boostLabel = "기력 부스트";
    [SerializeField] private string boostTicketLabel = "남은 부스트권";
    [SerializeField] private string spentStaminaLabel = "소모한 기력";
    [SerializeField] private string elapsedLabel = "절전 시간";
    [SerializeField] private string boostOnText = "ON";
    [SerializeField] private string boostOffText = "OFF";

    private readonly StringBuilder _sb = new();
    private string _labelHex;
    private string _valueHex;

    private DateTime _startedUtc;
    private string _stage = "";
    private int _stamina;
    private int _staminaMax;
    private bool _boostOn;
    private int _boostTickets;
    private int _spentStamina;

    public void Initialize(MainHudContext ctx) {
        // 앱이 잠깐 멈췄다 돌아와도 실제로 흐른 시간이 나오도록 벽시계 기준으로 잰다.
        _startedUtc = DateTime.UtcNow;
        _spentStamina = 0;
        _labelHex = ColorUtility.ToHtmlStringRGBA(labelColor);
        _valueHex = ColorUtility.ToHtmlStringRGBA(valueColor);

        var store = ctx.Store;

        store.CurrentDungeonIndex.Subscribe(index => {
            var dungeon = ctx.DungeonDb.Get(index);
            _stage = dungeon != null ? dungeon.DisplayName : $"#{index}";
            Render();
        }).AddTo(this);

        store.Stamina.Subscribe(value => {
            _stamina = value;
            _staminaMax = store.StaminaMax;
            Render();
        }).AddTo(this);

        ctx.Battle.BoostMode.Subscribe(on => {
            _boostOn = on;
            Render();
        }).AddTo(this);

        store.StaminaBoostTicket.Subscribe(count => {
            _boostTickets = count;
            Render();
        }).AddTo(this);

        // 쓴 기력은 클리어 정산마다 서버 확정 결과로 센다(기력 증감만 보면 자연 충전분이 섞인다).
        Observable.FromEvent<DungeonRewardDto>(h => ctx.Battle.RewardReceived += h,
                                               h => ctx.Battle.RewardReceived -= h)
            .Subscribe(reward => {
                _spentStamina += BoostRules.StaminaSpent(reward.ClearCount, reward.StaminaShort);
                Render();
            }).AddTo(this);

        // 지속 시간은 분 단위로 보이지만, 분이 넘어가는 순간을 놓치지 않도록 1초마다 다시 그린다.
        Observable.Interval(TimeSpan.FromSeconds(1), Scheduler.MainThreadIgnoreTimeScale)
            .Subscribe(_ => Render())
            .AddTo(this);
    }

    private void Render() {
        if (statusText == null || _labelHex == null) return;

        _sb.Clear();
        AppendLine(stageLabel, _stage);
        AppendLine(staminaLabel, _staminaMax > 0 ? $"{_stamina} / {_staminaMax}" : $"{_stamina}",
                   ColorUtility.ToHtmlStringRGBA(StaminaValueColor(_stamina)));
        AppendLine(boostLabel, _boostOn ? boostOnText : boostOffText,
                   _boostOn ? ColorUtility.ToHtmlStringRGBA(boostOnColor) : null);
        AppendLine(boostTicketLabel, $"{_boostTickets}");
        AppendLine(spentStaminaLabel, $"{_spentStamina}");
        AppendLine(elapsedLabel, KstTime.Elapsed(DateTime.UtcNow - _startedUtc));
        statusText.text = _sb.ToString();
    }

    // 남은 기력이 걸치는 단계의 색. 어느 단계에도 걸리지 않으면(넉넉하면) staminaHighColor.
    private Color StaminaValueColor(int stamina) {
        var color = staminaHighColor;
        int best = int.MaxValue;
        if (staminaColors == null) return color;

        foreach (var step in staminaColors) {
            if (stamina > step.atOrBelow || step.atOrBelow >= best) continue;
            best = step.atOrBelow;
            color = step.color;
        }
        return color;
    }

    // 라벨이 비어 있으면 그 줄은 표시하지 않는다(프리팹에서 항목을 빼고 싶을 때 라벨만 지우면 된다).
    // 값 색을 따로 주지 않으면(null) 기본 값 색을 쓴다.
    private void AppendLine(string label, string value, string valueHex = null) {
        if (string.IsNullOrEmpty(label)) return;
        if (_sb.Length > 0) _sb.Append('\n');
        _sb.Append("<color=#").Append(_labelHex).Append('>').Append(label).Append(": </color>")
           .Append("<color=#").Append(valueHex ?? _valueHex).Append('>').Append(value).Append("</color>");
    }
}
