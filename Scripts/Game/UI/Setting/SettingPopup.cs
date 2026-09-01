using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 설정 팝업. 메인 화면의 메뉴 버튼(HudMenuButton)에서 다른 팝업과 동일하게 연다.
///
/// 이 팝업이 하는 일은 "분류 탭 전환"뿐이다. 실제 설정 항목은 각 분류 페이지(SettingPage)가 갖고,
/// 저장/적용은 다시 각 항목의 주인(ISoundService, DisplaySettings, GameData)이 갖는다.
/// 따라서 분류를 늘릴 때는 SettingPage 를 상속한 페이지를 만들고 아래 목록에 한 줄 추가하면 되며,
/// 이 스크립트는 고치지 않는다.
/// </summary>
public sealed class SettingPopup : UIView, IHudPopup {
    /// <summary>분류 하나 = 탭 버튼 + 그 탭을 눌렀을 때 보여줄 페이지.</summary>
    [Serializable]
    private sealed class Category {
        [Tooltip("이 분류를 여는 탭 버튼.")]
        public UITabButton tab;
        [Tooltip("이 분류의 설정 항목을 담은 페이지.")]
        public SettingPage page;
        [Tooltip("탭에 표시할 이름. 비우면 탭 버튼에 적어 둔 문구를 그대로 쓴다.")]
        public string displayName;
    }

    [Tooltip("분류 탭 목록. 첫 번째 항목이 팝업을 열었을 때 기본으로 선택된다.")]
    [SerializeField] private Category[] categories;
    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private int _selected = -1;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;

        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
        }

        if (categories == null) return;

        for (int i = 0; i < categories.Length; i++) {
            var category = categories[i];
            if (category == null) continue;

            // 대부분의 설정 항목은 로컬 값이라 사운드만 있으면 되지만, 계정 탭은 약관/탈퇴 통신이 필요하다.
            // 페이지마다 다른 의존성을 팝업이 알 필요가 없도록 컨텍스트를 그대로 넘긴다.
            if (category.page != null) category.page.Initialize(ctx);

            int index = i;
            if (category.tab != null)
                category.tab.Bind(category.displayName, () => {
                    _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
                    Select(index);
                });
        }

        Select(0);
    }

    /// <summary>선택한 분류의 페이지만 켜고 나머지는 끈다. 같은 탭을 다시 눌러도 아무 일도 하지 않는다.</summary>
    private void Select(int index) {
        if (categories == null || index < 0 || index >= categories.Length || _selected == index) return;
        _selected = index;

        for (int i = 0; i < categories.Length; i++) {
            var category = categories[i];
            if (category == null) continue;

            bool on = i == index;
            if (category.tab != null) category.tab.SetSelected(on);
            if (category.page != null) category.page.gameObject.SetActive(on);
        }
    }
}
