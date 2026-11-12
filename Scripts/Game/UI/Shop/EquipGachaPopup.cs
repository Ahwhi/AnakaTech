using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 장비 가챠 연출 팝업. 캐릭터 가챠(GachaPopup)와 동일하게 뽑은 장비 카드를 한 장씩 위에서 화면 가운데로
/// 떨어뜨려 쌓는다. 연출이 끝나면 "탭하여 닫기" 프롬프트가 깜빡이고, 화면 아무 곳이나 탭하면 닫힌다.
/// SKIP 버튼은 연출 도중에도 바로 닫는다. 결과는 이미 지급된 상태라 연출을 끝까지 볼 이유가 없다.
/// 뽑기 진행(1회/10회, 재화)은 GachaMenuPopup 이 담당하며, 이 팝업은 결과 연출만 한다.
/// </summary>
public sealed class EquipGachaPopup : UIView {
    [SerializeField] private RectTransform cardRoot;
    [SerializeField] private EquipGachaCardView cardPrefab;
    [Tooltip("누르면 연출을 멈추고 바로 닫는다. 선택.")]
    [SerializeField] private Button skipButton;

    [Header("End (연출 종료 후)")]
    [Tooltip("연출이 끝나면 깜빡이며 나타나는 '탭하여 닫기' 그룹(배경 + 텍스트를 담는 루트). " +
             "비우면 아래 텍스트 오브젝트만 켠다.")]
    [SerializeField] private GameObject tapPromptGroup;
    [Tooltip("'탭하여 닫기' 안내 텍스트. 그룹에 CanvasGroup 이 없을 때 이 텍스트의 알파로 깜빡인다.")]
    [SerializeField] private TMP_Text tapPromptText;

    [Header("Animation")]
    [Tooltip("cardRoot 에 레이아웃 그룹이 없을 때만 사용하는 카드 간 수동 가로 간격.")]
    [SerializeField] private float cardSpacing = 180f;
    [SerializeField] private float dropHeight = 500f;
    [SerializeField] private float dropDuration = 0.22f;
    [SerializeField] private float staggerSeconds = 0.12f;
    [Tooltip("종료 프롬프트 깜빡임(밝음<->어두움) 한 주기 시간(초).")]
    [SerializeField] private float blinkDuration = 0.6f;

    private const float BlinkMinAlpha = 0.15f;   // 깜빡임의 가장 어두운 지점.

    private readonly List<GameObject> _cards = new();
    private CancellationToken _ct;
    private CancellationTokenSource _playCts;   // 연출 1회분. 닫히면 남은 드롭을 멈춘다.
    private MainHudContext _hud;
    private bool _endUiReady;
    private Tween _blinkTween;

    protected override void Awake() {
        base.Awake();
        _ct = this.GetCancellationTokenOnDestroy();
        if (skipButton != null) skipButton.onClick.AddListener(Skip);
    }

    // 남은 드롭(효과음/VFX 포함)을 즉시 멈추고 닫는다. 닫힘 페이드 중에 카드가 계속 떨어지지 않게 먼저 멈춘다.
    private void Skip() {
        StopPlay();
        CloseAsync().Forget();
    }

    // 연출 도중에는 닫히지 않는다. 배경 탭(UIView.tapToCloseArea)도 뒤로가기도 같은 기준을 따른다.
    protected override bool CanDismiss => _endUiReady;

    /// <summary>뽑은 장비 목록으로 연출을 시작한다. GachaMenuPopup 이 구매 결과를 받아 호출한다.</summary>
    public void Play(List<EquipRollDto> rolls, MainHudContext hud) {
        _hud = hud;
        StopPlay();
        _playCts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        ShowAsync().Forget();
        AnimateAsync(rolls, _playCts.Token).Forget();
    }

    private void StopPlay() {
        if (_playCts == null) return;
        _playCts.Cancel();
        _playCts.Dispose();
        _playCts = null;
    }

