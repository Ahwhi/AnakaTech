using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프로필 테두리 표시 카탈로그. id -> 스프라이트/이름. 서버 ProfileFrameCatalog 의 id 규약과 1:1로 일치한다.
/// (기본=0, 오픈=1, 병아리=2, 부자=3, 회장=4, 강아지=5, 고양이=6, 다이아몬드=7, 자수정=8, 별=9, 하트=10,
///  해골=11, 키드큐트=12, 오피스=13, 몽글토끼=14, 루나=15).
/// 새 테두리 추가 = 서버 ProfileFrameCatalog 에 id 추가 + 이 에셋에 Entry 추가.
/// MainLifetimeScope 가 RegisterInstance 로 등록해 프로필/테두리 UI 가 주입받는다(규칙 5).
///
/// id 0(DefaultId)은 "아무것도 장착하지 않은 상태"이자 곧 기본 테두리다. 서버는 0 을 지급 대상으로 다루지 않고
/// 장착 해제 값으로만 쓰므로, 이 에셋에 id 0 항목을 넣어 두면 미장착 계정도 기본 테두리를 두른 모습으로 보인다.
/// 항목을 비워 두면 미장착일 때 테두리 없이 맨 일러스트가 나온다(둘 다 정상 동작).
/// </summary>
[CreateAssetMenu(menuName = "Anaka/Profile Frame Catalog", fileName = "ProfileFrameCatalog")]
public sealed class ProfileFrameCatalog : ScriptableObject {
    /// <summary>기본 테두리 id. 미장착 상태(서버의 장착 해제 값)와 같은 값이며 모든 계정이 항상 쓸 수 있다.</summary>
    public const int DefaultId = 0;

    [Serializable]
    public struct Entry {
        [Tooltip("서버 ProfileFrameCatalog 의 id 와 일치해야 한다. 0 은 기본 테두리(미장착 상태) 전용이다.")]
        public int id;
        public string displayName;
        [Tooltip("대표 캐릭터 일러스트 위에 겹쳐 그릴 테두리 스프라이트. 가운데가 비어 있는(9-slice 또는 액자형) 이미지를 쓴다.")]
        public Sprite icon;
    }

    [SerializeField] private Entry[] entries;

    private Dictionary<int, Entry> _map;

    private void EnsureMap() {
        if (_map != null) return;
        _map = new Dictionary<int, Entry>();
        if (entries == null) return;
        foreach (var e in entries) _map[e.id] = e;
    }

    public IReadOnlyList<Entry> All => entries ?? Array.Empty<Entry>();

    public bool TryGet(int id, out Entry entry) {
        EnsureMap();
        return _map.TryGetValue(id, out entry);
    }

    public Sprite Icon(int id) => TryGet(id, out var e) ? e.icon : null;

    public string DisplayName(int id) {
        if (TryGet(id, out var e) && !string.IsNullOrEmpty(e.displayName)) return e.displayName;
        return id == DefaultId ? "기본 테두리" : $"테두리 {id}";
    }

    private void OnValidate() => _map = null;
}
