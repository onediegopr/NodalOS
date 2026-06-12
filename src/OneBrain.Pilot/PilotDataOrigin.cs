namespace OneBrain.Pilot;

public static class PilotDataOrigins
{
    public const string Runtime = "runtime";
    public const string DemoFixture = "demo_fixture";
    public const string Mock = "mock";
    public const string Unknown = "unknown";

    public static string Label(string origin)
    {
        return origin switch
        {
            Runtime => "Datos reales/runtime local",
            DemoFixture => "Demo / Simulacion segura",
            Mock => "Mock / Simulacion segura",
            _ => "Origen desconocido"
        };
    }

    public static bool IsDemoLike(string origin)
    {
        return origin is DemoFixture or Mock or Unknown;
    }
}
