using App.Contracts;
using System.Diagnostics;

namespace App.ModuleB
{
    public class ModuleBRunner
    {
        public ModuleBRunner(IEventBus eventBus)
        {
            eventBus.Subscribe<ModuleACompletedEvent>(OnACompleted);
        }

        private async Task OnACompleted(ModuleACompletedEvent e)
        {
            // 如果回调方法内部没有真正 await 未完成任务 → 实际是同步完成,若 await 了未完成任务 → 才是真异步
            await Task.Delay(2000);
            Console.WriteLine($"ModuleB 收到事件: {e.Message}");
        }

        public Task RunAsync()
        {
            Console.WriteLine("Module B is Running");
            return Task.CompletedTask;
        }
    }
}