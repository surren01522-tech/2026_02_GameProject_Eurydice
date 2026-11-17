using UnityEngine;

namespace Kamgam.SkyClouds.URP
{
    public class Logger
    {
        public delegate void LogCallback(string msg, LogLevel logLevel);
        
        public const string Prefix = "SkyClouds.URP: ";
        public static LogLevel CurrentLogLevel = LogLevel.Warning;

        /// <summary>
        /// Optional: leave as is or set to NULL to not use it.<br />
        /// Set this to a function which returns the log level (from settings for example).<br />
        /// This will be called before every log.
        /// <example>
        /// [RuntimeInitializeOnLoadMethod]
        /// private static void HookUpToLogger()
        /// {
        ///     Logger.OnGetLogLevel = () => GetOrCreateSettings().LogLevel;
        /// }
        /// </example>
        /// </summary>
        public static System.Func<LogLevel> OnGetLogLevel = null;

        public enum LogLevel
		{
			Log     =  0,
			Warning =  1,
			Error   =  2,
			Message =  3,
			NoLogs  = 99
		}

        public static bool IsLogLevelVisible(LogLevel logLevel)
        {
            return (int)logLevel >= (int)CurrentLogLevel;
        }

        public static void UpdateCurrentLogLevel()
        {
            if (OnGetLogLevel != null)
            {
                CurrentLogLevel = OnGetLogLevel();
            }
        }

        public static void Log(string message, GameObject go = null)
        {
            UpdateCurrentLogLevel();
            if (IsLogLevelVisible(LogLevel.Log))
            {
                if (go == null)
                    Debug.Log(Prefix + message);
                else
                    Debug.Log(Prefix + message, go);
            }
        }

        public static void LogWarning(string message, GameObject go = null)
        {
            UpdateCurrentLogLevel();
            if (IsLogLevelVisible(LogLevel.Warning))
            {
                if (go == null)
                    Debug.LogWarning(Prefix + message);
                else
                    Debug.LogWarning(Prefix + message, go);
            }
        }

        public static void LogError(string message, GameObject go = null)
        {
            UpdateCurrentLogLevel();
            if (IsLogLevelVisible(LogLevel.Error))
            {
                if (go == null)
                    Debug.LogError(Prefix + message);
                else
                    Debug.LogError(Prefix + message, go);
            }
        }

        public static void LogMessage(string message, GameObject go = null)
        {
            UpdateCurrentLogLevel();
            if (IsLogLevelVisible(LogLevel.Message))
            {
                if (go == null)
                    Debug.Log(Prefix + message);
                else
                    Debug.Log(Prefix + message, go);
            }
        }
    }
}
