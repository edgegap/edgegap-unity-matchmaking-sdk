using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public class ConnectionsDTO<SlotMetadata>
        where SlotMetadata : MetadataDTO, new()
    {
        public HashSet<string> PendingConfirmations = new HashSet<string>();
        public Dictionary<string, SlotUpdateDTO<SlotMetadata>> PendingUpdates =
            new Dictionary<string, SlotUpdateDTO<SlotMetadata>>();
        public ConfirmReservationsResponseDTO Confirmations;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
