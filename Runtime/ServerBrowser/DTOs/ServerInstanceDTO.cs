using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public abstract class ServerInstanceDTO<ServerInstanceMetadata>
    {
        [JsonProperty("request_id")]
        public string RequestID;

        [JsonProperty("metadata")]
        public ServerInstanceMetadata Metadata;

        [JsonProperty("server")]
        public DeploymentDTO Server;

        [JsonProperty("slots")]
        public List<SlotDTO> Slots;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class ListServerInstanceDTO { }

    public class UpdateServerInstanceDTO { }
}
