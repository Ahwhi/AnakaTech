using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일러스트 슬롯(재사용). 아트(캐릭터 일러스트/장비 아이콘 등)를 슬롯(마스크 프레임) 안에서 보여준다.
///
/// 프레이밍은 두 축으로 분리된다:
///   - 아트 축(IIllustrationSource): 그림 안의 관심점(얼굴 등)과 프레이밍 방식(Crop/Contain). 그림이 스스로 선언한다.
///   - 맥락 축(이 슬롯): 얼마나 크게(coverScale/containScale), 슬롯 어디에 관심점을 둘지(focusAnchor).
/// 크기와 위치를 전부 "마스크 크기 기준"의 상대값으로 잡으므로(원본 픽셀수/화면 해상도에 의존하지 않음),
/// 같은 그림이 작은 원형 초상이든 길쭉한 가챠 카드든 항상 같은 부위가 잡힌다.
///
/// 동작 원리(Crop): 자식 Image 의 pivot 을 아트의 관심점으로, anchor 를 슬롯의 focusAnchor 로 두면
///   관심점이 슬롯의 focusAnchor 위치에 정확히 놓인다(anchoredPosition = 0).
///   크기는 일러 높이 = 마스크 높이 x coverScale, 폭은 종횡비 유지. 마스크가 나머지를 클리핑한다.
/// 동작 원리(Contain): 그림 전체가 마스크 안에 들어오는 최대 크기로 맞추고 중앙에 놓는다.
///   "전체가 보인다" 는 보장이 크기와 위치를 모두 확정하므로 관심점/focusAnchor 는 쓰지 않는다.
///
/// 덕분에 캐릭터와 장비를 같은 슬롯에 섞어 넣어도 각자 올바르게 잡힌다(가챠 확률표/로테이션 목록 등).
/// 슬롯은 IIllustrationSource 만 알고 데이터 종류는 모른다.
///
/// 내부적으로 일러스트를 그릴 자식 Image 를 1회 생성해 재사용하므로, Bind 는 sprite/focus/fit 만 교체한다.
///
/// 사용법(에디터):
///   1) 일러스트가 들어갈 슬롯 오브젝트(RectTransform)에 원하는 마스크를 둔다.
///      - 원형: Image(원형 스프라이트) + Mask 컴포넌트
///      - 사각/둥근사각: RectMask2D
///   2) 같은 오브젝트에 이 컴포넌트를 붙인다(일러스트 Image 를 자식으로 생성하므로 마스크가 클리핑한다).
///   3) 슬롯 성격에 맞게 coverScale/focusAnchor 를 인스펙터에서 맞춘다(얼굴 초상=coverScale 크게, 전신 카드=1 근처).
///      아이콘도 들어오는 슬롯이면 containScale 로 여백을 잡는다.
///   4) 코드에서 slot.Bind(characterData) / slot.Bind(equipmentData) 호출. 교체 시 다시 Bind, 비울 때 Clear.
///
/// 화질은 이 컴포넌트가 아니라 원본 아트 해상도/Import 설정/CanvasScaler 로 결정된다.
/// coverScale 로 크게 확대하는 초상 슬롯은 원본 일러스트가 그만큼 커야 업스케일 블러가 없다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class CharacterIllustrationView : MonoBehaviour {
    [Header("Crop (캐릭터 일러스트 등 잘라 쓰는 그림)")]
    [Tooltip("일러스트 높이 = 마스크(슬롯) 높이 x 이 값. 1이면 슬롯 높이에 꽉 맞춤(전신형), 키우면 확대(얼굴 초상).")]
    [SerializeField] private float coverScale = 1f;
    [Tooltip("아트의 관심점이 슬롯 안에서 놓일 위치(정규화). (0.5,0.5)=중앙, y를 올리면 관심점(얼굴)이 위쪽에 온다. Contain 에서는 쓰지 않는다.")]
    [SerializeField] private Vector2 focusAnchor = new Vector2(0.5f, 0.5f);

    [Header("Contain (아이콘 등 전체가 보여야 하는 그림)")]
    [Tooltip("슬롯에 꽉 맞춘 크기 대비 배율. 1이면 슬롯에 닿게, 낮추면 사방 여백이 생긴다. 잘림을 보장상 막기 위해 1을 넘길 수 없다.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float containScale = 1f;

    [Header("Fallback (IIllustrationSource 없이 스프라이트만 Bind 할 때)")]
    [Tooltip("원본 데이터 없이 스프라이트만 받았을 때 적용할 프레이밍 방식.")]
    [SerializeField] private IllustrationFit defaultFit = IllustrationFit.Crop;
    [Tooltip("원본 데이터 없이 스프라이트만 받았을 때 적용할 관심점. Crop 일 때만 의미가 있다.")]
    [SerializeField] private Vector2 defaultFocus = new Vector2(0.5f, 0.5f);

    [Header("Shadow")]
    [Tooltip("생성되는 일러스트 Image 에 Shadow 를 적용할지 여부. 기본은 그림자 없음.")]
    [SerializeField] private bool useShadow = false;
    [Tooltip("그림자 색(알파 포함).")]
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 128f / 255f);
    [Tooltip("그림자 오프셋(px).")]
    [SerializeField] private Vector2 shadowEffectDistance = new Vector2(10f, -1f);
    [Tooltip("일러스트의 알파를 그림자에 곱할지 여부.")]
    [SerializeField] private bool shadowUseGraphicAlpha = true;

    private RectTransform _rt;
    private Image _image;
    private Shadow _shadow;
    private Vector2 _focus = new Vector2(0.5f, 0.5f);
    private IllustrationFit _fit = IllustrationFit.Crop;

    private RectTransform Rt => _rt != null ? _rt : (_rt = (RectTransform)transform);

    /// <summary>아트 원본(캐릭터/장비 등)을 슬롯에 표시한다. 관심점과 프레이밍 방식은 원본이 결정한다.</summary>
    public void Bind(IIllustrationSource source) {
        // 인터페이스 참조로는 UnityEngine.Object 의 null 판정 오버로드가 동작하지 않는다.
        // 파괴/미할당된 에셋(fake null)에 접근해 예외가 나지 않도록 Object 로 한 번 더 확인한다.
        bool missing = source == null || (source is Object obj && obj == null);
        if (missing) { BindInternal(null, defaultFocus, defaultFit); return; }

        BindInternal(source.Illustration, source.UiFocus, source.Fit);
    }

    /// <summary>임의의 스프라이트를 슬롯에 표시한다(원본 데이터를 알 수 없는 경우). 슬롯의 기본 focus/fit 을 쓴다.</summary>
    public void Bind(Sprite sprite) => BindInternal(sprite, defaultFocus, defaultFit);

    /// <summary>임의의 스프라이트를 지정 관심점으로 크롭 표시한다. 런타임 외형 교체(변신 초상 등)처럼
    /// 데이터 에셋이 아닌 곳에서 스프라이트/focus 짝이 오는 경우에 사용한다.</summary>
    public void Bind(Sprite sprite, Vector2 focus) => BindInternal(sprite, focus, IllustrationFit.Crop);

    /// <summary>
    /// 슬롯의 프레이밍 값(맥락 축)을 코드에서 지정한다. 프리팹에 미리 두지 않고 런타임에 슬롯을 만드는 곳
    /// (예: 상점 카드의 코스튬 일러스트)이 인스펙터 대신 이 메서드로 구성한다.
    /// 인스펙터로 구성한 슬롯은 이 메서드를 부르지 않는 한 인스펙터 값을 그대로 쓴다.
    /// </summary>
    public void Configure(float coverScale, Vector2 focusAnchor) {
        this.coverScale = coverScale;
        this.focusAnchor = focusAnchor;
        if (_image != null && _image.enabled && _image.sprite != null) Reframe();
    }

    /// <summary>표시 중인 일러스트를 비운다.</summary>
    public void Clear() {
        if (_image == null) return;
        _image.sprite = null;
        _image.enabled = false;
    }

    private void BindInternal(Sprite sprite, Vector2 focus, IllustrationFit fit) {
        _focus = focus;
        _fit = fit;
        var img = EnsureImage();
        ApplyShadow();
        if (sprite == null) { img.enabled = false; return; }

        img.sprite = sprite;
        img.enabled = true;
        Reframe();
    }

    // 슬롯 크기가 바뀌면(레이아웃 완료/동적 리사이즈) 다시 프레이밍한다. Bind 가 레이아웃 전에 불려도 안전.
    private void OnRectTransformDimensionsChange() {
        if (_image != null && _image.enabled && _image.sprite != null) Reframe();
    }

    // 일러스트를 그릴 자식 Image 를 1회 생성해 재사용한다(배치는 Reframe 이 담당).
    private Image EnsureImage() {
        if (_image != null) return _image;

        var go = new GameObject("Illustration", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(Rt, false);

        _image = go.GetComponent<Image>();
        _image.raycastTarget = false;
        ApplyShadow();
        return _image;
    }

    // 그림자 설정을 일러스트 Image 에 반영한다. 끄면 컴포넌트를 비활성만 하고 남겨 재사용한다.
    private void ApplyShadow() {
        if (_image == null) return;

        if (!useShadow) {
            if (_shadow != null) _shadow.enabled = false;
            return;
        }

        if (_shadow == null) {
            _shadow = _image.GetComponent<Shadow>();
            if (_shadow == null) _shadow = _image.gameObject.AddComponent<Shadow>();
        }
        _shadow.enabled = true;
        _shadow.effectColor = shadowColor;
        _shadow.effectDistance = shadowEffectDistance;
        _shadow.useGraphicAlpha = shadowUseGraphicAlpha;
    }

    // 마스크 크기 기준으로 크기/위치를 잡는다. 아직 레이아웃 전이면 건너뛰고 OnRectTransformDimensionsChange 에서 재시도된다.
    private void Reframe() {
        if (_image == null || _image.sprite == null) return;

        float maskW = Rt.rect.width;
        float maskH = Rt.rect.height;
        if (maskH <= 0f) return;

        float aspect = _image.sprite.rect.width / _image.sprite.rect.height;
        var rt = _image.rectTransform;

        if (_fit == IllustrationFit.Contain) {
            if (maskW <= 0f) return;
            // 그림 전체가 마스크 안에 들어오는 최대 높이(가로/세로 중 먼저 닿는 쪽이 결정한다).
            // 위치까지 확정되는 방식이므로 관심점/focusAnchor 는 쓰지 않고 중앙에 놓는다.
            float h = Mathf.Min(maskH, maskW / aspect) * containScale;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(h * aspect, h);
        } else {
            float h = maskH * coverScale;
            rt.anchorMin = rt.anchorMax = focusAnchor;   // 슬롯 내 기준점.
            rt.pivot = _focus;                            // 일러스트 내 관심점.
            rt.sizeDelta = new Vector2(h * aspect, h);
        }

        rt.anchoredPosition = Vector2.zero;   // 기준점끼리 정확히 겹친다.
    }

#if UNITY_EDITOR
    // 플레이 중 인스펙터에서 그림자/프레이밍 값을 바꿔도 바로 확인되도록 한다.
    private void OnValidate() {
        if (!Application.isPlaying) return;
        ApplyShadow();
        if (_image != null && _image.enabled && _image.sprite != null) Reframe();
    }
#endif
}
