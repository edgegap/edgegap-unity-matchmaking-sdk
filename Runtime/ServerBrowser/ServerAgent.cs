using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public MonoBehaviour Handler;

        // BaseUrl may only be set with constructor
        public string BaseUrl { get; }
        public string AuthToken { private get; set; }

        public bool AcceptExpiredReservations = false;
        public UpdateMode UpdateMode = UpdateMode.Heartbeat;
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
        public ConcurrentDictionary<string, byte> PendingConfirmations =
            new ConcurrentDictionary<string, byte>();
        public ConcurrentQueue<SlotUpdateDTO<SlotMetadata>> PendingUpdates =
            new ConcurrentQueue<SlotUpdateDTO<SlotMetadata>>();

        public Observable<ConfirmReservationsResponseDTO> Confirmations { get; private set; } =
            new Observable<ConfirmReservationsResponseDTO>() { };

        private bool FlushingSlotUpdates = false;

        public ServerAgent(
            MonoBehaviour handler,
            string baseUrl,
            string authToken,
            bool acceptExpiredReservations = false,
            UpdateMode updateMode = UpdateMode.Heartbeat,
            int requestTimeoutSeconds = 10,
            float heartbeatIntervalSeconds = 10f,
            int heartbeatMaxConsecutiveErrors = 10
        )
        {
            Handler = handler;

            BaseUrl = baseUrl;
            AuthToken = authToken;

            AcceptExpiredReservations = acceptExpiredReservations;
            UpdateMode = updateMode;
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

        public void ConfirmReservation(string pending)
        {
            if (!PendingConfirmations.TryAdd(pending, 1))
            {
                Confirmations._Notify("confirmation duplicate", ObservableActionType.Warn);
                return;
            }

            Confirmations._Notify("confirmation enqueued");

            if (UpdateMode == UpdateMode.Greedy)
            {
                FlushConfirmations();
            }
        }

        public void UpdateSlot(SlotUpdateDTO<SlotMetadata> update)
        {
            SlotDTO<SlotMetadata> slot = Instance.Current.Slots.Find(s => s.Name == update.Name);

            if (slot is null)
            {
                Instance._Error($"slot update failed (not found)\n{update.Name}");
                return;
            }

            if (slot.AvailableSeats + update.AvailableSeats < 0)
            {
                Instance._Error($"slot update failed (not enough seats)\n{update.Name}");
                return;
            }

            PendingUpdates.Enqueue(update);
            Instance._Notify("slot update enqueued");

            if (UpdateMode == UpdateMode.Greedy)
            {
                FlushConfirmations();
            }
        }

        public void UpdateInstance(ServerInstanceMetadata metadata)
        {
            // todo implement update instance
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
                Observable<ConfirmReservationsResponseDTO>,
                ObservableActionType,
                string
            > onConfirmationsUpdate = null
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

            SubscribeLogger(Monitor, "Monitor");
            Monitor.Subscribe(onMonitorUpdate);

            SubscribeLogger(Instance, "Instance");
            Instance.Subscribe(onInstanceUpdate);

            SubscribeLogger(Confirmations, "Confirmations");
            if (onConfirmationsUpdate is not null)
            {
                Confirmations.Subscribe(onConfirmationsUpdate);
            }

            if (HeartbeatIntervalSeconds < RequestTimeoutSeconds)
            {
                RequestTimeoutSeconds = (int)HeartbeatIntervalSeconds;
                Monitor._Notify("request timeout clamped to heartbeat", ObservableActionType.Warn);
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
                    if (!FlushingSlotUpdates)
                    {
                        FlushConfirmations();
                    }
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

        internal void FlushConfirmations()
        {
            if (PendingConfirmations.IsEmpty)
            {
                if (!PendingUpdates.IsEmpty)
                {
                    FlushSlotUpdates();
                }
                return;
            }

            List<string> staged = new List<string>();
            // simulate concurrent HashSet by removing from concurrent dictionary
            foreach (var (key, _) in PendingConfirmations)
            {
                if (PendingConfirmations.TryRemove(key, out _))
                {
                    staged.Add(key);
                }
            }

            Api.ConfirmReservations(
                Instance.Current.RequestID,
                new ConfirmReservationsDTO { UserIDs = staged },
                (ConfirmReservationsResponseDTO response, UnityWebRequest request) =>
                {
                    Confirmations._Update(response, "reservations confirmed");
                    response.Slots.ForEach(slot =>
                    {
                        int allocatedSeats = (
                            AcceptExpiredReservations
                                ? slot.AcceptedUserIDs.Count + slot.ExpiredUserIDs.Count
                                : slot.AcceptedUserIDs.Count
                        );
                        if (allocatedSeats > 0)
                        {
                            PendingUpdates.Enqueue(
                                new SlotUpdateDTO<SlotMetadata>(slot.Name, allocatedSeats * -1)
                            );
                            Instance._Notify("slot update enqueued");
                        }
                    });
                    FlushSlotUpdates();
                },
                (string error, UnityWebRequest request) =>
                {
                    Confirmations._Error($"confirmation failed\n{error}");
                }
            );
        }

        internal void FlushSlotUpdates()
        {
            if (FlushingSlotUpdates)
            {
                Instance._Notify(
                    "client throttled concurrent slot update",
                    ObservableActionType.Warn
                );
                return;
            }

            FlushingSlotUpdates = true;
            Dictionary<string, SlotUpdateDTO<SlotMetadata>> mergedUpdates =
                new Dictionary<string, SlotUpdateDTO<SlotMetadata>>();

            while (PendingUpdates.TryDequeue(out SlotUpdateDTO<SlotMetadata> update))
            {
                if (!mergedUpdates.ContainsKey(update.Name))
                {
                    SlotDTO<SlotMetadata> slot = Instance.Current.Slots.Find(s =>
                        s.Name == update.Name
                    );

                    mergedUpdates[update.Name] = new SlotUpdateDTO<SlotMetadata>(
                        slot.Name,
                        (int)slot.AvailableSeats,
                        slot.Metadata
                    );
                }
                mergedUpdates[update.Name] = new SlotUpdateDTO<SlotMetadata>(
                    update.Name,
                    mergedUpdates[update.Name].AvailableSeats + update.AvailableSeats,
                    mergedUpdates[update.Name]?.Metadata?.Merge(update.Metadata)
                );
            }

            ConcurrentQueue<string> updatesFinished = new ConcurrentQueue<string>();
            foreach (var update in mergedUpdates.Values)
            {
                Api.UpdateSlot(
                    Instance.Current.RequestID,
                    update,
                    (SlotDTO<SlotMetadata> slot, UnityWebRequest request) =>
                    {
                        Instance.Current.Slots[
                            Instance.Current.Slots.FindIndex(slot => slot.Name == update.Name)
                        ] = slot;
                        Instance._Update(Instance.Current, "slot updated");
                        updatesFinished.Enqueue(update.Name);
                    },
                    (string error, UnityWebRequest request) =>
                    {
                        Instance._Error($"slot update failed, enqueuing for retry\n{error}");
                        PendingUpdates.Enqueue(update);
                        updatesFinished.Enqueue(update.Name);
                    }
                );
            }
            Handler.StartCoroutine(WaitForUpdates(updatesFinished, mergedUpdates.Count));
        }

        internal IEnumerator WaitForUpdates(ConcurrentQueue<string> updates, int expectedCount)
        {
            yield return new WaitUntil(() => updates.Count == expectedCount);
            FlushingSlotUpdates = false;
        }
        #endregion
    }

    public enum UpdateMode
    {
        Heartbeat,
        Greedy,
    }
}
