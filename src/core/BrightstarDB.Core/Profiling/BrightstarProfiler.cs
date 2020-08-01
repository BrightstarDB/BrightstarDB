using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BrightstarDB.Profiling
{
    internal class BrightstarProfiler 
    {
        public Guid Id { get; private set; }
        public DateTime Started { get; private set; }
        public Timing Root { get; private set; }
        public Dictionary<string, long> Counters { get; private set; }
        private readonly Stopwatch _sw;
        internal Timing Head { get; set; }

        internal long ElapsedTicks
        {
            get { return _sw.ElapsedTicks; }
        }

        public BrightstarProfiler(string name)
        {
            Id = Guid.NewGuid();
            Started = DateTime.UtcNow;
            _sw = Stopwatch.StartNew();
            Counters = new Dictionary<string, long>(10);
            Root = new Timing(this, null, name);
        }

        internal decimal GetRoundedMilliseconds(long stopwatchElapsedTicks)
        {
            long z = 10000*stopwatchElapsedTicks;
            decimal msTimesTen = (int) (z/Stopwatch.Frequency);
            return msTimesTen/10;
        }

        internal IDisposable StepImpl(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name","Step name must not be null");
            }
            Timing t;
            if (Head.Children != null && (t = Head.Children.FirstOrDefault(c=>c.Name.Equals(name))) != null)
            {
                t.StartRepetition();
                return t;
            } 
            return new Timing(this, Head, name);
        }

        internal void IncrImpl(string name)
        {
            long val;
            if (!Counters.TryGetValue(name, out val))
            {
                Counters.Add(name, 1);
            } else
            {
                Counters[name] = val + 1;
            }
        }

        internal static BrightstarProfiler Current { get; set; }

        internal string GetLogString()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendFormat("{0,-44} {1,12} {2,10} {3,10}\r\n", "name", "duration(ms)", "count", "avg(ms)");
            Root.Log(sb);
            sb.AppendLine();
            sb.AppendLine("Counters:");
            foreach(var c in Counters.OrderBy(e=>e.Key))
            {
                sb.AppendFormat("\t{0, -44} : {1,10}\r\n", c.Key, c.Value);
            }
            return sb.ToString();
        }
    }

    internal static class BrightstarProfilerExtensions
    {
        internal static IDisposable Step(this BrightstarProfiler profiler, string name)
        {
            if (profiler == null) return null;
            return profiler.StepImpl(name);
        }

        internal static void Incr(this BrightstarProfiler profiler, string name)
        {
            if (profiler != null) profiler.IncrImpl(name);
        }
    }

    internal class Timing : IDisposable
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal? DurationMilliseconds { get; set; }
        public decimal? StartMilliseconds { get; set; }
        public List<Timing> Children { get; set; }
        public Timing ParentTiming { get; set; }
        public int Repetitions { get; private set; }
        internal BrightstarProfiler Profiler { get; private set; }
        private long _startTicks;
        private long _elapsedTicks;

        public Timing(BrightstarProfiler profiler, Timing parent, string name)
        {
            Id = Guid.NewGuid();
            Profiler = profiler;
            Profiler.Head = this;
            if (parent != null)
            {
                parent.AddChild(this);
            }
            Name = name;
            Repetitions = 1;
            _startTicks = profiler.ElapsedTicks;
            StartMilliseconds = profiler.GetRoundedMilliseconds(_startTicks);
        }

        public void AddChild(Timing timing)
        {
            if (Children == null) Children = new List<Timing>();
            Children.Add(timing);
            timing.ParentTiming = this;
        }

        public void StartRepetition()
        {
            _startTicks = Profiler.ElapsedTicks;
            Repetitions += 1;
            Profiler.Head = this;
        }

        public void Stop()
        {
            _elapsedTicks += (Profiler.ElapsedTicks - _startTicks);
            DurationMilliseconds = Profiler.GetRoundedMilliseconds(_elapsedTicks);
            Profiler.Head = ParentTiming;
        }

        public void Dispose()
        {
            Stop();
        }

        public void Log(StringBuilder sb, string indent="")
        {
            sb.AppendFormat("{0,-44} {1,12:F1} {2,10} {3,10:F1}\r\n", indent + Name, DurationMilliseconds, Repetitions, Profiler.GetRoundedMilliseconds(_elapsedTicks / Repetitions));
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.Log(sb, indent + "  ");
                }
            }
        }

    }

}
