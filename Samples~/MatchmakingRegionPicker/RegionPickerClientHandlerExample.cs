using Edgegap;
using Edgegap.Matchmaking;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using MyTicketsAttributes = Edgegap.Matchmaking.LatenciesAttributesDTO;
using MyTicketsRequestDTO = Edgegap.Matchmaking.SimpleTicketsRequestDTO;

// todo replace SimpleTicketsRequestDTO with CustomTicketsRequestDTO
// todo replace LatenciesAttributesDTO with CustomTicketsAttributes

public class RegionPickerClientHandlerExample : MonoBehaviour
{
    public static RegionPickerClientHandlerExample Instance { get; private set; }

    #region Matchmaking Configuration

    [Header("Matchmaker Instance")]
    public string BaseUrl;
    public string AuthToken;

    [Header("Exponential Retry")]
    public int RequestTimeoutSeconds = 3;
    public float PollingBackoffSeconds = 1f;
    public int MaxConsecutivePollingErrors = 10;

    [Header("Expiration and Cleanup")]
    public float RemoveAssignmentSeconds = 30f;
    public bool DeleteTicketOnPause = false;
    public bool DeleteTicketOnQuit = true;

    [Header("Logging")]
    public bool LogTicketUpdates = true;
    public bool LogAssignmentUpdates = true;
    public bool LogPollingUpdates = false;
    #endregion

    public Client<MyTicketsRequestDTO, MyTicketsAttributes> MatchmakingClient;

    #region Region Picker UI
    public GameObject ScrollListContainer;
    public GameObject DisconnectButton;
    public Text StatusDisplay;
    public GameObject HubBtnPrefab;

    private string ScrollListContainerDefaultPath = "/Canvas/Scroll View/Viewport/Content";
    private string DisconnectButtonDefaultPath = "/Canvas/DisconnectBtn";
    private string StatusDisplayDefaultPath = "/Canvas/StatusTxt";
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

            if (ScrollListContainer == null)
            {
                Debug.Log("RP ClientHandler | No Scroll List Container provided, using default.");
                ScrollListContainer = GameObject.Find(ScrollListContainerDefaultPath);

                if (ScrollListContainer == null)
                {
                    Debug.LogWarning($"RP ClientHandler | Unable to find default component {ScrollListContainerDefaultPath} in scene.");
                }
            }

            if (DisconnectButton == null)
            {
                Debug.Log("RP ClientHandler | No Disconnect Button provided, using default.");
                DisconnectButton = GameObject.Find(DisconnectButtonDefaultPath);

                if (DisconnectButton == null)
                {
                    Debug.LogWarning($"RP ClientHandler | Unable to find default component {DisconnectButtonDefaultPath} in scene.");
                }
                else
                {
                    DisconnectButton.GetComponent<Button>().onClick.AddListener(Disconnect);
                    DisconnectButton.SetActive(false);
                }
            }

            if (StatusDisplay == null)
            {
                Debug.Log("RP ClientHandler | No Status Display provided, using default.");
                StatusDisplay = GameObject.Find(StatusDisplayDefaultPath).GetComponent<Text>();

                if (StatusDisplay == null)
                {
                    Debug.LogWarning($"RP ClientHandler | Unable to find default component {StatusDisplayDefaultPath} in scene.");
                }
            }

