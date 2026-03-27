using Newtonsoft.Json;

namespace Edgegap.ServerBrowser
{
    public abstract class MetadataDTO
    {
        public MetadataDTO Clone()
        {
            return JsonConvert.DeserializeObject<MetadataDTO>(JsonConvert.SerializeObject(this));
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class SimpleInstanceMetadataDTO : MetadataDTO
    {
        [JsonProperty("policy_name")]
        public string PolicyName;

        [JsonProperty("name")]
        public string Name;
    }

    public class SimpleSlotMetadataDTO : MetadataDTO { }

    public class SocialInstanceMetadataDTO : MetadataDTO
    {
        [JsonProperty("policy_name")]
        public string PolicyName;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("third_party_id")]
        public string ThirdPartyID;

        [JsonProperty("level")]
        public string Level;

        [JsonProperty("mode")]
        public string Mode;

        [JsonProperty("difficulty")]
        public string Difficulty;

        [JsonProperty("seed")]
        public string Seed;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("app_version")]
        public string AppVersion;

        [JsonProperty("location.city")]
        public string City;
    }

    public class SocialSlotMetadataDTO : MetadataDTO
    {
        [JsonProperty("third_party_id")]
        public string ThirdPartyID;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("avg_latency")]
        public float AvgLatency;

        [JsonProperty("player_ids")]
        public string PlayerIDs;
    }

    public class CooperativeInstanceMetadataDTO : MetadataDTO
    {
        [JsonProperty("policy_name")]
        public string PolicyName;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("third_party_id")]
        public string ThirdPartyID;

        [JsonProperty("level")]
        public string Level;

        [JsonProperty("mode")]
        public string Mode;

        [JsonProperty("difficulty")]
        public string Difficulty;

        [JsonProperty("avg_rank")]
        public int AvgRank;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("app_version")]
        public string AppVersion;

        [JsonProperty("tags")]
        public string Tags;

        [JsonProperty("match_id")]
        public string MatchID;

        [JsonProperty("location.city")]
        public string City;
    }

    public class CooperativeSlotMetadataDTO : MetadataDTO
    {
        [JsonProperty("third_party_id")]
        public string ThirdPartyID;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("player_ids")]
        public string PlayerIDs;
    }

    public class CompetitiveInstanceMetadataDTO : MetadataDTO
    {
        [JsonProperty("policy_name")]
        public string PolicyName;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("third_party_id")]
        public string ThirdPartyID;

        [JsonProperty("avg_rank")]
        public int AvgRank;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("is_ranked")]
        public bool IsRanked;

        [JsonProperty("app_version")]
        public string AppVersion;

        [JsonProperty("cpu_frequency")]
        public int CpuFrequency;

        [JsonProperty("match_id")]
        public string MatchID;

        [JsonProperty("location.city")]
        public string City;
    }

    public class CompetitiveSlotMetadataDTO : MetadataDTO
    {
        [JsonProperty("third_party_id")]
        public string ThirdPartyID;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("player_ids")]
        public string PlayerIDs;

        [JsonProperty("avg_rank")]
        public int AvgRank;

        [JsonProperty("avg_latency")]
        public int AvgLatency;
    }
}
