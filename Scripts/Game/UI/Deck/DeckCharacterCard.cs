using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 덱 팝업의 보유 캐릭터 카드 1개. 초상화/이름/레벨/등급과 대표/편성/선택 상태 표시를 담당한다.
/// 클릭하면 팝업에 선택을 통지한다(상세 패널 갱신). 편성/대표 편집은 상세 패널의 버튼에서 수행한다.
/// </summary>
public sealed class DeckCharacterCard : MonoBehaviour {
    [FormerlySerializedAs("portraitView")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [Tooltip("등급 표시(GradeText). 색 적용 여부는 컴포넌트의 applyColor 체크로 결정.")]
    [SerializeField] private GradeText gradeText;
    [Tooltip("별레벨 표시(별 그리드). 비우면 표시 안 함.")]
    [SerializeField] private StarLevelView starLevelView;
    [Tooltip("무기 타입 아이콘. 비우면 표시 안 함.")]
    [SerializeField] private Image weaponTypeImage;
    [Tooltip("무기 타입별 아이콘 공용 테이블(SO). 비우면 이미지 숨김.")]
    [SerializeField] private WeaponIconTable weaponIconTable;

    [Header("Exp Bar")]
    [Tooltip("경험치 비율 막대(Image, Image Type = Filled). fillAmount 로 비율을 그린다. 비우면 표시 안 함.")]
    [SerializeField] private Image expBarFill;
    [Tooltip("경험치 막대의 등급별 색 적용(GradeGraphicStyle). 색 규칙은 컴포넌트 설정을 따르며, 비우면 프리팹 색 유지.")]
    [SerializeField] private GradeGraphicStyle expBarStyle;

    [Header("State Markers")]
    [Tooltip("대표 캐릭터 표시.")]
    [SerializeField] private GameObject representativeBadge;
    [Tooltip("덱 편성됨 표시.")]
    [SerializeField] private GameObject inDeckBadge;
    [Tooltip("현재 선택(상세 표시 중) 프레임.")]
    [SerializeField] private GameObject selectedFrame;
    [Tooltip("업그레이드 가능(육성/장비) 알림 배지(BadgeView, Source=Manual). 선택.")]
    [SerializeField] private BadgeView upgradeBadge;
    [Tooltip("한계돌파한 캐릭터에만 켜지는 그룹(연출 이펙트 등). 선택. 꺼진 채로 두면 코드가 상태에 맞춰 켠다.")]
    [SerializeField] private GameObject limitBreakGroup;
    [Tooltip("무한 타워 층 보너스를 받는 캐릭터에만 켜지는 그룹. 선택. 무한 타워 편성 화면만 켜며, 다른 화면에서는 꺼진 채로 둔다.")]
    [SerializeField] private GameObject towerBonusGroup;

    [SerializeField] private Button button;

    [Header("Composition Drag (선택)")]
    [Tooltip("편성 화면에서 이 카드를 진형 슬롯으로 끌 때 쓰는 드래그 손잡이. 편성 화면을 쓰지 않는 곳에 두는 카드라면 비워 둔다.")]
    [SerializeField] private CompositionDragHandle dragHandle;

    [Header("Selected Float (DOTween)")]
    [Tooltip("선택 시 상하로 부유시킬 '내부 비주얼 루트'. 카드 루트는 Horizontal Layout Group 이 위치를 제어하므로 반드시 카드 안의 자식을 지정해야 한다(비우면 부유 없음).")]
    [SerializeField] private RectTransform floatTarget;
    [Tooltip("부유 진폭(px).")]
    [SerializeField] private float floatAmplitude = 8f;
    [Tooltip("한 방향 이동 시간(초). 왕복이라 실제 주기는 이 값의 2배.")]
    [SerializeField] private float floatDuration = 0.9f;

    private CharacterData _data;
    private Tween _floatTween;
    private Vector2 _floatBasePos;

    public CharacterData Data => _data;

    /// <param name="appearance">표시할 외형(기본 일러스트 또는 장착 코스튬). 캐릭터 원본도 여기서 꺼내 쓴다.</param>
    public void Bind(CharacterAppearance appearance, CharacterProgress progress, Action<DeckCharacterCard> onClick) {
        var data = appearance.Character;
        _data = data;
        if (data == null) return;
        if (illustrationView != null) illustrationView.Bind(appearance);
        if (nameText != null) nameText.text = data.DisplayName;
        if (levelText != null && progress != null) levelText.text = progress.Level.ToString();
        if (starLevelView != null) starLevelView.SetStarLevel(progress != null ? progress.StarLevel : 0);
        // 등급색은 캐릭터가 바뀔 때만 갱신하면 되고(등급은 불변), 비율은 성장할 때마다 갱신한다.
        if (expBarStyle != null) expBarStyle.SetGrade(data.Grade);
        SetExpRatio(progress);
        SetLimitBreak(progress);

        if (weaponTypeImage != null) {
            var icon = weaponIconTable != null ? weaponIconTable.Get(data.WeaponType) : null;
            weaponTypeImage.sprite = icon;
            weaponTypeImage.enabled = icon != null;
        }

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }

    /// <summary>레벨/별레벨 등 성장치만 즉시 갱신한다(정렬/재생성 없이 실시간 반영용). 초상화/이름은 그대로 둔다.</summary>
    /// <summary>
    /// 이 카드를 편성 드래그의 출발점으로 연결한다. host 가 null 이면 드래그가 꺼진다(평소의 덱 화면).
    /// slotIndex 는 진형 슬롯 번호이며, 보유 목록의 카드는 CompositionDragHandle.RosterSlot 을 쓴다.
    /// </summary>
    public void SetDragSource(CompositionDragHandle.IHost host, int slotIndex) {
        if (dragHandle == null) return;
        dragHandle.Configure(host, _data != null ? _data.Id : 0, slotIndex);
        dragHandle.SetDimmed(false);
    }

    /// <summary>
    /// 일러스트만 제자리 교체한다(코스튬 변경 반영). 선택/편성 표식과 클릭 리스너는 그대로 두므로
    /// 다시 Bind 할 때처럼 카드 상태가 초기화되지 않는다.
    /// </summary>
    public void RefreshAppearance(CharacterAppearance appearance) {
        if (illustrationView != null) illustrationView.Bind(appearance);
    }

    public void RefreshProgress(CharacterProgress progress) {
        if (levelText != null && progress != null) levelText.text = progress.Level.ToString();
        if (starLevelView != null) starLevelView.SetStarLevel(progress != null ? progress.StarLevel : 0);
        SetExpRatio(progress);
        SetLimitBreak(progress);
    }

    // 한계돌파 표시(등급 뒤 + / 한계돌파 그룹). 성장 갱신(RefreshProgress)에서도 불리므로 돌파하는 즉시 바뀐다.
    // 그룹은 같은 값이면 건드리지 않아 그룹 안 연출이 처음부터 다시 돌지 않는다.
    private void SetLimitBreak(CharacterProgress progress) {
        bool on = progress != null && progress.LimitBroken;
        if (gradeText != null && _data != null) gradeText.SetGrade(_data.Grade, on);
        if (limitBreakGroup != null && limitBreakGroup.activeSelf != on) limitBreakGroup.SetActive(on);
    }

    // 경험치 비율(0~1)을 Filled 이미지의 채움량으로 반영한다. 진행도 계산은 CharacterProgress 가 소유한다.
    private void SetExpRatio(CharacterProgress progress) {
        if (expBarFill == null) return;
        expBarFill.fillAmount = progress != null ? progress.ExpRatio : 0f;
    }

    /// <summary>
    /// 이 카드를 튜토리얼 유도 대상에 포함할지. 편성 슬롯에 붙인 사본처럼 "보여 주기용 카드"는 꺼야 한다.
    /// 켜 두면 목록의 진짜 카드와 같은 자격으로 유도 후보가 되어, 아직 편성하지 않은 소녀를 가리켜야 할 때
    /// 이미 슬롯에 들어가 있는 카드가 대신 잡힌다.
    /// </summary>
    public void SetGuideTarget(bool on) {
        foreach (var anchor in GetComponentsInChildren<TutorialAnchor>(true)) anchor.enabled = on;
    }

    /// <summary>무한 타워 층 보너스 대상 표시. 판정은 화면이 TowerBonus.AppliesTo 로 해서 넘긴다.</summary>
    public void SetTowerBonus(bool on) {
        if (towerBonusGroup != null && towerBonusGroup.activeSelf != on) towerBonusGroup.SetActive(on);
    }

    /// <summary>업그레이드 가능(육성/장비) 배지 표시 여부. DeckPopup 이 스토어 기준으로 계산해 넘긴다.</summary>
    public void SetUpgradeBadge(bool on) {
        if (upgradeBadge != null) upgradeBadge.SetOn(on);   // 표시(프리팹/크기)는 BadgeCatalog(SO)가 결정.
    }

    public void SetState(bool inDeck, bool isRepresentative, bool isSelected) {
        if (representativeBadge != null) representativeBadge.SetActive(isRepresentative);
        if (inDeckBadge != null) inDeckBadge.SetActive(inDeck);
        if (selectedFrame != null) selectedFrame.SetActive(isSelected);
        SetFloating(isSelected);
    }

    // 선택된 카드만 내부 비주얼을 상하로 부유시킨다(레이아웃이 제어하는 카드 루트가 아니라 내부 자식을 움직임).
    // 부유를 실제로 시작할 때만 기준 위치를 캡처하고, 부유한 적 없는 카드는 위치를 건드리지 않는다
    // (선택이 바뀔 때 다른 카드들이 캡처된 값으로 끌려가 한곳에 모이는 문제 방지).
    private void SetFloating(bool on) {
        if (floatTarget == null) return;

        if (on) {
            if (_floatTween != null && _floatTween.IsActive()) return;   // 이미 부유 중이면 유지(기준 재캡처 금지)
            _floatBasePos = floatTarget.anchoredPosition;                // 부유 시작 시점의 위치를 기준으로
            _floatTween = floatTarget
                .DOAnchorPosY(_floatBasePos.y + floatAmplitude, floatDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);   // 타임스케일 0(일시정지) 중에도 부유 유지
        } else {
            if (_floatTween == null) return;   // 부유한 적 없으면 위치를 전혀 건드리지 않는다
            _floatTween.Kill();
            _floatTween = null;
            floatTarget.anchoredPosition = _floatBasePos;   // 시작 위치로 복귀
        }
    }

    private void OnDestroy() {
        _floatTween?.Kill();
        _floatTween = null;
    }
}
