using UniRx;
using UnityEngine;

/// <summary>
/// 메인 퀘스트를 튜토리얼처럼 안내하는 판단부. 지금 퀘스트에 맞는 경로(TutorialGuideRoutes)를 고르고,
/// 그 경로 위에서 "지금 화면에 보이는 지점 중 가장 뒤 단계"를 가리키게 한다.
///
/// 단계를 세어 가며 전진시키지 않는 것이 핵심이다. 미션마다 경로 깊이가 다르고
/// (메인 버튼 하나로 끝나기도 하고, 팝업 -> 탭 -> 버튼까지 들어가기도 한다),
/// 유저는 안내와 상관없이 먼저 들어가거나 도중에 나가 버린다. 단계 번호를 들고 있으면 그 모든 경우를
/// 예외 처리해야 하지만, "보이는 것 중 가장 뒤"라는 한 가지 규칙이면 전부 저절로 맞는다.
///   - 유저가 스스로 팝업을 열었다  -> 그 안 지점이 보이므로 자동으로 다음 단계를 가리킨다.
///   - 뒤로 나갔다                 -> 안쪽 지점이 사라지므로 자동으로 앞 단계로 되돌아온다.
///   - 퀘스트가 서버에서 완료됐다   -> 보상 수령 지점을 가리킨다.
/// 그래서 새 퀘스트가 추가돼도 이 스크립트는 손대지 않는다(경로 에셋과 앵커 배치만 하면 된다).
///
/// 화면 깊이로 순서가 갈리지 않는 경우(탭 선택과 실행 버튼이 한 팝업에 같이 있는 경우)만 예외인데,
/// 그때는 앞 단계를 "관문"으로 지정해 통과 전까지 뒤 단계로 넘어가지 않게 한다(TryShowPendingGate).
///
/// 판단은 짧은 주기로 반복한다. 앵커 등록/해제만으로는 스크롤이나 버튼 잠금 해제처럼
/// "등록은 그대로인데 가리킬 수 있게 된" 변화를 잡지 못하기 때문이다.
/// </summary>
public sealed class TutorialGuideDirector : HudInjectedBehaviour {
    [Tooltip("유도 표시(가이드 프레임). 비우면 아무것도 하지 않는다.")]
    [SerializeField] private TutorialGuideView view;

    [Tooltip("목표별 경로표(SO). 비우면 유도하지 않는다.")]
    [SerializeField] private TutorialGuideRoutes routes;

    [Tooltip("다시 판단하는 주기(초). 짧을수록 반응이 빠르고 길수록 가볍다.")]
    [SerializeField] private float resolveInterval = 0.1f;

    private readonly CompositeDisposable _subs = new();
    private MainQuestDto _quest;
    private TutorialGuideRoutes.Route _route;
    private float _timer;

    protected override void OnEnable() {
        base.OnEnable();
        // 켜고 끄기를 반복해도 구독이 겹치지 않게 한다(주입은 활성화될 때마다 이 경로를 탄다).
        _timer = 0f;
    }

    protected override void OnInjected() {
        _subs.Clear();

        if (Ctx == null || Ctx.MainQuest == null) {
            if (view != null) view.Hide();
            return;
        }

        Ctx.MainQuest.Current.Subscribe(OnQuestChanged).AddTo(_subs);
        // 앵커가 늘거나 줄면(팝업 열림/닫힘) 주기를 기다리지 않고 바로 다시 본다.
        TutorialAnchorRegistry.Changed.Subscribe(_ => Resolve()).AddTo(_subs);
    }

    private void OnDisable() {
        _subs.Clear();
        if (view != null) view.Hide();
    }

    private void OnDestroy() => _subs.Dispose();

    private void OnQuestChanged(MainQuestDto quest) {
        _quest = quest;
        // 경로는 퀘스트가 바뀔 때만 고르면 된다(주기 판단에서 매번 찾지 않게).
        _route = quest != null && quest.HasQuest && routes != null
            ? routes.Find(quest.Objective, quest.Param)
            : null;
        Resolve();
    }

    private void LateUpdate() {
        _timer -= Time.unscaledDeltaTime;
        if (_timer > 0f) return;
        _timer = Mathf.Max(0.02f, resolveInterval);
        Resolve();
    }

    private void Resolve() {
        if (view == null) return;

        if (routes == null || _quest == null || !_quest.HasQuest || !routes.ShouldGuide(_quest.Level)) {
            view.Hide();
            return;
        }

        // 완료된 퀘스트는 다음 행동이 "보상 수령" 하나뿐이므로 경로를 보지 않는다.
        if (_quest.IsCompleted) {
            var completed = routes.CompletedStep;
            var claim = completed != null ? TutorialAnchorRegistry.Find(completed.anchor) : null;
            if (claim != null) Show(completed, claim);
            else view.Hide();
            return;
        }

        if (_route == null) {
            view.Hide();
            return;
        }

        // 뒤에서부터 찾는다 = 지금 도달해 있는 가장 깊은 지점.
        for (int i = _route.steps.Length - 1; i >= 0; i--) {
            var anchor = TutorialAnchorRegistry.Find(_route.steps[i].anchor);
            if (anchor == null) continue;

            // 다만 앞쪽에 아직 통과하지 못한 관문이 남아 있으면 그쪽이 먼저다.
            if (TryShowPendingGate(i)) return;

            Show(_route.steps[i], anchor);
            return;
        }
        view.Hide();
    }

    /// <summary>
    /// 지정한 단계보다 앞에 있는 관문 중 아직 통과하지 못한 첫 관문을 가리킨다(가리켰으면 true).
    ///
    /// 화면 깊이만으로는 순서를 정할 수 없는 경우를 위한 보정이다. 탭(선택)과 실행 버튼이 한 팝업에
    /// 나란히 있으면 팝업이 열리는 순간 둘 다 보여, 그냥 두면 탭을 건너뛰고 실행 버튼을 가리킨다.
    /// 관문으로 지정한 지점(TutorialAnchor.satisfiedWhenActive 배선)에서만 이 보정이 걸리므로,
    /// 유저가 스스로 팝업을 열어 앞 단계를 지나쳐 버린 경우의 자연스러운 동작은 그대로 남는다.
    /// </summary>
    private bool TryShowPendingGate(int before) {
        for (int j = 0; j < before; j++) {
            // 관문으로 배선된 앵커만, 가리킬 수 있는지와 무관하게 찾는다(FindGate).
            // 표시 조건으로 걸러 버리거나 조건 없는 동명 앵커에 밀리면 관문이 조용히 사라져 순서가 무너진다.
            var gate = TutorialAnchorRegistry.FindGate(_route.steps[j].anchor);
            if (gate == null || gate.IsSatisfied) continue;

            Show(_route.steps[j], gate);
            return true;
        }
        return false;
    }

    private void Show(TutorialGuideRoutes.Step step, TutorialAnchor anchor) =>
        view.Show(anchor, Hint(step), step.blockOutside);

    // 문구는 켠 단계에서만 띄운다(경로표의 showHint). 켰는데 문구를 비워 두었으면 퀘스트 목표 문구를
    // 그대로 쓴다(경로표에 같은 말을 두 번 적지 않게). 빈 문자열을 주면 표시 쪽이 문구 묶음을 끈다.
    private string Hint(TutorialGuideRoutes.Step step) {
        if (!step.showHint) return string.Empty;

        return !string.IsNullOrEmpty(step.hint)
            ? step.hint
            : MainQuestRules.Describe(_quest.Objective, _quest.Param, Ctx != null ? Ctx.DungeonDb : null);
    }
}
