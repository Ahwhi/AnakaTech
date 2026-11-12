using DG.Tweening;
using UnityEngine;

/// <summary>
/// 전투 유닛의 마스크 일러스트 표현부(재사용). 일러스트/프레임/그림자 SpriteRenderer 와 전용 SpriteMask 를 묶어
/// 스프라이트 적용, 스폰 슬롯별 정렬순서 격리(인접 유닛과 마스크가 섞이지 않게), 피격 깜빡임을 담당한다.
/// 유닛의 '행동'(BattleUnit)과 '표현'을 분리한다: 유닛은 무엇을 보여줄지만 지시하고, 어떻게 보여줄지는 이 컴포넌트가 안다.
/// 덕분에 CharacterUnit/MonsterUnit 등 어느 전투 유닛이든 이 컴포넌트를 참조해 동일한 프리팹 구조를 공유할 수 있다.
///
/// 프레이밍은 항상 데이터의 정규화 focus 로 유도한다(프리팹 Transform 을 손으로 잡는 경로는 없다).
/// 아래 인스펙터 값은 '슬롯 규격'(창 크기/중심/기준 배율/앵커)이라 이 프리팹을 쓰는 모든 개체의 공통값이고,
/// 개체별 차이(관심점/크기)는 데이터가 IllustFocus 로 공급한다.
/// </summary>
public sealed class BattleUnitIllustration : MonoBehaviour {
    [Tooltip("일러스트 렌더러(마스크 내부 표시). 스프라이트/스케일/위치가 적용된다.")]
    [SerializeField] private SpriteRenderer illustRenderer;
    [Tooltip("프레임 렌더러(마스크 내부 표시). 선택.")]
    [SerializeField] private SpriteRenderer frameRenderer;
    [Tooltip("그림자 렌더러(마스크 외부 표시). 선택.")]
    [SerializeField] private SpriteRenderer shadowRenderer;
    [Tooltip("이 유닛 전용 스프라이트 마스크. 스폰 슬롯별 정렬순서 범위로 인접 유닛과 격리한다. 선택.")]
    [SerializeField] private SpriteMask spriteMask;

    [Header("Focus Framing (슬롯 규격)")]
    [Tooltip("focus 기반 프레이밍 창의 로컬 크기(illust 부모 기준). UI 슬롯의 '마스크 창'에 해당하는 전투용 기준 사각형.")]
    [SerializeField] private Vector2 focusFrameSize = new Vector2(4f, 4f);
    [Tooltip("프레이밍 창의 로컬 중심(illust 부모 기준). 보통 (0,0).")]
    [SerializeField] private Vector2 focusFrameCenter = Vector2.zero;
    [Tooltip("illust 높이 = 창 높이 x 이 값 x 데이터의 CoverScale. 1이면 창 높이에 꽉, 키우면 얼굴 클로즈업. 여기 값은 이 프리팹을 쓰는 모든 개체의 공통 기준이다.")]
    [SerializeField] private float focusCoverScale = 1f;
    [Tooltip("focus 점이 창 안에서 놓일 위치(정규화). (0.5,0.5)=중앙, y를 올리면 얼굴이 위로.")]
    [SerializeField] private Vector2 focusAnchor = new Vector2(0.5f, 0.5f);

    // 마스크 격리 시 유닛 1개가 점유하는 정렬순서 폭(Shadow/Illust/Frame 3칸 + 꾸밈/팀 표시 등 여유).
    // 아웃핏 프리팹은 꾸밈 스프라이트가 늘어날 수 있어 넉넉히 둔다. 넘치면 다음 유닛 대역을 침범하므로 경고한다.
    // (가장 큰 슬롯 x 이 값이 정렬순서 상한 32767 을 넘지 않아야 한다.)
    private const int MaskOrderBand = 20;

    // 피격 깜빡임 대상(Illust/Frame/Shadow)과 각자의 원래 알파값 캐시.
    private readonly SpriteRenderer[] _blinkTargets = new SpriteRenderer[3];
    private readonly float[] _blinkBaseAlpha = new float[3];

    /// <summary>
    /// 정규화 focus 만으로 프레이밍한다(크기 배율은 프리팹 기준값 그대로).
    /// 개체별 크기차가 없는 캐릭터 경로(CharacterData.UiFocus)가 사용한다.
    /// </summary>
    public void Apply(Sprite sprite, Vector2 focus) =>
        Apply(sprite, new IllustFocus { Point = focus, CoverScale = 1f });

