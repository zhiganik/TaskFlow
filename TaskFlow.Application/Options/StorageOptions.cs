namespace TaskFlow.Application.Options;

public class StorageOptions
{
    public string   BasePath          { get; set; } = "/app/uploads";
    public long     MaxFileSizeBytes  { get; set; } = 20_971_520; // 20 MB
    public string[] BlockedExtensions { get; set; } =
    [
        ".exe", ".bat", ".cmd", ".ps1", ".sh", ".msi",
        ".dll", ".com", ".scr", ".vbs", ".jar", ".app", ".dmg", ".bin", ".run"
    ];
}
