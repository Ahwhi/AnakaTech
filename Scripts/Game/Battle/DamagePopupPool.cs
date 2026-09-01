using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 데미지 팝업 스타일. 스타일마다 별도 프리팹/풀로 관리한다.
///
/// 축은 "누가 맞았는가"다: 적이 맞으면 내가 준 피해(Normal/Critical), 내 캐릭터가 맞으면 피격(PlayerHit*).
/// 회피(MISS)도 숫자가 문구로 바뀔 뿐 같은 축을 그대로 탄다. 그래서 MISS 전용 스타일을 두지 않는다
/// (전용 스타일을 두면 "내 공격이 빗나갔는데 피격 팝업으로 뜨는" 식으로 축이 어긋난다).
/// </summary>
public enum DamagePopupStyle {
    Normal,           // 내 캐릭터가 적에게 준 일반 피해
    Critical,         // 내 캐릭터가 적에게 준 치명타 피해
    PlayerHit,        // 적이 내 캐릭터를 때려 입힌 일반 피해(피격, 전용 프리팹)
    PlayerHitCritical, // 적이 내 캐릭터를 때려 입힌 치명타 피해(피격 치명타, 전용 프리팹)
    Heal               // 내 캐릭터가 회복한 양(투자 스쿼드 재생 등). 피해와 색/기호가 달라야 하므로 전용 프리팹을 쓴다.
}

/// <summary>
/// 데미지 팝업 오브젝트 풀. 일반/치명타/피격 프리팹을 각각 별도 풀로 관리한다(PLANNING 1-14).
/// BattleDirector 가 Configure 로 설정값을 넘기고 Spawn 으로 팝업을 띄운다.
/// 새 스타일이 필요하면 enum + 프리팹 필드 + 풀 하나씩만 추가하면 된다.
/// </summary>
public sealed class DamagePopupPool : MonoBehaviour {
    [SerializeField] private DamagePopup normalPrefab;
    [SerializeField] private DamagePopup critPrefab;
    [Tooltip("적이 내 캐릭터를 때려 피격될 때(플레이어 피해) 전용 팝업 프리팹. 비우면 일반 팝업으로 대체된다.")]
    [SerializeField] private DamagePopup playerHitPrefab;
    [Tooltip("적이 내 캐릭터를 치명타로 때렸을 때 전용 팝업 프리팹. 비우면 일반 피격 팝업으로 대체된다.")]
    [SerializeField] private DamagePopup playerHitCritPrefab;
    [Tooltip("회복량 표시 전용 팝업 프리팹. 비우면 회복 표시가 나오지 않는다(피해 팝업으로 대체하면 회복인지 피해인지 구분되지 않는다).")]
    [SerializeField] private DamagePopup healPrefab;
    [Tooltip("생성된 팝업을 담을 부모(비우면 이 오브젝트).")]
    [SerializeField] private Transform container;

    private readonly Queue<DamagePopup> _normalPool = new();
    private readonly Queue<DamagePopup> _critPool = new();
    private readonly Queue<DamagePopup> _playerHitPool = new();
    private readonly Queue<DamagePopup> _playerHitCritPool = new();
    private readonly Queue<DamagePopup> _healPool = new();
    private BattleSettings _settings;

    // 최근에 뜬 팝업들의 위치/시각. 근처에 겹쳐 뜬 개수만큼 좌우로 벌려 읽기 쉽게 하기 위해 추적한다.
    private readonly List<RecentPopup> _recent = new();

    private struct RecentPopup {
        public Vector3 pos;
        public float time;
    }

    public void Configure(BattleSettings settings) {
        _settings = settings;
        if (container == null) container = transform;
    }

    /// <param name="text">숫자 대신 표시할 문구(MISS 등). 비우면 amount 를 숫자로 표시한다.</param>
    public void Spawn(Vector3 worldPos, float amount, DamagePopupStyle style, string text = null) {
        if (_settings == null) return;
        // 전용 프리팹 미할당 시 단계적으로 폴백(프리팹/풀을 같은 스타일로 일치시킨다).
        // 피격 치명타 -> 일반 피격 -> 일반. 프리팹을 안 만들어도 표시가 사라지지 않는다.
        if (style == DamagePopupStyle.PlayerHitCritical && playerHitCritPrefab == null) style = DamagePopupStyle.PlayerHit;
        if (style == DamagePopupStyle.PlayerHit && playerHitPrefab == null) style = DamagePopupStyle.Normal;

        var prefab = PrefabFor(style);
        if (prefab == null) return;

        int spreadIndex = CountRecentNear(worldPos);   // 근처에 겹친 최근 팝업 수 = 이 팝업이 벌어질 칸.

        var pool = PoolFor(style);
        DamagePopup popup = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab, container);
        popup.gameObject.SetActive(true);
        popup.Bind(p => {
            p.gameObject.SetActive(false);
            PoolFor(style).Enqueue(p);
        });
        popup.Play(worldPos, amount, _settings, spreadIndex, text);
    }

    // 최근(윈도우 내) & 반경 안에서 뜬 팝업 수를 세고, 지난 것들은 정리한다.
    // 격리 대상이 없으면 0(중앙 표시)이 되어 단발 피해는 대상 위에 그대로 뜬다.
    private int CountRecentNear(Vector3 worldPos) {
        float now = Time.time;
        float radiusSqr = _settings.DamagePopupSpreadRadius * _settings.DamagePopupSpreadRadius;
        int count = 0;
        for (int i = _recent.Count - 1; i >= 0; i--) {
            if (now - _recent[i].time > _settings.DamagePopupSpreadWindow) {
                _recent.RemoveAt(i);
                continue;
            }
            if ((_recent[i].pos - worldPos).sqrMagnitude <= radiusSqr) count++;
        }
        _recent.Add(new RecentPopup { pos = worldPos, time = now });
        return count;
    }

    private DamagePopup PrefabFor(DamagePopupStyle style) => style switch {
        DamagePopupStyle.Critical => critPrefab,
        DamagePopupStyle.PlayerHit => playerHitPrefab,
        DamagePopupStyle.PlayerHitCritical => playerHitCritPrefab,
        DamagePopupStyle.Heal => healPrefab,
        _ => normalPrefab
    };

    private Queue<DamagePopup> PoolFor(DamagePopupStyle style) => style switch {
        DamagePopupStyle.Critical => _critPool,
        DamagePopupStyle.PlayerHit => _playerHitPool,
        DamagePopupStyle.PlayerHitCritical => _playerHitCritPool,
        DamagePopupStyle.Heal => _healPool,
        _ => _normalPool
    };
}
