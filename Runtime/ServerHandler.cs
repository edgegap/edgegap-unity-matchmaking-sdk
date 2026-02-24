using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Edgegap
{
    using L = Logger;

    public class ServerHandler : MonoBehaviour
    {
        public ServerEnvironmentDTO DeploymentEnv { get; private set; }

        public static ServerHandler Instance { get; private set; }
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

            string stringEnv = JsonConvert.SerializeObject(Environment.GetEnvironmentVariables());
            DeploymentEnv = JsonConvert.DeserializeObject<ServerEnvironmentDTO>(stringEnv);

            L._Log(
                $"Edgegap Server Handler | Started successfully for deployment '{DeploymentEnv.RequestID}'."
            );
            SelfStopDeployment();
        }

        public void SelfStopDeployment()
        {
            string mockEnv = Environment.GetEnvironmentVariable("ARBITRIUM_MOCK_ENV");
            if (!string.IsNullOrEmpty(mockEnv) && mockEnv.ToLower() == "true")
            {
                L._Log("Edgegap Server Handler | Invoking Application.Quit() in mock environment.");
                Application.Quit();
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
    }
}
