using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public class SlotDTO
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("metadata")]
        public MetadataDTO Metadata;

        [JsonProperty("available_seats")]
        public uint AvailableSeats;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class SlotResponseDTO : SlotDTO
    {
        [JsonProperty("reserved_seats")]
        public uint ReservedSeats;

        [JsonProperty("joinable_seats")]
        public uint JoinableSeats;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
