using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 코스튬 목록 1칸. 코스튬 일러스트(또는 기본 일러스트) + 이름 + 보유/장착 상태를 보여준다.
///
/// "기본" 칸도 같은 프리팹으로 그린다(Costume 이 null 인 칸). 기본 외형으로 되돌리는 것도 결국 코스튬 선택과
/// 같은 동작이라, 칸을 나누면 목록/선택/장착 로직이 두 벌이 된다.
///
/// 일러스트는 CharacterIllustrationView 를 쓰므로 코스튬마다 다른 관심점(UiFocus)으로 정확히 크롭된다.
/// </summary>
public sealed class CostumeSlotView : MonoBehaviour {
    [Tooltip("코스튬 일러 슬롯. 코스튬별 관심점으로 프레이밍되므로 raw Image 가 아니라 이 컴포넌트를 쓴다.")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [Tooltip("코스튬 이름. 기본 칸에서는 '기본'이 들어간다.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("등급 표시(GradeText). 기본 칸에서는 숨긴다. 선택.")]
    [SerializeField] private GradeText gradeText;
    [Tooltip("미보유 표시 묶음(자물쇠/어둡게 등). 보유 중이면 꺼진다. 선택.")]
    [SerializeField] private GameObject lockedGroup;
    [Tooltip("현재 장착 중 표식. 선택.")]
    [SerializeField] private GameObject equippedMark;
    [Tooltip("목록에서 지금 고른 칸 표식(선택 테두리). 선택.")]
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private Button button;

    [Header("Effect")]
    [Tooltip("장착 효과 요약(예: 공격력 +15). 효과가 없는 코스튬과 기본 칸에서는 '장착 효과 없음'이 들어간다. 선택.")]
    [SerializeField] private TMP_Text effectText;
    [Tooltip("효과가 있을 때의 글자색. 효과가 눈에 띄어야 하는 자리라 색을 따로 준다.")]
    [SerializeField] private Color effectColor = Color.white;
    [Tooltip("'장착 효과 없음'일 때의 글자색. 있는 효과와 헷갈리지 않도록 죽은 색으로 둔다.")]
    [SerializeField] private Color noEffectColor = Color.black;

    /// <summary>이 칸의 코스튬. null 이면 기본 외형 칸이다.</summary>
    public CostumeData Data { get; private set; }

    /// <summary>이 칸이 가리키는 코스튬 id(기본 칸은 0). 장착 요청에 그대로 쓰인다.</summary>
    public int CostumeId => Data != null ? Data.Id : 0;

    /// <summary>보유 중인지. 미보유 코스튬은 목록에는 보이지만 장착할 수 없다.</summary>
    public bool Owned { get; private set; }

    /// <param name="appearance">이 칸이 보여줄 외형(기본 칸이면 캐릭터 기본 일러스트).</param>
    /// <param name="costume">이 칸의 코스튬. null 이면 기본 외형 칸이다.</param>
    /// <param name="owned">보유 여부. 기본 칸은 항상 보유로 취급한다(되돌리기는 언제나 가능).</param>
    public void Bind(CharacterAppearance appearance, CostumeData costume, bool owned, bool equipped,
                     Action<CostumeSlotView> onClick) {
        Data = costume;
        Owned = owned;

        if (illustrationView != null) {
            if (appearance.Character != null) illustrationView.Bind(appearance);
            else illustrationView.Clear();
        }
        if (nameText != null) nameText.text = costume != null ? costume.DisplayName : DefaultLabel;

        if (gradeText != null) {
            gradeText.gameObject.SetActive(costume != null);
            if (costume != null) gradeText.SetGrade(costume.Grade);
        }
        ApplyEffectText(costume);
        if (lockedGroup != null) lockedGroup.SetActive(!owned);
        if (equippedMark != null) equippedMark.SetActive(equipped);
        SetSelected(false);

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }

    // 효과 요약과 그 색. 효과가 있으면 지정한 강조색, 없으면(기본 칸 포함) 죽은 색으로 그린다.
    private void ApplyEffectText(CostumeData costume) {
        if (effectText == null) return;

        string summary = costume != null ? costume.EffectSummary() : "";
        bool has = !string.IsNullOrEmpty(summary);
        effectText.text = has ? summary : NoEffectText;
        effectText.color = has ? effectColor : noEffectColor;
    }

    public void SetSelected(bool selected) {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }

    public void SetEquipped(bool equipped) {
        if (equippedMark != null) equippedMark.SetActive(equipped);
    }

    /// <summary>기본 외형 칸의 표시 이름. 목록과 상세가 같은 문구를 쓰도록 여기 한 곳에 둔다.</summary>
    public const string DefaultLabel = "기본";

    /// <summary>외형만 바꾸는 코스튬(과 기본 칸)의 효과 문구. 목록과 상세가 같은 문구를 쓴다.</summary>
    public const string NoEffectText = "장착 효과 없음";
}
