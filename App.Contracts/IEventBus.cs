using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Contracts
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T @event) where T : IEvent;
        void Subscribe<T>(Func<T, Task> handler) where T : IEvent;
    }
}
