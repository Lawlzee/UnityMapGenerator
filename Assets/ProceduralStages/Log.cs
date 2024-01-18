using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProceduralStages
{
    public static class Log
    {
        private static ManualLogSource _logSource;

        public static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        public static void Debug(object data)
        {
            if (_logSource is null)
            {
                UnityEngine.Debug.Log(data);
                return;
            }

            _logSource.LogDebug(data);
        }

        public static void Error(object data)
        {
            if (_logSource is null)
            {
                UnityEngine.Debug.LogError(data);
                return;
            }

            _logSource.LogError(data);
        }

        public static void Fatal(object data)
        {
            if (_logSource is null)
            {
                UnityEngine.Debug.LogError(data);
                return;
            }

            _logSource.LogFatal(data);
        }

        public static void Info(object data)
        {
            if (_logSource is null)
            {
                UnityEngine.Debug.Log(data);
                return;
            }

            _logSource.LogInfo(data);
        }

        public static void Message(object data)
        {
            if (_logSource is null)
            {
                UnityEngine.Debug.Log(data);
                return;
            }

            _logSource.LogMessage(data);
        }

        public static void Warning(object data)
        {
            if (_logSource is null)
            {
                UnityEngine.Debug.LogWarning(data);
                return;
            }

            _logSource.LogWarning(data);
        }
    }
}