    /// <summary>
    /// 데이터의 프레이밍 값(IllustFocus)으로 스프라이트를 적용한다(UI 초상과 단일 진실 원천 공유).
    /// 프레이밍 창(focusFrame*) 대비 uniform 스케일로 크기를 잡고, 스프라이트의 focus 점이
    /// 창 안 focusAnchor 위치에 오도록 localPosition 을 계산한다. 원본 픽셀수/PPU 에 무관하게 항상 같은 부위가 잡힌다.
    /// 최종 크기 = 창 높이 x 프리팹 focusCoverScale(슬롯 규격) x 데이터 CoverScale(개체차) 이므로,
    /// 하나의 본체 프리팹을 여러 개체가 공유하면서 개체마다 다른 크기를 낼 수 있다.
    /// </summary>
    public void Apply(Sprite sprite, IllustFocus focus) {
        if (illustRenderer != null && sprite != null) {
            illustRenderer.sprite = sprite;

            Vector2 spriteSize = sprite.bounds.size;   // scale 1 기준 월드 크기(pivot 중심).
            if (spriteSize.y > 0f) {
                float s = focusFrameSize.y * focusCoverScale * focus.SafeCoverScale / spriteSize.y;

                // SpriteRenderer 는 스프라이트의 pivot 을 transform 위치에 놓는다. focus 점의 pivot 대비 오프셋을 보정.
                Vector2 pivotNorm = new Vector2(sprite.pivot.x / sprite.rect.width,
                                                sprite.pivot.y / sprite.rect.height);
                Vector2 focusOffset = new Vector2((focus.Point.x - pivotNorm.x) * spriteSize.x,
                                                  (focus.Point.y - pivotNorm.y) * spriteSize.y) * s;

                Vector2 anchorPoint = focusFrameCenter + new Vector2(
                    (focusAnchor.x - 0.5f) * focusFrameSize.x,
                    (focusAnchor.y - 0.5f) * focusFrameSize.y);

                var t = illustRenderer.transform;
                t.localScale = new Vector3(s, s, 1f);
                Vector2 pos = anchorPoint - focusOffset;   // focus 점이 anchorPoint 에 정확히 놓인다.
                t.localPosition = new Vector3(pos.x, pos.y, t.localPosition.z);
            }
        }
        CacheBlinkTargets();
    }

    /// <summary>
    /// 이 유닛만의 정렬순서 대역을 부여하고 마스크의 커스텀 범위를 그 대역으로 좁혀,
    /// 인접한 다른 유닛의 스프라이트가 이 마스크에 걸리지 않도록 격리한다.
    /// 같은 정렬 레이어에 마스크 유닛이 여럿일 때는 유닛마다 유일한 slot 을 넘겨야 한다.
    /// 명시적으로 연결된 그림자/일러스트/프레임뿐 아니라, 프리팹에서 illust 컴포넌트에
    /// 연결되지 않았지만 마스크에 반응하는 자식 렌더러(예: 미연결 프레임)도 같은 대역으로 끌어와,
    /// 인접 유닛의 마스크에 잡혀 새어 나오는 것(스프라이트 겹침)을 원천 차단한다.
    /// </summary>
    /// <param name="scope">
    /// 격리할 범위(보통 유닛 루트). 비우면 이 컴포넌트 아래만 본다. 유닛 루트를 넘기면 일러스트 밖에 둔
    /// 꾸밈/팀 표시 렌더러와 마스크까지 같은 대역으로 묶인다.
    /// </param>
    public void IsolateMask(int slot, Transform scope = null) {
        int baseOrder = slot * MaskOrderBand;
        int layer = illustRenderer != null ? illustRenderer.sortingLayerID : 0;
        var root = scope != null ? scope : transform;

        // 뒤 -> 앞 순서(그림자 -> 일러스트 -> 프레임)를 유지하며 대역의 앞쪽으로 순차 배치.
        int next = baseOrder;
        next = PlaceInBand(shadowRenderer, layer, next);   // 가장 뒤(그림자)
        next = PlaceInBand(illustRenderer, layer, next);
        next = PlaceInBand(frameRenderer,  layer, next);   // 가장 앞(프레임)

        // 위에서 처리하지 못한(프리팹 미연결) 마스크 반응 렌더러도 같은 대역 안으로 격리한다.
        // 이것이 없으면 미연결 렌더러가 프리팹 기본 정렬순서에 남아 다른 유닛의 마스크에 걸린다.
        var all = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < all.Length; i++) {
            var r = all[i];
            if (r == null || r == shadowRenderer || r == illustRenderer || r == frameRenderer) continue;
            if (r.maskInteraction == SpriteMaskInteraction.None) continue;   // 마스크와 무관한 렌더러는 건드리지 않음
            SetSorting(r, layer, next);
            next++;
        }

