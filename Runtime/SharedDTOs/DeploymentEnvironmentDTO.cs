using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Edgegap
{
    public class DeploymentEnvironmentDTO
    {
        [JsonProperty("ARBITRIUM_REQUEST_ID")]
        public string RequestID { get; set; }

        [JsonProperty("ARBITRIUM_HOST_ID")]
        public string HostID { get; set; }

        [JsonProperty("ARBITRIUM_PUBLIC_IP")]
        public string PublicIP { get; set; }

        [JsonProperty("ARBITRIUM_DEPLOYMENT_TAGS")]
        public List<string> Tags { get; set; }

        [JsonProperty("ARBITRIUM_HOST_BASE_CLOCK_FREQUENCY")]
        public uint HostBaseClockFrequency { get; set; }

        [JsonProperty("ARBITRIUM_DEPLOYMENT_VCPU_UNITS")]
        public uint DeploymentVCPUUnits { get; set; }

        [JsonProperty("ARBITRIUM_DEPLOYMENT_MEMORY_MB")]
        public uint DeploymentMemoryMB { get; set; }

        [JsonProperty("ARBITRIUM_DELETE_URL")]
        public string SelfStopURL { get; set; }

        [JsonProperty("ARBITRIUM_DELETE_TOKEN")]
        public string SelfStopToken { get; set; }

        [JsonProperty("ARBITRIUM_BEACON_ENABLED")]
        public bool PrivateBeaconEnabled { get; set; }

        [JsonProperty("ARBITRIUM_HOST_BEACON_PUBLIC_IP")]
        public string BeaconPublicIP { get; set; }

        [JsonProperty("ARBITRIUM_HOST_BEACON_PORT_UDP_EXTERNAL")]
        public uint BeaconPortUDP { get; set; }

        [JsonProperty("ARBITRIUM_HOST_BEACON_PORT_TCP_EXTERNAL")]
        public uint BeaconPortTCP { get; set; }

        [JsonProperty("ARBITRIUM_DEPLOYMENT_LOCATION")]
        public LocationDTO Location { get; set; }

        [JsonProperty("ARBITRIUM_PORTS_MAPPING")]
        internal PortMappingEnvironmentVariable _ports;

        [JsonIgnore]
        public Dictionary<string, PortMappingDTO> PortMapping { get; set; }

        [OnError]
        internal void OnError(StreamingContext context, ErrorContext errorContext)
        {
            errorContext.Handled = true;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (_ports == null)
                return;

            PortMapping = _ports.Ports;
            foreach (KeyValuePair<string, PortMappingDTO> entry in PortMapping)
            {
                entry.Value.Link = $"{RequestID}.pr.edgegap.net:{entry.Value.External}";
            }
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class PortMappingEnvironmentVariable
    {
        [JsonProperty("ports")]
        public Dictionary<string, PortMappingDTO> Ports { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
