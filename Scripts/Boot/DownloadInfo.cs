/// <summary>
/// 어드레서블 다운로드 진행 정보. 퍼센트뿐 아니라 받은/전체 바이트를 함께 전달해 상용 게임식 표시
/// (예: "123.4 / 456.7 MB (27%)")를 가능하게 한다. 어드레서블은 파일 개수가 아니라 바이트(DownloadStatus)로
/// 진행을 보고하므로, 여기서도 바이트를 진행 단위로 쓴다.
/// </summary>
public readonly struct DownloadInfo {
    public readonly long DownloadedBytes;
    public readonly long TotalBytes;
    public readonly float Percent;   // 0~1

    public DownloadInfo(long downloadedBytes, long totalBytes, float percent) {
        DownloadedBytes = downloadedBytes;
        TotalBytes = totalBytes;
        Percent = percent;
    }

    public static readonly DownloadInfo Zero = new(0, 0, 0f);

    // 사람이 읽기 쉬운 단위 문자열(KB/MB/GB). 표시/로그 공용.
    public static string FormatBytes(long bytes) {
        if (bytes >= 1024L * 1024L * 1024L) return $"{bytes / (1024f * 1024f * 1024f):0.00} GB";
        if (bytes >= 1024L * 1024L)         return $"{bytes / (1024f * 1024f):0.0} MB";
        if (bytes >= 1024L)                 return $"{bytes / 1024f:0.0} KB";
        return $"{bytes} B";
    }
}
