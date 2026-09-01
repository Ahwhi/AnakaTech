using UnityEngine;

/// <summary>
/// 던전 그룹 정적 데이터. 던전을 묶어 보여주는 단위(예: "노말", "하드", "이벤트")이며 표시 전용이라 서버와 공유하지 않는다.
/// DungeonData 가 자신이 속한 그룹을 참조하고, DungeonDatabase 가 던전 목록에서 그룹 목록을 유도한다(그룹 목록을 따로 관리하지 않음).
/// 새 그룹이 필요하면 이 SO 에셋을 만들어 던전에 할당하기만 하면 던전 선택 드롭다운에 자동 반영된다.
/// </summary>
[CreateAssetMenu(fileName = "DungeonGroupData", menuName = "Anaka/Data/Dungeon Group")]
public sealed class DungeonGroupData : ScriptableObject {
    [Header("Identity")]
    [Tooltip("그룹 고유 id. 표시 순서와 무관하며 중복되면 안 된다.")]
    [SerializeField] private int groupId;
    [Tooltip("드롭다운에 표시할 이름(예: \"노말\"). 비우면 에셋 이름을 사용한다.")]
    [SerializeField] private string displayName;

    [Header("Sound")]
    [Tooltip("이 그룹(장)의 전투 BGM. 비우면 공용 전투 BGM(BgmKey.Battle)이 나온다. " +
             "메인 전투뿐 아니라 의뢰/길드레이드/쟁탈전도 진행 중인 스테이지의 그룹 BGM을 따라간다.")]
    [SerializeField] private AssetReferenceAudioClip battleBgm;

    [Header("Ordering")]
    [Tooltip("드롭다운 정렬 순서. 값이 작을수록 위에 온다(같으면 groupId 순).")]
    [SerializeField] private int sortOrder;

    public int GroupId => groupId;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public int SortOrder => sortOrder;

    /// <summary>
    /// 이 그룹의 전투 BGM(미할당이면 null = 호출측이 공용 전투 BGM을 쓴다).
    /// 그룹이 "다른 곡을 쓸지"만 표현하고, 기본 곡이 무엇인지는 알지 않는다(FormationOverride 와 같은 규약).
    /// </summary>
    public AssetReferenceAudioClip BattleBgm =>
        battleBgm != null && battleBgm.RuntimeKeyIsValid() ? battleBgm : null;
}
