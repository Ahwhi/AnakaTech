using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using static Enums;

/// <summary>
/// 증강 선택 창. 페이즈가 시작될 때마다(1페이즈 포함) 열리며, 고를 때까지 닫히지 않는다.
/// 고른 증강 종류와 수치를 await 로 돌려준다.
///
/// 규칙이 두 가지 있다.
///   1) 같은 창에 같은 종류가 두 번 나오지 않는다. 이미 고른 종류는 다음 창에 다시 나올 수 있다.
///   2) 새로고침은 칸마다 1회씩이다. 다시 뽑을 때도 그 창에 이미 떠 있는 종류는 피한다.
///
/// 이 창이 떠 있는 동안 전투는 멈춰 있어야 한다. 멈추는 일은 부르는 쪽(DoomSceneManager)이 하며,
/// 무대의 SetPaused 를 쓰면 유닛뿐 아니라 투자 스쿼드 재생 같은 무대 처리까지 함께 멈춘다.
/// </summary>
public sealed class DoomAugmentSelectPopup : UIView {
    /// <summary>고른 결과.</summary>
    public readonly struct Pick {
        public readonly DoomAugmentKind Kind;
        public readonly float Value;
        public Pick(DoomAugmentKind kind, float value) { Kind = kind; Value = value; }
    }

    [Header("Cards")]
    [Tooltip("증강 카드 3장. 배열 길이가 곧 선택지 수이며, DoomAugmentRules.ChoiceCount 와 맞춰 둔다.")]
    [SerializeField] private DoomAugmentCard[] cards;

    [Header("Header")]
    [Tooltip("선택. 몇 페이즈를 시작하는지 알리는 문구.")]
    [SerializeField] private TMP_Text phaseText;
    [Tooltip("선택. 안내 문구.")]
    [SerializeField] private TMP_Text guideText;

    private BattleHudContext _ctx;
    private UniTaskCompletionSource<Pick> _result;
    private DoomAugmentState _state;
    private System.Random _random;

    // 칸마다 남은 새로고침 횟수.
    private readonly List<int> _rerollLeft = new();

    // 지금 창에 떠 있는 종류들(중복 금지 판정의 기준).
    private readonly List<DoomAugmentKind> _onScreen = new();

    // 지금은 아무 일도 하지 않아 내보내면 안 되는 종류들(쓰러진 소녀가 없을 때의 부활 등).
    // 중복 금지와 같은 방식으로 후보에서 빼므로, 뽑기 규칙은 한 곳(Draw)에 그대로 남는다.
    private readonly List<DoomAugmentKind> _blocked = new();

    /// <returns>고른 증강. 이 창은 고르지 않으면 닫히지 않으므로 항상 유효한 값이 돌아온다.</returns>
    /// <param name="blocked">지금 뽑히면 안 되는 종류(꽝 카드 방지). 없으면 null.</param>
    public async UniTask<Pick> ShowAsync(BattleHudContext ctx, int phase, DoomAugmentState state, System.Random random,
                                         IReadOnlyCollection<DoomAugmentKind> blocked = null) {
        _ctx = ctx;
        _state = state;
        _random = random ?? new System.Random();

        _blocked.Clear();
        if (blocked != null) _blocked.AddRange(blocked);

        if (phaseText != null) phaseText.text = $"{phase} 페이즈";
        if (guideText != null) guideText.text = "증강을 하나 선택하세요.";

        BuildInitial();

        _result = new UniTaskCompletionSource<Pick>();
        await base.ShowAsync();
        return await _result.Task;
    }

    // 창을 열 때의 첫 구성. 중복 없이 한 번에 뽑는다.
    private void BuildInitial() {
        _onScreen.Clear();
        _rerollLeft.Clear();
        if (cards == null) return;

        var kinds = DoomAugmentRules.Draw(cards.Length, _blocked, _state, _random);

        for (int i = 0; i < cards.Length; i++) {
            var card = cards[i];
            if (card == null) continue;

            // 후보가 카드 수보다 적을 수 있다(상한에 닿은 종류가 많아지면). 남는 카드는 숨긴다.
            bool has = i < kinds.Count;
            card.gameObject.SetActive(has);
            if (!has) continue;

            var kind = kinds[i];
            _onScreen.Add(kind);
            _rerollLeft.Add(DoomAugmentRules.RerollPerChoice);

            card.Initialize(OnSelect, OnReroll);
            card.Bind(kind, RollFor(kind), DoomAugmentRules.RerollPerChoice);
        }
    }

    private float RollFor(DoomAugmentKind kind) {
        var def = DoomAugmentRules.Get(kind);
        return DoomAugmentRules.Roll(def, _random);
    }

    private void OnSelect(DoomAugmentCard card) {
        if (card == null || _result == null) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);

        // 고르는 순간 나머지 카드도 잠근다(연타로 두 번 고르는 것을 막는다).
        SetAllInteractable(false);
        _result.TrySetResult(new Pick(card.Kind, card.Value));
        CloseAsync().Forget();
    }

    private void OnReroll(DoomAugmentCard card) {
        if (card == null || cards == null) return;

        int index = System.Array.IndexOf(cards, card);
        if (index < 0 || index >= _rerollLeft.Count || _rerollLeft[index] <= 0) return;

        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        _rerollLeft[index]--;

        // 다시 뽑을 때도 지금 창에 떠 있는 종류는 피한다(자기 자신 포함 - 같은 것이 또 나오면 새로고침이 무의미하다).
        // 못 내보낼 종류도 처음 뽑을 때와 같은 기준으로 계속 제외한다.
        var exclude = new List<DoomAugmentKind>(_onScreen.Count + _blocked.Count);
        exclude.AddRange(_onScreen);
        exclude.AddRange(_blocked);

        var replacement = DoomAugmentRules.Draw(1, exclude, _state, _random);
        if (replacement.Count == 0) {
            // 더 뽑을 종류가 없다. 수치만 다시 굴려 준다(새로고침을 썼는데 아무 일도 없으면 안 된다).
            card.Bind(card.Kind, RollFor(card.Kind), _rerollLeft[index]);
            return;
        }

        var kind = replacement[0];
        int onScreenIndex = _onScreen.IndexOf(card.Kind);
        if (onScreenIndex >= 0) _onScreen[onScreenIndex] = kind;

        card.Bind(kind, RollFor(kind), _rerollLeft[index]);
    }

    private void SetAllInteractable(bool on) {
        if (cards == null) return;
        foreach (var c in cards)
            if (c != null) c.SetInteractable(on);
    }

    // 이 창은 배경 탭이나 뒤로가기로 닫히면 안 된다. 고르지 않은 채 닫히면 전투가 멈춘 채로 남는다.
    protected override bool CanDismiss => false;

    // 그래도 외부에서 일괄로 닫히는 경로(씬 전환 등)가 있으므로 대기가 영영 풀리지 않는 일은 막는다.
    // 첫 카드를 고른 것으로 보되, 카드가 하나도 없으면 공격력 최소치로 떨어뜨린다.
    protected override void OnClosed() {
        if (_result == null) return;
        if (cards != null)
            foreach (var c in cards)
                if (c != null && c.gameObject.activeSelf) { _result.TrySetResult(new Pick(c.Kind, c.Value)); return; }

        var fallback = DoomAugmentRules.Get(DoomAugmentKind.Attack);
        _result.TrySetResult(new Pick(DoomAugmentKind.Attack, fallback.Min));
    }
}