    private async UniTaskVoid AnimateAsync(List<EquipRollDto> rolls, CancellationToken ct) {
        HideEndUI();

        if (cardPrefab == null || cardRoot == null || rolls == null) { ShowEndUI(); return; }

        // 1) 뽑은 장비 카드를 Content 자식으로 모두 생성한다.
        var equipDb = _hud != null ? _hud.EquipDb : null;
        var rts = new List<RectTransform>();
        var grades = new List<Grade>();   // 착지 VFX 등급별 선택용(rts 와 1:1).
        for (int i = 0; i < rolls.Count; i++) {
            var data = equipDb != null ? equipDb.Get(rolls[i].EquipDataId) : null;
            if (data == null) continue;
            var card = Instantiate(cardPrefab, cardRoot);
            card.Bind(data, rolls[i], OwnerNameOf(data), true);
            _cards.Add(card.gameObject);
            rts.Add((RectTransform)card.transform);
            grades.Add(data.Grade);
        }

        int n = rts.Count;
        if (n == 0) { ShowEndUI(); return; }

        // 2) 강제 레이아웃 갱신으로 최종 위치 확정.
        var layoutGroup = cardRoot.GetComponent<LayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = true;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardRoot);

        // 3) 최종 위치를 캡처한 뒤 레이아웃 그룹을 끈다(드롭 연출이 매 프레임 레이아웃에 덮이지 않도록).
        var targets = new List<Vector2>(n);
        for (int i = 0; i < n; i++) {
            Vector2 pos;
            if (layoutGroup != null) {
                pos = rts[i].anchoredPosition;
            } else {
                pos = new Vector2((i - (n - 1) * 0.5f) * cardSpacing, 0f);   // 레이아웃 그룹 없으면 수동 배치 폴백
                rts[i].anchoredPosition = pos;
            }
            targets.Add(pos);
        }
        if (layoutGroup != null) layoutGroup.enabled = false;

        // 3-1) 착지 지점의 화면 좌표를 미리 캡처한다. 드롭 시작 순간엔 카드가 상단(시작 위치)에 있으므로,
        //      맞춰둔 "착지 위치"에 VFX 를 띄우려면 여기서 최종 위치를 저장해 둔다(캐릭터 가챠와 동일).
        var landingScreens = new List<Vector2>(n);
        var cardCanvas = cardRoot.GetComponentInParent<Canvas>();
        Camera cardCam = (cardCanvas != null && cardCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? cardCanvas.worldCamera : null;
        var corners = new Vector3[4];
        for (int i = 0; i < n; i++) {
            rts[i].GetWorldCorners(corners);   // 0:BL 2:TR
            Vector3 center = (corners[0] + corners[2]) * 0.5f;
            landingScreens.Add(RectTransformUtility.WorldToScreenPoint(cardCam, center));
        }

        // 4) 연출 시작 전 모든 카드를 완전히 숨긴다(스케일 0).
        for (int i = 0; i < n; i++) {
            rts[i].anchoredPosition = targets[i] + new Vector2(0f, dropHeight);
            rts[i].localScale = Vector3.zero;
        }

        // 5) 한 장씩 위에서 떨어뜨리며 공개(스케일 0 -> 1, 위 -> 제자리).
        for (int i = 0; i < n; i++) {
            var rt = rts[i];
            PlayCardVfx(landingScreens[i], grades[i]);   // 낙하 시작 순간, 착지할 자리에 등급별 VFX.
            PlayCardSfx(grades[i]);                      // 같은 순간 등급별 공개음(미배정 등급은 무음).
            // 카드에 묶어 두어 SKIP 으로 카드가 먼저 파괴돼도 트윈이 남지 않게 한다.
            var seq = DOTween.Sequence().SetUpdate(true).SetLink(rt.gameObject);
            seq.Append(rt.DOAnchorPos(targets[i], dropDuration).SetEase(Ease.OutBack));
            seq.Join(rt.DOScale(1f, dropDuration).SetEase(Ease.OutBack));

            bool cancelled = await UniTask.Delay(TimeSpan.FromSeconds(staggerSeconds), cancellationToken: ct)
                .SuppressCancellationThrow();
            if (cancelled) return;
        }

        // 6) 마지막 카드 드롭이 끝나도록 잠시 대기(그리드를 너무 일찍 켜 드롭을 끊지 않도록).
        bool endCancelled = await UniTask.Delay(TimeSpan.FromSeconds(dropDuration), cancellationToken: ct)
            .SuppressCancellationThrow();
        if (endCancelled) return;

        // 7) 연출 종료: 레이아웃 그룹을 다시 켜 최종 정렬을 복구(카드는 이미 그 위치로 떨어져 있어 튐이 없다) 후 프롬프트.
        if (layoutGroup != null) layoutGroup.enabled = true;
        ShowEndUI();
    }

