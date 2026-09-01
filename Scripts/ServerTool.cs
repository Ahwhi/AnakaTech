using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ServerTool : MonoBehaviour {
    private static string RootPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

    private static string ServerPath =>
        Path.Combine(RootPath, "Server");

    [Header("Paths (Auto)")]
    [SerializeField] private string serverSolution;
    [SerializeField] private string appsettings;
    [SerializeField] private string serverExe;
    [SerializeField] private string genPackets;

    private void OnValidate() {
        serverSolution = Path.Combine(ServerPath, "Server.sln");
        appsettings = Path.Combine(ServerPath, "Server", "bin", "Debug", "net8.0", "Data", "appsettings.json");
        serverExe = Path.Combine(ServerPath, "Server", "bin", "Debug", "net8.0", "Server.exe");
        genPackets = Path.Combine(ServerPath, "Common", "Packet", "GenPackets.bat");
    }

    public void OpenSolution() => Launch(serverSolution);
    public void OpenConfig() => Launch(appsettings);
    public void RunServer() => Launch(serverExe, useWorkingDir: true);
    public void RunGenPackets() => Launch(genPackets, useWorkingDir: true);

    private static void Launch(string path, bool useWorkingDir = false) {
        if (!File.Exists(path)) {
            Debug.LogError($"[ServerTool] Not Found: {path}");
            return;
        }

        var startInfo = new ProcessStartInfo(path) { UseShellExecute = true };
        if (useWorkingDir) startInfo.WorkingDirectory = Path.GetDirectoryName(path);

        Process.Start(startInfo);
    }
}
