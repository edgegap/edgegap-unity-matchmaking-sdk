using UnityEngine;

namespace Edgegap
{
    public static class Logger
    {
        public static void _Log<T>(T message)
        {
            if (!Debug.isDebugBuild)
                return;
            Debug.Log($"Edgegap {message}");
        }

        public static void _Warn<T>(T message)
        {
            Debug.LogWarning($"Edgegap {message}");
        }

        public static void _Error<T>(T message)
        {
            Debug.LogError($"Edgegap {message}");
        }

        public static string _FormatNotifyMessage<T>(
            string service,
            string subject,
            string message,
            T value
        )
        {
            return $"{service} | {subject}.notify('{message}')\n{_ToStringOrNull(value)}";
        }

        public static string _FormatUpdateMessage<T>(
            string service,
            string subject,
            string message,
            T previous,
            T current
        )
        {
            return string.Join(
                "\n",
                new string[]
                {
                    $"{service} | {subject}.changed('{message}')",
                    $"current: {_ToStringOrNull(current)}",
                    $"previous: {_ToStringOrNull(previous)}",
                }
            );
        }

        public static string _FormatErrorMessage<T>(
            string service,
            string subject,
            string message,
            T value
        )
        {
            return $"{service} | {subject}.error:{message}\n{_ToStringOrNull(value)}";
        }

        public static string _ToStringOrNull<T>(T value)
        {
            return value is null ? "null" : value.ToString();
        }
    }
}
