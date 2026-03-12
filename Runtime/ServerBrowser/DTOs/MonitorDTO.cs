using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public class MonitorDTO
    {
        [JsonProperty("status")]
        public string Status;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
