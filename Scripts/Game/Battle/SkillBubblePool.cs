using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 말풍선 오브젝트 풀. BattleDirector 가 Spawn 으로 시전자 머리 위에 말풍선을 띄운다(데미지 팝업 풀과 동일 패턴).
/// 프리팹 미할당이면 아무 것도 하지 않는다(널 세이프). 표시 여부(설정)는 상위(디렉터)가 판단한다.
/// </summary>
public sealed class SkillBubblePool : MonoBehaviour {
    [SerializeField] private SkillBubble prefab;
    [Tooltip("생성된 말풍선을 담을 부모(비우면 이 오브젝트).")]
    [SerializeField] private Transform container;

    private readonly Queue<SkillBubble> _pool = new();

    private void Awake() {
        if (container == null) container = transform;
    }

    public void Spawn(Vector3 worldPos, string text, Color color) {
        if (prefab == null) return;

        SkillBubble bubble = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(prefab, container);
        bubble.gameObject.SetActive(true);
        bubble.Bind(b => {
            b.gameObject.SetActive(false);
            _pool.Enqueue(b);
        });
        bubble.Play(worldPos, text, color);
    }
}
