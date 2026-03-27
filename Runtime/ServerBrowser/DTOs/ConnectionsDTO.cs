using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap
{
    public class ConnectionsDTO
    {
        public HashSet<string> PendingConfirmations;
        public ConfirmReservationsResponseDTO Confirmations;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
