using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Ping = UnityEngine.Ping;
using Random = UnityEngine.Random;

namespace Edgegap.Matchmaking
{
    using L = Logger;

    public class Client<T, A>
        where T : TicketsRequestDTO<A>
    {
        private Api<T, A> Gen2Api;

        public MonoBehaviour Handler;

        // BaseUrl may only be set with constructor
        public string BaseUrl { get; }
        public string AuthToken { private get; set; }

        public string ClientVersion;
        public bool SaveStateInPlayerPrefs;
        internal string PLAYER_PREFS_KEY_VERSION;
        internal string PLAYER_PREFS_KEY_TICKET;
        internal string PLAYER_PREFS_KEY_ASSIGNMENT;

        public int RequestTimeoutSeconds;
        public float PollingBackoffSeconds;
        public int MaxConsecutivePollingErrors;

        public float RemoveAssignmentSeconds;

        public bool LogTicketUpdates;
        public bool LogAssignmentUpdates;
        public bool LogPollingUpdates;

        public Observable<TicketResponseDTO> Assignment { get; private set; } =
            new Observable<TicketResponseDTO> { };
        public Observable<MonitorResponseDTO> Monitor { get; private set; } =
            new Observable<MonitorResponseDTO> { };
        public Observable<T> Ticket { get; private set; } = new Observable<T> { };
        private protected bool Polling = false;

        public Client(
            MonoBehaviour handler,
            string baseUrl,
            string authToken,
            string clientVersion = "1.0.0",
            bool saveStateInPlayerPrefs = true,
            string pLAYER_PREFS_KEY_VERSION = "EdgegapGen2ClientVersion",
            string pLAYER_PREFS_KEY_TICKET = "EdgegapGen2ClientTicket",
            string pLAYER_PREFS_KEY_ASSIGNMENT = "EdgegapGen2ClientAssignment",
            int requestTimeoutSeconds = 3,
            float pollingBackoffSeconds = 1f,
            int maxConsecutivePollingErrors = 10,
            float removeAssignmentSeconds = 30f,
            bool logTicketUpdates = true,
            bool logAssignmentUpdates = true,
            bool logPollingUpdates = false
        )
        {
            if (handler is null)
            {
                throw new Exception("Gen2Client Handler not assigned.");
            }

            Handler = handler;
            BaseUrl = baseUrl;
            AuthToken = authToken;
            ClientVersion = clientVersion;
            SaveStateInPlayerPrefs = saveStateInPlayerPrefs;
            PLAYER_PREFS_KEY_VERSION = pLAYER_PREFS_KEY_VERSION;
            PLAYER_PREFS_KEY_TICKET = pLAYER_PREFS_KEY_TICKET;
            PLAYER_PREFS_KEY_ASSIGNMENT = pLAYER_PREFS_KEY_ASSIGNMENT;
            RequestTimeoutSeconds = requestTimeoutSeconds;
            PollingBackoffSeconds = pollingBackoffSeconds;
            MaxConsecutivePollingErrors = maxConsecutivePollingErrors;
            RemoveAssignmentSeconds = removeAssignmentSeconds;
            LogTicketUpdates = logTicketUpdates;
            LogAssignmentUpdates = logAssignmentUpdates;
            LogPollingUpdates = logPollingUpdates;
        }

        #region Client API

        public void Status()
        {
            Gen2Api.GetMonitor(
                (MonitorResponseDTO monitor, UnityWebRequest request) =>
                {
                    if (monitor.Status == "HEALTHY")
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

        public void Beacons(
            Action<BeaconsResponseDTO> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Gen2Api.GetBeacons(
                (BeaconsResponseDTO beacons, UnityWebRequest request) =>
                {
                    onSuccessDelegate(beacons);
                },
                (string error, UnityWebRequest request) =>
                {
                    L._Error(error);
                    onErrorDelegate(error, request);
                }
            );
        }

        public void MeasureBeaconsRoundTripTime(
            BeaconDTO[] beacons,
            Action<Dictionary<string, float>> onCompleteDelegate,
            int requests = 3
        )
        {
            Handler.StartCoroutine(_GetLatencies(beacons, onCompleteDelegate, requests));
        }

        public void ResumeMatchmaking()
        {
            if (Assignment.Current is null)
            {
                Assignment._Notify("not cached, restart suggested");
                return;
            }

            if (Assignment.Current.Status == "HOST_ASSIGNED")
            {
                Assignment._Notify("cached, reconnect suggested");
            }
            else
            {
                Assignment._Notify("cached, resuming polling");
                Polling = true;
                Handler.StartCoroutine(_ScheduleGetAssignmentRecursively());
            }
        }

        public void StartMatchmaking(T ticket, bool abandon = false)
        {
            if (Assignment.Current is not null && !abandon)
            {
                Assignment._Notify("cached, resume or abandon", ObservableActionType.Error);
                return;
            }

            _Abandon(() =>
            {
                Gen2Api.CreateTicketAsync(
                    ticket,
                    (TicketResponseDTO assignment, UnityWebRequest request) =>
                    {
                        Ticket._Update(ticket, "saved");
                        Assignment._Update(assignment, "received");
                        Polling = true;
                        Handler.StartCoroutine(_ScheduleGetAssignmentRecursively());
                    },
                    (string error, UnityWebRequest request) =>
                    {
                        L._Error(error);
                        _Abandon();
                    }
                );
            });
        }

        public void StartGroupMatchmaking(
            T hostTicket,
            List<T> memberTickets,
            Action<List<TicketResponseDTO>, UnityWebRequest> onSuccessDelegate,
            bool abandon = false
        )
        {
            if (Assignment.Current is not null && !abandon)
            {
                Assignment._Notify("cached, resume or abandon", ObservableActionType.Error);
                return;
            }

            GroupTicketsRequestDTO<A> groupTicket = new GroupTicketsRequestDTO<A>(
                memberTickets.Append(hostTicket).ToArray()
            );

            _Abandon(() =>
            {
                Gen2Api.CreateGroupTicketAsync(
                    groupTicket,
                    (GroupTicketsResponseDTO assignment, UnityWebRequest request) =>
                    {
                        Ticket._Update(groupTicket.Tickets.Last(), "saved");
                        Assignment._Update(assignment.Tickets.Last(), "received");
                        onSuccessDelegate(assignment.Tickets.SkipLast(1).ToList(), request);
                        Handler.StartCoroutine(_ScheduleGetAssignmentRecursively());
                    },
                    (string error, UnityWebRequest request) =>
                    {
                        L._Error(error);
                        _Abandon();
                    }
                );
            });
        }

        public void JoinGroupMatchmaking(TicketResponseDTO assignment, bool abandon = false)
        {
            if (Assignment.Current is not null && !abandon)
            {
                Assignment._Notify("cached, resume or abandon", ObservableActionType.Error);
                return;
            }

            _Abandon(() =>
            {
                Assignment._Update(assignment, "joined");
                Handler.StartCoroutine(_ScheduleGetAssignmentRecursively());
            });
        }

        public void StopMatchmaking(Action onCompleteDelegate = null)
        {
            if (Ticket.Current is null && Assignment.Current is null)
            {
                L._Warn("No ticket or assignment found, stopped.");
                if (onCompleteDelegate is not null)
                {
                    onCompleteDelegate();
                }
            }
            else
            {
                _Abandon(onCompleteDelegate);
            }
        }
        #endregion

        #region Initialization
        public void Initialize(
            UnityAction<
                Observable<MonitorResponseDTO>,
                ObservableActionType,
                string
            > onMonitorUpdate,
            UnityAction<
                Observable<TicketResponseDTO>,
                ObservableActionType,
                string
            > onAssignmentUpdate,
            UnityAction<Observable<T>, ObservableActionType, string> onTicketUpdate = null
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

            if (SaveStateInPlayerPrefs)
            {
                _LoadStateFromPlayerPrefs();
            }

            Gen2Api = new Api<T, A>(Handler, AuthToken, BaseUrl);

            _SubscribeLogger(Monitor, "Monitor");
            _SubscribeLogger(Ticket, "Ticket", LogTicketUpdates);
            _SubscribeLogger(Assignment, "Assignment", LogAssignmentUpdates);

            Gen2Api.GetMonitor(
                (MonitorResponseDTO monitor, UnityWebRequest request) =>
                {
                    _SubscribePlayerPrefSave(Ticket, "Ticket", PLAYER_PREFS_KEY_TICKET);
                    _SubscribePlayerPrefSave(Assignment, "Assignment", PLAYER_PREFS_KEY_ASSIGNMENT);

                    if (onTicketUpdate is not null)
                    {
                        Ticket.Subscribe(onTicketUpdate);
                    }
                    Assignment.Subscribe(onAssignmentUpdate);
                    Monitor.Subscribe(onMonitorUpdate);

                    if (monitor.Status == "HEALTHY")
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

        internal void _LoadStateFromPlayerPrefs()
        {
            string version = ClientVersion;
            try
            {
                version = PlayerPrefs.GetString(PLAYER_PREFS_KEY_VERSION);
            }
            catch (Exception e)
            {
                L._Error($"Deserializing client version failed: {e.Message}");
            }

            // skip reading ticket and assignment if version increased
            if (
                string.IsNullOrEmpty(ClientVersion)
                || (!string.IsNullOrEmpty(version) && version.CompareTo(ClientVersion) > 0)
            )
                return;

            try
            {
                string ticket = PlayerPrefs.GetString(PLAYER_PREFS_KEY_TICKET);
                if (!string.IsNullOrEmpty(ticket))
                {
                    Ticket._Update(
                        JsonConvert.DeserializeObject<T>(ticket),
                        "loaded from PlayerPrefs"
                    );
                }
            }
            catch (Exception e)
            {
                L._Error($"Deserializing ticket failed, create new ticket.\n{e.Message}");
            }

            try
            {
                string assignment = PlayerPrefs.GetString(PLAYER_PREFS_KEY_ASSIGNMENT);
                if (assignment.Length > 0)
                {
                    Assignment._Update(
                        JsonConvert.DeserializeObject<TicketResponseDTO>(assignment),
                        "loaded from PlayerPrefs"
                    );
                }
            }
            catch (Exception e)
            {
                L._Error($"Deserializing assignment failed, restart matchmaking.\n{e.Message}");
            }
        }

        internal void _SubscribeLogger<O>(
            Observable<O> observable,
            string name,
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
                        L._Log(L._FormatUpdateMessage(name, message, obs.Previous, obs.Current));
                    }
                    else
                    {
                        string log = L._FormatNotifyMessage(name, message, obs.Current);
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

        internal void _SubscribePlayerPrefSave<O>(
            Observable<O> observable,
            string name,
            string prefKey
        )
        {
            observable.Subscribe(
                (Observable<O> obs, ObservableActionType type, string message) =>
                {
                    if (type != ObservableActionType.Update)
                        return;

                    try
                    {
                        if (obs.Current is null)
                        {
                            PlayerPrefs.DeleteKey(prefKey);
                        }
                        else
                        {
                            PlayerPrefs.SetString(
                                prefKey,
                                JsonConvert.SerializeObject(obs.Current)
                            );
                        }
                    }
                    catch (Exception e)
                    {
                        L._Error($"Serializing {name} failed.\n{e.Message}");
                    }
                }
            );
        }
        #endregion

        #region Internals
        internal IEnumerator _ScheduleGetAssignmentRecursively(int consecutiveErrors = 0)
        {
            if (!Polling)
            {
                if (LogPollingUpdates)
                {
                    Assignment._Notify("polling stopped");
                }
                yield break;
            }

            yield return new WaitForSeconds(PollingBackoffSeconds + (0.1f * Random.value));

            if (LogPollingUpdates)
            {
                Assignment._Notify("polling now");
            }

            Gen2Api.GetTicketAsync(
                Assignment.Current.ID,
                (TicketResponseDTO assignment, UnityWebRequest request) =>
                {
                    if (
                        Assignment.Current is not null
                        && assignment.Status != Assignment.Current.Status
                    )
                    {
                        Assignment._Update(assignment, $"updated={assignment.Status}");
                        if (
                            Assignment.Current.Status == "HOST_ASSIGNED"
                            || Assignment.Current.Status == "CANCELLED"
                        )
                        {
                            Handler.StartCoroutine(_ExpireAssignment());
                        }
                        else if (Ticket.Current is not null)
                        {
                            Handler.StartCoroutine(_ScheduleGetAssignmentRecursively());
                        }
                    }
                    else
                    {
                        Handler.StartCoroutine(_ScheduleGetAssignmentRecursively());
                    }
                },
                (string error, UnityWebRequest request) =>
                {
                    if (consecutiveErrors + 1 > MaxConsecutivePollingErrors)
                    {
                        Monitor._Notify(
                            $"reached MaxConsecutivePollingErrors={MaxConsecutivePollingErrors}",
                            ObservableActionType.Error
                        );
                        _Abandon();
                    }
                    else
                    {
                        L._Error(error);
                        Monitor._Notify(
                            $"polling error={consecutiveErrors + 1} < {MaxConsecutivePollingErrors}",
                            ObservableActionType.Warn
                        );

                        if (request.responseCode == 429 || request.responseCode >= 500)
                        {
                            Handler.StartCoroutine(
                                _ScheduleGetAssignmentRecursively(consecutiveErrors + 1)
                            );
                        }
                        else
                        {
                            _Abandon();
                        }
                    }
                }
            );
        }

        internal void _Abandon(Action onCompletedDelegate = null)
        {
            Polling = false;

            if (Ticket.Current is not null)
            {
                Ticket._Update(null, "abandoned");
            }

            if (Assignment.Current is null)
            {
                if (onCompletedDelegate is not null)
                {
                    onCompletedDelegate();
                }
                return;
            }

            Gen2Api.DeleteTicketAsync(
                Assignment.Current.ID,
                (UnityWebRequest request) =>
                {
                    Assignment._Update(null, "abandoned");
                    if (onCompletedDelegate is not null)
                    {
                        onCompletedDelegate();
                    }
                },
                (string error, UnityWebRequest request) =>
                {
                    L._Warn(error);
                    Assignment._Update(null, "abandon failed, deleted");
                    if (onCompletedDelegate is not null)
                    {
                        onCompletedDelegate();
                    }
                }
            );
        }

        internal IEnumerator _ExpireAssignment()
        {
            Polling = false;
            Ticket._Update(null, "expired");
            yield return new WaitForSeconds(RemoveAssignmentSeconds);
            Assignment._Update(null, "removed");
        }

        internal IEnumerator _GetLatencies(
            BeaconDTO[] beacons,
            Action<Dictionary<string, float>> onCompleteDelegate,
            int requests
        )
        {
            Dictionary<string, float> results = new Dictionary<string, float>();
            foreach (BeaconDTO beacon in beacons)
            {
                Handler.StartCoroutine(
                    _GetAverageRoundTripTime(
                        beacon.PublicIP,
                        (double ping) => results.Add(beacon.Location.City, (float)ping),
                        requests
                    )
                );
            }

            yield return new WaitUntil(() => results.Keys.Count == beacons.Count());
            onCompleteDelegate(results);
        }

        internal IEnumerator _GetAverageRoundTripTime(
            string ip,
            Action<double> onCompleteDelegate,
            int requests
        )
        {
            List<int> pings = new List<int>();
            for (int i = 0; i < requests; i++)
            {
                Handler.StartCoroutine(_IcmpPing(ip, (int rtt) => pings.Add(rtt)));
            }

            yield return new WaitUntil(() => pings.Count == requests);

            List<int> finishedPings = pings.Where((int p) => p > 0).ToList();
            onCompleteDelegate(
                finishedPings.Count() > 0 ? Math.Round(finishedPings.Average(), 2) : 0f
            );
        }

        internal IEnumerator _IcmpPing(string ip, Action<int> onCompleteDelegate)
        {
            Ping ping = new Ping(ip);
            double start = Time.realtimeSinceStartupAsDouble;

            yield return new WaitUntil(
                () =>
                    ping.isDone || Time.realtimeSinceStartupAsDouble - start > RequestTimeoutSeconds
            );

            onCompleteDelegate(ping.time);
            ping.DestroyPing();
        }
        #endregion
    }
}
