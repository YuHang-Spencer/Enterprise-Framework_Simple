using App.Contracts;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace App.ModuleA
{
    public class ModuleARunner
    {
        private readonly IEventBus eventBus;

        public ModuleARunner(IEventBus eventBus)
        {
            this.eventBus = eventBus;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Module A Running");
        
            await eventBus.PublishAsync(new ModuleACompletedEvent
            {
                Message = "A Has Done"
            });
        }
    }
}
