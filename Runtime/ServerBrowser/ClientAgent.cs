using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Edgegap.ServerBrowser
{
    using L = Logger;

    public class ClientAgent<ServerInstanceMetadata, SlotMetadata>
        where ServerInstanceMetadata : MetadataDTO, new()
        where SlotMetadata : MetadataDTO, new()
    {
        private Api<ServerInstanceMetadata, SlotMetadata> Api;

        public MonoBehaviour Handler { get; private set; }

        // BaseUrl may only be set with constructor
        public string BaseUrl { get; }
        public string AuthToken { private get; set; }

        public Observable<MonitorResponseDTO> Monitor { get; private set; } =
            new Observable<MonitorResponseDTO> { };
        public Observable<InstanceListResponseDTO<ServerInstanceMetadata, SlotMetadata>> Instances
        {
            get;
            private set;
        } = new Observable<InstanceListResponseDTO<ServerInstanceMetadata, SlotMetadata>> { };

        private FilterCompiler Filter;
        private string Order = "";
        private uint Limit = 20;

        public ClientAgent(MonoBehaviour handler, string baseUrl, string authToken)
        {
            Handler = handler;
            BaseUrl = baseUrl;
            AuthToken = authToken;
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

        public void ListInstances(
            string cursor = null,
            FilterCompiler filter = null,
            string order = "",
            uint limit = 20
        )
        {
            Filter = filter;
            Order = order;
            Limit = limit;

            Api.ListServerInstances(
                (
                    InstanceListResponseDTO<ServerInstanceMetadata, SlotMetadata> response,
                    UnityWebRequest request
                ) =>
                {
                    Instances._Update(response, "list retrieved");
                },
                (string error, UnityWebRequest request) =>
                {
                    Instances._Error($"list retrieval failed\n{error}", null);
                },
                cursor,
                filter,
                order,
                limit
            );
        }

        public void GetNextPage()
        {
            if (Instances.Current is null || Instances.Current.Pagination.NextCursor is null)
            {
                Instances._Error("last page reached");
            }

            ListInstances(Instances.Current.Pagination.NextCursor, Filter, Order, Limit);
        }

        public void GetPreviousPage() { }

        public void Refresh()
        {
            Instances._Update(null, "cache deleted");
            ListInstances(null, Filter, Order, Limit);
        }
        #endregion

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

            Api = new Api<ServerInstanceMetadata, SlotMetadata>(Handler, AuthToken, BaseUrl);

            L.SubscribeLogger(Monitor, "ServerBrowser", "Monitor");
            Monitor.Subscribe(onMonitorUpdate);

            Status();
        }
    }
}
