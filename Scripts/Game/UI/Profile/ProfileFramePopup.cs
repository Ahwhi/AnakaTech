using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 프로필 테두리 장착/변경 팝업. 카탈로그의 전체 테두리를 그리드로 보여주고,
/// 보유한 테두리만 장착 가능하다(미보유는 잠금 표시).
/// 장착 중인 테두리를 다시 누르면 기본 테두리로 되돌린다. 결과는 서버 권위(PlayerStateStore.EquippedProfileFrameId)로 갱신된다.
///
/// 기본 테두리(ProfileFrameCatalog.DefaultId = 0)는 지급 대상이 아니라 모든 계정이 항상 쓰는 값이라
/// 서버 보유 목록에 없더라도 보유한 것으로 취급한다(카탈로그에 id 0 항목을 넣어 두면 칸으로도 보인다).
/// </summary>
public sealed class ProfileFramePopup : UIView, IHudPopup {
    [SerializeField] private Transform content;
    [SerializeField] private ProfileFrameCard cardPrefab;
    [Tooltip("선택. 기본 테두리로 되돌리는 버튼(이미 기본이면 비활성).")]
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button closeButton;

    private MainHudContext _hud;
    private CancellationToken _ct;
    private readonly List<ProfileFrameCard> _cards = new();
    private readonly HashSet<int> _owned = new();
    private bool _busy;

    protected override void Awake() {
        base.Awake();
        _ct = this.GetCancellationTokenOnDestroy();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
        if (unequipButton != null) unequipButton.onClick.AddListener(() => Equip(ProfileFrameCatalog.DefaultId));
    }

    public void Initialize(MainHudContext ctx) {
        _hud = ctx;
        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        try {
            var profile = await _hud.Profile.RequestProfileAsync(_ct);
            _owned.Clear();
            // 기본 테두리는 서버가 지급/보유로 관리하지 않는 값이라 항상 보유로 넣어 준다(잠금/거부되지 않게).
            _owned.Add(ProfileFrameCatalog.DefaultId);
            foreach (var id in profile.ProfileFrameIds) _owned.Add(id);
            _hud.Store.EquippedProfileFrameId.Value = profile.EquippedProfileFrameId;
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("프로필 테두리 정보를 불러오지 못했습니다.");
            return;
        }
        Build();
    }

    private void Build() {
        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();
        if (content == null || cardPrefab == null || _hud.ProfileFrames == null) return;

        int equipped = _hud.Store.EquippedProfileFrameId.Value;
        foreach (var e in _hud.ProfileFrames.All) {
            var card = Instantiate(cardPrefab, content);
            // 이름은 카탈로그를 거쳐 얻는다(에셋에 이름이 비어 있어도 "기본 테두리" 같은 대체 문구가 나온다).
            card.Bind(e.id, _hud.ProfileFrames.DisplayName(e.id), e.icon,
                      _owned.Contains(e.id), e.id == equipped, OnCardClick);
            _cards.Add(card);
        }
        if (unequipButton != null) unequipButton.interactable = equipped != ProfileFrameCatalog.DefaultId;
    }

    private void OnCardClick(ProfileFrameCard card) {
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        if (!_owned.Contains(card.ProfileFrameId)) { _hud.Ui.ShowToast("보유하지 않은 테두리입니다."); return; }

        // 장착 중인 테두리를 다시 누르면 기본으로 되돌린다.
        int current = _hud.Store.EquippedProfileFrameId.Value;
        int target = card.ProfileFrameId == current ? ProfileFrameCatalog.DefaultId : card.ProfileFrameId;
        if (target == current) return;   // 기본 상태에서 기본 칸을 누른 경우: 서버에 물어볼 것이 없다.
        Equip(target);
    }

    private void Equip(int profileFrameId) {
        if (_busy) return;
        EquipAsync(profileFrameId).Forget();
    }

    private async UniTaskVoid EquipAsync(int profileFrameId) {
        _busy = true;
        try {
            var res = await _hud.Profile.EquipProfileFrameAsync(profileFrameId, _ct);
            if (!res.IsSuccess) {
                _hud.Ui.ShowToast(res.FailReason == 1 ? "보유하지 않은 테두리입니다." : "테두리를 변경할 수 없습니다.");
                return;
            }
            _hud.Store.EquippedProfileFrameId.Value = res.EquippedProfileFrameId;
            Build();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("테두리를 변경할 수 없습니다.");
        } finally {
            _busy = false;
        }
    }

    protected override void OnClosed() {
        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();
    }
}
