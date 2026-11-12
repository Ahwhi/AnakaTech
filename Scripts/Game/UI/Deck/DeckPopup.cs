using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 덱 팝업(PLANNING 10). 보유 캐릭터 그리드(대표->편성->등급->레벨 순 정렬), 선택 캐릭터의
/// 일러스트(DOTween 부유) + 전투력/레벨/무기타입/등급/이름/능력치 패널, 덱 편성/대표 지정, 서버 반영을 담당한다.
/// 덱 편집은 로컬 작업본(DeckEditSession)에 쌓이고, 편집이 잠잠해지면 그때그때 서버(ChangeDeck)로 확정된다.
/// 닫을 때까지 미루면 편성을 끝내고도 "스쿼드 구성" 같은 퀘스트가 완료되지 않아 유저가 무엇이 남았는지 알 수 없다.
/// 편집 중의 확정은 다음 런부터 적용(immediate=false)이라 이미 싸우고 있는 런의 보상 계산을 건드리지 않는다.
/// 진행 중인 런에 반영하는 것은 지금까지처럼 닫을 때 한 번만 하며, 그때 새 편성으로 즉시 다시 시작한다
/// (기력은 클리어에만 들므로 런을 버려도 손해가 없다. 별도 확인은 묻지 않는다).
/// 육성/장비는 별도 프리팹이 아니라 DeckPopup 하위에 비활성으로 배치된 GrowthPopup/EquipmentPopup 패널이며,
/// 육성/장비 버튼으로 우측에 도킹 슬라이드하며 열린다.
/// </summary>
public sealed class DeckPopup : UIView, IHudPopup {
    [Header("Grid")]
    [SerializeField] private Transform gridContent;
    [SerializeField] private DeckCharacterCard cardPrefab;
    [Tooltip("그리드를 채우는 동안 표시할 로딩 그룹(문구/스피너). 비우면 표시 없이 채운다.")]
    [SerializeField] private GameObject loadingGroup;

