using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Edgegap.ServerBrowser
{
    using L = Logger;

    public class ServerAgent<ServerInstanceMetadata, SlotMetadata>
        where ServerInstanceMetadata : MetadataDTO, new()
        where SlotMetadata : MetadataDTO, new()
    {
        private Api<ServerInstanceMetadata, SlotMetadata> Api;
        private Edgegap.Ping Ping;

        public MonoBehaviour Handler;

        // BaseUrl may only be set with constructor
        public string BaseUrl { get; }
        public string AuthToken { private get; set; }

        public int RequestTimeoutSeconds;
        public float HeartbeatIntervalSeconds;
        public int HeartbeatMaxConsecutiveErrors;

        public Observable<MonitorResponseDTO> Monitor { get; private set; } =
            new Observable<MonitorResponseDTO> { };
        public Observable<ServerInstanceDTO<ServerInstanceMetadata, SlotMetadata>> Instance
        {
            get;
            private set;
        } = new Observable<ServerInstanceDTO<ServerInstanceMetadata, SlotMetadata>>() { };

        public Observable<ConnectionsDTO<SlotMetadata>> Connections { get; private set; } =
            new Observable<ConnectionsDTO<SlotMetadata>>() { };

        public ServerAgent(
            MonoBehaviour handler,
            string baseUrl,
            string authToken,
            int requestTimeoutSeconds = 10,
            float heartbeatIntervalSeconds = 10f,
            int heartbeatMaxConsecutiveErrors = 10
        )
        {
            Handler = handler;

            BaseUrl = baseUrl;
            AuthToken = authToken;

            RequestTimeoutSeconds = requestTimeoutSeconds;
            HeartbeatIntervalSeconds = heartbeatIntervalSeconds;
            HeartbeatMaxConsecutiveErrors = heartbeatMaxConsecutiveErrors;
        }

        #region Agent API
        public void Status()
        {
            Api.GetMonitor(
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
                    Monitor._Error($"get monitor failed (unexpected error)\n{error}", null);
                }
            );
        }

        public void DiscoverInstance(
            ServerInstanceDTO<ServerInstanceMetadata, SlotMetadata> instance
        )
        {
            Api.CreateServerInstance(
                instance,
                (
                    ServerInstanceDTO<ServerInstanceMetadata, SlotMetadata> response,
                    UnityWebRequest request
                ) =>
                {
                    Instance._Update(response, "discovered");
                    Heartbeat();
                },
                (string error, UnityWebRequest request) =>
                {
                    if (request.responseCode == 409)
                    {
                        Instance._Error("discovery failed (duplicate)");
                    }
                    else
                    {
                        Instance._Error($"discovery failed\n{error}");
                    }
                }
            );
        }

        public void DeleteInstance()
        {
            if (Instance.Current is null)
            {
                Instance._Update(null, "deleted");
                return;
            }

            Api.DeleteServerInstance(
                Instance.Current.RequestID,
                (UnityWebRequest request) =>
                {
                    Instance._Update(null, "deleted");
                },
                (string error, UnityWebRequest request) =>
                {
                    if (request.responseCode == 404)
                    {
                        Instance._Update(null, "delete failed (not found)");
                    }
                    else
                    {
                        Instance._Error($"delete failed\n{error}", null);
                    }
                }
            );
        }

        public void UpdateSlot(
            SlotUpdateDTO<SlotMetadata> update,
            UpdateMode mode = UpdateMode.Heartbeat
        )
        {
            SlotDTO<SlotMetadata> slot = Instance.Current.Slots.Find(slot =>
                slot.Name == update.Name
            );
            if (slot is null)
            {
                Connections._Error($"update failed (slot not found)\n{update.Name}");
                return;
            }

            if (mode == UpdateMode.Heartbeat)
            {
                bool foundPendingUpdate = Connections.Current.PendingUpdates.TryGetValue(
                    update.Name,
                    out SlotUpdateDTO<SlotMetadata> pendingUpdate
                );
                // todo queue updates by timestamp, don't merge yet
                if (!foundPendingUpdate)
                {
                    Connections.Current.PendingUpdates.Add(update.Name, update);
                }
                else
                {
                    Connections.Current.PendingUpdates[update.Name] =
                        new SlotUpdateDTO<SlotMetadata>(
                            update.Name,
                            update.AvailableSeats,
                            pendingUpdate.Metadata.Merge(update.Metadata)
                        );
                }
            }
            else
            {
                FlushSlotUpdates(
                    new Dictionary<string, SlotUpdateDTO<SlotMetadata>>()
                    {
                        { update.Name, update },
                    }
                );
            }
            // todo finish implementation
        }

        public void UpdateInstance(ServerInstanceMetadata metadata)
        {
            // todo implement update instance
        }

        public void ConfirmReservations(
            HashSet<string> pending,
            UpdateMode mode = UpdateMode.Heartbeat
        )
        {
            if (pending.IsSubsetOf(Connections.Current.PendingConfirmations))
            {
                Connections._Notify("noop, already confirmed");
                return;
            }
            if (mode == UpdateMode.Heartbeat)
            {
                Connections._Update(
                    new ConnectionsDTO<SlotMetadata>()
                    {
                        PendingConfirmations = new HashSet<string>(
                            Connections.Current.PendingConfirmations.Concat(pending)
                        ),
                        Confirmations = Connections.Current.Confirmations,
                    },
                    "queued confirmations"
                );
            }
            else
            {
                FlushConfirmations(pending);
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
                Observable<ServerInstanceDTO<ServerInstanceMetadata, SlotMetadata>>,
                ObservableActionType,
                string
            > onInstanceUpdate,
            UnityAction<
                Observable<ConnectionsDTO<SlotMetadata>>,
                ObservableActionType,
                string
            > onConnectionsUpdate
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

            Api = new Api<ServerInstanceMetadata, SlotMetadata>(Handler, AuthToken, BaseUrl);
            Ping = new Edgegap.Ping(Handler);

            SubscribeLogger(Monitor, "Monitor");
            Monitor.Subscribe(onMonitorUpdate);

            SubscribeLogger(Instance, "Instance");
            Instance.Subscribe(onInstanceUpdate);

            SubscribeLogger(Connections, "Connections");
            Connections.Subscribe(onConnectionsUpdate);

            if (HeartbeatIntervalSeconds < RequestTimeoutSeconds)
            {
                RequestTimeoutSeconds = (int)HeartbeatIntervalSeconds;
                Monitor._Notify(
                    "clamped timeout to match update interval",
                    ObservableActionType.Warn
                );
            }

            Status();
        }

        internal void SubscribeLogger<O>(
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
                                "Server Browser",
                                subject,
                                message,
                                obs.Previous,
                                obs.Current
                            )
                        );
                    }
                    else if (type == ObservableActionType.Error)
                    {
                        L._Error(
                            L._FormatErrorMessage("Server Browser", subject, message, obs.Current)
                        );
                    }
                    else
                    {
                        string log = L._FormatNotifyMessage(
                            "Server Browser",
                            subject,
                            message,
                            obs.Current
                        );
                        if (type == ObservableActionType.Log)
                        {
                            L._Log(log);
                        }
                        else
                        {
                            L._Warn(log);
                        }
                    }
                }
            );
        }
        #endregion

        #region Internals

        internal void Heartbeat(int consecutiveErrors = 0)
        {
            if (consecutiveErrors > HeartbeatMaxConsecutiveErrors)
            {
                DeleteInstance();
                return;
            }
            else if (Instance.Current is null)
            {
                return;
            }

            Api.KeepAliveServerInstance(
                Instance.Current.RequestID,
                (KeepAliveResponseDTO keepalive, UnityWebRequest request) =>
                {
                    Handler.StartCoroutine(DelayedHeartbeat());
                    FlushConfirmations();
                },
                (string error, UnityWebRequest request) =>
                {
                    if (request.responseCode == 404)
                    {
                        DiscoverInstance(Instance.Current);
                    }
                    else
                    {
                        Handler.StartCoroutine(DelayedHeartbeat(consecutiveErrors + 1));
                    }
                }
            );
        }

        internal IEnumerator DelayedHeartbeat(int consecutiveErrors = 0)
        {
            yield return new WaitForSeconds(HeartbeatIntervalSeconds + (0.1f * Random.value));
            Heartbeat(consecutiveErrors);
        }

        internal void FlushConfirmations(HashSet<string> pending = null)
        {
            pending ??= Connections.Current.PendingConfirmations;

            if (pending.Count == 0)
            {
                Connections._Notify("noop, no pending confirmations");
                return;
            }

            Api.ConfirmReservations(
                Instance.Current.RequestID,
                pending.ToList(),
                (ConfirmReservationsResponseDTO response, UnityWebRequest request) =>
                {
                    Connections._Update(
                        new ConnectionsDTO<SlotMetadata>()
                        {
                            PendingConfirmations = new HashSet<string>(
                                Connections.Current.PendingConfirmations.Except(pending)
                            ),
                            Confirmations = response,
                        },
                        "confirmed reservations"
                    );

                    // todo update slots capacity accordingly
                },
                (string error, UnityWebRequest request) =>
                {
                    Connections._Error($"confirmation failed\n{error}");
                }
            );
        }

        internal void FlushSlotUpdates(Dictionary<string, SlotUpdateDTO<SlotMetadata>> updates)
        {
            // todo semaphore PER SLOT to prevent concurrent updates
            foreach (var update in updates)
            {
                // todo merge updates into a single update per slot
                Api.UpdateSlot(
                    Instance.Current.RequestID,
                    update.Value,
                    (SlotDTO<SlotMetadata> slot, UnityWebRequest request) => {
                        // todo remove update from pending updates
                        // todo update slot in instance
                    },
                    (string error, UnityWebRequest request) =>
                    {
                        Connections._Error($"slot update failed\n{error}");
                    }
                );
            }
        }
        #endregion
    }

    public enum UpdateMode
    {
        Heartbeat,
        Greedy,
    }
}
