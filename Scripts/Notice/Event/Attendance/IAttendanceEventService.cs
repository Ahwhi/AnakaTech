using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 출석형 이벤트 전용 통신(서버 AttendanceEventCatalog). 이벤트 키로 구분하므로 출석형 이벤트가 늘어도 이 창구는 그대로다.
/// 표시 형식은 월간 출석과 같은 DTO(AttendanceInfo / AttendanceClaimResult)를 쓴다.
/// 수령 실패 사유: 0=성공, 1=없는 이벤트, 2=잠김, 3=오늘 이미 받음 또는 모두 받음, 4=인증 전, 5=지갑 없음.
/// </summary>
public interface IAttendanceEventService {
    UniTask<AttendanceInfo> GetInfoAsync(string eventKey, CancellationToken ct = default);
    UniTask<AttendanceClaimResult> ClaimAsync(string eventKey, CancellationToken ct = default);
}
