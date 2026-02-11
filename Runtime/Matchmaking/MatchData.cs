using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap.Matchmaking
{
    using L = Logger;

    public class MatchData<A>
    {
        public List<string> TicketIds { get; private set; }
        public Dictionary<string, InjectedTicketDTO<A>> Tickets { get; private set; }
        public Dictionary<string, List<string>> Groups { get; private set; }
        public Dictionary<string, List<string>> Teams { get; private set; }
        public string MatchId { get; private set; }
        public string MatchProfile { get; private set; }
        public Dictionary<string, string> Equality { get; private set; }
        public Dictionary<string, List<string>> Intersection { get; private set; }

        public MatchData()
        {
            Tickets = new Dictionary<string, InjectedTicketDTO<A>>();

            foreach (DictionaryEntry envEntry in Environment.GetEnvironmentVariables())
            {
                string key = envEntry.Key.ToString();

                if (key == "MM_TICKET_IDS")
                {
                    TicketIds = TryParseEnvVariable<List<string>>(envEntry);
                }
                else if (key.StartsWith("MM_TICKET_"))
                {
                    InjectedTicketDTO<A> ticket = TryParseEnvVariable<InjectedTicketDTO<A>>(
                        envEntry
                    );
                    Tickets[ticket.ID] = ticket;
                }
                else if (key == "MM_GROUPS")
                {
                    Groups = TryParseEnvVariable<Dictionary<string, List<string>>>(envEntry);
                }
                else if (key == "MM_TEAMS")
                {
                    Teams = TryParseEnvVariable<Dictionary<string, List<string>>>(envEntry);
                }
                else if (key == "MM_MATCH_ID")
                {
                    MatchId = TryParseEnvVariable<string>(envEntry);
                }
                else if (key == "MM_MATCH_PROFILE")
                {
                    MatchProfile = TryParseEnvVariable<string>(envEntry);
                }
                else if (key == "MM_EQUALITY")
                {
                    Equality = TryParseEnvVariable<Dictionary<string, string>>(envEntry);
                }
                else if (key == "MM_INTERSECTION")
                {
                    Intersection = TryParseEnvVariable<Dictionary<string, List<string>>>(envEntry);
                }
            }

            foreach (string id in TicketIds)
            {
                if (!Tickets.ContainsKey(id))
                {
                    L._Warn($"Couldn't find injected ticket body for injected ticket ID {id}.");
                }
            }
        }

        public V TryParseEnvVariable<V>(DictionaryEntry keyValuePair)
        {
            V value;
            try
            {
                value = JsonConvert.DeserializeObject<V>(keyValuePair.Value.ToString());
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Edgegap Envs | Couldn't parse variable '{keyValuePair.Key}', consider updating Edgegap SDK.\n{e.Message}"
                );
            }
            return value;
        }
    }
}
