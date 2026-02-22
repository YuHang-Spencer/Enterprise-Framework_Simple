using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace App.Infrastructure.Tracing
{
    public static class TraceManager
    {
        public static AsyncLocal<string> TraceId = new AsyncLocal<string>();

        public static void Trace(string message)
        {
            Console.WriteLine(
                $"[Trace:{TraceId.Value ?? "NULL"}] " +
                $"[Thread:{Environment.CurrentManagedThreadId}] " +
                message);
        }
    }
}
