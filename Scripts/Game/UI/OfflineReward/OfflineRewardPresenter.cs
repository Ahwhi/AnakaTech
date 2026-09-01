using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>오프라인 보상 표시 흐름.</summary>
public interface IOfflineRewardPresenter {
    /// <summary>
    /// 정산을 요청하고 지급된 것이 있으면 팝업을 띄운다. 팝업이 닫힐 때까지 기다린다.
    /// 지급이 없으면 아무것도 하지 않는다(빈 팝업을 띄우지 않는다).
    /// </summary>
    UniTask PresentAsync(CancellationToken ct = default);
}

/// <summary>
/// 오프라인 보상 정산 요청 -> 지갑 동기화 -> 팝업 표시.
///
/// 표시 시점은 메인 진입 흐름이 끝난 직후 한 번뿐이다. 로그인과 필수 약관 동의는 그보다 앞선 부트 단계에서
/// 이미 끝나 있고(TermsStep), 플레이어 데이터도 받은 뒤이므로 다른 필수 팝업과 겹치지 않는다.
/// 다만 게임 진행을 막지는 않는다. 호출부(MainSceneManager)는 전투를 시작한 뒤 이 흐름을 기다리지 않고 띄운다.
///
/// 보상은 서버가 이미 지급했다. 이 화면은 결과를 보여 줄 뿐이므로, 팝업을 닫거나 앱이 죽어도
/// 지급이 취소되거나 다시 일어나지 않는다.
/// </summary>
public sealed class OfflineRewardPresenter : IOfflineRewardPresenter {
    private readonly IOfflineRewardService _service;
    private readonly PlayerStateStore _store;
    private readonly IUIService _ui;
    private readonly UIPopupCatalog _popups;
    private readonly MainHudContext _hud;
    private readonly IPushCoordinator _push;

    public OfflineRewardPresenter(IOfflineRewardService service, PlayerStateStore store,
                                  IUIService ui, UIPopupCatalog popups, MainHudContext hud,
                                  IPushCoordinator push) {
        _service = service;
        _store = store;
        _ui = ui;
        _popups = popups;
        _hud = hud;
        _push = push;
    }

    public async UniTask PresentAsync(CancellationToken ct = default) {
        // 알림을 눌러 앱이 열린 경우의 이동 대상을 여기서 소비한다.
        //
        // 지금은 알림이 오프라인 보상 하나뿐이고, 그 보상은 메인에 들어오면 아래에서 어차피 표시된다.
        // 그래서 따로 화면을 옮길 것이 없고, 값을 비워 두기만 하면 된다(비우지 않으면 다음 접속에
        // 옛 알림의 이동 대상이 남는다). 이동 대상이 늘어나면 여기서 갈라 처리한다.
        _push.TakePendingAction();

        OfflineRewardDto reward;
        try {
            reward = await _service.RequestAsync(ct);
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            // 보상을 못 받아도 게임 진행에는 지장이 없다. 지급 자체는 서버가 이미 끝냈고,
            // 다음 접속에서 같은 정산을 다시 읽어 올 수 있다.
            Debug.LogWarning($"[OfflineReward] 정산 요청 실패: {e.Message}");
            return;
        }

        if (reward == null || !reward.HasReward) return;

        var reference = _popups.OfflineRewardPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) {
            // 프리팹이 아직 배선되지 않았어도 받은 사실은 알려야 한다.
            _ui.ShowToast($"오프라인 보상을 받았습니다. 골드 {reward.Gold:N0} / 기력 {reward.Stamina} / 다이아 {reward.Diamond}");
            Apply(reward);
            return;
        }

        try {
            var popup = await _ui.ShowPopupAsync<OfflineRewardPopup>(reference, ct);
            await popup.ShowAsync(_hud, reward);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogWarning($"[OfflineReward] 팝업 표시 실패: {e.Message}");
        } finally {
            Apply(reward);
        }
    }

    /// <summary>
    /// 받은 보상을 화면 상태에 한꺼번에 반영한다. 지급 자체는 서버가 이미 끝냈고 여기서는 그 결과를 비추기만 한다.
    ///
    /// 확인을 누른 뒤(팝업이 닫힌 뒤)에 반영하는 이유는 "받았다"는 순간을 하나로 만들기 위해서다.
    /// 재화만 먼저 올려 두면 팝업을 여는 순간 이미 숫자가 바뀌어 있어 확인을 눌러도 아무 일도 일어나지 않고,
    /// 경험치만 뒤늦게 들어오면 보상마다 들어오는 시점이 달라 보인다. 재화와 경험치를 같은 자리에서 올리면
    /// 확인을 누르는 순간 숫자가 함께 오르고 레벨업 연출도 그때 터진다.
    ///
    /// 레벨업 연출이 이 시점이어야 하는 이유도 같다. 연출은 HUD 안쪽 레이어에 재생돼 팝업에 가리므로
    /// (BattleCharacterPanel.PlayLevelUpVfx), 팝업이 떠 있는 동안 올리면 딤 뒤에서 조용히 지나간다.
    ///
    /// 팝업이 열리지 못한 경우에도 반드시 반영되도록 양쪽 경로에서 부르고, 중복 반영은 플래그로 막는다
    /// (경험치의 스탯포인트는 더하는 값이라 두 번 반영되면 실제보다 많아진다).
    /// </summary>
    private void Apply(OfflineRewardDto reward) {
        if (_applied) return;
        _applied = true;

        // 지갑은 서버가 알려 준 값으로 맞춘다. 화면이 더하지 않는다(더하면 서버와 어긋날 여지가 생긴다).
        _store.Gold.Value = reward.NewGold;
        _store.Diamond.Value = reward.NewDiamond;
        _store.SetStamina(reward.NewStamina);
        // 경험치는 지갑이 아니라 편성원에게 들어간다(레벨/경험치/스탯포인트).
        _store.ApplyCharExp(reward.CharExp);
    }

    private bool _applied;
}
