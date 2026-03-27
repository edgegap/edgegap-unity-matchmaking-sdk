using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public class KeepAliveResponseDTO
    {
        [JsonProperty("created_at")]
        public DateTime CreatedAt;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
