using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 소셜 분류 페이지. 다른 사람과의 관계에 걸리는 설정을 모은다(지금은 귓속말 수신 범위 하나).
///
/// 다른 페이지와 달리 값의 주인이 서버다. 기기가 아니라 계정에 붙는 설정이고, 남이 보내는 귓속말을
/// 막는 판정도 서버가 하기 때문이다. 그래서 진입할 때 현재 값을 받아 온 뒤에 선택지를 붙인다
/// (받기 전에 붙이면 잠깐 다른 값이 켜져 있다가 바뀌어 보인다).
/// </summary>
public sealed class SettingSocialPage : SettingPage {
    [Header("Whisper")]
    [Tooltip("귓속말 수신 범위 선택. 배열 순서가 아래 Policies 와 같아야 한다(전체 허용 / 친구만 / 전체 차단).")]
    [SerializeField] private Toggle[] whisperToggles;
    [Tooltip("값을 받아오는 동안 켜는 안내 묶음. 선택.")]
    [SerializeField] private GameObject loadingGroup;

    // 토글 배열과 짝을 이루는 값. 순서를 바꾸면 인스펙터의 토글 순서도 함께 바꿔야 한다.
    private static readonly WhisperPolicy[] Policies = {
        WhisperPolicy.All,
        WhisperPolicy.FriendsOnly,
        WhisperPolicy.None,
    };

    private bool _busy;

    protected override void OnInitialize() {
        SetToggles(false);
        if (loadingGroup != null) loadingGroup.SetActive(true);
        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        var ct = this.GetCancellationTokenOnDestroy();
        try {
            var policy = await Ctx.Chat.RequestWhisperPolicyAsync(ct);
            BindRadio(whisperToggles, Policies, IndexOf(policy), ApplyPolicy);
            SetToggles(true);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            Ctx.Ui.ShowToast("설정을 불러오지 못했습니다.");
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }
    }

    // 저장은 서버가 한다. 실패하면 화면만 바뀌고 실제로는 그대로이므로, 그때는 서버가 준 값으로 되돌린다.
    private void ApplyPolicy(WhisperPolicy policy) {
        if (_busy) return;
        SaveAsync(policy).Forget();
    }

    private async UniTaskVoid SaveAsync(WhisperPolicy policy) {
        _busy = true;
        SetToggles(false);
        try {
            var applied = await Ctx.Chat.SetWhisperPolicyAsync(policy, this.GetCancellationTokenOnDestroy());
            if (applied != policy) SelectWithoutNotify(IndexOf(applied));
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            Ctx.Ui.ShowToast("설정을 저장하지 못했습니다.");
        } finally {
            _busy = false;
            SetToggles(true);
        }
    }

    private static int IndexOf(WhisperPolicy policy) {
        for (int i = 0; i < Policies.Length; i++)
            if (Policies[i] == policy) return i;
        return 0;
    }

    // 저장이 거절됐을 때 서버 값으로 되돌린다. 알림 없이 바꿔 저장 요청이 다시 나가지 않게 한다.
    private void SelectWithoutNotify(int index) {
        if (whisperToggles == null) return;
        for (int i = 0; i < whisperToggles.Length; i++)
            if (whisperToggles[i] != null) whisperToggles[i].SetIsOnWithoutNotify(i == index);
    }

    // 값을 받아오는 동안과 저장 중에는 조작을 막는다(연달아 누르면 어느 것이 저장됐는지 알 수 없다).
    private void SetToggles(bool on) {
        if (whisperToggles == null) return;
        foreach (var toggle in whisperToggles)
            if (toggle != null) toggle.interactable = on;
    }
}
