using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Edgegap.Matchmaking
{
    using L = Logger;

    public class MatchData<A>
    {
        [JsonProperty("MM_TICKET_IDS")]
        public List<string> TicketIds { get; private set; }

        [JsonIgnore]
        public Dictionary<string, InjectedTicketDTO<A>> Tickets { get; private set; }

        [JsonProperty("MM_GROUPS")]
        public Dictionary<string, List<string>> Groups { get; private set; }

        [JsonProperty("MM_TEAMS")]
        public Dictionary<string, List<string>> Teams { get; private set; }

        [JsonProperty("MM_MATCH_ID")]
        public string MatchId { get; private set; }

        [JsonProperty("MM_MATCH_PROFILE")]
        public string MatchProfile { get; private set; }

        [JsonProperty("MM_EQUALITY")]
        public Dictionary<string, string> Equality { get; private set; }

        [JsonProperty("MM_INTERSECTION")]
        public Dictionary<string, List<string>> Intersection { get; private set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            Tickets = new Dictionary<string, InjectedTicketDTO<A>>();

            foreach (DictionaryEntry envEntry in Environment.GetEnvironmentVariables())
            {
                if (!envEntry.Key.ToString().StartsWith("MM_TICKET_"))
                {
                    continue;
                }
                InjectedTicketDTO<A> ticket = JsonConvert.DeserializeObject<InjectedTicketDTO<A>>(
                    envEntry.Value.ToString()
                );
                Tickets[ticket.ID] = ticket;
            }

            foreach (string id in TicketIds)
            {
                if (!Tickets.ContainsKey(id))
                {
                    L._Warn($"Couldn't find injected ticket body for injected ticket ID {id}.");
                }
            }
        }
    }
}
