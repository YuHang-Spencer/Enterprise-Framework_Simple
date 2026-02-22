using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace App.Infrastructure
{
    public static class Telemetry
    {
        public static readonly ActivitySource ActivitySource =
            new ActivitySource("App");

        public const string ServiceName = "App";
    }
}

