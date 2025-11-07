using System.Collections.Generic;
using Edgegap.Matchmaking;
using UnityEngine;
using UnityEngine.Networking;
using MyTicketsAttributes = Edgegap.Matchmaking.LatenciesAttributesDTO;
using MyTicketsRequestDTO = Edgegap.Matchmaking.SimpleTicketsRequestDTO;

// todo replace SimpleTicketsRequestDTO with CustomTicketsRequestDTO
// todo replace LatenciesAttributesDTO with CustomTicketsAttributes
public class MatchmakingClientHandlerExample : MonoBehaviour
{
    public static MatchmakingClientHandlerExample Instance { get; private set; }

    #region Matchmaking Configuration
    public string BaseUrl;
    public string AuthToken;

    public string ClientVersion = "1.0.0";
    public bool SaveStateInPlayerPrefs = true;
    public string PLAYER_PREFS_KEY_VERSION = "EdgegapMatchmakingClientVersion";
    public string PLAYER_PREFS_KEY_TICKET = "EdgegapMatchmakingClientTicket";
    public string PLAYER_PREFS_KEY_ASSIGNMENT = "EdgegapMatchmakingClientAssignment";

    public int RequestTimeoutSeconds = 3;
    public float PollingBackoffSeconds = 1f;
    public int MaxConsecutivePollingErrors = 10;

    public float RemoveAssignmentSeconds = 30f;
    public bool DeleteTicketOnPause = false;
    public bool DeleteTicketOnQuit = true;

    public bool LogTicketUpdates = true;
    public bool LogAssignmentUpdates = true;
    public bool LogPollingUpdates = false;
    #endregion

    public Client<MyTicketsRequestDTO, MyTicketsAttributes> MatchmakingClient;

    private string State;

    public void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void Start()
    {
        // configure Matchmaking
        MatchmakingClient = new Client<MyTicketsRequestDTO, MyTicketsAttributes>(
            this,
            BaseUrl,
            AuthToken,
            ClientVersion,
            SaveStateInPlayerPrefs,
            PLAYER_PREFS_KEY_VERSION,
            PLAYER_PREFS_KEY_TICKET,
            PLAYER_PREFS_KEY_ASSIGNMENT,
            RequestTimeoutSeconds,
            PollingBackoffSeconds,
            MaxConsecutivePollingErrors,
            RemoveAssignmentSeconds,
            LogTicketUpdates,
            LogAssignmentUpdates,
            LogPollingUpdates
        );

        // initialize Matchmaking
        MatchmakingClient.Initialize(
            // handle service monitoring
            (
                Observable<MonitorResponseDTO> monitor,
                ObservableActionType action,
                string message
            ) =>
            {
                if (action == ObservableActionType.Update)
                {
                    if (State is null && message == "healthy")
                    {
                        // todo update UI
                        MatchmakingClient.ResumeMatchmaking();
                    }
                    else if (message != "healthy")
                    {
                        // todo handle outage/maintenance
                        Debug.LogError($"Matchmaking error.\n{monitor.Current}");
                        MatchmakingClient.StopMatchmaking();
                    }
                }
            },
            // handle ticket assignment
            (
                Observable<TicketResponseDTO> assignment,
                ObservableActionType action,
                string message
            ) =>
            {
                if (action == ObservableActionType.Log && message.Contains("restart suggested"))
                {
                    MatchmakingClient.Beacons(
                        (BeaconsResponseDTO beacons) =>
                        {
                            Debug.Log($"beacons: {beacons}");

                            MatchmakingClient.MeasureBeaconsRoundTripTime(
                                beacons.Beacons,
                                (Dictionary<string, float> pings) =>
                                    MatchmakingClient.StartMatchmaking(new MyTicketsRequestDTO(pings))
                            );
                        },
                        (string error, UnityWebRequest request) =>
                        {
                            // todo handle beacon downtime, create tickets without beacons?
                            Debug.Log($"beacon error: {request}");
                        }
                    );
                }
                else if (
                    action == ObservableActionType.Update
                    && (
                        message.Contains("received")
                        || message.Contains("updated")
                        || message.Contains("abandoned")
                    )
                )
                {
                    // todo update UI
                }

                if (
                    (
                        action == ObservableActionType.Update
                        && message.Contains("updated")
                        && assignment.Current.Status == "HOST_ASSIGNED"
                    )
                    || (
                        action == ObservableActionType.Log
                        && message.Contains("reconnect suggested")
                    )
                )
                {
                    // todo join game on pre-defined game port
                    Debug.Log(
                        $"joining game: {assignment.Current.Assignment.Ports["gameport"].Link}"
                    );
                }
            }
        );
    }

    public void OnApplicationPause(bool pause)
    {
        if (!DeleteTicketOnPause || MatchmakingClient.Ticket.Current is null)
            return;
        StopMatchmaking();
    }

    public void OnApplicationQuit()
    {
        if (!DeleteTicketOnQuit)
            return;
        StopMatchmaking();
    }

    public void StartMatchmaking(MyTicketsRequestDTO ticket)
    {
        MatchmakingClient.StartMatchmaking(ticket);
    }

    // group members need to share tickets to group host to start matchmaking
    public void StartGroupMatchmaking(
        MyTicketsRequestDTO hostTicket,
        List<MyTicketsRequestDTO> memberTickets,
        bool abandon = false
    )
    {
        MatchmakingClient.StartGroupMatchmaking(
            hostTicket,
            memberTickets,
            (List<TicketResponseDTO> memberAssignments, UnityWebRequest request) =>
            {
                // todo send assignment IDs to group members to track their tickets
                Debug.Log($"member assignemnts: {memberAssignments}");
            },
            abandon
        );
    }

    public void StopMatchmaking()
    {
        MatchmakingClient.StopMatchmaking();
    }
}
