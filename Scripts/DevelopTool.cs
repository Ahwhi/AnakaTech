using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 개발용 시간 이동 도구. Bootstrap 씬에 두고 인스펙터(DevelopToolEditor)에서 조작한다.
///
/// 왜 필요한가: 길드 대항전(토 17:00 매칭 - 일요일 경기 - 정산)이나 레이드 자정 초기화처럼
/// 주간/일일 일정에 걸린 컨텐츠는 실제로 그 시각이 될 때까지 기다리지 않으면 확인할 수 없다.
/// 이 도구는 서버 시계를 통째로 옮겨(그리고 클라의 "지금"도 같이 맞춰) 원하는 시점의 상태를 바로 본다.
///
/// 판정은 전부 서버가 하므로 시간 이동도 서버에 요청한다. 클라만 옮기면 화면의 카운트다운만 바뀌고
/// 매칭/정산은 그대로다. 받은 오프셋을 KstTime 에도 넣어 서버가 보낸 시각과 클라의 지금이 어긋나지 않게 한다.
///
/// 서버가 Debug 빌드일 때만 동작한다. 운영 빌드에서는 거절 메시지가 돌아온다.
/// </summary>
public class DevelopTool : MonoBehaviour {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    /// <summary>서버 명령 코드(서버 Room.Dev.cs 의 DevCommand 와 같아야 한다).</summary>
    private static class Command {
        public const int Query = 0;
        public const int SetTimeOffset = 1;
        public const int WarMatch = 2;
        public const int WarSettle = 3;
        public const int RaidSettle = 4;
        public const int WarStatus = 5;
        public const int WarResetRound = 6;
        public const int ExpeditionMatch = 7;
        public const int ExpeditionStatus = 8;
        public const int ExpeditionResetRound = 9;
    }

    /// <summary>서버가 마지막으로 알려 준 "지금"(unix). 0 이면 아직 조회 전.</summary>
    public long ServerNowUnix { get; private set; }
    /// <summary>서버에 적용 중인 시간 이동(초).</summary>
    public long OffsetSeconds { get; private set; }
    /// <summary>마지막 응답 문구(에디터 표시용).</summary>
    public string LastMessage { get; private set; } = "";
    /// <summary>요청이 오가는 중인지. 버튼 중복 클릭을 막는 데 쓴다.</summary>
    public bool Busy { get; private set; }

    /// <summary>
    /// 서버가 생각하는 현재 KST 시각. 조회 전이면 null.
    /// 응답 시점의 값을 그대로 들고 있으면 그 뒤로 흐른 시간만큼 어긋나므로, 오프셋이 반영된 KstTime 을 읽는다.
    /// </summary>
    public DateTime? ServerNowKst => ServerNowUnix > 0 ? KstTime.NowKst : null;

    public void Query() => SendAsync(Command.Query, 0).Forget();

    /// <summary>지금 기준으로 시계를 이만큼 더 옮긴다(음수면 되돌린다).</summary>
    public void Jump(TimeSpan delta) =>
        SendAsync(Command.SetTimeOffset, OffsetSeconds + (long)delta.TotalSeconds).Forget();

    /// <summary>시간 이동을 해제해 실제 시각으로 되돌린다.</summary>
    public void ResetTime() => SendAsync(Command.SetTimeOffset, 0).Forget();

    /// <summary>
    /// 다음 대항전 매칭 시각(토 17:00 KST) 1분 전으로 이동한다.
    /// 카운트다운이 0 이 되고 매칭이 잡히는 순간을 화면에서 그대로 지켜볼 때 쓴다.
    /// </summary>
    public void JumpToBeforeWarMatch() => JumpTo(NextWarMatch().AddMinutes(-1));

    /// <summary>
    /// 다음 대항전 매칭 시각 직후로 이동한다.
    /// 이미 그 시각을 지났으면 다음 주로 넘어가므로, 매번 새 회차를 만들어 볼 수 있다.
    /// </summary>
    public void JumpToAfterWarMatch() => JumpTo(NextWarMatch().AddMinutes(1));

    /// <summary>다음 KST 자정 1분 전(23:59)으로 이동한다. 날짜가 넘어가는 순간의 화면 변화를 보는 용도.</summary>
    public void JumpToBeforeMidnight() => JumpTo(NextMidnight().AddMinutes(-1));

    /// <summary>다음 KST 자정 직후로 이동한다(레이드 초기화 확인용).</summary>
    public void JumpToAfterMidnight() => JumpTo(NextMidnight().AddMinutes(1));

    /// <summary>매칭이 왜 안 잡히는지 서버가 보는 그대로 받아 온다(후보 수 / 이번 회차 기록 / 내 길드 상태).</summary>
    public void CheckWarStatus() => SendAsync(Command.WarStatus, 0).Forget();

