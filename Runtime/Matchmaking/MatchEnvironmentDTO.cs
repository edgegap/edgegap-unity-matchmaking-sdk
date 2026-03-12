using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Edgegap.Matchmaking
{
    using L = Logger;

    public class MatchEnvironmentDTO<A>
    {
        [JsonProperty("MM_MATCH_PROFILE")]
        public string MatchProfile { get; set; }

        [JsonProperty("MM_TICKET_IDS")]
        public List<string> TicketIds { get; set; }

        [JsonIgnore]
        public Dictionary<string, InjectedTicketDTO<A>> Tickets { get; set; }

        [JsonProperty("MM_GROUPS")]
        public Dictionary<string, List<string>> Groups { get; set; }

        [JsonProperty("MM_TEAMS")]
        public Dictionary<string, List<string>> Teams { get; set; }

        [JsonProperty("MM_MATCH_ID")]
        public string MatchId { get; set; }

        [JsonProperty("MM_EQUALITY")]
        public Dictionary<string, string> Equality { get; set; }

        [JsonProperty("MM_INTERSECTION")]
        public Dictionary<string, List<string>> Intersection { get; set; }

        [OnError]
        internal void OnError(StreamingContext context, ErrorContext errorContext)
        {
            errorContext.Handled = true;
        }

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

            if (TicketIds == null)
                TicketIds = new List<string>();

            foreach (string id in TicketIds)
            {
                if (!Tickets.ContainsKey(id))
                {
                    L._Warn($"Match Data | Ticket data not found for injected ticket ID {id}.");
                }
            }
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
