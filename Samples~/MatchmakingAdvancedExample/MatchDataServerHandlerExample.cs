using Edgegap.Matchmaking;
using UnityEngine;
using MyTicketsAttributes = Edgegap.Matchmaking.AdvancedTicketsAttributesDTO;
using MyTicketsEqualityVariables = Edgegap.Matchmaking.AdvancedTicketsEqualityVariables;
using MyTicketsIntersectionVariables = Edgegap.Matchmaking.AdvancedTicketsIntersectionVariables;
using MyTicketsRequestDTO = Edgegap.Matchmaking.AdvancedTicketsRequestDTO;

public class MatchDataServerHandlerExample : MonoBehaviour
{
    public static MatchDataServerHandlerExample Instance { get; private set; }
    public MatchData<
        MyTicketsRequestDTO,
        MyTicketsAttributes,
        MyTicketsEqualityVariables,
        MyTicketsIntersectionVariables
    > MmInjectedVariableStore;

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
                MmInjectedVariableStore =
                    new MatchData<
                        MyTicketsRequestDTO,
                        MyTicketsAttributes,
                        MyTicketsEqualityVariables,
                        MyTicketsIntersectionVariables
                    >();
            }
        }
    }
}
