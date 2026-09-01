using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 던전 정보 패널(PLANNING 2-6). 현재 던전 이름 + 남은 몬스터 수 + 휴식 상태 표시 + 던전선택 버튼(선택 팝업).
/// 예상 보상은 서버 권위라 던전 선택 팝업이 서버 미리보기 패킷으로 받아 표시한다.
/// </summary>
public sealed class DungeonInfoPanel : MonoBehaviour {
    [SerializeField] private TMP_Text dungeonNameText;
    [SerializeField] private TMP_Text remainingText;
    [SerializeField] private TMP_Text restingText;
    [SerializeField] private Button dungeonSelectButton;

    private MainHudContext _ctx;
    private readonly CompositeDisposable _disposables = new();

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;

        ctx.Store.CurrentDungeonIndex.Subscribe(i => {
            if (dungeonNameText == null) return;
            var d = ctx.DungeonDb.Get(i);
            dungeonNameText.text = d != null ? d.DisplayName : $"#{i}";
        }).AddTo(_disposables);

        ctx.Battle.RemainingMonsters.Subscribe(n => {
            if (remainingText != null) remainingText.text = $"{n}";
        }).AddTo(_disposables);

        ctx.Battle.IsResting.Subscribe(r => {
            if (restingText != null) restingText.gameObject.SetActive(r);
        }).AddTo(_disposables);

        if (dungeonSelectButton != null) {
            dungeonSelectButton.onClick.AddListener(() => {
                ctx.Sound.PlaySfx(SfxKey.ButtonClick);
                OpenSelectAsync(this.GetCancellationTokenOnDestroy()).Forget();
            });
        }
    }

    private async UniTaskVoid OpenSelectAsync(CancellationToken ct) {
        var popup = await _ctx.Ui.ShowPopupAsync<DungeonSelectPopup>(_ctx.Popups.DungeonSelectPopup, ct);
        popup.Setup(_ctx, index => OnDungeonSelectedAsync(index, ct).Forget());
        await popup.ShowAsync();
    }

    // 던전 선택 시 분기(PLANNING 1-3): 같은 던전은 무시, 휴식 중이면 즉시 전환(잃을 보상 없음),
    // 진행 중이면 변경 예약(보상 후 전환)/즉시 변경(보상 없음)을 고르게 한다.
    private async UniTaskVoid OnDungeonSelectedAsync(int index, CancellationToken ct) {
        if (index == _ctx.Store.CurrentDungeonIndex.Value) return;

        if (_ctx.Battle.IsResting.Value) {
            _ctx.Battle.SwitchDungeon(index);
            return;
        }

        var target = _ctx.DungeonDb.Get(index);
        string name = target != null ? target.DisplayName : $"#{index}";

        var popup = await _ctx.Ui.ShowPopupAsync<DungeonChangePopup>(_ctx.Popups.DungeonChangePopup, ct);
        var result = await popup.ShowAsync("스테이지 변경", $"'{name}'(으)로 변경합니다.\n진행 중인 스테이지를 어떻게 할까요?");

        switch (result) {
            case DungeonChangePopup.Result.Reserve:
                _ctx.Battle.ReserveDungeonChange(index);
                _ctx.Ui.ShowToast("현재 스테이지를 끝내고 자동으로 변경합니다.");
                break;
            case DungeonChangePopup.Result.Immediate:
                _ctx.Battle.SwitchDungeon(index);
                _ctx.Ui.ShowToast("새 스테이지로 변경했습니다.");
                break;
        }
    }

    private void OnDestroy() => _disposables.Dispose();
}
