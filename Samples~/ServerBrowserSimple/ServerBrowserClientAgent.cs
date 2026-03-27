using Edgegap;
using Edgegap.ServerBrowser;
using Newtonsoft.Json;
using UnityEngine;

public class ServerBrowserClientAgent : MonoBehaviour
{
    public static ServerBrowserClientAgent Instance { get; private set; }

    [Header("Matchmaker Instance")]
    public string BaseUrl;
    public string AuthToken;

    [Header("Exponential Retry")]
    public int RequestTimeoutSeconds = 3;
    public float PollingBackoffSeconds = 1f;
    public int MaxConsecutivePollingErrors = 10;

    //[Header("Local Caching")]
    //public bool SaveStateInPlayerPrefs = true; // toggle cache on/off
    //public string ClientVersion = ""; // changing or deleting version resets cache
    //public float RemoveAssignmentSeconds = 30f;
    //public bool DeleteTicketOnPause = false;
    //public bool DeleteTicketOnQuit = true;

    //private string PLAYER_PREFS_KEY_VERSION = "EdgegapMatchmakingClientVersion";
    //private string PLAYER_PREFS_KEY_TICKET = "EdgegapMatchmakingClientTicket";
    //private string PLAYER_PREFS_KEY_ASSIGNMENT = "EdgegapMatchmakingClientAssignment";

    //[Header("Logging")]
    //public bool LogTicketUpdates = true;
    //public bool LogAssignmentUpdates = true;
    //public bool LogPollingUpdates = false;

    public ServerAgent<MyServerInstanceMetadata> ServerBrowserClient;

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
        ServerBrowserClient = new ServerAgent<MyServerInstanceMetadata>(
            this,
            BaseUrl,
            AuthToken,
            RequestTimeoutSeconds,
            PollingBackoffSeconds,
            MaxConsecutivePollingErrors
        //SaveStateInPlayerPrefs,
        //ClientVersion,
        //RemoveAssignmentSeconds,
        //PLAYER_PREFS_KEY_VERSION,
        //PLAYER_PREFS_KEY_TICKET,
        //PLAYER_PREFS_KEY_ASSIGNMENT,
        //LogTicketUpdates,
        //LogAssignmentUpdates,
        //LogPollingUpdates
        );

        // initialize Server Browser
        ServerBrowserClient.Initialize(
            // handle service monitoring
            (Observable<MonitorDTO> monitor, ObservableActionType action, string message) =>
            {
                if (action == ObservableActionType.Update)
                {
                    if (message == "healthy")
                    {
                        // todo update UI
                        Debug.Log("start browsing");
                        ServerBrowserClient.RegisterInstance();
                    }
                    else if (message != "healthy")
                    {
                        // todo handle outage/maintenance
                        Debug.LogError($"Matchmaking error.\n{monitor.Current}");
                    }
                }
            }
        );
    }
}
