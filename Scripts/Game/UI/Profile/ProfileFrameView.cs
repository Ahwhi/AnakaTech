using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프로필 테두리 표시 전용 재사용 위젯. 테두리 id 를 받아 카탈로그의 스프라이트로 이미지를 채운다.
/// 미장착(0)/미등록 id 면 이미지를 숨겨 테두리 없는 맨 일러스트가 그대로 보인다.
///
/// 용도는 대표 캐릭터 일러스트 위에 겹쳐 그리는 액자다(프로필 팝업 / 메인 정보 패널).
/// 배치는 일러스트와 같은 영역을 덮는 형제 오브젝트로 두고, 이 오브젝트의 Image 는
/// Raycast Target 을 꺼서 아래 일러스트/프로필 버튼의 입력을 가리지 않게 한다.
///
/// 테두리를 보여주려는 어느 UI 든 이 컴포넌트만 배치하면 표시 규칙(스프라이트/이름/숨김 처리)이
/// 한 곳으로 통일되어 확장 시 개별 코드 중복이 사라진다.
/// - Bind: 변하지 않는 고정 id 를 1회 표시(예: 타인 프로필, 미리보기).
/// - BindReactive: 내 장착 테두리처럼 변할 수 있는 id 를 구독해 자동 갱신(예: 정보 패널, 프로필 팝업).
/// 두 메서드 모두 이전 구독/상태를 정리하므로 반복 호출과 재바인딩에 안전하다.
/// </summary>
public sealed class ProfileFrameView : MonoBehaviour {
    [Tooltip("테두리 스프라이트를 표시할 이미지. 비워두면 이 오브젝트의 Image 를 사용한다. Raycast Target 은 꺼 둘 것.")]
    [SerializeField] private Image image;
    [Tooltip("선택. 테두리 이름을 함께 표시할 때만 배선한다.")]
    [SerializeField] private TMP_Text nameText;

    private ProfileFrameCatalog _catalog;
    private IDisposable _sub;
    private bool _resolved;

    private void ResolveImage() {
        if (_resolved) return;
        if (image == null) image = GetComponent<Image>();
        _resolved = true;
    }

    /// <summary>고정 테두리 id 를 1회 표시한다. 변하지 않는 대상(타인 프로필 등)에 사용한다.</summary>
    public void Bind(ProfileFrameCatalog catalog, int profileFrameId) {
        Unsubscribe();
        _catalog = catalog;
        Apply(profileFrameId);
    }

    /// <summary>
    /// 변할 수 있는 테두리 id(내 장착 테두리 등)를 구독해 자동 갱신한다.
    /// 구독은 이 컴포넌트 파괴 시 또는 다음 Bind/BindReactive 호출 시 해제된다.
    /// </summary>
    public void BindReactive(ProfileFrameCatalog catalog, IReadOnlyReactiveProperty<int> source) {
        Unsubscribe();
        _catalog = catalog;
        if (source == null) { Apply(0); return; }
        _sub = source.Subscribe(Apply);
    }

    private void Apply(int profileFrameId) {
        ResolveImage();
        var sprite = _catalog != null ? _catalog.Icon(profileFrameId) : null;
        if (image != null) { image.sprite = sprite; image.enabled = sprite != null; }
        if (nameText != null) nameText.text = _catalog != null ? _catalog.DisplayName(profileFrameId) : string.Empty;
    }

    private void Unsubscribe() {
        _sub?.Dispose();
        _sub = null;
    }

    private void OnDestroy() => Unsubscribe();
}
