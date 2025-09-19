using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Edgegap.Gen2SDK;

using MyTicketsRequestDTO = Edgegap.Gen2SDK.AdvancedTicketsRequestDTO;
using MyTicketsAttributes = Edgegap.Gen2SDK.AdvancedTicketsAttributesDTO;
using MyTicketsEqualityVariables = Edgegap.Gen2SDK.AdvancedTicketsEqualityVariables;
using MyTicketsIntersectionVariables = Edgegap.Gen2SDK.AdvancedTicketsIntersectionVariables;

public class MatchmakingServerDataStoreExample : MonoBehaviour 
{
    public static MatchmakingServerDataStoreExample Instance { get; private set; }
    public MatchmakingInjectedVariableStore<MyTicketsRequestDTO, MyTicketsAttributes, MyTicketsEqualityVariables, MyTicketsIntersectionVariables> MmInjectedVariableStore;

    public void Awake()
    {
        if (Application.isBatchMode)
        {
            // If there is an instance, and it's not me, delete myself.
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else if (Instance == null)
            {
                Instance = this;
                MmInjectedVariableStore = new MatchmakingInjectedVariableStore<MyTicketsRequestDTO, MyTicketsAttributes, MyTicketsEqualityVariables, MyTicketsIntersectionVariables>();
            }
        }
    }
}