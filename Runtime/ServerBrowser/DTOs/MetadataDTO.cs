using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public abstract class MetadataDTO
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
