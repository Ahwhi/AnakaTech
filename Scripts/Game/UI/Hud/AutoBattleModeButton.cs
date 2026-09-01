using DG.Tweening;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 자동 전투 모드 토글 버튼(메인 HUD). 누르면 팝업 없이 사냥 <-> 도전 모드가 바로 바뀐다.
///
/// 도전 모드일 때만 아이콘이 계속 회전해 "지금 위로 올라가는 중"임을 한눈에 보여준다.
/// 사냥 모드에서는 회전을 멈추고 각도를 0으로 되돌린다.
///
/// 자가 주입(HudInjectedBehaviour)이라 컨트롤러가 이 버튼을 참조/관리할 필요가 없다.
/// 모드의 단일 소스는 BattleDirector.Mode 이며, 이 버튼은 그 값을 구독해 표시만 맞춘다
/// (전멸로 서버 진행이 사냥으로 되돌아가도 버튼이 저절로 따라온다).
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class AutoBattleModeButton : HudInjectedBehaviour {
    [SerializeField] private Button button;
    [Tooltip("도전 모드일 때 회전시킬 아이콘. 비우면 이 오브젝트의 RectTransform 을 돌린다.")]
    [SerializeField] private RectTransform spinIcon;
    [Tooltip("아이콘이 한 바퀴 도는 데 걸리는 시간(초). 작을수록 빠르게 돈다.")]
    [SerializeField] private float spinSeconds = 2f;
    [Tooltip("선택. 모드 이름 표시(사냥/도전).")]
    [SerializeField] private TMP_Text label;
    [Tooltip("선택. 사냥 모드에서만 켤 오브젝트(아이콘/배경 등).")]
    [SerializeField] private GameObject huntOnlyRoot;
    [Tooltip("선택. 도전 모드에서만 켤 오브젝트.")]
    [SerializeField] private GameObject challengeOnlyRoot;

    private Tween _spin;
    private CompositeDisposable _subs;
    private bool _wired;

    private RectTransform SpinTarget => spinIcon != null ? spinIcon : (RectTransform)transform;

    protected override void OnEnable() {
        base.OnEnable();   // 아직 주입 전이면 씬 스코프에서 Ctx 를 채운다.

        if (button == null) button = GetComponent<Button>();
        if (!_wired && button != null) {
            button.onClick.AddListener(Toggle);
            _wired = true;
        }

        if (Ctx == null || Ctx.Battle == null) return;
        _subs?.Dispose();
        _subs = new CompositeDisposable();
        Ctx.Battle.Mode.Subscribe(Apply).AddTo(_subs);
    }

    private void OnDisable() {
        _subs?.Dispose();
        _subs = null;
        StopSpin();
    }

    private void Toggle() {
        if (Ctx == null || Ctx.Battle == null) return;
        Ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        Ctx.Battle.Mode.Value = Ctx.Battle.Mode.Value == AutoBattleMode.Challenge
            ? AutoBattleMode.Hunt
            : AutoBattleMode.Challenge;

        // 모드는 클라 안에서만 바뀌는 값이라 서버가 관측할 수 없다. 메인 퀘스트가 "모드를 바꿔 봤다"를
        // 알 수 있도록 여기서 알린다(어느 방향으로 바꿨는지는 따지지 않는다. 목적은 기능을 만나게 하는 것이다).
        Ctx.MainQuest?.ReportAction(MainQuestObjective.AutoBattleToggle);
    }

    private void Apply(AutoBattleMode mode) {
        bool challenge = mode == AutoBattleMode.Challenge;

        if (label != null) label.text = challenge ? "도전" : "사냥";
        if (huntOnlyRoot != null) huntOnlyRoot.SetActive(!challenge);
        if (challengeOnlyRoot != null) challengeOnlyRoot.SetActive(challenge);

        if (challenge) StartSpin();
        else StopSpin();
    }

    // 무한 회전. 전투가 멈춘 상태(Time.timeScale=0)에서도 계속 돌도록 SetUpdate(true) 를 쓴다.
    private void StartSpin() {
        if (_spin != null) return;
        float duration = Mathf.Max(0.05f, spinSeconds);
        _spin = SpinTarget
            .DOLocalRotate(new Vector3(0f, 0f, -360f), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void StopSpin() {
        _spin?.Kill();
        _spin = null;
        SpinTarget.localEulerAngles = Vector3.zero;
    }
}
