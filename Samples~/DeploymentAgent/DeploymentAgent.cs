using System;
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
        System.Collections.IDictionary env = Environment.GetEnvironmentVariables();

#if UNITY_EDITOR
        env["ARBITRIUM_MOCK_ENV"] = "true";
#endif

        mockEnv = env["ARBITRIUM_MOCK_ENV"].ToString() == "true";
        if (mockEnv)
        {
            env["ARBITRIUM_REQUEST_ID"] = "Editor";
            // define other mock env variables here
        }

        string stringEnv = JsonConvert.SerializeObject(env);
        DeploymentEnv = JsonConvert.DeserializeObject<DeploymentEnvironmentDTO>(stringEnv);

        L._Log(
            $"Edgegap Server Handler | Started successfully for deployment '{DeploymentEnv.RequestID}'."
        );
        SelfStopDeployment();
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
