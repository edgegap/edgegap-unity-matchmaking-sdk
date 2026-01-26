using System;
using UnityEngine;

namespace Edgegap.Matchmaking
{
    public static class Logger
    {
        public static void _Log<T>(T message)
        {
            if (!Debug.isDebugBuild)
                return;
            Debug.Log(_FormatLog(message));
        }

        public static void _Warn<T>(T message)
        {
            Debug.LogWarning(_FormatLog(message));
        }

        public static void _Error<T>(T message)
        {
            Debug.LogError(_FormatLog(message));
        }

        public static string _FormatLog<T>(T message) =>
            $"{DateTime.UtcNow} Matchmaker | {_ToStringOrNull(message)}";

        public static string _FormatNotifyMessage<T>(string prefix, string message, T value)
        {
            return $"{prefix}.notify / {message}\n{_ToStringOrNull(value)}";
        }

        public static string _FormatUpdateMessage<T>(
            string prefix,
            string message,
            T previous,
            T current
        )
        {
            return string.Join(
                "\n",
                new string[]
                {
                    $"{prefix}.changed / {message}",
                    $"current: {_ToStringOrNull(current)}",
                    $"previous: {_ToStringOrNull(previous)}",
                }
            );
            ;
        }

        public static string _ToStringOrNull<T>(T value)
        {
            return value is null ? "null" : value.ToString();
        }
    }
}

