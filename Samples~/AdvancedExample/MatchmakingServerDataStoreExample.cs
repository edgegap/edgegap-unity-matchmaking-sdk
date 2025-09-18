using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Edgegap.Gen2SDK;

using MyTicketsRequestDTO = Edgegap.Gen2SDK.AdvancedTicketsRequestDTO;
using MyTicketsEqualityVariables = Edgegap.Gen2SDK.AdvancedTicketsEqualityVariables;
using MyTicketsIntersectionVariables = Edgegap.Gen2SDK.AdvancedTicketsIntersectionVariables;

public class MatchmakingServerDataStoreExample : MonoBehaviour 
{
    public static MatchmakingServerDataStoreExample Instance { get; private set; }
    public MatchmakingInjectedVariableStore<MyTicketsRequestDTO, MyTicketsEqualityVariables, MyTicketsIntersectionVariables> MmInjectedVariableStore;

    public void Awake()
    {
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else if (Instance == null)
        {
            Instance = this;
            MmInjectedVariableStore = new MatchmakingInjectedVariableStore<MyTicketsRequestDTO, MyTicketsEqualityVariables, MyTicketsIntersectionVariables>();
        }
    }
}