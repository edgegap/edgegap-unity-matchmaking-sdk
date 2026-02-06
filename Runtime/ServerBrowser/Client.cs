using System;
using System.Net.Sockets;
using Edgegap.Matchmaking;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Edgegap.ServerBrowser
{
    using L = Logger;

    public class Client
    {
        private Api ServerBrowserApi;
        private Edgegap.Ping Ping;

        public MonoBehaviour Handler;

        // BaseUrl may only be set with constructor
        public string BaseUrl { get; }
        public string AuthToken { private get; set; }

        public int RequestTimeoutSeconds;
        public float PollingBackoffSeconds;
        public int MaxConsecutivePollingErrors;

        //public bool SaveStateInPlayerPrefs;
        //public string ClientVersion;
        //public float RemoveAssignmentSeconds;

        //internal string PLAYER_PREFS_KEY_VERSION;
        //internal string PLAYER_PREFS_KEY_TICKET;
        //internal string PLAYER_PREFS_KEY_ASSIGNMENT;

        //public bool LogTicketUpdates;
        //public bool LogAssignmentUpdates;
        //public bool LogPollingUpdates;

        public Observable<MonitorResponseDTO> Monitor { get; private set; } =
            new Observable<MonitorResponseDTO> { };

        public Client(
            MonoBehaviour handler,
            string baseUrl,
            string authToken,
            int requestTimeoutSeconds = 3,
            float pollingBackoffSeconds = 1f,
            int maxConsecutivePollingErrors = 10
        //bool saveStateInPlayerPrefs = true,
        //string clientVersion = "1.0.0",
        //float removeAssignmentSeconds = 30f,
        //string pLAYER_PREFS_KEY_VERSION = "EdgegapMatchmakingClientVersion",
        //string pLAYER_PREFS_KEY_TICKET = "EdgegapMatchmakingClientTicket",
        //string pLAYER_PREFS_KEY_ASSIGNMENT = "EdgegapMatchmakingClientAssignment",
        //bool logTicketUpdates = true,
        //bool logAssignmentUpdates = true,
        //bool logPollingUpdates = false
        )
        {
            Handler = handler;

            BaseUrl = baseUrl;
            AuthToken = authToken;

            RequestTimeoutSeconds = requestTimeoutSeconds;
            PollingBackoffSeconds = pollingBackoffSeconds;
            MaxConsecutivePollingErrors = maxConsecutivePollingErrors;

            //SaveStateInPlayerPrefs = saveStateInPlayerPrefs;
            //ClientVersion = clientVersion;
            //RemoveAssignmentSeconds = removeAssignmentSeconds;

            //PLAYER_PREFS_KEY_VERSION = pLAYER_PREFS_KEY_VERSION;
            //PLAYER_PREFS_KEY_TICKET = pLAYER_PREFS_KEY_TICKET;
            //PLAYER_PREFS_KEY_ASSIGNMENT = pLAYER_PREFS_KEY_ASSIGNMENT;

            //LogTicketUpdates = logTicketUpdates;
            //LogAssignmentUpdates = logAssignmentUpdates;
            //LogPollingUpdates = logPollingUpdates;
        }

        #region Client API
        public void Status()
        {
            //MatchmakingApi.GetMonitor(
        }
        #endregion

        #region Initialization
        public void Initialize(
            UnityAction<
                Observable<MonitorResponseDTO>,
                ObservableActionType,
                string
            > onMonitorUpdate
        )
        {
            if (string.IsNullOrEmpty(BaseUrl.Trim()))
            {
                throw new Exception("BaseUrl not declared.");
            }

            if (string.IsNullOrEmpty(AuthToken.Trim()))
            {
                throw new Exception("AuthToken not declared.");
            }

            //if (SaveStateInPlayerPrefs)
            //{
            //    _LoadStateFromPlayerPrefs();
            //}

            ServerBrowserApi = new Api(Handler, AuthToken, BaseUrl);
            Ping = new Edgegap.Ping(Handler);

            _SubscribeLogger(Monitor, "Monitor");
            Monitor.Subscribe(onMonitorUpdate);

            ServerBrowserApi.GetMonitor(
                (MonitorResponseDTO monitor, UnityWebRequest request) =>
                {
                    if (monitor.Status.ToLower() == "healthy")
                    {
                        Monitor._Update(monitor, "healthy");
                    }
                    else
                    {
                        Monitor._Update(monitor, "unhealthy");
                    }
                },
                (string error, UnityWebRequest request) =>
                {
                    L._Error(error);
                    Monitor._Update(null, "error");
                }
            );
        }

        //internal void _LoadStateFromPlayerPrefs()
        //{
        //    string version = ClientVersion;
        //    try
        //    {
        //        version = PlayerPrefs.GetString(PLAYER_PREFS_KEY_VERSION);
        //    }
        //    catch (Exception e)
        //    {
        //        L._Error($"Deserializing client version failed: {e.Message}");
        //    }

        //    // skip reading ticket and assignment if version increased
        //    if (
        //        string.IsNullOrEmpty(ClientVersion)
        //        || (!string.IsNullOrEmpty(version) && version.CompareTo(ClientVersion) > 0)
        //    )
        //        return;

        //    try
        //    {
        //        string ticket = PlayerPrefs.GetString(PLAYER_PREFS_KEY_TICKET);
        //        if (!string.IsNullOrEmpty(ticket))
        //        {
        //            Ticket._Update(
        //                JsonConvert.DeserializeObject<T>(ticket),
        //                "loaded from PlayerPrefs"
        //            );
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        L._Error($"Deserializing ticket failed, create new ticket.\n{e.Message}");
        //    }

        //    try
        //    {
        //        string assignment = PlayerPrefs.GetString(PLAYER_PREFS_KEY_ASSIGNMENT);
        //        if (assignment.Length > 0)
        //        {
        //            Assignment._Update(
        //                JsonConvert.DeserializeObject<TicketResponseDTO>(assignment),
        //                "loaded from PlayerPrefs"
        //            );
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        L._Error($"Deserializing assignment failed, restart matchmaking.\n{e.Message}");
        //    }
        //}

        internal void _SubscribeLogger<O>(
            Observable<O> observable,
            string subject,
            bool enabled = true
        )
        {
            observable.Subscribe(
                (Observable<O> obs, ObservableActionType type, string message) =>
                {
                    if (!enabled)
                        return;

                    if (type == ObservableActionType.Update)
                    {
                        L._Log(
                            L._FormatUpdateMessage(
                                "Matchmaking",
                                subject,
                                message,
                                obs.Previous,
                                obs.Current
                            )
                        );
                    }
                    else
                    {
                        string log = L._FormatNotifyMessage(
                            "Matchmaking",
                            subject,
                            message,
                            obs.Current
                        );
                        if (type == ObservableActionType.Log)
                        {
                            L._Log(log);
                        }
                        else if (type == ObservableActionType.Warn)
                        {
                            L._Warn(log);
                        }
                        else
                        {
                            L._Error(log);
                        }
                    }
                }
            );
        }
        #endregion
    }
}
