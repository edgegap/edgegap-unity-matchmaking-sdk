using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Edgegap.Gen2SDK
{
    using L = Logger;

    public class AdvancedTicketsEqualityVariables
    {
        [JsonProperty("selected_game_mode")]
        public string SelectedGameMode;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class AdvancedTicketsIntersectionVariables
    {
        [JsonProperty("selected_map")]
        public List<string> SelectedMap;

        [JsonProperty("selected_region")]
        public List<string> SelectedRegion;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class MatchmakingInjectedVariableStore<T, A, E, I>
        where T : TicketsRequestDTO<A>
    {
        public List<string> TicketIds { get; private set; }
        public Dictionary<string, T> TicketsData { get; private set; }
        public string MatchId { get; private set; }
        public string MatchProfile { get; private set; }
        public E Equality { get; private set; }
        public I Intersection { get; private set; }
        public Dictionary<string, List<string>> Teams {get; private set;}
        public Dictionary<string, List<string>> Groups {get; private set;}

        public MatchmakingInjectedVariableStore() {
            try
            {
                IDictionary envs = Environment.GetEnvironmentVariables();
                TicketsData = new Dictionary<string, T>();

                foreach (DictionaryEntry envEntry in envs)
                {
                    string key = envEntry.Key.ToString();

                    if (key.StartsWith("MM_TICKET_") && !key.Contains("_IDS"))
                    {
                        string id = key.Split(
                            "MM_TICKET_",
                            StringSplitOptions.RemoveEmptyEntries
                        )[0];

                        TicketsData[id] =
                            JsonConvert.DeserializeObject<T>(
                                envEntry.Value.ToString()
                            );
                    }
                    else if (key == "MM_MATCH_PROFILE") 
                    {
                        MatchProfile = envEntry.Value.ToString();
                    }
                    else if (key == "MM_TICKET_IDS")
                    {
                        TicketIds = JsonConvert.DeserializeObject<List<string>>(
                            envEntry.Value.ToString()
                        );
                    }
                    else if (key == "MM_MATCH_ID")
                    {
                        MatchId = envEntry.Value.ToString();
                    }
                    else if (key == "MM_EQUALITY")
                    {
                        Equality = JsonConvert.DeserializeObject<E>(
                            envEntry.Value.ToString()
                        );
                    }
                    else if (key == "MM_INTERSECTION")
                    {
                        Intersection = JsonConvert.DeserializeObject<I>(
                            envEntry.Value.ToString()
                        );
                    }
                    else if (key == "MM_TEAMS")
                    {
                        Teams = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(
                            envEntry.Value.ToString()
                        );
                    }
                    else if (key == "MM_GROUPS")
                    {
                        Groups = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(
                            envEntry.Value.ToString()
                        );
                    }
                }

                foreach (string id in TicketIds)
                {
                    if (!TicketsData.ContainsKey(id))
                    {
                        L._Warn($"Couldn't find injected ticket body for injected ticket ID {id}.");
                    }
                }
            }
            catch (Exception e)
            {
                L._Error($"Couldn't parse envs, consider updating Gen2 SDK. {e.Message}");
            }
        }
    }
}
