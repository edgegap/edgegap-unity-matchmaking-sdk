using Edgegap.Matchmaking;
using UnityEngine;
using MyTicketsAttributes = Edgegap.Matchmaking.LatenciesAttributesDTO;

public class MatchmakingServerAgent : MonoBehaviour
{
    public static MatchmakingServerAgent Instance { get; private set; }
    public MatchData<MyTicketsAttributes> Match;

    public void Awake()
    {
        if (Application.isBatchMode)
        {
            // If there is an instance, and it's not me, delete myself.
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else if (Instance == null)
            {
                Instance = this;
                Match = new MatchData<MyTicketsAttributes>();
                // read injected match values
                Debug.Log($"Edgegap Ticket IDs | {string.Join(", ", Match.TicketIds)}");
            }
        }
    }
}
