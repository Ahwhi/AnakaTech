using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

/// <summary>스탯 목록의 한 줄(이름 + 값 + 설명 버튼). StatListView 가 프리팹으로 복제해 채운다. TextMeshPro 전용.
/// 이름/설명은 스탯 종류에서 유도하므로(GrowthRules) 호출측이 문구를 들고 다니지 않는다.
///
/// 설명은 툴팁이 아니라 팝업으로 띄운다. 손가락으로 누르는 기기에서는 누른 자리 바로 옆에 뜨는 툴팁이
/// 손에 가려 읽을 수가 없다. 팝업은 화면 가운데에 뜨고 확인을 눌러야 닫히므로 그 문제가 없다.
///
/// 팝업을 여는 데 필요한 것(UI 서비스 + 팝업 카탈로그)은 씬 스코프에서 직접 해석한다. 이 행은 목록이
/// 런타임에 복제해 만드는 오브젝트라 컨테이너 주입이 닿지 않기 때문이다(BadgeView 와 같은 이유).
/// 덕분에 이 행을 쓰는 화면들은 아무것도 넘겨 주지 않아도 되고, 새로 쓰는 화면이 생겨도 배선이 늘지 않는다.</summary>
public sealed class StatRowView : MonoBehaviour {
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text valueText;
    [Tooltip("행 배경 이미지. 비우면 이 오브젝트의 Image 를 사용한다. 색은 StatListView 가 행마다 지정한다.")]
    [SerializeField] private Image background;
    [Tooltip("설명(i) 버튼. 누르면 스탯 설명 팝업이 열린다. 비우면 설명을 볼 수 없다(선택).")]
    [SerializeField] private Button infoButton;

    private StatType _stat;
    private bool _hasDescription;
    private bool _wired;

    private IUIService _ui;
    private UIPopupCatalog _popups;
    private bool _resolved;

    private Image Background => background != null ? background : (background = GetComponent<Image>());

    /// <summary>행을 스탯 1종으로 채운다. 이름/설명은 스탯에서 유도하고, 값 문자열만 호출측이 포맷해 넘긴다.</summary>
    public void Set(StatType stat, string value) {
        _stat = stat;
        if (labelText != null) labelText.text = GrowthRules.StatName(stat);
        if (valueText != null) valueText.text = value;

        // 설명이 없는 스탯(미정의)은 버튼을 꺼 둔다. 눌러도 빈 팝업만 뜨면 고장으로 보인다.
        _hasDescription = !string.IsNullOrEmpty(GrowthRules.StatDescription(stat));
        if (infoButton == null) return;

        infoButton.gameObject.SetActive(_hasDescription);
        if (_wired) return;
        _wired = true;
        infoButton.onClick.AddListener(OpenDescription);
    }

    /// <summary>행 배경색 지정(목록에서 줄 구분용). 배경이 없으면 무시한다.</summary>
    public void SetBackgroundColor(Color color) {
        var bg = Background;
        if (bg != null) bg.color = color;
    }

    private void OpenDescription() {
        if (!_hasDescription) return;

        EnsureServices();
        if (_ui == null || _popups == null || _popups.ConfirmPopup == null || !_popups.ConfirmPopup.RuntimeKeyIsValid()) return;

        OpenDescriptionAsync(GrowthRules.StatName(_stat), GrowthRules.StatDescription(_stat)).Forget();
    }

    private async UniTaskVoid OpenDescriptionAsync(string title, string description) {
        try {
            var popup = await _ui.ShowPopupAsync<ConfirmPopup>(_popups.ConfirmPopup, this.GetCancellationTokenOnDestroy());
            await popup.ShowAsync(title, description);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // 씬 스코프에서 직접 해석한다(복제 생성이라 AutoInject 대상이 아니다). 실패해도 설명만 뜨지 않는다.
    private void EnsureServices() {
        if (_resolved) return;
        _resolved = true;

        var scope = FindAnyObjectByType<LifetimeScope>();
        if (scope == null || scope.Container == null) return;
        try {
            _ui = scope.Container.Resolve<IUIService>();
            _popups = scope.Container.Resolve<UIPopupCatalog>();
        } catch {
            _ui = null;
            _popups = null;
        }
    }
}
