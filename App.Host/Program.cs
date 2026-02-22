using App.Contracts;
using App.Infrastructure;
using App.Infrastructure.Tracing;
using App.ModuleA;
using App.ModuleB;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

using OpenTelemetry;
using OpenTelemetry.Trace;

class Program
{
    static async Task Main()
    {
    
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        var listener = new ActivityListener
        {
            ShouldListenTo = source => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) =>
                ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
                Console.WriteLine(
                    $"START {activity.DisplayName} " +
                    $"TraceId:{activity.TraceId} " +
                    $"SpanId:{activity.SpanId}"),
            ActivityStopped = activity =>
                Console.WriteLine(
                    $"STOP  {activity.DisplayName}")
        };

        ActivitySource.AddActivityListener(listener);

        TraceManager.TraceId.Value = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        // 注册基础设施
        services.AddSingleton<IEventBus, SimpleEventBus>();

        // 注册模块
        services.AddTransient<ModuleARunner>();
        services.AddTransient<ModuleBRunner>();
        
        var provider = services.BuildServiceProvider();

        // 解析模块
        var moduleA = provider.GetRequiredService<ModuleARunner>();
        var moduleB = provider.GetRequiredService<ModuleBRunner>();

        await moduleA.RunAsync();
        await moduleB.RunAsync();
        //await Task.WhenAll(
        //    moduleA.RunAsync(),
        //    moduleB.RunAsync()
        //    );


        Console.ReadKey();
    }
}

