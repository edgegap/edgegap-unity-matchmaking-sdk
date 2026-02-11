using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Edgegap
{
    using L = Logger;

    public class ServerHandler : MonoBehaviour
    {
        public static ServerHandler Instance { get; private set; }

        [Header("Controls")]
        public bool SelfStopOnQuit = true;

        [Header("Identifiers")]
        public string RequestID { get; private set; }
        public string HostID { get; private set; }
        public string PublicIP { get; private set; }
        public List<string> Tags { get; private set; }

        [Header("Resource Specifications")]
        public uint HostBaseClockFrequency { get; private set; }
        public uint DeploymentVCPUUnits { get; private set; }
        public uint DeploymentMemoryMB { get; private set; }

        [Header("Lifecycle Management")]
        public string SelfStopURL { get; private set; }
        public string SelfStopToken { get; private set; }

        [Header("Discoverability")]
        public bool PrivateBeaconEnabled { get; private set; }
        public string BeaconPublicIP { get; private set; }
        public uint BeaconPortUDP { get; private set; }
        public uint BeaconPortTCP { get; private set; }

        public LocationDTO Location;
        public Dictionary<string, PortMappingDTO> PortMapping;

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
        }

        public void Start()
        {
            Request = new SafeHttpRequest(this);

            foreach (DictionaryEntry envEntry in Environment.GetEnvironmentVariables())
            {
                string key = envEntry.Key.ToString();

                if (key == "ARBITRIUM_HOST_ID")
                {
                    HostID = TryParseEnvVariable<string>(envEntry);
                }
                else if (key == "ARBITRIUM_PUBLIC_IP")
                {
                    PublicIP = TryParseEnvVariable<string>(envEntry);
                }
                else if (key == "ARBITRIUM_DEPLOYMENT_TAGS")
                {
                    Tags = TryParseEnvVariable<List<string>>(envEntry);
                }
                else if (key == "ARBITRIUM_HOST_BASE_CLOCK_FREQUENCY")
                {
                    HostBaseClockFrequency = TryParseEnvVariable<uint>(envEntry);
                }
                else if (key == "ARBITRIUM_DEPLOYMENT_VCPU_UNITS")
                {
                    DeploymentVCPUUnits = TryParseEnvVariable<uint>(envEntry);
                }
                else if (key == "ARBITRIUM_DEPLOYMENT_MEMORY_MB")
                {
                    DeploymentMemoryMB = TryParseEnvVariable<uint>(envEntry);
                }
                else if (key == "ARBITRIUM_DELETE_URL")
                {
                    SelfStopURL = TryParseEnvVariable<string>(envEntry);
                }
                else if (key == "ARBITRIUM_DELETE_TOKEN")
                {
                    SelfStopToken = TryParseEnvVariable<string>(envEntry);
                }
                else if (key == "ARBITRIUM_BEACON_ENABLED")
                {
                    PrivateBeaconEnabled = TryParseEnvVariable<bool>(envEntry);
                }
                else if (key == "ARBITRIUM_HOST_BEACON_PUBLIC_IP")
                {
                    BeaconPublicIP = TryParseEnvVariable<string>(envEntry);
                }
                else if (key == "ARBITRIUM_HOST_BEACON_PORT_UDP_EXTERNAL")
                {
                    BeaconPortUDP = TryParseEnvVariable<uint>(envEntry);
                }
                else if (key == "ARBITRIUM_HOST_BEACON_PORT_TCP_EXTERNAL")
                {
                    BeaconPortTCP = TryParseEnvVariable<uint>(envEntry);
                }
            }

            L._Log($"Edgegap Server Handler | Started successfully for '{RequestID}'.");
            SelfStopDeployment();
        }

        public void SelfStopDeployment()
        {
            if (Environment.GetEnvironmentVariable("ARBITRIUM_MOCK_ENV").ToLower() == "true")
            {
                L._Log("Edgegap Server Handler | Invoking Application.Quit() in mock environment.");
                Application.Quit();
                return;
            }

            if (string.IsNullOrEmpty(SelfStopURL) || string.IsNullOrEmpty(SelfStopToken))
            {
                L._Error(
                    "Edgegap Server Handler | Self-Stop URL or Token not set, unable to self-stop."
                );
                return;
            }

            Request.Delete(
                SelfStopURL,
                SelfStopToken,
                (string response, UnityWebRequest request) =>
                {
                    L._Log(
                        $"Edgegap Server Handler | Successfully called Self-Stop API.\n{response}"
                    );
                },
                (string error, UnityWebRequest request) =>
                {
                    L._Error($"Edgegap Server Handler | Couldn't reach Self-Stop API.\n{error}");
                }
            );
        }

        public static V TryParseEnvVariable<V>(DictionaryEntry keyValuePair)
        {
            V value = default;
            try
            {
                value = JsonConvert.DeserializeObject<V>(keyValuePair.Value.ToString());
            }
            catch (Exception e)
            {
                L._Warn(
                    $"Edgegap Env | Couldn't parse variable '{keyValuePair.Key.ToString()}', "
                        + $"consider updating Edgegap SDK.\n{e.Message}"
                );
            }
            return value;
        }
    }
}
