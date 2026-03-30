using System;
using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public class SlotDTO<SlotMetadata>
        where SlotMetadata : MetadataDTO, new()
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

        public SlotDTO<SlotMetadata> ApplyUpdate(SlotUpdateDTO<SlotMetadata> update)
        {
            if (update.Name != Name)
            {
                throw new InvalidOperationException(
                    $"Slot name mismatch, expected '{Name}' but got '{update.Name}' (update)."
                );
            }

            return new SlotDTO<SlotMetadata>()
            {
                Name = Name,
                Metadata = Metadata.Merge(update.Metadata),
                AvailableSeats = update.AvailableSeats,
                ReservedSeats = ReservedSeats,
                JoinableSeats = JoinableSeats,
            };
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class SlotUpdateDTO<SlotMetadata> : SlotDTO<SlotMetadata>
        where SlotMetadata : MetadataDTO, new()
    {
        private new string Name;

        [JsonIgnore]
        private new uint ReservedSeats;

        [JsonIgnore]
        private new uint JoinableSeats;

        public SlotUpdateDTO(string name, uint availableSeats, SlotMetadata metadata = null)
        {
            Name = name;
            AvailableSeats = availableSeats;
            Metadata = metadata;
        }
    }
}
