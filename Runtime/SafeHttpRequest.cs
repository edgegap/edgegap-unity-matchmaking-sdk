using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Edgegap
{
    using L = Logger;

    public class SafeHttpRequest
    {
        internal MonoBehaviour Parent;
        internal int TimeoutSeconds = 3;
        internal Func<float> BackoffSeconds = () => 1 + (0.1f * Random.value);

        public SafeHttpRequest(MonoBehaviour parent)
        {
            Parent = parent;
        }

        public SafeHttpRequest(MonoBehaviour parent, int timeoutSeconds, Func<float> backoffSeconds)
        {
            Parent = parent;
            BackoffSeconds = backoffSeconds;
            TimeoutSeconds = timeoutSeconds;
        }

        public void Post(
            string url,
            string authToken,
            string body,
            Action<string, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            int requestAttemptsLeft = 3
        )
        {
#if UNITY_2022_3_OR_NEWER
            UnityWebRequest request = UnityWebRequest.Post(url, body, "application/json");
#else
            UnityWebRequest request = UnityWebRequest.Post(url, body);
            request.SetRequestHeader("Content-Type", "application/json");
#endif
            request.SetRequestHeader("Authorization", authToken);
            request.timeout = TimeoutSeconds;

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
                                url,
                                authToken,
                                body,
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
            string url,
            string authToken,
            Action<string, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            int requestAttemptsLeft = 1
        )
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", authToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = TimeoutSeconds;
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
                            Get(
                                url,
                                authToken,
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

        internal void Delete(
            string url,
            string authToken,
            Action<string, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            int requestAttemptsLeft = 3
        )
        {
            UnityWebRequest request = UnityWebRequest.Delete(url);
            request.SetRequestHeader("Authorization", authToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = TimeoutSeconds;
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
                                url,
                                authToken,
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
                onErrorDelegate(
                    $"HTTP {request.responseCode}: {request.error}\n{request.downloadHandler.text}",
                    request
                );
            }
            else
            {
                string stringResponse =
                    request.downloadHandler == null ? "" : request.downloadHandler.text;
                onSuccessDelegate(stringResponse, request);
            }
            request.Dispose();
        }
    }
}
