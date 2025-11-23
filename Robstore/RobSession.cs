using System;
using System.Threading;

namespace Nexus.Robstore
{
    public class RobSession
    {
        public string StoreName;
        public DateTime StartedAtUtc;
        public CancellationTokenSource Cts;
        public int Reward;
        public int DurationSeconds;
    }
}

