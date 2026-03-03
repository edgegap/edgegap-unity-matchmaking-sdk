using System;
using System.Collections;
using Edgegap;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using L = Edgegap.Logger;

public class DeploymentAgent : MonoBehaviour
{
    public DeploymentEnvironmentDTO DeploymentEnv { get; private set; }

    public static DeploymentAgent Instance { get; private set; }
    private SafeHttpRequest Request;
    private bool mockEnv = false;

    public void Awake()
    {
        // if there is an instance, and it's not me, delete myself.

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
        Request = new SafeHttpRequest(this);

        IDictionary env = Environment.GetEnvironmentVariables();

#if UNITY_EDITOR
        env["ARBITRIUM_MOCK_ENV"] = "true";
#endif

        mockEnv = env["ARBITRIUM_MOCK_ENV"].ToString() == "true";
        if (mockEnv)
        {
            // define mock env variables here
            env["ARBITRIUM_REQUEST_ID"] = "Editor";
            env["ARBITRIUM_HOST_ID"] = "chicago-edge-od-1205-e3-30a8e1";
            env["ARBITRIUM_PUBLIC_IP"] = "172.236.117.196";
            env["ARBITRIUM_DEPLOYMENT_TAGS"] = "tag1,tag2";
            env["ARBITRIUM_HOST_BASE_CLOCK_FREQUENCY"] = "2000";
            env["ARBITRIUM_DEPLOYMENT_VCPU_UNITS"] = "1536";
            env["ARBITRIUM_DEPLOYMENT_MEMORY_MB"] = "3072";
            env["ARBITRIUM_DELETE_URL"] =
                "https://api.edgegap.com/v1/self/stop/23c01225b99d/463660";
            env["ARBITRIUM_DELETE_TOKEN"] = "b3792f3b25efd24e2e43268b9570d9ff";
            env["ARBITRIUM_HOST_IN_PRIVATE_FLEET"] = "false";
            env["ARBITRIUM_HOST_BEACON_ENABLED"] = "false";
            env["ARBITRIUM_PRIVATE_FLEET_ID"] = "PUBLIC_CLOUD";
            env["ARBITRIUM_HOST_BEACON_PUBLIC_IP"] = "";
            env["ARBITRIUM_HOST_BEACON_PORT_UDP_EXTERNAL"] = "";
            env["ARBITRIUM_HOST_BEACON_PORT_TCP_EXTERNAL"] = "";
            env["ARBITRIUM_DEPLOYMENT_LOCATION"] =
                "{\"city\": \"Chicago\", \"country\": \"United States of America\", \"continent\": \"North America\", \"administrative_division\": \"Illinois\", \"timezone\": \"Central Time\", \"latitude\": 41.9981, \"longitude\": -88.0219}";
            env["ARBITRIUM_PORTS_MAPPING"] =
                "{\"ports\": {\"gameport\": {\"name\": \"gameport\", \"internal\": 7777, \"external\": 32013, \"protocol\": \"UDP\"}}}";
        }

        string stringEnv = JsonConvert.SerializeObject(env);
        DeploymentEnv = JsonConvert.DeserializeObject<DeploymentEnvironmentDTO>(stringEnv);

        Debug.Log(DeploymentEnv);

        L._Log(
            $"Edgegap Server Handler | Started successfully for deployment '{DeploymentEnv.RequestID}'."
        );
    }

    public void SelfStopDeployment()
    {
        if (mockEnv)
        {
            L._Log("Edgegap Server Handler | Invoking Application.Quit() in mock environment.");
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return;
        }

        if (
            string.IsNullOrEmpty(DeploymentEnv.SelfStopURL)
            || string.IsNullOrEmpty(DeploymentEnv.SelfStopToken)
        )
        {
            L._Error(
                "Edgegap Server Handler | Self-Stop URL or Token not set, unable to self-stop."
            );
            return;
        }

        Request.Delete(
            DeploymentEnv.SelfStopURL,
            DeploymentEnv.SelfStopToken,
            (string response, UnityWebRequest request) =>
            {
                L._Log($"Edgegap Server Handler | Successfully called Self-Stop API.\n{response}");
            },
            (string error, UnityWebRequest request) =>
            {
                L._Error($"Edgegap Server Handler | Couldn't reach Self-Stop API.\n{error}");
            }
        );
    }
}
