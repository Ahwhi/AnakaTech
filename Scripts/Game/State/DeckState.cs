using System.Collections.Generic;

/// <summary>현재 덱 구성. 선택 캐릭터(최대 5, 최소 1)와 대표 캐릭터(PLANNING 1-2,4).</summary>
public sealed class DeckState {
    public readonly List<int> SelectedCharIds = new();
    public int RepresentativeCharId;

    public bool IsValid => SelectedCharIds.Count >= 1 && SelectedCharIds.Contains(RepresentativeCharId);

    public void Set(IReadOnlyList<int> ids, int representativeId) {
        SelectedCharIds.Clear();
        if (ids != null) {
            for (int i = 0; i < ids.Count && SelectedCharIds.Count < 5; i++)
                if (!SelectedCharIds.Contains(ids[i])) SelectedCharIds.Add(ids[i]);
        }
        RepresentativeCharId = representativeId;
        // 대표가 선택 목록에 없으면 첫 캐릭터를 대표로 보정.
        if (!SelectedCharIds.Contains(RepresentativeCharId) && SelectedCharIds.Count > 0)
            RepresentativeCharId = SelectedCharIds[0];
    }
}
