using Edgegap;
using Edgegap.ServerBrowser;
using UnityEngine;
using MyInstanceMetadata = Edgegap.ServerBrowser.SimpleInstanceMetadataDTO;
using MySlotMetadata = Edgegap.ServerBrowser.SimpleSlotMetadataDTO;

// todo replace SimpleInstanceMetadataDTO with custom class
// todo replace SimpleSlotMetadataDTO with custom class

public class ServerBrowserClientHandler : MonoBehaviour
{
    [Header("Matchmaker Instance")]
    public string BaseUrl;
    public string ClientToken;

    [Header("Exponential Retry")]
    public int RequestTimeoutSeconds = 10;
    public int MaxConsecutiveRetryErrors = 10;

    public static ServerBrowserClientHandler Instance { get; private set; }
    private ClientAgent<MyInstanceMetadata, MySlotMetadata> ClientAgent;
    private SafeHttpRequest Request;

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

        DontDestroyOnLoad(this.gameObject);
    }

    public void Start() { }
}
