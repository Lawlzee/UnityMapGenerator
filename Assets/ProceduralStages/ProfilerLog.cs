using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProceduralStages
{
    public class ProfilerLog : IDisposable
    {
        public static ProfilerLog Current { get; private set; } = new ProfilerLog(null, null);

        private readonly ProfilerLog _parent;
        private readonly Stopwatch _stopwatch;
        private readonly string _name;
        private readonly int _depth;

        private ProfilerLog(ProfilerLog parent, string name)
        {
            _parent = parent;
            _stopwatch = Stopwatch.StartNew();
            _name = name;
            _depth = (parent?._depth ?? 0) + 1;
        }

        public static void Reset()
        {
            while (Current._parent != null)
            {
                Current = Current._parent;
            }

            Current._stopwatch.Restart();
        }

        public static void Debug(object data)
        {
            Log.Debug(GetMessage(data));
            Current._stopwatch.Restart();
        }

        public static void Error(object data)
        {
            Log.Error(GetMessage(data));
            Current._stopwatch.Restart();
        }

        public static void Fatal(object data)
        {
            Log.Fatal(GetMessage(data));
            Current._stopwatch.Restart();
        }

        public static void Info(object data)
        {
            Log.Info(GetMessage(data));
            Current._stopwatch.Restart();
        }

        public static void Message(object data)
        {
            Log.Message(GetMessage(data));
            Current._stopwatch.Restart();
        }

        public static void Warning(object data)
        {
            Log.Warning(GetMessage(data));
            Current._stopwatch.Restart();
        }

        private static string GetMessage(object data)
        {
            return $"Profiler <{Current._depth}> {data}: {Current._stopwatch.Elapsed}";
        }

        public static ProfilerLog CreateScope(string name)
        {
            Current = new ProfilerLog(Current, name);
            return Current;
        }

        public void Dispose()
        {
            Current = _parent;
            Debug(_name);
        }
    }
}
