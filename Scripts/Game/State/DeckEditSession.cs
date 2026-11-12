using System;
using System.Collections.Generic;

/// <summary>
/// 편집 중인 덱 작업본. 덱 팝업이 소유하고 편성 패널이 함께 편집한다.
///
/// 왜 따로 두는가: 편성 편집이 두 화면에 걸쳐 있는데(덱 팝업이 열고 닫으며 커밋, 편성 패널이 실제 편집)
/// 규칙(정원/중복/최소 인원/대표)이 양쪽에 흩어지면 한쪽만 고쳐졌을 때 조용히 어긋난다. 규칙을 이 한 곳에 모으고
/// 두 화면은 이 객체만 만지게 한다.
///
/// 편집은 여기(로컬)에 쌓이고, 덱 팝업이 편집이 잠잠해질 때마다 "다음 런부터"(immediate=false) 확정한다.
/// 진행 중인 런에 즉시 끼워 넣지 않는 이유는, 그렇게 하면 던전이 끝날 때 실제로 싸우지 않은 새 편성원에게
/// 경험치가 지급되기 때문이다. 진행 중인 런에 반영하는 확정은 덱 팝업을 닫을 때 한 번만 한다.
///
/// 대표 규칙: 편성 순서의 첫 칸(index 0)이 곧 대표다. 편성 화면의 제일 왼쪽 슬롯이 대표 슬롯이며,
/// 거기에 끌어다 놓는 행위 자체가 대표 지정이 된다(대표 지정 버튼이 따로 필요 없다).
/// 편성 순서는 전투 배치 순서이기도 하므로(전장이 순서대로 원형 배치한다) 슬롯 순서는 그대로 의미가 있다.
/// </summary>
public sealed class DeckEditSession {
    /// <summary>편성 정원. 화면의 슬롯 수와 같아야 한다.</summary>
    public const int Capacity = 5;

    private readonly List<int> _members = new(Capacity);

    // 편성 해제로 남길 수 있는 최소 인원. 덱은 1명(전투가 비지 않게), 전선 스쿼드는 0명(비워 둘 수 있다).
    private readonly int _minMembers;

    /// <param name="minMembers">편성 해제로 남길 수 있는 최소 인원(기본 1).</param>
    public DeckEditSession(int minMembers = 1) {
        _minMembers = System.Math.Max(0, minMembers);
    }

    /// <summary>편성원(순서 = 슬롯 순서). 빈 슬롯은 목록에 담기지 않으므로 항상 앞쪽부터 채워진다.</summary>
    public IReadOnlyList<int> Members => _members;

    /// <summary>편집으로 확정본과 달라졌는지. 덱 팝업이 닫을 때 커밋 여부를 이 값으로 판단한다.</summary>
    public bool IsDirty { get; private set; }

    /// <summary>편성이 바뀔 때마다 발생(표시부 갱신용).</summary>
    public event Action Changed;

    /// <summary>대표 캐릭터. 첫 칸이 곧 대표이며, 비어 있으면 0.</summary>
    public int RepresentativeCharId => _members.Count > 0 ? _members[0] : 0;

    public int Count => _members.Count;
    public bool IsFull => _members.Count >= Capacity;

    /// <summary>슬롯의 캐릭터(빈 슬롯이면 0).</summary>
    public int At(int slotIndex) =>
        slotIndex >= 0 && slotIndex < _members.Count ? _members[slotIndex] : 0;

    public bool Contains(int charDataId) => charDataId != 0 && _members.Contains(charDataId);

    /// <summary>이 캐릭터가 있는 슬롯 번호(없으면 -1).</summary>
    public int IndexOf(int charDataId) => charDataId == 0 ? -1 : _members.IndexOf(charDataId);

    /// <summary>확정된 덱으로 작업본을 되돌린다(진입 시 / 커밋 실패 롤백 시).</summary>
    public void Reset(DeckState deck) {
        _members.Clear();
        if (deck != null) {
            // 대표를 첫 칸으로 끌어올린다. 확정본은 대표를 별도 필드로 들고 있지만 작업본은 "첫 칸 = 대표"라
            // 여기서 한 번 옮겨 두면 이후에는 순서만 다루면 된다.
            if (deck.SelectedCharIds.Contains(deck.RepresentativeCharId)) _members.Add(deck.RepresentativeCharId);
            foreach (int id in deck.SelectedCharIds) {
                if (id == deck.RepresentativeCharId || _members.Contains(id)) continue;
                if (_members.Count >= Capacity) break;
                _members.Add(id);
            }
        }
        IsDirty = false;
        Changed?.Invoke();
    }

