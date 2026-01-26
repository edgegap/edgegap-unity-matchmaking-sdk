using System;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Edgegap.Matchmaking
{
    using L = Logger;

    public class Api<T, A>
        where T : TicketsRequestDTO<A>
    {
        internal MonoBehaviour Parent;
        internal string AuthToken;
        internal string BaseUrl;
        internal int RequestTimeoutSeconds;
        internal Func<float> BackoffSeconds = () => 1 + (0.1f * Random.value);

        internal string PATH_MONITOR = "monitor";
        internal string PATH_BEACONS = "locations/beacons";
        internal string PATH_TICKETS = "tickets";
        internal string PATH_GROUP_TICKETS = "group-tickets";

        public Api(
            MonoBehaviour parent,
            string authToken,
            string baseUrl,
            int requestTimeoutSeconds = 3
        )
        {
            Parent = parent;
            AuthToken = authToken;
            BaseUrl = baseUrl;
        }

        public Api(
            MonoBehaviour parent,
            string authToken,
            string baseUrl,
            Func<float> backoffSeconds,
            int requestTimeoutSeconds = 3
        )
        {
            Parent = parent;
            AuthToken = authToken;
            BaseUrl = baseUrl;
            RequestTimeoutSeconds = requestTimeoutSeconds;
            BackoffSeconds = backoffSeconds;
        }

        public void GetMonitor(
            Action<MonitorResponseDTO, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate
        )
        {
            Get(
                $"{BaseUrl}/{PATH_MONITOR}",
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
                            $"Couldn't parse monitor, consider updating Matchmaking SDK. {e.Message}"
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
            Get(
                $"{BaseUrl}/{PATH_BEACONS}",
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
                        L._Error(
                            $"Couldn't parse beacons, consider updating Matchmaking SDK. {e.Message}"
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
            Post(
                $"{BaseUrl}/{PATH_TICKETS}",
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
                        L._Error(
                            $"Couldn't parse assignment, consider updating Matchmaking SDK. {e.Message}"
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
            Post(
                $"{BaseUrl}/{PATH_GROUP_TICKETS}",
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
                        L._Error(
                            $"Couldn't parse assignment, consider updating Matchmaking SDK. {e.Message}"
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
            Get(
                $"{BaseUrl}/{PATH_TICKETS}/{ticketID}",
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
                        L._Error(
                            $"Couldn't parse assignment, consider updating Matchmaking SDK. {e.Message}"
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
            Delete(
                $"{BaseUrl}/{PATH_TICKETS}/{ticketID}",
                (string response, UnityWebRequest request) =>
                {
                    onSuccessDelegate(request);
                },
                onErrorDelegate
            );
        }

        #region WebGL-friendly WebRequest
        internal void Post(
            string Url,
            string requestBody,
            Action<string, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            int requestAttemptsLeft = 3
        )
        {
#if UNITY_2022_3_OR_NEWER
            UnityWebRequest request = UnityWebRequest.Post(Url, requestBody, "application/json");
#else
            UnityWebRequest request = UnityWebRequest.Post(Url, requestBody);
#endif
            request.SetRequestHeader("Authorization", AuthToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = RequestTimeoutSeconds;

            Parent.StartCoroutine(
                _SendRequestEnumerator(
                    request,
                    onSuccessDelegate,
                    (string error, UnityWebRequest req) =>
                    {
                        if (
                            requestAttemptsLeft > 0
                            && (req.responseCode == 429 || req.responseCode >= 500)
                        )
                        {
                            L._Warn($"{error}, retries left: {requestAttemptsLeft}");
                            Post(
                                Url,
                                requestBody,
                                onSuccessDelegate,
                                onErrorDelegate,
                                requestAttemptsLeft - 1
                            );
                        }
                        else
                        {
                            onErrorDelegate(error, req);
                        }
                    },
                    requestAttemptsLeft > 0
                )
            );
        }

        internal void Get(
            string Url,
            Action<string, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            int requestAttemptsLeft = 1
        )
        {
            UnityWebRequest request = UnityWebRequest.Get(Url);
            request.SetRequestHeader("Authorization", AuthToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = RequestTimeoutSeconds;
            Parent.StartCoroutine(
                _SendRequestEnumerator(
                    request,
                    onSuccessDelegate,
                    (string error, UnityWebRequest req) =>
                    {
                        if (
                            requestAttemptsLeft > 0
                            && (req.responseCode == 429 || req.responseCode >= 500)
                        )
                        {
                            L._Warn($"{error}, retries left: {requestAttemptsLeft}");
                            Get(Url, onSuccessDelegate, onErrorDelegate, requestAttemptsLeft - 1);
                        }
                        else
                        {
                            onErrorDelegate(error, req);
                        }
                    },
                    requestAttemptsLeft > 0
                )
            );
        }

        internal void Delete(
            string Url,
            Action<string, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            int requestAttemptsLeft = 3
        )
        {
            UnityWebRequest request = UnityWebRequest.Delete(Url);
            request.SetRequestHeader("Authorization", AuthToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = RequestTimeoutSeconds;
            Parent.StartCoroutine(
                _SendRequestEnumerator(
                    request,
                    onSuccessDelegate,
                    (string error, UnityWebRequest req) =>
                    {
                        if (
                            requestAttemptsLeft > 0
                            && (req.responseCode == 429 || req.responseCode >= 500)
                        )
                        {
                            L._Warn($"{error}, retries left: {requestAttemptsLeft}");
                            Delete(
                                Url,
                                onSuccessDelegate,
                                onErrorDelegate,
                                requestAttemptsLeft - 1
                            );
                        }
                        else
                        {
                            onErrorDelegate(error, req);
                        }
                    },
                    requestAttemptsLeft > 0
                )
            );
        }

        internal IEnumerator _SendRequestEnumerator(
            UnityWebRequest request,
            Action<string, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            bool backoff = false
        )
        {
            if (backoff)
                yield return new WaitForSeconds(BackoffSeconds());

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onErrorDelegate($"HTTP {request.responseCode}: {request.error}", request);
            }
            else
            {
                string stringResponse =
                    request.downloadHandler == null ? "" : request.downloadHandler.text;
                onSuccessDelegate(stringResponse, request);
            }
        }
        #endregion
    }
}