        int topOrder = Mathf.Max(baseOrder, next - 1);   // 이 유닛이 실제로 점유한 마지막 정렬순서
        if (topOrder >= baseOrder + MaskOrderBand)
            Debug.LogWarning($"[BattleUnitIllustration] {root.name}: 마스크 반응 렌더러가 {topOrder - baseOrder + 1}개라 " +
                             $"대역({MaskOrderBand})을 넘었습니다. 옆 유닛 일러스트와 섞일 수 있습니다.", this);

        // 주 마스크뿐 아니라 프리팹 안의 모든 마스크(꾸밈/팀 표시용 마스크 등)를 이 유닛 대역으로 좁힌다.
        // 범위를 좁히지 않은 마스크는 같은 정렬 레이어의 모든 유닛 일러스트를 잡아 그 모양대로 비춰 낸다.
        var masks = root.GetComponentsInChildren<SpriteMask>(true);
        for (int i = 0; i < masks.Length; i++) Confine(masks[i], layer, baseOrder, topOrder);
        if (spriteMask != null) Confine(spriteMask, layer, baseOrder, topOrder);
    }

    private static void Confine(SpriteMask mask, int layer, int back, int front) {
        if (mask == null) return;
        mask.isCustomRangeActive = true;
        mask.frontSortingLayerID = layer;
        mask.backSortingLayerID  = layer;
        mask.frontSortingOrder   = front;
        mask.backSortingOrder    = back;
    }

    // 렌더러가 있으면 지정 정렬순서를 부여하고 다음 순서를 반환한다. 없으면 순서를 소비하지 않는다.
    private static int PlaceInBand(SpriteRenderer r, int layer, int order) {
        if (r == null) return order;
        SetSorting(r, layer, order);
        return order + 1;
    }

    /// <summary>스프라이트 알파를 낮췄다 되돌리며 깜빡이는 피격 연출.</summary>
    public void PlayHitBlink(float alphaFactor, float duration, int loops) {
        for (int i = 0; i < _blinkTargets.Length; i++) {
            var r = _blinkTargets[i];
            if (r == null) continue;
            float baseA = _blinkBaseAlpha[i];
            r.DOKill();
            SetAlpha(r, baseA);
            r.DOFade(baseA * alphaFactor, duration)
             .SetLoops(Mathf.Max(1, loops) * 2, LoopType.Yoyo)
             .OnComplete(() => SetAlpha(r, baseA));
        }
    }

    /// <summary>깜빡임 트윈을 정리하고 알파를 원래대로 복구한다(사망/부활 시).</summary>
    public void StopBlink() {
        for (int i = 0; i < _blinkTargets.Length; i++) {
            var r = _blinkTargets[i];
            if (r == null) continue;
            r.DOKill();
            SetAlpha(r, _blinkBaseAlpha[i]);
        }
    }

    private void CacheBlinkTargets() {
        // 깜빡임 진행 중에 Apply(일러스트 교체: 변신 등)가 불리면 낮아진 중간 알파가 "원래 알파"로
        // 캐시되어 이후 깜빡임/복구 기준이 계속 어두워진다(래칫). 먼저 트윈을 정리하고
        // 이전 캐시 기준으로 알파를 원복한 뒤 캐시해 항상 온전한 기준값만 저장한다.
        StopBlink();

        _blinkTargets[0] = illustRenderer;
        _blinkTargets[1] = frameRenderer;
        _blinkTargets[2] = shadowRenderer;
        for (int i = 0; i < _blinkTargets.Length; i++)
            _blinkBaseAlpha[i] = _blinkTargets[i] != null ? _blinkTargets[i].color.a : 1f;
    }

    private static void SetSorting(SpriteRenderer r, int layer, int order) {
        if (r == null) return;
        r.sortingLayerID = layer;
        r.sortingOrder = order;
    }

    private static void SetAlpha(SpriteRenderer r, float a) {
        var c = r.color;
        c.a = a;
        r.color = c;
    }
}