    /// <summary>
    /// 순서가 정해진 편성 목록으로 작업본을 되돌린다(대표 필드가 없는 편성: 전선 스쿼드 등).
    /// 0/중복은 건너뛰고 정원까지만 담는다.
    /// </summary>
    public void Reset(IReadOnlyList<int> members) {
        _members.Clear();
        if (members != null) {
            foreach (int id in members) {
                if (id == 0 || _members.Contains(id)) continue;
                if (_members.Count >= Capacity) break;
                _members.Add(id);
            }
        }
        IsDirty = false;
        Changed?.Invoke();
    }

    /// <summary>커밋이 성공했을 때 "확정과 같아졌음"을 표시한다.</summary>
    public void MarkCommitted() => IsDirty = false;

    /// <summary>
    /// 캐릭터를 지정 슬롯에 놓는다(편성 목록에서 슬롯으로 끌어다 놓는 동작).
    ///   - 이미 편성된 캐릭터면 그 슬롯과 자리를 맞바꾼다(순서 변경).
    ///   - 슬롯이 차 있으면 그 자리의 캐릭터를 밀어내고 대신 들어간다(교체).
    ///   - 슬롯 번호가 현재 인원보다 뒤면 맨 뒤에 붙는다(빈 칸을 사이에 두지 않는다).
    /// 정원이 찼는데 새 캐릭터를 빈 슬롯에 놓으려 하면 아무 일도 하지 않고 false 를 돌려준다.
    /// </summary>
    public bool Place(int slotIndex, int charDataId) {
        if (charDataId == 0 || slotIndex < 0 || slotIndex >= Capacity) return false;

        int current = IndexOf(charDataId);
        if (current >= 0) return MoveSlot(current, slotIndex);   // 이미 편성됨 -> 자리 이동(맞바꿈)

        if (slotIndex < _members.Count) {
            if (_members[slotIndex] == charDataId) return false;
            _members[slotIndex] = charDataId;                     // 그 자리의 캐릭터를 교체
        } else {
            if (IsFull) return false;
            _members.Add(charDataId);                             // 빈 슬롯 -> 맨 뒤에 편성
        }
        MarkChanged();
        return true;
    }

    /// <summary>두 슬롯의 캐릭터를 맞바꾼다. 대상 슬롯이 비어 있으면 그쪽으로 옮긴다(순서 이동).</summary>
    public bool MoveSlot(int fromSlot, int toSlot) {
        if (fromSlot < 0 || fromSlot >= _members.Count || toSlot < 0 || toSlot >= Capacity) return false;
        if (fromSlot == toSlot) return false;

        if (toSlot < _members.Count) {
            (_members[fromSlot], _members[toSlot]) = (_members[toSlot], _members[fromSlot]);
        } else {
            // 빈 슬롯으로 끌면 맨 뒤로 보낸다(중간에 빈 칸이 생기지 않게).
            int id = _members[fromSlot];
            _members.RemoveAt(fromSlot);
            _members.Add(id);
        }
        MarkChanged();
        return true;
    }

    /// <summary>
    /// 슬롯을 비운다(편성 해제). 최소 인원(기본 1명)보다 줄일 수는 없다.
    /// 대표(첫 칸)를 빼면 다음 칸이 자동으로 대표가 된다.
    /// </summary>
    public bool RemoveAt(int slotIndex) {
        if (slotIndex < 0 || slotIndex >= _members.Count) return false;
        if (_members.Count <= _minMembers) return false;

        _members.RemoveAt(slotIndex);
        MarkChanged();
        return true;
    }

    /// <summary>지정 캐릭터를 편성에서 뺀다.</summary>
    public bool Remove(int charDataId) => RemoveAt(IndexOf(charDataId));

    private void MarkChanged() {
        IsDirty = true;
        Changed?.Invoke();
    }
}
