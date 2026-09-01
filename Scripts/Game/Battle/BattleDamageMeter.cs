using System.Collections.Generic;

/// <summary>
/// 이번 런의 캐릭터별 누적 딜량 집계(딜량 그래프의 단일 진실 원천).
/// 전투 유닛이 데미지를 입을 때마다 가해자 기준으로 누적하고, UI 는 순위 스냅샷만 읽어간다.
/// 순수 C# 이라 UI/유닛 어느 쪽에도 의존하지 않는다(집계 = 모델, 표현 = 패널).
///
/// 캐릭터 id 로 누적하므로 런 도중 유닛이 죽고 부활해도, 덱이 바뀌어도 값이 이어지거나 끊기지 않는다.
/// 던전이 바뀔 때마다 디렉터가 Reset 을 호출해 런 단위 기록으로 유지한다.
/// </summary>
public sealed class BattleDamageMeter {
    /// <summary>순위 스냅샷 1행. Share 는 전체 대비 비율(0~1), TopRatio 는 1위 대비 비율(0~1, 막대 길이용).</summary>
    public struct Entry {
        public int CharId;
        public float Damage;
        public float Share;
        public float TopRatio;
    }

    private readonly Dictionary<int, float> _damageByChar = new();
    private float _total;

    /// <summary>이번 런의 총 누적 딜량(전체 대비 비율의 분모).</summary>
    public float Total => _total;

    /// <summary>가해자 캐릭터의 누적 딜량에 amount 를 더한다. 음수/0 은 무시한다.</summary>
    public void Add(int charId, float amount) {
        if (amount <= 0f) return;
        _damageByChar.TryGetValue(charId, out float cur);
        _damageByChar[charId] = cur + amount;
        _total += amount;
    }

    /// <summary>특정 캐릭터의 누적 딜량(기록이 없으면 0).</summary>
    public float DamageOf(int charId) =>
        _damageByChar.TryGetValue(charId, out float v) ? v : 0f;

    /// <summary>런 종료/시작 시 기록을 비운다.</summary>
    public void Reset() {
        _damageByChar.Clear();
        _total = 0f;
    }

    /// <summary>
    /// charIds 에 해당하는 행을 딜량 내림차순으로 outList 에 채운다(호출측 리스트를 재사용하므로 매 갱신 할당이 없다).
    /// 아직 딜을 넣지 않은 캐릭터도 0 으로 포함되므로, 패널은 파티 구성 그대로의 행 수를 유지할 수 있다.
    /// </summary>
    public void BuildRanking(IReadOnlyList<int> charIds, List<Entry> outList) {
        outList.Clear();
        if (charIds == null) return;

        float top = 0f;
        for (int i = 0; i < charIds.Count; i++) {
            float dmg = DamageOf(charIds[i]);
            if (dmg > top) top = dmg;
            outList.Add(new Entry { CharId = charIds[i], Damage = dmg });
        }

        for (int i = 0; i < outList.Count; i++) {
            var e = outList[i];
            e.Share = _total > 0f ? e.Damage / _total : 0f;
            e.TopRatio = top > 0f ? e.Damage / top : 0f;
            outList[i] = e;
        }

        // 딜량 내림차순. 동점이면 id 순으로 고정해 매 갱신마다 순서가 흔들리지 않게 한다(0딜 구간 포함).
        outList.Sort((a, b) => {
            int cmp = b.Damage.CompareTo(a.Damage);
            return cmp != 0 ? cmp : a.CharId.CompareTo(b.CharId);
        });
    }
}
