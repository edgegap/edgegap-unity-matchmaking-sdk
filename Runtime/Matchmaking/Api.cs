using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Edgegap.Matchmaking
{
    using L = Logger;

    public class Api<T, A>
        where T : TicketsRequestDTO<A>
    {
        internal SafeHttpRequest Request;
        internal string AuthToken;
        internal string BaseUrl;

        internal string PATH_MONITOR = "monitor";
        internal string PATH_BEACONS = "locations/beacons";
        internal string PATH_TICKETS = "tickets";
        internal string PATH_GROUP_TICKETS = "group-tickets";

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
                        L.Error(
                            $"Matchmaking | Couldn't parse monitor, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate,
                3
            );
        }

        public void GetBeacons(
            Action<BeaconsResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Get(
                $"{BaseUrl}/{PATH_BEACONS}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        BeaconsResponseDTO beacons =
                            JsonConvert.DeserializeObject<BeaconsResponseDTO>(response);
                        onSuccessDelegate(beacons, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Matchmaking | Couldn't parse beacons, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate,
                3
            );
        }

        public void CreateTicketAsync(
            T ticket,
            Action<TicketResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Post(
                $"{BaseUrl}/{PATH_TICKETS}",
                AuthToken,
                JsonConvert.SerializeObject(ticket),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        TicketResponseDTO assignment =
                            JsonConvert.DeserializeObject<TicketResponseDTO>(response);
                        onSuccessDelegate(assignment, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Matchmaking | Couldn't parse assignment, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void CreateGroupTicketAsync(
            GroupTicketsRequestDTO<A> groupTicket,
            Action<GroupTicketsResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Post(
                $"{BaseUrl}/{PATH_GROUP_TICKETS}",
                AuthToken,
                JsonConvert.SerializeObject(groupTicket),
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        GroupTicketsResponseDTO assignment =
                            JsonConvert.DeserializeObject<GroupTicketsResponseDTO>(response);
                        onSuccessDelegate(assignment, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Matchmaking | Couldn't parse assignment, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void GetTicketAsync(
            string ticketID,
            Action<TicketResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Get(
                $"{BaseUrl}/{PATH_TICKETS}/{ticketID}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    try
                    {
                        TicketResponseDTO assignment =
                            JsonConvert.DeserializeObject<TicketResponseDTO>(response);
                        onSuccessDelegate(assignment, request);
                    }
                    catch (Exception e)
                    {
                        L.Error(
                            $"Matchmaking | Couldn't parse assignment, update Edgegap SDK.\n{e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate
            );
        }

        public void DeleteTicketAsync(
            string ticketID,
            Action<UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Request.Delete(
                $"{BaseUrl}/{PATH_TICKETS}/{ticketID}",
                AuthToken,
                (string response, UnityWebRequest request) =>
                {
                    onSuccessDelegate(request);
                },
                onErrorDelegate
            );
        }
    }
}
