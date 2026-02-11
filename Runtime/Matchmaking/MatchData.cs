using System;
using System.Collections;
using System.Collections.Generic;

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
                    TicketIds = ServerHandler.TryParseEnvVariable<List<string>>(envEntry);
                }
                else if (key.StartsWith("MM_TICKET_"))
                {
                    InjectedTicketDTO<A> ticket = ServerHandler.TryParseEnvVariable<
                        InjectedTicketDTO<A>
                    >(envEntry);
                    Tickets[ticket.ID] = ticket;
                }
                else if (key == "MM_GROUPS")
                {
                    Groups = ServerHandler.TryParseEnvVariable<Dictionary<string, List<string>>>(
                        envEntry
                    );
                }
                else if (key == "MM_TEAMS")
                {
                    Teams = ServerHandler.TryParseEnvVariable<Dictionary<string, List<string>>>(
                        envEntry
                    );
                }
                else if (key == "MM_MATCH_ID")
                {
                    MatchId = ServerHandler.TryParseEnvVariable<string>(envEntry);
                }
                else if (key == "MM_MATCH_PROFILE")
                {
                    MatchProfile = ServerHandler.TryParseEnvVariable<string>(envEntry);
                }
                else if (key == "MM_EQUALITY")
                {
                    Equality = ServerHandler.TryParseEnvVariable<Dictionary<string, string>>(
                        envEntry
                    );
                }
                else if (key == "MM_INTERSECTION")
                {
                    Intersection = ServerHandler.TryParseEnvVariable<
                        Dictionary<string, List<string>>
                    >(envEntry);
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
    }
}
