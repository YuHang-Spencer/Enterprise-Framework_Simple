using App.Contracts;
using App.Infrastructure.Tracing;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace App.Infrastructure
{
    public class EventBus : IEventBus
    {
        // 使用一个字典 _handlers 用来存储每种事件类型对应的处理器列表。
        // ConcurrentDictionary 内部使用分段锁，是线程安全的，支持并发读写，可以在多线程环境中安全地添加和读取处理器列表。
        private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _handlers
        = new();
        private readonly int _retryCount;

        public EventBus(int retryCount = 0)
        {
            _retryCount = retryCount;
        }
        // Publish 将 handler 的执行放在一个新的任务中并在未来某个时间段执行，这样 Publish 方法可以立即返回，而不需要等待处理器完成。
        // 异步方式适合处理器执行时间较长的情况，避免阻塞发布事件的线程。这样可以提高系统的响应性。
        public async Task PublishAsync<T>(T @event) where T : IEvent
        {
            using var activity =
                Telemetry.ActivitySource.StartActivity(
                    "EventBus.Publish",
                    ActivityKind.Internal);
            activity?.SetTag("EventType", typeof(T).Name);

            // 这里使用 Console.WriteLine 来输出发布事件的日志信息，包括事件类型和一个唯一的 Trace ID。Trace ID 可以帮助我们在
            // 日志中跟踪事件的流转，尤其是在复杂的系统中，多个事件可能同时发生，Trace ID 可以帮助我们区分不同的事件流。
            // TraceManager.Trace("Before Publish");

            if (!_handlers.TryGetValue(typeof(T), out var handlers))
                return;

            List<Func<object, Task>> snapshot;
            // Dic是线程安全的，但 List 不是，所以在访问 handlers 时需要加锁，确保在创建快照时不会有其他线程修改 handlers 列表。
            lock (handlers)
            {
                snapshot = handlers.ToList();  // LINQ 默认是延迟执行，ToList（）是 立即执行 + 转成List 拍快照
            }

            // Select 是映射，集合中的每个元素都通过一个函数进行转换，返回一个新的集合。可以理解成：
            // foreach (var h in snapshot)
            // {
            //     var task = ExecuteWithIsolation(h, @event);
            // }
            var tasks = snapshot.Select(h => {
                return ExecuteWithIsolation(h, @event);
            });
            // 并发执行所有处理器，等待它们完成。这样可以提高处理效率，尤其是在有多个处理器的情况下。
            // 等价于：
            // foreach (var task in tasks)
            // {
            //     await task;
            // }
            // 区别：foreach +await = 顺序执行,WhenAll = 并发执行（优化为并行等待，等待一组 Task 完成
            // 它：不创建线程(线程是否产生取决于 Task 本身如何创建)、不调度线程、不决定任务在哪里执行、只是“组合等待器”）
            await Task.WhenAll(tasks);
            // TraceManager.Trace("After Publish");
        }
        private async Task ExecuteWithIsolation(Func<object, Task> handler, object evt)
        {
            int attempt = 0;

            while (true)
            {
                try
                {
                    TraceManager.Trace("Before Handler");
                    await handler(evt);
                    TraceManager.Trace("After Handler");
                    return;
                }
                catch (Exception ex)
                {
                    attempt++;

                    Console.WriteLine($"Handler failed: {ex.Message}");

                    if (attempt > _retryCount)
                    {
                        Console.WriteLine("Retry limit reached.");
                        return; // 异常隔离，不抛出
                    }

                    await Task.Delay(200);
                }
            }
        }
        public void Subscribe<T>(Func<T, Task> handler) where T : IEvent
        {
            var handlers = _handlers.GetOrAdd(typeof(T), _ => new List<Func<object, Task>>());

            lock (handlers)   // ConcurrentDic线程安全，不保证其单键或值同样安全，故只锁单个事件类型
            {
                handlers.Add(e => handler((T)e));
            }
        }
        // 同步是异步的特殊情况，直接调用 handler 并返回一个已完成的任务。
        public void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            // 同步 handler 的处理：整个 Lambda 表达式就是一个 Func<T,Task>,将其作为参数传给另一个Subscribe方法，,把同步 Action<T> 包装成 异步 Func<T, Task>
            // 这种方式允许用户使用同步的处理器，同时保持事件总线的异步特性。
            Subscribe<T>(e =>
            {
                // 调用外部传进来同步方法，真正执行事件处理逻辑的地方。这里直接调用 handler(e)，然后返回一个已完成的任务，表示处理器已经完成了它的工作。 
                handler(e);
                return Task.CompletedTask;
            });
        }
    }
}
