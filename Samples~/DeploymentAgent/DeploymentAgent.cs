using System;
using System.Collections;
using Edgegap;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using L = Edgegap.Logger;

public class DeploymentAgent : MonoBehaviour
{
    public bool mockEnv = false;
    public DeploymentEnvironmentDTO DeploymentEnv { get; private set; }

    public static DeploymentAgent Instance { get; private set; }
    private SafeHttpRequest Request;

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

        DontDestroyOnLoad(this.gameObject);
    }

    public void Start()
    {
        Request = new SafeHttpRequest(this);
        IDictionary env = Environment.GetEnvironmentVariables();

#if UNITY_EDITOR
        mockEnv = true;
#endif

        #region mock data
        mockEnv = mockEnv || !string.IsNullOrEmpty(env["ARBITRIUM_MOCK_ENV"]?.ToString());
        if (mockEnv)
        {
            // define mock env variables here
            env["ARBITRIUM_REQUEST_ID"] = "Editor";
            env["ARBITRIUM_PUBLIC_IP"] = "172.236.117.196";
            env["ARBITRIUM_DEPLOYMENT_TAGS"] = "tag1,tag2";
            env["ARBITRIUM_HOST_BASE_CLOCK_FREQUENCY"] = "2000";
            env["ARBITRIUM_DEPLOYMENT_VCPU_UNITS"] = "1536";
            env["ARBITRIUM_DEPLOYMENT_MEMORY_MB"] = "3072";
            env["ARBITRIUM_DEPLOYMENT_LOCATION"] =
                "{\"city\":\"Chicago\",\"country\":\"United States of America\",\"continent\":\"North America\",\"administrative_division\":\"Illinois\",\"timezone\":\"Central Time\"}";
            env["ARBITRIUM_PORTS_MAPPING"] =
                "{\"ports\":{\"gameport\":{\"name\":\"GamePort\",\"internal\":7777,\"external\":31504,\"protocol\":\"UDP\"}}}";
        }
        #endregion

        DeploymentEnv = new DeploymentEnvironmentDTO(env);

        L._Log(
            $"Server Handler | Started successfully for deployment '{DeploymentEnv.RequestID}'."
        );
    }

    public void OnApplicationQuit()
    {
        if (enabled)
        {
            SelfStopDeployment();
        }
    }

    public void SelfStopDeployment()
    {
        if (mockEnv)
        {
            L._Log("Server Handler | Invoking Application.Quit() in mock environment.");
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
            L._Error("Server Handler | Self-Stop URL or Token not set, unable to self-stop.");
            return;
        }

        Request.Delete(
            DeploymentEnv.SelfStopURL,
            DeploymentEnv.SelfStopToken,
            (string response, UnityWebRequest request) =>
            {
                L._Log($"Server Handler | Successfully called Self-Stop API.\n{response}");
            },
            (string error, UnityWebRequest request) =>
            {
                L._Error($"Server Handler | Couldn't reach Self-Stop API.\n{error}");
            }
        );
    }
}