    /// <summary>이번 회차 매치 기록을 지운다. 같은 시각대에서 매칭을 다시 시험할 때 쓴다.</summary>
    public void ResetWarRound() => SendAsync(Command.WarResetRound, 0).Forget();

    /// <summary>다음 원정대 매칭 시각(수 00:00 KST) 1분 전으로 이동한다.</summary>
    public void JumpToBeforeExpeditionMatch() => JumpTo(NextExpeditionMatch().AddMinutes(-1));

    /// <summary>
    /// 다음 원정대 매칭 시각 직후로 이동한다. 스케줄러가 경계를 넘은 것을 보고 1분 안에 지난 회차 정산과 새 매칭을 돌린다.
    /// </summary>
    public void JumpToAfterExpeditionMatch() => JumpTo(NextExpeditionMatch().AddMinutes(1));

    /// <summary>원정대 지난 회차 정산 + 이번 회차 매칭을 즉시 돌린다(이미 매칭된 회차면 매칭은 건너뛴다).</summary>
    public void RunExpeditionMatch() => SendAsync(Command.ExpeditionMatch, 0).Forget();

    /// <summary>원정대 자격자 수 / 이번 회차 상태 / 내 자격과 소속을 받아 온다.</summary>
    public void CheckExpeditionStatus() => SendAsync(Command.ExpeditionStatus, 0).Forget();

    /// <summary>원정대 이번 회차 파티를 전부 지운다. 같은 회차를 다시 매칭해 볼 때 쓴다.</summary>
    public void ResetExpeditionRound() => SendAsync(Command.ExpeditionResetRound, 0).Forget();

    public void RunWarMatch() => SendAsync(Command.WarMatch, 0).Forget();
    public void RunWarSettle() => SendAsync(Command.WarSettle, 0).Forget();
    public void RunRaidSettle() => SendAsync(Command.RaidSettle, 0).Forget();

    // 목표 KST 시각에 정확히 맞추는 오프셋을 계산해 보낸다.
    private void JumpTo(DateTime targetKst) {
        var delta = targetKst - CurrentKst();
        SendAsync(Command.SetTimeOffset, OffsetSeconds + (long)delta.TotalSeconds).Forget();
    }

    // 오프셋이 반영된 "지금". 서버 응답 이후 흐른 시간까지 포함되므로 목표 시각 계산이 어긋나지 않는다.
    private DateTime CurrentKst() => KstTime.NowKst;

    private DateTime NextWarMatch() => NextWeekly(GuildWarRules.MatchDay, GuildWarRules.MatchHour);

    private DateTime NextExpeditionMatch() => NextWeekly(ExpeditionRules.MatchDay, 0);

    private DateTime NextMidnight() => CurrentKst().Date.AddDays(1);

    // 기준 시각 이후에 처음 오는 요일/시각(KST).
    private DateTime NextWeekly(DayOfWeek day, int hour) {
        var now = CurrentKst();
        int forward = ((int)day - (int)now.DayOfWeek + 7) % 7;
        var candidate = now.Date.AddDays(forward).AddHours(hour);
        return candidate > now ? candidate : candidate.AddDays(7);
    }

    private async UniTaskVoid SendAsync(int command, long value) {
        if (Busy) return;

        var net = FindAnyObjectByType<NetworkManager>();
        if (net == null || !net.IsConnected) {
            LastMessage = "서버에 연결되어 있지 않습니다. 플레이 중에 사용하세요.";
            return;
        }

        Busy = true;
        try {
            using var cts = new CancellationTokenSource(ResponseTimeout);
            var wait = net.WaitPacketAsync<S_DevTool>(cts.Token);
            net.Send(new C_DevTool { command = command, value = value }.Write());
            var res = await wait;

            ServerNowUnix = res.serverNowUnix;
            OffsetSeconds = res.offsetSeconds;
            LastMessage = res.message;

            // 클라의 "지금"도 같은 만큼 옮겨야 서버가 보낸 시각과 카운트다운이 맞는다.
            KstTime.DevOffsetSeconds = res.offsetSeconds;

            if (res.ok == 0) Debug.LogWarning($"[DevelopTool] {res.message}");
            else Debug.Log($"[DevelopTool] 서버 시각 {KstTime.Format(res.serverNowUnix)} (offset {res.offsetSeconds}s) {res.message}");
        } catch (OperationCanceledException) {
            LastMessage = "응답 시간 초과.";
        } catch (Exception e) {
            LastMessage = e.Message;
            Debug.LogException(e);
        } finally {
            Busy = false;
        }
    }
}
