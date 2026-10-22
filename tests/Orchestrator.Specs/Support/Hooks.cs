namespace Orchestrator.Specs.Support;

[Binding]
public sealed class Hooks
{
    [BeforeTestRun]
    public static Task BeforeTestRun() => ApiHost.StartAsync();

    [AfterTestRun]
    public static Task AfterTestRun() => ApiHost.StopAsync();
}
