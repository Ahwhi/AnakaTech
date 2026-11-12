using System.Collections.Generic;

/// <summary>
/// 캐릭터 id 목록의 CSV 직렬화 규약. 편성(파견/전선 등)은 패킷에 리스트를 중첩하지 않기 위해 CSV 한 줄로 오가며,
/// 그 규칙(0 은 빈 칸이라 싣지 않는다, 숫자가 아닌 조각은 버린다)을 이 한 곳에 둔다.
/// </summary>
public static class IdCsv {
    public static string Format(IReadOnlyList<int> ids) {
        if (ids == null || ids.Count == 0) return "";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < ids.Count; i++) {
            if (ids[i] == 0) continue;
            if (sb.Length > 0) sb.Append(',');
            sb.Append(ids[i]);
        }
        return sb.ToString();
    }

    public static void Parse(string csv, List<int> into) {
        into.Clear();
        if (string.IsNullOrEmpty(csv)) return;
        foreach (var part in csv.Split(',')) {
            if (int.TryParse(part.Trim(), out int v) && v != 0) into.Add(v);
        }
    }
}