    // 전용무기의 주인 캐릭터 이름(무기가 아니거나 캐릭터 데이터가 없으면 빈 문자열).
    private string OwnerNameOf(EquipmentData data) {
        if (data == null || !data.IsSignature) return "";
        var owner = _hud != null && _hud.CharDb != null ? _hud.CharDb.Get(data.OwnerCharDataId) : null;
        return owner != null ? owner.DisplayName : "";
    }

    // 지정 화면 좌표에 카드 등급별 UI 파티클 VFX 재생(전역 VFX 레이어). 카탈로그/레이어 미배선 시 안전하게 무시된다.
    private void PlayCardVfx(Vector2 screenPoint, Grade grade) {
        if (_hud == null || _hud.UiVfx == null) return;
        var prefab = _hud.UiVfx.GachaVfx(GachaVfxKind.Equipment, grade);
        if (prefab != null) _hud.Ui.PlayVfx(prefab, screenPoint);
    }

    // 카드 등급별 공개음. 클립을 배정하지 않은 등급은 SoundLibrary 단계에서 무음 처리된다.
    private void PlayCardSfx(Grade grade) {
        if (_hud == null || _hud.Sound == null) return;
        _hud.Sound.PlaySfx(grade.GachaRevealSfx());
    }

    // 켜고 끌 대상. 그룹(배경 포함)이 지정돼 있으면 그것을, 없으면 텍스트 오브젝트만 쓴다.
    private GameObject PromptRoot => tapPromptGroup != null ? tapPromptGroup
                                   : (tapPromptText != null ? tapPromptText.gameObject : null);

    private void HideEndUI() {
        _endUiReady = false;
        KillBlink();
        var root = PromptRoot;
        if (root != null) root.SetActive(false);
    }

    private void ShowEndUI() {
        _endUiReady = true;
        var root = PromptRoot;
        if (root == null) return;
        root.SetActive(true);

        // 배경까지 함께 깜빡이도록 그룹에 CanvasGroup 이 있으면 그 알파를 쓰고, 없으면 텍스트 알파로 폴백한다.
        var group = root.GetComponent<CanvasGroup>();
        if (group != null) {
            group.alpha = 1f;
            _blinkTween = DOTween.To(() => group.alpha, a => group.alpha = a, BlinkMinAlpha, blinkDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        } else if (tapPromptText != null) {
            tapPromptText.alpha = 1f;
            _blinkTween = DOTween.To(() => tapPromptText.alpha, a => tapPromptText.alpha = a, BlinkMinAlpha, blinkDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }
    }

    private void KillBlink() {
        if (_blinkTween != null) { _blinkTween.Kill(); _blinkTween = null; }
    }

    private void ClearCards() {
        foreach (var c in _cards) if (c != null) Destroy(c);
        _cards.Clear();
    }

    protected override void OnClosed() {
        StopPlay();
        KillBlink();
        ClearCards();
    }
}
