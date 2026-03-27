using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public class SlotDTO<SlotMetadata>
        where SlotMetadata : MetadataDTO
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("metadata")]
        public SlotMetadata Metadata;

        [JsonProperty("available_seats")]
        public uint AvailableSeats;

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
