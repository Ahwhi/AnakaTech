using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 등급에 따라 그래픽(Image 등)의 스프라이트와 색을 바꾸는 공용 컴포넌트.
/// 텍스트 전용인 GradeText 와 같은 규약을 쓰되, 대상이 Graphic 이라 프레임/배경/글로우 같은 이미지에 그대로 쓴다.
///
/// 두 가지를 각각 켤 수 있다:
///   - 스프라이트 교체(GradeSpriteCatalog): 등급마다 아예 다른 그림을 끼운다. 대상이 Image 일 때만 동작.
///   - 색 적용(GradeColorCatalog): 같은 그림을 등급 색으로 물들인다.
/// 둘 다 켜면 그림을 바꾼 뒤 색까지 입힌다. 카탈로그에 해당 등급이 없으면 그 항목은 건드리지 않는다
/// (프리팹에 지정된 기본 그림/색 유지).
///
/// 등급이 필요한 이미지 오브젝트에 이 컴포넌트를 붙이고, 카드/팝업 쪽에서 SetGrade 만 호출하면 된다.
/// </summary>
public sealed class GradeGraphicStyle : MonoBehaviour {
    [Tooltip("등급을 반영할 그래픽(Image 등). 비우면 같은 오브젝트의 Graphic 을 사용.")]
    [SerializeField] private Graphic target;

    [Header("Sprite (등급별 그림 교체)")]
    [Tooltip("체크 시 등급별 스프라이트로 교체한다. 대상이 Image 일 때만 동작.")]
    [SerializeField] private bool applySprite = true;
    [Tooltip("등급별 스프라이트 세트(SO). 비우면 그림을 교체하지 않는다.")]
    [SerializeField] private GradeSpriteCatalog spriteCatalog;

    [Header("Color (등급별 색 적용)")]
    [Tooltip("체크 시 등급별 색(GradeColorCatalog)을 적용. 미체크면 색을 건드리지 않는다.")]
    [SerializeField] private bool applyColor = true;
    [Tooltip("체크 시 프리팹에 지정된 알파를 유지하고 RGB 만 교체한다. 미체크면 카탈로그 색의 알파까지 적용.")]
    [SerializeField] private bool preserveAlpha = true;
    [Tooltip("색 소스(SO). 비우면 앱이 등록한 기본 카탈로그를 사용.")]
    [SerializeField] private GradeColorCatalog catalog;

    private Graphic Target => target != null ? target : (target = GetComponent<Graphic>());

    /// <summary>등급에 맞는 스프라이트/색을 대상 그래픽에 반영한다.</summary>
    public void SetGrade(Grade grade) {
        var g = Target;
        if (g == null) return;

        if (applySprite && spriteCatalog != null && g is Image img) {
            var sp = spriteCatalog.Get(grade);
            if (sp != null) img.sprite = sp;
        }

        if (!applyColor) return;
        var cat = catalog != null ? catalog : GradeText.DefaultCatalog;
        if (cat == null || !cat.TryGetColor(grade, out var col)) return;
        g.color = preserveAlpha ? new Color(col.r, col.g, col.b, g.color.a) : col;
    }
}
