using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 정보 패널(PLANNING 2-1). 대표 캐릭터 초상화(+장착 프로필 테두리) + 계정 닉네임 + 총 전투력을 표시한다.
/// 닉네임/테두리는 서버 권위 값(PlayerStateStore)을 구독해 변경 시 자동 갱신된다. 프로필 버튼으로 프로필 팝업을 연다.
/// </summary>
public sealed class MainInfoPanel : MonoBehaviour {
    [FormerlySerializedAs("portraitView")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text combatPowerText;
    [Tooltip("선택. 대표 캐릭터 초상화 위에 겹쳐 그릴 장착 테두리. 미장착이면 스스로 숨김. " +
             "illustrationView 와 같은 영역을 덮도록 배치하고 Image 의 Raycast Target 은 끈다(프로필 버튼 입력을 가리지 않도록).")]
    [FormerlySerializedAs("nameplateView")]
    [SerializeField] private ProfileFrameView profileFrameView;
    [Tooltip("선택. 프로필 팝업을 여는 버튼.")]
    [SerializeField] private Button profileButton;

    private MainHudContext _ctx;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        ctx.Store.DeckChanged += Refresh;
        ctx.Store.CharactersChanged += OnCharactersChanged;   // 대표/총 전투력은 어떤 변경이든 다시 계산(인자 무시).
        ctx.Store.Nickname.Subscribe(_ => RefreshNickname()).AddTo(this);
        if (profileFrameView != null) profileFrameView.BindReactive(ctx.ProfileFrames, ctx.Store.EquippedProfileFrameId);
        if (profileButton != null) profileButton.onClick.AddListener(OpenProfile);
        Refresh();
    }

    private void OnCharactersChanged(CharacterChange _) => Refresh();

    private void Refresh() {
        int repId = _ctx.Store.Deck.RepresentativeCharId;
        var appearance = _ctx.Appearance.Mine(repId);
        if (appearance.Character != null && illustrationView != null) illustrationView.Bind(appearance);

        RefreshNickname();
        if (combatPowerText != null)
            combatPowerText.text = Mathf.RoundToInt(AccountStats.TotalDeckCombatPower(_ctx)).ToString("N0");
    }

    private void RefreshNickname() {
        if (nicknameText != null) nicknameText.text = _ctx.Store.Nickname.Value;
    }

    private void OpenProfile() {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenProfileAsync().Forget();
    }

    private async UniTaskVoid OpenProfileAsync() {
        var reference = _ctx.Popups.ProfilePopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return;
        var popup = await _ctx.Ui.ShowPopupAsync<ProfilePopup>(reference);
        popup.Initialize(_ctx);
        await popup.ShowAsync();
    }

    private void OnDestroy() {
        if (_ctx == null) return;
        _ctx.Store.DeckChanged -= Refresh;
        _ctx.Store.CharactersChanged -= OnCharactersChanged;
    }
}
