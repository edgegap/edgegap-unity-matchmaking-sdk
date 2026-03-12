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
using MyTicketsAttributes = Edgegap.Matchmaking.LatenciesAttributesDTO;

public class MatchmakingServerAgent : MonoBehaviour
{
    private bool mockEnv = false;
    public DeploymentEnvironmentDTO DeploymentEnv { get; private set; }
    public MatchEnvironmentDTO<MyTicketsAttributes> MatchEnv;

    public static MatchmakingServerAgent Instance { get; private set; }
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
        MatchEnv = JsonConvert.DeserializeObject<MatchEnvironmentDTO<MyTicketsAttributes>>(
            stringEnv
        );

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

            MatchEnv.MatchProfile = "advanced-example";
            MatchEnv.Tickets = new Dictionary<string, InjectedTicketDTO<MyTicketsAttributes>>()
            {
                {
                    "cusfn10msflc73beiik0",
                    new InjectedTicketDTO<MyTicketsAttributes>()
                    {
                        ID = "cusfn10msflc73beiik0",
                        CreatedAt = DateTime.Parse(
                            "2025-02-21T22:17:42.3886970Z",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind
                        ),
                        PlayerIP = "174.93.233.25",
                        GroupID = "b2080c27-19c9-4fb0-8fe7-4bf1e5d285d1",
                        TeamID = "cusfn1gmsflc73beiim0",
                        Attributes = new MyTicketsAttributes(
                            new Dictionary<string, float>()
                            {
                                { "Chicago", 12.3f },
                                { "Los Angeles", 145.6f },
                                { "Tokyo", 233.2f },
                            }
                        ),
                    }
                },
                {
                    "cusfn18msflc73beiil0",
                    new InjectedTicketDTO<MyTicketsAttributes>()
                    {
                        ID = "cusfn18msflc73beiil0",
                        CreatedAt = DateTime.Parse(
                            "2025-02-21T22:17:42.2548390Z",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind
                        ),
                        PlayerIP = "174.93.233.23",
                        GroupID = "015d4dc8-6c79-4b5c-bbc6-f309b9787c8f",
                        TeamID = "cusfn1gmsflc73beiim0",
                        Attributes = new MyTicketsAttributes(
                            new Dictionary<string, float>()
                            {
                                { "Chicago", 87.3f },
                                { "LosAngeles", 32.4f },
                                { "Tokyo", 253.2f },
                            }
                        ),
                    }
                },
            };
            MatchEnv.TicketIds = MatchEnv.Tickets.Keys.ToList();
            MatchEnv.Groups = new Dictionary<string, List<string>>()
            {
                {
                    "b2080c27-19c9-4fb0-8fe7-4bf1e5d285d1",
                    "cusfn10msflc73beiik0".Split(",").ToList()
                },
                {
                    "015d4dc8-6c79-4b5c-bbc6-f309b9787c8f",
                    "cusfn18msflc73beiil0".Split(",").ToList()
                },
            };
            MatchEnv.Teams = new Dictionary<string, List<string>>()
            {
                {
                    "cusfn1gmsflc73beiim0",
                    "b2080c27-19c9-4fb0-8fe7-4bf1e5d285d1,015d4dc8-6c79-4b5c-bbc6-f309b9787c8f"
                        .Split(",")
                        .ToList()
                },
            };
            MatchEnv.Equality = new Dictionary<string, string>()
            {
                { "selected_game_mode", "quickplay" },
            };
            MatchEnv.Intersection = new Dictionary<string, List<string>>()
            {
                { "selected_map", "Airport".Split(",").ToList() },
                { "backfill_group_size", "new,1".Split(",").ToList() },
            };
        }
        #endregion

        L._Log(
            $"Server Handler | Started successfully for deployment '{DeploymentEnv.RequestID}'."
        );

        L._Log($"Matchmaking | {MatchEnv}");

        Debug.Log(DeploymentEnv);
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