    [Header("Detail")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("선택 캐릭터의 이명/수식어(칭호). 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("등급 표시(GradeText). 색 적용 여부는 컴포넌트의 applyColor 체크로 결정.")]
    [SerializeField] private GradeText gradeText;
    [Tooltip("등급 표시용 보조(외곽선/그림자 등 레이어드 표현용). gradeText 와 같은 등급이 들어간다.")]
    [SerializeField] private GradeText gradeText2;
    [SerializeField] private GradeText gradeText3;
    [SerializeField] private TMP_Text levelText;
    [Header("Detail - Experience")]
    [Tooltip("현재 경험치/최대 경험치 비율 슬라이더(MinValue=0, MaxValue=1, Interactable=off).")]
    [SerializeField] private Slider expSlider;
    [Tooltip("현재/필요 경험치를 함께 표시(예: 30 / 100).")]
    [SerializeField] private TMP_Text expText;
    [Tooltip("선택 캐릭터의 별레벨 표시(별 그리드). 카드와 동일 방식. 비우면 표시 안 함.")]
    [SerializeField] private StarLevelView starLevelView;
    [SerializeField] private TMP_Text combatPowerText;
    [SerializeField] private TMP_Text weaponTypeText;
    [SerializeField] private Image weaponTypeImage;
    [FormerlySerializedAs("illustrationPortrait")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [Tooltip("무기 타입별 아이콘 공용 테이블(SO). 비우면 이미지 숨김.")]
    [SerializeField] private WeaponIconTable weaponIconTable;

    [Header("Detail - Skill")]
    [Tooltip("선택 캐릭터의 스킬 이름. 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text skillNameText;
    [Tooltip("선택 캐릭터의 스킬 아이콘. 비우면 표시 안 함.")]
    [SerializeField] private Image skillIcon;
    [Tooltip("선택 캐릭터의 스킬 설명. 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text skillDescText;
    [Tooltip("상태이상 설명(i) 버튼. 스킬이 상태이상을 걸 때만 눌리며, 누르면 그 상태이상들의 설명 팝업을 연다. 비우면 사용 안 함.")]
    [SerializeField] private Button skillStatusInfoButton;

    [Header("Detail - Stats (각 능력치를 값만 표시. 라벨은 정적 UI로 옆에 배치. 비우면 표시 안 함)")]
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text attackSpeedText;
    [SerializeField] private TMP_Text moveSpeedText;
    [SerializeField] private TMP_Text critRateText;
    [SerializeField] private TMP_Text critDamageText;
    [Tooltip("프리팹 기반 전체 스탯 목록(권장). 할당하면 모든 스탯을 자동 노출한다(개별 텍스트와 병행 가능).")]
    [SerializeField] private StatListView statListView;

    [Header("Illustration Float (DOTween)")]
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float floatDuration = 1.5f;

    [Header("Edit Buttons")]
    [Tooltip("편성 패널 열기(진형 슬롯에 드래그로 편성/해제/대표 지정). 덱 편집은 전부 이 패널에서 한다.")]
    [SerializeField] private Button compositionButton;
    [SerializeField] private Button growButton;
    [SerializeField] private Button equipButton;
    [Tooltip("코스튬 패널 열기(선택 캐릭터의 외형 교체). 아직 코스튬이 없는 캐릭터도 열 수 있다(기본 칸만 보인다).")]
    [SerializeField] private Button costumeButton;
    [Tooltip("아웃핏 패널 열기(선택 캐릭터의 전투 유닛 아웃핏 교체, V1.12).")]
    [SerializeField] private Button unitSkinButton;
    [Tooltip("편성 프리셋 팝업 열기(3칸 저장/적용).")]
    [SerializeField] private Button presetButton;
    [Tooltip("아이템 사용 팝업 열기(선택 캐릭터에게 경험치 쿠폰 등을 사용).")]
    [SerializeField] private Button itemButton;
    [Tooltip("육성 가능(스탯포인트 보유 or 별 승급 가능) 알림 배지(BadgeView, Source=Manual). 선택.")]
    [SerializeField] private BadgeView growBadge;

    [Header("Sub Panels (DeckPopup 내부에 비활성으로 배치, 별도 프리팹 아님)")]
    [Tooltip("육성 패널. DeckPopup 하위에 비활성 상태로 두고 육성 버튼으로 연다.")]
    [SerializeField] private GrowthPopup growthPanel;
    [Tooltip("장비 패널. DeckPopup 하위에 비활성 상태로 두고 장비 버튼으로 연다.")]
    [SerializeField] private EquipmentPopup equipmentPanel;
    [Tooltip("코스튬 패널. DeckPopup 하위에 비활성 상태로 두고 코스튬 버튼으로 연다.")]
    [SerializeField] private CostumePopup costumePanel;
    [Tooltip("아웃핏 패널. DeckPopup 하위에 비활성 상태로 두고 아웃핏 버튼으로 연다.")]
    [SerializeField] private UnitSkinPopup unitSkinPanel;
    [Tooltip("편성 패널. DeckPopup 하위에 비활성 상태로 두고 편성 버튼으로 연다.")]
    [SerializeField] private CompositionPanel compositionPanel;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    [Header("Currency")]
    [Tooltip("덱 팝업 상단 등에서 재화를 표시(선택). 배치한 경우에만 연결.")]
    [SerializeField] private CurrencyPanel currencyPanel;

    [Header("Dock Slide (육성/장비 오픈 시 좌측으로 슬라이드해 공간 확보)")]
    [Tooltip("정보 패널. 육성/장비가 열리면 왼쪽으로 슬라이드되어 사라진다.")]
    [SerializeField] private RectTransform infoPanel;
    [Tooltip("일러스트 패널. 육성/장비가 열리면 도킹 슬롯 자리로 슬라이드된다.")]
    [SerializeField] private RectTransform illustPanel;
    [Tooltip("일러스트 패널이 착지할 자리. 빈 RectTransform 을 원하는 위치/크기로 배치해 연결한다. " +
             "슬롯은 유니티 앵커로 배치되므로 해상도가 바뀌어도 착지점이 정확하다. 비우면 정보 패널 자리로 폴백.")]
    [SerializeField] private RectTransform illustDockedSlot;
    [Tooltip("도킹 중 카드 스크롤뷰의 오른쪽 끝 기준. 빈 RectTransform 을 놓으면 그 오른쪽 경계까지 스크롤뷰 폭을 줄여 " +
             "패널에 가려진 카드도 스크롤로 꺼내 누를 수 있게 한다. 비우면 스크롤뷰는 그대로 둔다.")]
    [SerializeField] private RectTransform scrollDockedEdge;
    [Tooltip("정보 패널을 화면 밖으로 뺄 때 추가로 더 밀어낼 여유(기준 해상도 px). 그림자가 걸쳐 보이면 늘린다.")]
    [SerializeField] private float dockOffscreenMargin = 32f;
    [SerializeField] private float dockSlideDuration = 0.3f;

    private const int BuildPerFrame = 8;   // 한 프레임에 생성할 카드 수(나머지는 다음 프레임으로 분산).

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private CancellationTokenSource _gridCts;   // 그리드 재구성 겹칠 때 이전 생성 취소용.

    private readonly List<DeckCharacterCard> _cards = new();
    // 편성 편집 작업본. 규칙(정원/중복/최소 인원/대표=첫 칸)은 전부 이 모델이 들고 있고,
    // 실제 편집은 편성 패널이 한다. 이 팝업은 작업본을 만들고 닫을 때 확정하는 역할이다.
    private readonly DeckEditSession _session = new();
    // 편성 패널이 열려 있는지. 그리드 카드의 드래그를 켜고 끄는 근거다.
    private bool _compositionOpen;
    // 이번에 연 편성 패널에서 실제로 편성을 바꿨는지. 닫을 때 그리드를 다시 정렬할지 판단한다
    // (열었다 그냥 닫은 경우까지 재정렬하면 카드가 이유 없이 다시 만들어지고 스크롤이 튄다).
    private bool _compositionEdited;
    // 편집이 잠잠해지면 서버에 확정하기 위한 통지(연달아 오는 편집을 한 번으로 묶는다).
    private Subject<Unit> _deckCommitRequest;
    // 이 팝업을 여는 동안 편성이 한 번이라도 바뀌었는지. 닫을 때 진행 중인 런에 반영할지 판단한다
    // (편집 중 조기 확정으로 작업본이 깨끗해져 있어도 런에는 아직 반영되지 않았기 때문에 따로 들고 있는다).
    private bool _deckChangedWhileOpen;

    private ScrollRect _scrollRect;

    /// <summary>편집을 묶어 보내는 시간(초). 자리를 맞바꾸는 동안 패킷이 연달아 나가지 않게 한다.</summary>
    private const float DeckCommitDelay = 0.4f;
    // 그리드가 속한 스크롤뷰(재구성 시 스크롤 위치 보존용). gridContent 부모에서 1회 탐색해 캐시.
    private ScrollRect Scroll => _scrollRect != null ? _scrollRect
        : (_scrollRect = gridContent != null ? gridContent.GetComponentInParent<ScrollRect>() : null);

    private CharacterData _selected;
    private Tween _floatTween;
    private Vector3 _illustrationBasePos;
    private bool _illustrationBaseCaptured;

    private Tween _dockTween;
    private Vector2 _infoHome;
    private Vector2 _illustHome;
    private CanvasGroup _infoGroup;
    private bool _dockHomesCaptured;
    private bool _docked;
    private bool _scrollDocked;   // 이번 도킹에서 카드 스크롤뷰를 줄였는지(패널마다 다르다).
    private RectTransform _scrollRt;
    private Vector2 _scrollHomeOffsetMax;
    private readonly Vector3[] _corners = new Vector3[4];   // GetWorldCorners 재사용 버퍼(할당 방지).

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        _session.Reset(ctx.Store.Deck);

        // 편성 편집을 잠잠해진 뒤 한 번만 서버에 확정한다(퀘스트가 편성 직후에 완료되도록).
        _deckCommitRequest = new Subject<Unit>();
        _deckCommitRequest.Throttle(TimeSpan.FromSeconds(DeckCommitDelay))
                          .Subscribe(_ => CommitEditedDeckAsync().Forget())
                          .AddTo(this);

        if (currencyPanel != null) currencyPanel.Initialize(ctx);

        WireButtons();

        RebuildGrid();

        var repData = ctx.CharDb.Get(_session.RepresentativeCharId);
        Select(repData != null ? repData : (_cards.Count > 0 ? _cards[0].Data : null));

        // 성장치 변경(별 승급/전투 레벨업 등)을 실시간 반영하도록 구독. 중복 구독 방지 후 등록.
        _ctx.Store.CharactersChanged -= OnCharactersChanged;
        _ctx.Store.CharactersChanged += OnCharactersChanged;

        // 덱 진입 = 현재 보유 캐릭을 "봤음"으로 기록(메인 덱 버튼 New 배지 해제).
        if (_ctx.Badges != null) _ctx.Badges.MarkDeckSeen();
    }

    private void WireButtons() {
        if (compositionButton != null)
            compositionButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenCompositionAsync().Forget(); });
        if (growButton != null)
            growButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenGrowthAsync().Forget(); });
        if (equipButton != null)
            equipButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenEquipmentAsync().Forget(); });
        if (costumeButton != null)
            costumeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenCostumeAsync().Forget(); });
        if (unitSkinButton != null)
            unitSkinButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenUnitSkinAsync().Forget(); });
        if (presetButton != null)
            presetButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenPresetAsync().Forget(); });
        if (itemButton != null)
            itemButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenItemAsync().Forget(); });
        if (closeButton != null)
            closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OnCloseRequestedAsync().Forget(); });
        if (skillStatusInfoButton != null)
            skillStatusInfoButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenSkillStatusInfoAsync().Forget(); });
    }

    // ── 그리드 ─────────────────────────────────────
    // preserveScroll: 재구성(카드 재생성) 후 이전 스크롤 위치를 복원한다(라이브 갱신으로 인한 리셋 방지).
    // 최초 진입 등은 false 로 두어 맨 위에서 시작한다.
    private void RebuildGrid(bool preserveScroll = false) => RebuildGridAsync(preserveScroll).Forget();

    private async UniTaskVoid RebuildGridAsync(bool preserveScroll) {
        // 재구성이 겹치면 이전 생성을 취소한다.
        _gridCts?.Cancel();
        _gridCts?.Dispose();
        _gridCts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        var ct = _gridCts.Token;

        float savedScroll = preserveScroll && Scroll != null ? Scroll.verticalNormalizedPosition : -1f;

        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();

        if (loadingGroup != null) loadingGroup.SetActive(true);

        var owned = new List<CharacterData>();
        foreach (var kv in _ctx.Store.Characters) {
            var d = _ctx.CharDb.Get(kv.Key);
            if (d != null) owned.Add(d);
        }
        owned.Sort(Compare);

        try {
            await AsyncBuild.SpawnChunkedAsync(owned, BuildPerFrame, d => {
                var card = Instantiate(cardPrefab, gridContent);
                card.Bind(_ctx.Appearance.Mine(d), _ctx.Store.GetProgress(d.Id), OnCardClicked);
                // 편성 패널이 열려 있는 동안 생성된 카드도 곧바로 집을 수 있어야 한다(그리드는 조금씩 채워진다).
                card.SetDragSource(RosterDragHost, CompositionDragHandle.RosterSlot);
                int id = d.Id;
                // 부분 생성 중에도 상태가 맞도록 카드별로 즉시 반영(전체 갱신은 완료 후 한 번 더).
                card.SetState(_session.Contains(id), id == _session.RepresentativeCharId,
                              _selected != null && id == _selected.Id);
                _cards.Add(card);
            }, ct);
        } catch (OperationCanceledException) {
            return;
        }

        if (loadingGroup != null) loadingGroup.SetActive(false);
        RefreshCardsState();
        RefreshUpgradeBadges();
        // 대표가 유효하지 않아 선택이 비어 있으면 첫 카드로 폴백 선택.
        if (_selected == null && _cards.Count > 0) Select(_cards[0].Data);

        // 재구성 전 스크롤 위치 복원(레이아웃이 갱신된 뒤 적용해야 정확하다).
        if (savedScroll >= 0f && Scroll != null) {
            Canvas.ForceUpdateCanvases();
            Scroll.verticalNormalizedPosition = savedScroll;
        }
    }

    private int Compare(CharacterData a, CharacterData b) {
        // 대표 -> 편성 -> 등급(높은 순) -> 레벨(높은 순) 순으로 정렬한다.
        int rep = _session.RepresentativeCharId;
        int ra = a.Id == rep ? 0 : 1, rb = b.Id == rep ? 0 : 1;
        if (ra != rb) return ra - rb;

        int da = _session.Contains(a.Id) ? 0 : 1, db = _session.Contains(b.Id) ? 0 : 1;
        if (da != db) return da - db;

        int gradeCmp = b.Grade.CompareTo(a.Grade);           // 높은 등급 먼저
        if (gradeCmp != 0) return gradeCmp;

        int la = _ctx.Store.GetProgress(a.Id)?.Level ?? 0;
        int lb = _ctx.Store.GetProgress(b.Id)?.Level ?? 0;
        int levelCmp = lb.CompareTo(la);                     // 높은 레벨 먼저
        if (levelCmp != 0) return levelCmp;

        return a.Id.CompareTo(b.Id);                         // 동률 시 안정적 순서 보장(재정렬 흔들림 방지)
    }

    private void RefreshCardsState() {
        foreach (var card in _cards) {
            if (card == null || card.Data == null) continue;
            int id = card.Data.Id;
            card.SetState(_session.Contains(id), id == _session.RepresentativeCharId,
                          _selected != null && id == _selected.Id);
        }
    }

    private void OnCardClicked(DeckCharacterCard card) {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        Select(card.Data);
    }

    // ── 선택 / 상세 ────────────────────────────────
    private void Select(CharacterData data) {
        _selected = data;
        RefreshDetail();
        RefreshCardsState();
        RefreshEditButtons();
        RefreshUpgradeBadges();
        SyncOpenSubPanels();
    }

    // 육성 가능(스탯포인트 보유 or 별 승급 가능) 배지 갱신: 각 카드 + 선택 캐릭의 육성 버튼. 스토어 기준 순수 계산(장비 제외).
    private void RefreshUpgradeBadges() {
        if (_ctx == null) return;
        int ownedDia = _ctx.Store.Diamond.Value;

        foreach (var card in _cards) {
            if (card == null || card.Data == null) continue;
            card.SetUpgradeBadge(CharacterUpgrade.CanGrow(_ctx.Store, card.Data.Id, ownedDia));
        }

        SetBadge(growBadge, _selected != null && CharacterUpgrade.CanGrow(_ctx.Store, _selected.Id, ownedDia));
    }

    // 배지 토글. 표시(프리팹/크기)는 BadgeCatalog(SO)가 결정하므로 여기서는 켜고 끄기만 한다.
    private static void SetBadge(BadgeView badge, bool on) {
        if (badge != null) badge.SetOn(on);
    }

    // 육성/장비 패널이 열린 상태에서 그리드의 다른 캐릭터를 선택하면, 열려 있는 패널을 선택 캐릭터 기준으로
    // 다시 초기화한다. 이렇게 하지 않으면 패널이 처음 열 때의 캐릭터에 고정되어 엉뚱한 캐릭터를 편집하게 된다.
    private void SyncOpenSubPanels() {
        if (_selected == null) return;
        if (growthPanel != null && growthPanel.gameObject.activeSelf)
            growthPanel.Initialize(_ctx, _selected.Id);
        if (equipmentPanel != null && equipmentPanel.gameObject.activeSelf)
            equipmentPanel.Initialize(_ctx, _selected.Id);
        if (costumePanel != null && costumePanel.gameObject.activeSelf)
            costumePanel.Initialize(_ctx, _selected.Id);
        if (unitSkinPanel != null && unitSkinPanel.gameObject.activeSelf)
            unitSkinPanel.Initialize(_ctx, _selected.Id);
    }

    private void RefreshDetail() {
        if (_selected == null) return;
        RefreshDetailValues();
        ShowIllustration(_selected);
    }

    // 일러스트 재생성 없이 이름/등급/레벨/별/전투력/스탯 값만 갱신한다(성장치 실시간 반영에 사용).
    private void RefreshDetailValues() {
        if (_selected == null) return;
        var progress = _ctx.Store.GetProgress(_selected.Id);
        // 시너지는 편집 중인 편성 기준으로 본다(편성 패널에서 막 맞춘 시너지가 상세 전투력에도 바로 보이도록).
        var runtime = _ctx.Store.RuntimeFor(_selected, progress, _ctx.Settings, _ctx.EquipDb, _session.Members);
        var s = runtime.FinalStats;

        if (nameText != null) nameText.text = _selected.DisplayName;
        if (titleText != null) titleText.text = _selected.Title;
        // 등급 표시 3개(있는 것만) 모두 같은 등급으로 채운다(레이어드 표현용). 색 적용은 각 GradeText 의 applyColor 로 결정.
        bool limitBroken = progress != null && progress.LimitBroken;   // 한계돌파면 등급 뒤에 + 를 붙인다
        if (gradeText != null) gradeText.SetGrade(_selected.Grade, limitBroken);
        if (gradeText2 != null) gradeText2.SetGrade(_selected.Grade, limitBroken);
        if (gradeText3 != null) gradeText3.SetGrade(_selected.Grade, limitBroken);
        if (levelText != null) levelText.text = progress != null ? progress.Level.ToString() : "";
        if (starLevelView != null) starLevelView.SetStarLevel(progress != null ? progress.StarLevel : 0);

        // 경험치(현재/최대) 슬라이더 + "현재 / 필요 (백분율%)" 결합 텍스트.
        int exp = progress != null ? progress.Exp : 0;
        int maxExp = progress != null ? progress.MaxExp : 0;
        float expRatio = progress != null ? progress.ExpRatio : 0f;
        if (expSlider != null) expSlider.value = expRatio;
        if (expText != null) expText.text = $"{exp:N0} / {maxExp:N0} ({expRatio * 100f:0.00}%)";
        if (combatPowerText != null) combatPowerText.text = runtime.CombatPower.ToString("N0");
        if (weaponTypeText != null) weaponTypeText.text = GameEnumRules.WeaponDisplayName(_selected.WeaponType);

        if (weaponTypeImage != null) {
            var icon = weaponIconTable != null ? weaponIconTable.Get(_selected.WeaponType) : null;
            weaponTypeImage.sprite = icon;
            weaponTypeImage.enabled = icon != null;
        }

        RefreshSkill();
        SetStatTexts(s);
    }

    // 선택 캐릭터가 지금 쓰는 스킬(한계돌파했으면 한계돌파 스킬). 표시와 상태이상 설명이 같은 스킬을 본다.
    private UnitSkill SelectedSkill {
        get {
            if (_selected == null) return null;
            var progress = _ctx.Store.GetProgress(_selected.Id);
            return _selected.SkillFor(progress != null && progress.LimitBroken);
        }
    }

    // 선택 캐릭터의 스킬 이름/아이콘/설명을 표시한다(설명은 툴팁 대신 바로 노출).
    private void RefreshSkill() {
        var skill = SelectedSkill;
        if (skillNameText != null) skillNameText.text = skill != null ? skill.SkillName : "";
        if (skillDescText != null) skillDescText.text = skill != null ? SkillTextFormat.Colorize(skill.Description) : "";
        if (skillIcon != null) {
            skillIcon.sprite = skill != null ? skill.SkillIcon : null;
            skillIcon.enabled = skill != null && skill.SkillIcon != null;
        }

        // 상태이상을 거는 스킬일 때만 설명 버튼이 눌린다. 눌러도 빈 팝업만 뜨면 고장으로 보이기 때문이다.
        if (skillStatusInfoButton != null)
            skillStatusInfoButton.interactable = !string.IsNullOrEmpty(SkillStatusDescription(skill));
    }

    // 선택 스킬이 거는 상태이상들의 설명(없으면 빈 문자열). 버튼 활성 판정과 팝업 본문이 같은 값을 쓴다.
    private static string SkillStatusDescription(UnitSkill skill) =>
        skill != null ? StatusEffectRules.DescribeAll(skill.InflictedStatusEffects) : "";

    // 공용 확인 팝업(제목 + 본문 + 확인)으로 띄운다. 스탯 설명(i) 버튼(StatRowView)과 같은 방식이다.
    private async UniTaskVoid OpenSkillStatusInfoAsync() {
        string body = SkillStatusDescription(SelectedSkill);
        var reference = _ctx.Popups != null ? _ctx.Popups.ConfirmPopup : null;
        if (string.IsNullOrEmpty(body) || reference == null || !reference.RuntimeKeyIsValid()) return;

        try {
            var popup = await _ctx.Ui.ShowPopupAsync<ConfirmPopup>(reference, _ct);
            await popup.ShowAsync("상태이상", body);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // 스토어의 캐릭터 변경을 팝업에 "정밀"하게 반영한다. 원인별로 필요한 최소 범위만 갱신하며,
    // 재정렬(Sort)/재생성은 하지 않는다 - 정렬은 팝업을 새로 열 때(Initialize)만 수행한다.
    // 이렇게 해서 편집/전투 중 어떤 갱신에도 스크롤과 선택이 튀지 않게 한다.
    private void OnCharactersChanged(CharacterChange change) {
        if (_ctx == null) return;
        ApplyCharacterChange(change);
    }

    private void ApplyCharacterChange(CharacterChange change) {
        switch (change.Reason) {
            case CharacterChangeReason.Snapshot:
                // 전체 스냅샷 교체(메인 진입 등). 보유 수가 달라졌을 때만 재구성(스크롤 보존), 아니면 값만 제자리 갱신.
                if (_ctx.Store.Characters.Count != _cards.Count) { RebuildGrid(preserveScroll: true); return; }
                UpdateCards(null);
                RefreshDetailValues();
                RefreshEditButtons();
                RefreshSelectedGrowBadge();
                break;

            case CharacterChangeReason.Roster:
                // 신규 획득 등으로 카드 수가 늘면 재구성(스크롤 보존). 아니면 해당 캐릭 카드만 갱신.
                if (_ctx.Store.Characters.Count != _cards.Count) { RebuildGrid(preserveScroll: true); return; }
                goto case CharacterChangeReason.Growth;

            case CharacterChangeReason.Exp:
            case CharacterChangeReason.Growth:
                // "바뀐 캐릭 카드"만 제자리 갱신(재정렬/재생성 없음). ids 가 null 이면(장비 장착/강화 등)
                // 카드 표시(레벨/별/등급)에는 영향이 없으므로 그리드는 아예 건드리지 않는다.
                if (change.Ids != null) UpdateCards(change.Ids);
                // 상세(전투력/스탯)는 장비 영향을 받으므로 선택 캐릭이 대상일 때 갱신한다.
                if (SelectedAffected(change.Ids)) {
                    RefreshDetailValues();
                    RefreshEditButtons();
                    RefreshSelectedGrowBadge();
                }
                break;
        }
    }

    // 지정 캐릭(ids)만 카드 값(레벨/별)과 육성 배지를 제자리 갱신한다. ids 가 null 이면 전체.
    // 재정렬/재생성은 없지만, 여러 카드를 한 번에 건드리면(특히 전체=null, 예: 장비 장착/강화 반영)
    // 레이아웃 재빌드로 스크롤이 맨 위로 튈 수 있어 위치를 저장/복원한다(단일 카드 갱신도 무해).
    private void UpdateCards(IReadOnlyList<int> ids) {
        float saved = Scroll != null ? Scroll.verticalNormalizedPosition : -1f;

        int ownedDia = _ctx.Store.Diamond.Value;
        HashSet<int> set = ids != null ? new HashSet<int>(ids) : null;
        foreach (var card in _cards) {
            if (card == null || card.Data == null) continue;
            if (set != null && !set.Contains(card.Data.Id)) continue;
            card.RefreshProgress(_ctx.Store.GetProgress(card.Data.Id));
            card.SetUpgradeBadge(CharacterUpgrade.CanGrow(_ctx.Store, card.Data.Id, ownedDia));
        }

        // 보류된 레이아웃 갱신을 지금 확정한 뒤(그때 스크롤이 튄다) 저장한 위치로 되돌린다.
        if (saved >= 0f && Scroll != null) {
            Canvas.ForceUpdateCanvases();
            Scroll.verticalNormalizedPosition = saved;
        }
    }

    // 변경 대상(ids)에 현재 선택 캐릭터가 포함되는지. ids 가 null 이면 대상 불명 = 전체로 간주.
    private bool SelectedAffected(IReadOnlyList<int> ids) {
        if (_selected == null) return false;
        if (ids == null) return true;
        foreach (var id in ids) if (id == _selected.Id) return true;
        return false;
    }

    // 선택 캐릭터의 육성 버튼 배지만 갱신(전체 카드 스캔 없이).
    private void RefreshSelectedGrowBadge() {
        SetBadge(growBadge, _selected != null && CharacterUpgrade.CanGrow(_ctx.Store, _selected.Id, _ctx.Store.Diamond.Value));
    }

    // 각 능력치를 개별 텍스트에 값만 표시한다(라벨은 정적 UI). 할당되지 않은 필드는 건너뛴다.
    private void SetStatTexts(CombatStats s) {
        if (attackText != null) attackText.text = StatFormat.Value(s, StatType.Attack);
        if (defenseText != null) defenseText.text = StatFormat.Value(s, StatType.Defense);
        if (healthText != null) healthText.text = StatFormat.Value(s, StatType.Health);
        if (rangeText != null) rangeText.text = StatFormat.Value(s, StatType.Range);
        if (attackSpeedText != null) attackSpeedText.text = StatFormat.Value(s, StatType.AttackSpeed);
        if (moveSpeedText != null) moveSpeedText.text = StatFormat.Value(s, StatType.MoveSpeed);
        if (critRateText != null) critRateText.text = StatFormat.Value(s, StatType.CritRate);
        if (critDamageText != null) critDamageText.text = StatFormat.Value(s, StatType.CritDamage);
        if (statListView != null) statListView.Populate(s);
    }

    private void RefreshEditButtons() {
        // 아이템은 덱 편성 여부와 무관하게 "지금 보고 있는 캐릭터"에게 쓴다.
        if (itemButton != null) itemButton.interactable = _selected != null;
        // 코스튬은 아직 없는 캐릭터라도 열 수 있게 둔다. 버튼이 눌리지 않으면 "고장난 것인지 없는 것인지"를
        // 유저가 구분할 수 없고, 캐릭터를 넘겨 볼 때마다 버튼이 켜졌다 꺼졌다 해서 화면도 불안정해진다.
        // 코스튬이 없는 캐릭터는 패널에 기본 칸만 나오므로 "아직 코스튬이 없다"가 그대로 읽힌다.
        if (costumeButton != null) costumeButton.interactable = _selected != null;
        if (unitSkinButton != null) unitSkinButton.interactable = _selected != null;
    }

    private void ShowIllustration(CharacterData data) {
        KillIllustration();
        if (illustrationView == null) return;

        // 장착 코스튬이 있으면 그 일러스트로 그린다(코스튬은 자기 관심점을 갖고 있어 크롭도 함께 맞는다).
        illustrationView.Bind(_ctx.Appearance.Mine(data));

        if (!_illustrationBaseCaptured) {
            _illustrationBasePos = illustrationView.transform.localPosition;
            _illustrationBaseCaptured = true;
        }

        var t = illustrationView.transform;
        t.localPosition = _illustrationBasePos;
        _floatTween = t.DOLocalMoveY(_illustrationBasePos.y + floatAmplitude, floatDuration)
                       .SetEase(Ease.InOutSine)
                       .SetLoops(-1, LoopType.Yoyo);
    }

    private void KillIllustration() {
        _floatTween?.Kill();
        _floatTween = null;
        if (illustrationView != null && _illustrationBaseCaptured)
            illustrationView.transform.localPosition = _illustrationBasePos;
    }

    /// <summary>작업본을 서버에 확정하고 스토어에 반영한다. 실패하면 작업본을 스토어 값으로 되돌린다.</summary>
    private async UniTask<bool> CommitDeckAsync(bool immediate) {
        try {
            var members = _session.Members;
            var res = await _ctx.MainData.ChangeDeckAsync(members, _session.RepresentativeCharId, immediate, _ct);
            if (res.IsSuccess) {
                _ctx.Store.ApplyDeck(members, _session.RepresentativeCharId);
                _session.MarkCommitted();
                return true;
            }
            _ctx.Ui.ShowToast("덱 구성이 올바르지 않습니다.");
            RevertWorkingFromStore();
            RefreshCardsState();
            RefreshEditButtons();
            return false;
        } catch (OperationCanceledException) {
            return false;
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("덱 변경에 실패했습니다.");
            RevertWorkingFromStore();
            return false;
        }
    }

    private void RevertWorkingFromStore() {
        _session.Reset(_ctx.Store.Deck);
        // 실패 롤백도 표식만 제자리 갱신(재생성 없이 스크롤 유지).
        RefreshCardsState();
        RefreshEditButtons();
    }

    private async UniTaskVoid OpenGrowthAsync() {
        if (_selected == null) { _ctx.Ui.ShowToast("캐릭터를 선택하세요."); return; }
        if (growthPanel == null) return;
        DockOpen(shrinkCardScroll: true);   // 육성 패널만 카드 스크롤뷰를 덮는다. 우측에 육성 패널이 도킹될 공간 확보(정보 좌측 퇴장, 일러스트 좌측 이동).
        growthPanel.Initialize(_ctx, _selected.Id);
        await growthPanel.ShowAsync();
        await growthPanel.WaitUntilClosedAsync();
        DockClose();   // 원상 복구.
        // 육성 패널이 닫히면 상세 패널을 갱신(스탯/전투력/별레벨 변경 반영).
        RefreshDetail();
    }

    private async UniTaskVoid OpenEquipmentAsync() {
        if (_selected == null) { _ctx.Ui.ShowToast("캐릭터를 선택하세요."); return; }
        if (equipmentPanel == null) return;
        DockOpen(shrinkCardScroll: false);   // 장비 패널도 육성과 동일 크기라 같은 방식으로 공간 확보.
        equipmentPanel.Initialize(_ctx, _selected.Id);
        await equipmentPanel.ShowAsync();
        await equipmentPanel.WaitUntilClosedAsync();
        DockClose();   // 원상 복구.
        // 장비 변경이 능력치에 영향을 주므로 상세를 갱신.
        RefreshDetail();
    }

    private async UniTaskVoid OpenCostumeAsync() {
        if (_selected == null) { _ctx.Ui.ShowToast("캐릭터를 선택하세요."); return; }
        if (costumePanel == null) return;
        DockOpen(shrinkCardScroll: false);   // 코스튬 패널도 육성/장비와 같은 크기라 같은 방식으로 공간 확보.
        costumePanel.Initialize(_ctx, _selected.Id);
        await costumePanel.ShowAsync();
        await costumePanel.WaitUntilClosedAsync();
        DockClose();   // 원상 복구.
        // 코스튬은 일러스트와 능력치를 함께 바꾸므로 상세를 통째로 다시 그린다.
        RefreshDetail();
        RefreshCardsAppearance();
    }

    private async UniTaskVoid OpenUnitSkinAsync() {
        if (_selected == null) { _ctx.Ui.ShowToast("캐릭터를 선택하세요."); return; }
        if (unitSkinPanel == null) return;
        DockOpen(shrinkCardScroll: false);   // 코스튬 패널과 같은 방식으로 공간 확보.
        unitSkinPanel.Initialize(_ctx, _selected.Id);
        await unitSkinPanel.ShowAsync();
        await unitSkinPanel.WaitUntilClosedAsync();
        DockClose();
        // 아웃핏은 능력치를 바꾸므로 상세를 다시 그린다(유닛 모습은 다음 전투부터 바뀐다).
        RefreshDetail();
    }

    // 코스튬이 바뀐 캐릭터의 그리드 카드 일러스트를 제자리 갱신한다(재정렬/재생성 없이 스크롤 유지).
    private void RefreshCardsAppearance() {
        if (_selected == null) return;
        foreach (var card in _cards) {
            if (card == null || card.Data == null || card.Data.Id != _selected.Id) continue;
            card.RefreshAppearance(_ctx.Appearance.Mine(card.Data));
        }
    }

    // 편성 패널이 열려 있는 동안에만 그리드 카드를 드래그 출발점으로 쓴다.
    // 평소에는 null 이라 카드가 드래그를 집지 않고, 목록 스크롤이 그대로 동작한다.
    //
    // 패널의 activeSelf 로 판단하면 안 된다. UIView.CloseAsync 는 닫힘 대기를 먼저 풀고 그 다음에
    // 오브젝트를 비활성화하므로, 닫힌 직후에도 잠깐 활성으로 보여 드래그가 계속 켜진 채 남는다.
    private CompositionDragHandle.IHost RosterDragHost => _compositionOpen ? compositionPanel : null;

    // 그리드의 모든 카드에 현재 드래그 출발점 설정을 반영한다(패널을 열고 닫는 순간).
    private void ApplyRosterDragHost() {
        var host = RosterDragHost;
        foreach (var card in _cards) if (card != null) card.SetDragSource(host, CompositionDragHandle.RosterSlot);
    }

    /// <summary>
    /// 편성 패널을 연다. 편성을 목적으로 이 팝업을 연 곳(프로필의 덱 열기 등)이 팝업을 띄운 뒤 이어 부른다.
    /// 편성 버튼을 누른 것과 완전히 같은 경로를 타므로, 여는 방법이 둘로 갈리지 않는다.
    /// </summary>
    public void OpenComposition() => OpenCompositionAsync().Forget();

    private async UniTaskVoid OpenCompositionAsync() {
        if (compositionPanel == null) return;
        DockOpen(shrinkCardScroll: false);   // 편성 패널도 육성/장비와 같은 크기라 같은 방식으로 공간 확보.
        _compositionEdited = false;
        compositionPanel.Changed += OnCompositionChanged;
        compositionPanel.Initialize(_ctx, _session);
        await compositionPanel.ShowAsync();

        _compositionOpen = true;
        ApplyRosterDragHost();   // 패널이 켜진 뒤라야 그리드 카드가 드래그를 집는다.

        await compositionPanel.WaitUntilClosedAsync();

        _compositionOpen = false;
        ApplyRosterDragHost();   // 닫혔으므로 드래그를 끈다(목록 스크롤 복구).
        compositionPanel.Changed -= OnCompositionChanged;
        DockClose();   // 원상 복구.

        // 편성이 실제로 바뀐 경우에만 그리드를 다시 정렬한다.
        //
        // 평소에는 정렬을 막아 둔다. 편집 도중에 카드가 손 밑에서 자리를 옮기면 방금 누른 칸이 다른 캐릭터가
        // 되어 버리기 때문이다. 하지만 편성을 마치고 패널을 닫는 이 순간만은 손이 그리드를 떠나 있고,
        // 대표와 편성 순서가 막 바뀐 직후라 새 순서로 보여 주는 편이 낫다.
        //
        // 스크롤은 복원하지 않는다. 순서가 통째로 달라졌으므로 이전 위치는 더 이상 같은 곳을 가리키지 않는다
        // (맨 위에서 시작하면 대표가 바로 보인다).
        if (_compositionEdited) RebuildGrid();
        else RefreshCardsState();

        RefreshEditButtons();
        RefreshDetail();
    }

    // 편성 패널이 편집할 때마다 온다.
    //
    // 서버 확정을 패널 닫을 때까지 미루면, 편성을 다 해 놓고도 "스쿼드 구성" 같은 퀘스트가 완료되지 않아
    // 유저가 무엇이 남았는지 알 수 없다. 그래서 편집이 잠잠해지는 즉시 확정한다.
    // 다만 카드를 옮길 때마다 보내면 자리 맞바꾸는 동안 패킷이 연달아 나가므로, 잠깐 묶어서 한 번만 보낸다.
    //
    // 그리드 재정렬은 여기서 하지 않는다(닫을 때만). 편집 도중에 카드가 손 밑에서 자리를 옮기면
    // 방금 집으려던 칸이 다른 소녀가 되어 버린다.
    private void OnCompositionChanged() {
        _compositionEdited = true;
        _deckChangedWhileOpen = true;
        RefreshCardsState();
        RefreshEditButtons();
        _deckCommitRequest?.OnNext(Unit.Default);
    }

    // 편집이 잠잠해진 뒤의 조기 확정. 진행 중인 런에 끼워 넣지 않으므로(immediate: false) 이미 싸우고 있는
    // 런의 보상 계산은 그대로 두고, 새 편성은 다음 런부터 적용된다. 진행 중인 런에 즉시 반영하는 처리는
    // 지금까지처럼 팝업을 닫을 때 한 번만 한다(런을 버리고 재투입하는 동작이라 편집 중에 할 일이 아니다).
    private async UniTaskVoid CommitEditedDeckAsync() {
        if (!_session.IsDirty) return;
        await CommitDeckAsync(immediate: false);
    }

    // 편성 프리셋 팝업. 프리셋을 적용하면 서버 덱이 통째로 바뀌므로, 편집 중이던 작업본을 버리고
    // 스토어 값 기준으로 화면을 다시 만든다(작업본을 남기면 닫을 때 옛 편성이 다시 커밋된다).
    private async UniTaskVoid OpenPresetAsync() {
        // 저장은 "서버에 저장된 현재 덱"을 복사하는 동작이라, 아직 커밋하지 않은 편집이 있으면 먼저 확정해야
        // 사용자가 화면에서 본 편성이 그대로 저장된다.
        if (_session.IsDirty && !await CommitDeckAsync(false)) return;

        var popup = await _ctx.Ui.ShowPopupAsync<DeckPresetPopup>(_ctx.Popups.DeckPresetPopup, _ct);
        popup.Applied += OnPresetApplied;
        popup.Initialize(_ctx);
        await popup.ShowAsync();
        await popup.WaitUntilClosedAsync();
        popup.Applied -= OnPresetApplied;
    }

    // 아이템 사용 팝업. 경험치 쿠폰처럼 레벨을 올리는 아이템이 있으므로 닫힌 뒤 상세를 다시 그린다.
    private async UniTaskVoid OpenItemAsync() {
        if (_selected == null) { _ctx.Ui.ShowToast("캐릭터를 선택하세요."); return; }

        var popup = await _ctx.Ui.ShowPopupAsync<ItemUsePopup>(_ctx.Popups.ItemUsePopup, _ct);
        popup.Initialize(_ctx, _selected.Id);
        await popup.ShowAsync();
        await popup.WaitUntilClosedAsync();

        if (popup.Changed) RefreshDetail();
    }

    private void OnPresetApplied() {
        // 서버가 확정한 덱을 작업본으로 다시 가져오고, 정렬까지 반영해 그리드를 새로 만든다.
        RevertWorkingFromStore();
        RebuildGrid(preserveScroll: true);
    }

    // ── 도킹 슬라이드(우측에 육성/장비 팝업이 열릴 때 좌측 패널들을 밀어 공간 확보) ─────
    // 목표 좌표는 "열 때마다" 현재 해상도의 실제 레이아웃에서 다시 계산한다. 고정 오프셋을 쓰면
    // 화면비마다 착지점이 달라지므로, 착지 자리는 에디터에 배치한 슬롯(illustDockedSlot)으로 지정하고
    // 코드는 그 슬롯 중심에 일러스트 중심을 맞추기만 한다.
    private void CaptureDockHomes() {
        if (_dockHomesCaptured) return;
        if (infoPanel != null) {
            _infoHome = infoPanel.anchoredPosition;
            _infoGroup = infoPanel.GetComponent<CanvasGroup>();
        }
        if (illustPanel != null) _illustHome = illustPanel.anchoredPosition;
        if (scrollDockedEdge != null && Scroll != null) {
            _scrollRt = (RectTransform)Scroll.transform;
            _scrollHomeOffsetMax = _scrollRt.offsetMax;
        }
        _dockHomesCaptured = true;
    }

    // shrinkCardScroll: 패널이 카드 스크롤뷰를 덮는 경우에만 true. 덮지 않는 패널은 스크롤뷰를 그대로 둔다.
    private void DockOpen(bool shrinkCardScroll) {
        if (_docked) return;   // 이미 도킹된 상태면 중복 실행 방지.
        // 레이아웃이 확정된 뒤 측정해야 첫 오픈에서도(세이프에어리어/레이아웃 그룹 반영 전) 좌표가 정확하다.
        Canvas.ForceUpdateCanvases();
        CaptureDockHomes();
        _docked = true;
        _dockTween?.Kill();

        var seq = DOTween.Sequence().SetUpdate(true);
        if (illustPanel != null) {
            seq.Join(illustPanel.DOAnchorPos(IllustDockedPos(), dockSlideDuration).SetEase(Ease.InOutCubic));
        }
        // 정보 패널: 화면(루트 캔버스) 왼쪽 밖으로 완전히 퇴장. CanvasGroup 이 있으면 함께 페이드.
        if (infoPanel != null) {
            seq.Join(infoPanel.DOAnchorPos(InfoOffscreenPos(), dockSlideDuration).SetEase(Ease.InOutCubic));
            if (_infoGroup != null) seq.Join(_infoGroup.DOFade(0f, dockSlideDuration));
        }
        _scrollDocked = shrinkCardScroll && _scrollRt != null;
        if (_scrollDocked) seq.Join(ScrollRightTween(ScrollDockedOffsetMaxX()));
        _dockTween = seq;
    }

    private void DockClose() {
        if (!_docked) return;
        _docked = false;
        _dockTween?.Kill();
        var seq = DOTween.Sequence().SetUpdate(true);
        if (infoPanel != null) {
            seq.Join(infoPanel.DOAnchorPos(_infoHome, dockSlideDuration).SetEase(Ease.InOutCubic));
            if (_infoGroup != null) seq.Join(_infoGroup.DOFade(1f, dockSlideDuration));
        }
        if (illustPanel != null)
            seq.Join(illustPanel.DOAnchorPos(_illustHome, dockSlideDuration).SetEase(Ease.InOutCubic));
        if (_scrollDocked) seq.Join(ScrollRightTween(_scrollHomeOffsetMax.x));
        _scrollDocked = false;
        _dockTween = seq;
    }

    // 도킹 중 해상도/화면 회전/세이프에어리어가 바뀌면 착지점이 달라지므로 즉시 다시 맞춘다.
    private void OnRectTransformDimensionsChange() {
        if (!_docked || !_dockHomesCaptured) return;
        _dockTween?.Kill();
        _dockTween = null;
        if (illustPanel != null) illustPanel.anchoredPosition = IllustDockedPos();
        if (infoPanel != null) infoPanel.anchoredPosition = InfoOffscreenPos();
        if (_scrollDocked) _scrollRt.offsetMax = new Vector2(ScrollDockedOffsetMaxX(), _scrollRt.offsetMax.y);
    }

    // 카드 스크롤뷰의 오른쪽 끝만 움직인다(왼쪽 끝과 카드 배치는 그대로, 보이는 폭만 줄었다 늘어난다).
    private Tween ScrollRightTween(float targetX) =>
        DOTween.To(() => _scrollRt.offsetMax.x,
                   x => _scrollRt.offsetMax = new Vector2(x, _scrollRt.offsetMax.y),
                   targetX, dockSlideDuration).SetEase(Ease.InOutCubic);

    // 도킹 중 스크롤뷰의 offsetMax.x. 오른쪽 끝이 scrollDockedEdge 의 오른쪽 경계에 닿는 값이다.
    // 트윈 중에 호출되어도 같은 값이 나오도록 홈 기준으로 역산한다(AnchoredPosForWorldCenter 와 같은 방식).
    private float ScrollDockedOffsetMaxX() {
        var parent = _scrollRt.parent as RectTransform;
        if (parent == null || scrollDockedEdge == null) return _scrollHomeOffsetMax.x;

        scrollDockedEdge.GetWorldCorners(_corners);
        float edgeRight = parent.InverseTransformPoint(_corners[2]).x;
        _scrollRt.GetWorldCorners(_corners);
        float rightAtHome = parent.InverseTransformPoint(_corners[2]).x + (_scrollHomeOffsetMax.x - _scrollRt.offsetMax.x);
        return _scrollHomeOffsetMax.x + (edgeRight - rightAtHome);
    }

    // 일러스트 패널의 도킹 목표. 슬롯이 있으면 슬롯 중심에 정확히 맞추고,
    // 없으면 정보 패널 자리(가로만)로 폴백한다.
    private Vector2 IllustDockedPos() {
        if (illustDockedSlot != null)
            return AnchoredPosForWorldCenter(illustPanel, _illustHome, WorldCenter(illustDockedSlot));
        if (infoPanel == null) return _illustHome;
        Vector2 fallback = AnchoredPosForWorldCenter(illustPanel, _illustHome, WorldCenter(infoPanel));
        fallback.y = _illustHome.y;   // 폴백은 가로 이동만(세로는 홈 유지).
        return fallback;
    }

    // 정보 패널의 퇴장 목표(루트 캔버스 왼쪽 바깥). 패널 우측 경계를 캔버스 좌측 경계 밖으로 보낸다.
    private Vector2 InfoOffscreenPos() {
        var canvas = infoPanel.GetComponentInParent<Canvas>();
        if (canvas == null) return _infoHome;

        var canvasRect = (RectTransform)canvas.rootCanvas.transform;
        canvasRect.GetWorldCorners(_corners);
        float canvasLeftX = _corners[0].x;

        infoPanel.GetWorldCorners(_corners);
        float halfWidth = (_corners[2].x - _corners[0].x) * 0.5f;

        // 목표 중심의 월드 X 는 현재 위치와 무관한 절대값이라, 트윈 중에 호출해도 같은 결과가 나온다.
        Vector3 target = WorldCenter(infoPanel);
        target.x = canvasLeftX - halfWidth;
        Vector2 anchored = AnchoredPosForWorldCenter(infoPanel, _infoHome, target);
        anchored.x -= dockOffscreenMargin;
        return anchored;
    }

    private static Vector3 WorldCenter(RectTransform rt) => rt.TransformPoint(rt.rect.center);

    /// <summary>
    /// rt 의 중심이 지정한 월드 좌표에 오도록 하는 anchoredPosition.
    /// 현재 위치가 아니라 home 을 기준으로 역산하므로, 트윈이 재생 중인(중간 위치인) 패널에 대해서도
    /// 항상 같은 값이 나온다. 패널을 실제로 움직이지 않고 값만 계산한다.
    /// </summary>
    private static Vector2 AnchoredPosForWorldCenter(RectTransform rt, Vector2 home, Vector3 worldCenter) {
        var parent = rt.parent as RectTransform;
        if (parent == null) return home;
        // anchoredPosition 의 변화량은 부모 로컬 좌표의 변화량과 1:1 이므로, 현재 중심에서 홈일 때의 중심을 역산한다.
        Vector2 currentCenterLocal = parent.InverseTransformPoint(WorldCenter(rt));
        Vector2 centerAtHome = currentCenterLocal + (home - rt.anchoredPosition);
        Vector2 targetLocal = parent.InverseTransformPoint(worldCenter);
        return home + (targetLocal - centerAtHome);
    }

    // 닫기 요청. 편집 내용은 여기서 처음으로 확정된다(편집 중에는 서버/스토어가 바뀌지 않는다).
    //
    // 예전에는 진행 중인 런이 있으면 즉시/예약/취소를 물었다. 기력을 던전 "시작"에 걷던 시절에는
    // 런을 버리고 재시작하는 것이 곧 기력 손실이라 유저에게 물어볼 값어치가 있었기 때문이다.
    // 지금은 기력을 "클리어"에만 걷으므로 진행 중인 런을 버려도 잃는 것이 없다. 그래서 묻지 않고
    // 바로 확정한 뒤 새 편성으로 다시 시작한다(현재 런은 보상 없이 버려지므로, 실제로 싸우지 않은
    // 새 편성원에게 경험치가 지급되는 문제도 생기지 않는다).
    private async UniTaskVoid OnCloseRequestedAsync() {
        bool resting = _ctx.Battle.IsResting.Value;

        // 편집 중 조기 확정(OnCompositionChanged)으로 작업본이 이미 깨끗해졌을 수 있다. 그 확정은
        // "다음 런부터"(immediate: false)라 진행 중인 런에는 아직 반영되지 않았으므로, 런이 돌고 있으면
        // 여기서 한 번 더 즉시 반영으로 확정한다. 휴식 중이면 다음 런에 자연히 반영되므로 다시 보내지 않는다.
        bool needsCommit = _session.IsDirty || (_deckChangedWhileOpen && !resting);
        if (needsCommit && !await CommitDeckAsync(immediate: !resting)) return;

        // 진행 중인 런이 있었다면 새 편성으로 즉시 재투입한다(휴식 중이면 다음 런에 자연히 반영된다).
        if (_deckChangedWhileOpen && !resting) _ctx.Battle.SwitchDungeon(_ctx.Store.CurrentDungeonIndex.Value);

        await CloseAsync();
    }

    protected override void OnClosed() {
        if (_ctx != null) _ctx.Store.CharactersChanged -= OnCharactersChanged;
        KillIllustration();
        _dockTween?.Kill();
        _dockTween = null;
        _gridCts?.Cancel();
        _gridCts?.Dispose();
        _gridCts = null;
        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();
    }

    private void OnDestroy() {
        // 팝업이 파괴될 때 이벤트 누수를 막는다(OnClosed 를 거치지 않고 파괴되는 경우 대비).
        if (_ctx != null) _ctx.Store.CharactersChanged -= OnCharactersChanged;
    }
}
