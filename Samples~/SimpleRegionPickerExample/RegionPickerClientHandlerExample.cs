using Edgegap.Matchmaking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using MyTicketsAttributes = Edgegap.Matchmaking.LatenciesAttributesDTO;
using MyTicketsRequestDTO = Edgegap.Matchmaking.SimpleTicketsRequestDTO;

public class RegionPickerClientHandlerExample : MonoBehaviour
{
    public static RegionPickerClientHandlerExample Instance { get; private set; }

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

    #region Region Picker UI
    [SerializeField]
    private string _scrollListContainerPath = "/Canvas/Scroll View/Viewport/Content";

    [SerializeField]
    private string _statusDisplayPath = "/Canvas/StatusTxt";

    [SerializeField]
    private string _hubBtnPrefabPath = "Assets/SimpleRegionPickerExample/BeaconHubButton.prefab";

    private GameObject _ScrollListContainer;
    private Text _StatusDisplay;
    private GameObject _HubBtnPrefab;
    #endregion

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

            _ScrollListContainer = GameObject.Find(_scrollListContainerPath);
            _StatusDisplay = GameObject.Find(_statusDisplayPath).GetComponent<Text>();
            _HubBtnPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(_hubBtnPrefabPath, typeof(GameObject));

            if (_ScrollListContainer == null)
            {
                Debug.LogWarning($"Unable to find component {_scrollListContainerPath} in scene.");
            }

            if (_StatusDisplay == null)
            {
                Debug.LogWarning($"Unable to find component {_statusDisplayPath} in scene.");
            }

            if (_HubBtnPrefab == null)
            {
                Debug.LogWarning($"Unable to find prefab {_hubBtnPrefabPath} in assets.");
            }
        }
    }

    public void Start()
    {
        LoadBeacons();
    }

    public void LoadBeacons()
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
                    _StatusDisplay.text = "Fetching beacons...";

                    MatchmakingClient.Beacons(
                        (BeaconsResponseDTO beacons) =>
                        {
                            Debug.Log($"beacons: {beacons}");

                            MatchmakingClient.MeasureBeaconsRoundTripTime(
                                beacons.Beacons,
                                (Dictionary<string, float> pings) =>
                                {
                                    _StatusDisplay.text = "";

                                    foreach (KeyValuePair<string, float> entry in pings.OrderBy(key => key.Value))
                                    {
                                        GameObject btn = Instantiate(_HubBtnPrefab, _ScrollListContainer.transform);
                                        btn.GetComponent<BeaconHubButton>().SetLabel(entry.Key);
                                        btn.GetComponent<BeaconHubButton>().SetLatencyIcon(entry.Value);
                                        btn.GetComponent<Button>().onClick.AddListener(() => OnHubBtnClick(entry.Key, entry.Value));
                                    }
                                }   
                            );
                        },
                        (string error, UnityWebRequest request) =>
                        {
                            // todo handle beacon downtime, create tickets without beacons?
                            _StatusDisplay.text = "Beacon error, see logs";
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
                    action == ObservableActionType.Update
                    && message.Contains("updated")
                    && assignment.Current.Status == "MATCH_FOUND"
                )
                {
                    _StatusDisplay.text = "Match found, awaiting assignment";
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
                    _StatusDisplay.text = "Host assigned, joining game";
                    Debug.Log(
                        $"joining game: {assignment.Current.Assignment.Ports["gameport"].Link}"
                    );
                    StartCoroutine(DisconnectTimer());
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
        MatchmakingClient.Status();
    }

    public void OnHubBtnClick(string cityName, float ping)
    {
        Debug.Log($"selected region: {cityName} - {ping}");
        MyTicketsRequestDTO ticket = new(new Dictionary<string, float> { { cityName, ping } });
        MatchmakingClient.StartMatchmaking(ticket);
        
        foreach (Transform child in _ScrollListContainer.transform)
        {
            Destroy(child.gameObject);
        }

        _StatusDisplay.text = $"Selected region: {cityName}\nMatchmaking in progress...";
    }

    public IEnumerator DisconnectTimer()
    {
        yield return new WaitForSeconds(10f);
        _StatusDisplay.text = "Disconnecting from server, returning to matchmaking";
        Debug.Log("disconnecting from server, returning to matchmaking");
        StopMatchmaking();
    }
}
