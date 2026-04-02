using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap
{
    public class ConfirmReservationsDTO
    {
        [JsonProperty("user_ids")]
        public List<string> UserIDs;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class ConfirmReservationsResponseDTO
    {
        [JsonProperty("unknown_user_ids")]
        public List<string> UnknownIDs;

        [JsonProperty("slots")]
        public List<SlotConfirmations> Slots;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class SlotConfirmations
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("accepted_user_ids")]
        public List<string> AcceptedUserIDs;

        [JsonProperty("expired_user_ids")]
        public List<string> ExpiredUserIDs;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
