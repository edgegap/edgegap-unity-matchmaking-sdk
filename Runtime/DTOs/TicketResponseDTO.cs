using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap.Gen2SDK
{
    public class TicketResponseDTO
    {
        [JsonProperty("id")]
        public string ID;

        [JsonProperty("profile")]
        public string Profile;

        [JsonProperty("created_at")]
        public DateTime CreatedAt;

        [JsonProperty("status")]
        public string Status;

#nullable enable
        [JsonProperty("player_ip")]
        public string? PlayerIP;

        [JsonProperty("group_id")]
        public string? GroupID;

        [JsonProperty("team_id")]
        public string? TeamID;

        [JsonProperty("assignment")]
        public AssignmentDTO? Assignment;

#nullable disable

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class AssignmentDTO
    {
        [JsonProperty("fqdn")]
        public string Fqdn;

        [JsonProperty("public_ip")]
        public string PublicIP;

        [JsonProperty("ports")]
        public Dictionary<string, PortMappingDTO> Ports;

        [JsonProperty("location")]
        public LocationDTO Location;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class PortMappingDTO
    {
        [JsonProperty("internal")]
        public string Internal;

        [JsonProperty("external")]
        public string External;

        [JsonProperty("link")]
        public string Link;

        [JsonProperty("protocol")]
        public string Protocol;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
