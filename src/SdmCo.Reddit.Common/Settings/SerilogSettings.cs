namespace SdmCo.Reddit.Common.Settings;

public sealed class SerilogSettings
{
    public const string SectionName = "Serilog";

    public bool UseConsole { get; set; } = true;

    public string LogTemplate { get; set; } =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level} - {Message:lj}{NewLine}{Exception}";
}