            if (HubBtnPrefab == null)
            {
                Debug.Log("RP ClientHandler | No Hub Button Prefab provided, using default.");
                string guid = AssetDatabase.FindAssets($"t:Script {nameof(RegionPickerClientHandlerExample)}")[0];
                string currentAssetPath = AssetDatabase.GUIDToAssetPath(guid);
                string hubBtnPrefabDefaultPath = currentAssetPath.Split(nameof(RegionPickerClientHandlerExample))[0] + "BeaconHubButton.prefab";

                HubBtnPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(hubBtnPrefabDefaultPath, typeof(GameObject));

                if (HubBtnPrefab == null)
                {
                    Debug.LogWarning($"RP ClientHandler | Unable to find default prefab {hubBtnPrefabDefaultPath} in assets.");
                }
            }
        }
    }

    public void Start()
    {
        // configure Matchmaking
        MatchmakingClient = new Client<MyTicketsRequestDTO, MyTicketsAttributes>(
            this,
            BaseUrl,
            AuthToken,
            RequestTimeoutSeconds,
            PollingBackoffSeconds,
            MaxConsecutivePollingErrors,
            RemoveAssignmentSeconds,
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
                if (action == ObservableActionType.Update && message == "healthy")
                {
                    // todo update UI

                    if (ScrollListContainer.transform.childCount > 0)
                    {
                        StatusDisplay.text = "";

                        foreach (Transform child in ScrollListContainer.transform)
                        {
                            child.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        StatusDisplay.text = "Fetching beacons...";

                        MatchmakingClient.Beacons(
                            (BeaconsResponseDTO beacons) =>
                            {
                                MatchmakingClient.MeasureBeaconsRoundTripTime(
                                    beacons.Beacons,
                                    (Dictionary<string, float> pings) =>
                                    {
                                        StatusDisplay.text = "";

                                        foreach (KeyValuePair<string, float> entry in pings.OrderBy(key => key.Value))
                                        {
                                            GameObject btn = Instantiate(HubBtnPrefab, ScrollListContainer.transform);
                                            btn.GetComponent<BeaconHubButton>().BtnLabel.text = entry.Key;
                                            btn.GetComponent<BeaconHubButton>().SetLatencyIcon(entry.Value);
                                            btn.GetComponent<Button>().onClick.AddListener(() => OnHubBtnClick(entry.Key, entry.Value));
                                        }
                                    }
                                );
                            },
                            (string error, UnityWebRequest request) =>
                            {
                                // todo handle beacon downtime, create tickets without beacons?
                                StatusDisplay.text = "Beacon error, see logs";
                                Debug.Log($"RP ClientHandler |  Beacon error.\n{error}");
                            }
                        );
                    }
                }
                else if (action == ObservableActionType.Error || message == "unhealthy")
                {
                    // todo handle outage/maintenance
                    Debug.LogError($"RP ClientHandler | Service is unhealthy.\n{monitor.Current}");
                    MatchmakingClient.StopMatchmaking();
                }
            },
            // handle ticket assignment
            (
                Observable<TicketResponseDTO> assignment,
                ObservableActionType action,
                string message
            ) =>
            {
                if (
                    action == ObservableActionType.Update
                    && (
                        message.Contains("received")
                        || message.Contains("updated")
                        || message.Contains("abandon")
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
                    StatusDisplay.text = "Match found, awaiting assignment";
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
                    foreach (Transform child in ScrollListContainer.transform)
                    {
                        Destroy(child.gameObject);
                    }

                    // todo join game on pre-defined game port
                    StatusDisplay.text = "Host assigned, joining game";
                    Debug.Log(
                        $"RP ClientHandler | Joining game: {assignment.Current.Assignment.Ports["gameport"].Link}"
                    );

                    DisconnectButton.SetActive(true);
                    DisconnectButton.GetComponent<Button>().interactable = false;
                }

                if (
                    action == ObservableActionType.Update
                        && message.Contains("removed")
                        && assignment.Previous?.Status == "HOST_ASSIGNED"
                )
                {
                    DisconnectButton.GetComponent<Button>().interactable = true;
                }
            }
        );
    }

    public void OnApplicationPause(bool pause)
    {
        if (!DeleteTicketOnPause || MatchmakingClient.Assignment.Current is null)
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
                Debug.Log($"RP ClientHandler | Member assignemnts: {memberAssignments}");
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
        MyTicketsRequestDTO ticket = new(new Dictionary<string, float> { { cityName, ping } });
        MatchmakingClient.StartMatchmaking(ticket);

        foreach (Transform child in ScrollListContainer.transform)
        {
            child.gameObject.SetActive(false);
        }

        StatusDisplay.text = $"Selected region: {cityName}\nMatchmaking in progress...";
    }

    public void Disconnect()
    {
        DisconnectButton.SetActive(false);
        StatusDisplay.text = "Disconnecting from server, returning to matchmaking";
        Debug.Log("RP ClientHandler | Disconnecting from server, returning to matchmaking.");
        StopMatchmaking();
    }
}
