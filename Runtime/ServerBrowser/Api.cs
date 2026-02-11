using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Edgegap.ServerBrowser
{
    using L = Logger;

    public class Api<ServerInstanceMetadata>
        where ServerInstanceMetadata : MetadataDTO
    {
        internal SafeHttpRequest Request;
        internal string AuthToken;
        internal string BaseUrl;

        internal string PATH_MONITOR = "monitor";
        internal string PATH_SERVER_INSTANCES = "/server-instances";

        public Api(MonoBehaviour parent, string authToken, string baseUrl)
        {
            Request = new SafeHttpRequest(parent);
            AuthToken = authToken;
            BaseUrl = baseUrl;
        }

        public void GetMonitor(
            Action<MonitorDTO, UnityWebRequest> onSuccessDelegate,
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
                        MonitorDTO monitor = JsonConvert.DeserializeObject<MonitorDTO>(response);
                        onSuccessDelegate(monitor, request);
                    }
                    catch (Exception e)
                    {
                        L._Error(
                            $"Couldn't parse monitor, consider updating Edgegap SDK. {e.Message}"
                        );
                        throw;
                    }
                },
                onErrorDelegate,
                3
            );
        }

        public void CreateServerInstance(
            ServerInstanceDTO<ServerInstanceMetadata> serverInstance,
            Action<ServerInstanceDTO<ServerInstanceMetadata>, UnityWebRequest> onSuccessDelegate,
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
                        ServerInstanceDTO<ServerInstanceMetadata> instance =
                            JsonConvert.DeserializeObject<
                                ServerInstanceDTO<ServerInstanceMetadata>
                            >(response);
                        onSuccessDelegate(instance, request);
                    }
                    catch (Exception e)
                    {
                        L._Error(
                            $"Couldn't parse server instance, consider updating Edgegap SDK. {e.Message}"
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
