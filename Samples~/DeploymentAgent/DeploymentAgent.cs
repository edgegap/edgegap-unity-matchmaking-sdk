using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Edgegap;
using Edgegap.Matchmaking;
using Newtonsoft.Json;
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
    }

    public void Start()
    {
        Request = new SafeHttpRequest(this);
        IDictionary env = Environment.GetEnvironmentVariables();

#if UNITY_EDITOR
        mockEnv = true;
#endif

        string stringEnv = JsonConvert.SerializeObject(env);
        DeploymentEnv = JsonConvert.DeserializeObject<DeploymentEnvironmentDTO>(stringEnv);

        #region mock data
        mockEnv = mockEnv || !string.IsNullOrEmpty(env["ARBITRIUM_MOCK_ENV"].ToString());
        if (mockEnv)
        {
            // define mock env variables here
            DeploymentEnv.RequestID = "Editor";
            DeploymentEnv.PublicIP = "172.236.117.196";
            DeploymentEnv.Tags = "tag1,tag2".Split(",").ToList();
            DeploymentEnv.HostBaseClockFrequency = 2000;
            DeploymentEnv.DeploymentVCPUUnits = 1536;
            DeploymentEnv.DeploymentMemoryMB = 3072;
            DeploymentEnv.Location = new LocationDTO()
            {
                City = "Chicago",
                Country = "United States of America",
                Continent = "North America",
                AdministrativeDivision = "Illinois",
                Timezone = "Central Time",
            };
            DeploymentEnv.PortMapping = new Dictionary<string, PortMappingDTO>()
            {
                {
                    "gameport",
                    new PortMappingDTO()
                    {
                        Internal = "7777",
                        External = "32013",
                        Protocol = "UDP",
                    }
                },
            };
        }
        #endregion

        L._Log(
            $"Server Handler | Started successfully for deployment '{DeploymentEnv.RequestID}'."
        );
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
