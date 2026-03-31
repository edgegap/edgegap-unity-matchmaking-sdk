using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Edgegap.ServerBrowser
{
    using L = Logger;

    public class Api<ServerInstanceMetadata, SlotMetadata>
        where ServerInstanceMetadata : MetadataDTO, new()
        where SlotMetadata : MetadataDTO, new()
    {
        internal SafeHttpRequest Request;
        internal string AuthToken;
        internal string BaseUrl;

        internal string PATH_MONITOR = "monitor";
        internal string PATH_SERVER_INSTANCES = "server-instances";
        internal string PATH_SLOTS = "slots";
        internal string PATH_RESERVATIONS = "reservations";

        public Api(MonoBehaviour parent, string authToken, string baseUrl)
        {
            Request = new SafeHttpRequest(parent);
            AuthToken = authToken;
            BaseUrl = baseUrl;
        }

        public void GetMonitor(
            Action<MonitorResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Get(
                $"{BaseUrl}/{PATH_MONITOR}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        MonitorResponseDTO monitor =
                            JsonConvert.DeserializeObject<MonitorResponseDTO>(response);
                        onSuccessDelegate(monitor, request);
                    }
                    catch (Exception e)
                    {
                        L._Error(
                            $"Matchmaking | Couldn't parse monitor, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate,
                3
            );
        }

        public void CreateServerInstance(
            ServerInstanceDTO<ServerInstanceMetadata, SlotMetadata> serverInstance,
            Action<
                ServerInstanceDTO<ServerInstanceMetadata, SlotMetadata>,
                UnityWebRequest
            > onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Post(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}",
                AuthToken,
                JsonConvert.SerializeObject(serverInstance),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        ServerInstanceDTO<ServerInstanceMetadata, SlotMetadata> instance =
                            JsonConvert.DeserializeObject<
                                ServerInstanceDTO<ServerInstanceMetadata, SlotMetadata>
                            >(response);
                        onSuccessDelegate(instance, request);
                    }
                    catch (Exception e)
                    {
                        L._Error(
                            $"Server Browser | Couldn't parse server instance, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate,
                3
            );
        }

        public void DeleteServerInstance(
            string requestID,
            Action<UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Delete(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}/{requestID}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    onSuccessDelegate(request);
                },
                onErrorDelegate,
                3
            );
        }

        public void KeepAliveServerInstance(
            string requestID,
            Action<KeepAliveResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Post(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}/{requestID}:keep-alive",
                AuthToken,
                "",
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        KeepAliveResponseDTO keepAlive =
                            JsonConvert.DeserializeObject<KeepAliveResponseDTO>(response);
                        onSuccessDelegate(keepAlive, request);
                    }
                    catch (Exception e)
                    {
                        L._Error(
                            $"Server Browser | Couldn't parse keepalive, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void ConfirmReservations(
            string requestID,
            ConfirmReservationsDTO userIDs,
            Action<ConfirmReservationsResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Post(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}/{requestID}/{PATH_RESERVATIONS}:confirm",
                AuthToken,
                JsonConvert.SerializeObject(userIDs),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        ConfirmReservationsResponseDTO confirmation =
                            JsonConvert.DeserializeObject<ConfirmReservationsResponseDTO>(response);
                        onSuccessDelegate(confirmation, request);
                    }
                    catch (Exception e)
                    {
                        L._Error(
                            $"Server Browser | Couldn't parse reservation confirmations, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate,
                3
            );
        }

        public void UpdateSlot(
            string requestID,
            SlotUpdateDTO<SlotMetadata> update,
            Action<SlotDTO<SlotMetadata>, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Debug.Log($"wip update slot {update.Name}: {update}");
            Request.Patch(
                $"{BaseUrl}/{PATH_SERVER_INSTANCES}/{requestID}/{PATH_SLOTS}/{update.Name}",
                AuthToken,
                JsonConvert.SerializeObject(update),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        SlotDTO<SlotMetadata> slot = JsonConvert.DeserializeObject<
                            SlotDTO<SlotMetadata>
                        >(response);
                        onSuccessDelegate(slot, request);
                    }
                    catch (Exception e)
                    {
                        L._Error(
                            $"Server Browser | Couldn't parse slot update response, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate,
                3
            );
        }
    }
}
