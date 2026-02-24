using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Edgegap
{
    public class ServerEnvironmentDTO
    {
        [JsonProperty("ARBITRIUM_REQUEST_ID")]
        public string RequestID { get; private set; }

        [JsonProperty("ARBITRIUM_HOST_ID")]
        public string HostID { get; private set; }

        [JsonProperty("ARBITRIUM_PUBLIC_IP")]
        public string PublicIP { get; private set; }

        [JsonProperty("ARBITRIUM_DEPLOYMENT_TAGS")]
        public List<string> Tags { get; private set; }

        [JsonProperty("ARBITRIUM_HOST_BASE_CLOCK_FREQUENCY")]
        public uint HostBaseClockFrequency { get; private set; }

        [JsonProperty("ARBITRIUM_DEPLOYMENT_VCPU_UNITS")]
        public uint DeploymentVCPUUnits { get; private set; }

        [JsonProperty("ARBITRIUM_DEPLOYMENT_MEMORY_MB")]
        public uint DeploymentMemoryMB { get; private set; }

        [JsonProperty("ARBITRIUM_DELETE_URL")]
        public string SelfStopURL { get; private set; }

        [JsonProperty("ARBITRIUM_DELETE_TOKEN")]
        public string SelfStopToken { get; private set; }

        [JsonProperty("ARBITRIUM_BEACON_ENABLED")]
        public bool PrivateBeaconEnabled { get; private set; }

        [JsonProperty("ARBITRIUM_HOST_BEACON_PUBLIC_IP")]
        public string BeaconPublicIP { get; private set; }

        [JsonProperty("ARBITRIUM_HOST_BEACON_PORT_UDP_EXTERNAL")]
        public uint BeaconPortUDP { get; private set; }

        [JsonProperty("ARBITRIUM_HOST_BEACON_PORT_TCP_EXTERNAL")]
        public uint BeaconPortTCP { get; private set; }

        [JsonProperty("ARBITRIUM_DEPLOYMENT_LOCATION")]
        public LocationDTO Location;

        [JsonProperty("ARBITRIUM_PORTS_MAPPING")]
        public Dictionary<string, PortMappingDTO> PortMapping;

        [OnError]
        internal void OnError(StreamingContext context, ErrorContext errorContext)
        {
            errorContext.Handled = true;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
