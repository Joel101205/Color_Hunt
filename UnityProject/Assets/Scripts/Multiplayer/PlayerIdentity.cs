using UnityEngine;
using System.Collections.Generic;

public class PlayerIdentity
{
    public string unityPlayerId { get; set; }
    public string firebasePlayerId { get; set; }
    public string userName { get; set; }

    public ColorInventory colorInventory { get; private set; }
    
    public string gamesPlayed { get; set; }
    
    public string gamesWon { get; set; }

    public PlayerIdentity(string firebaseId, string unityId)
    {
        firebasePlayerId = firebaseId;
        unityPlayerId = unityId;
        colorInventory = new ColorInventory();
        colorInventory.InitializeColorInventory();
    }
    
    public PlayerIdentity(string firebaseId)
    {
        firebasePlayerId = firebaseId;
        colorInventory = new ColorInventory();
        colorInventory.InitializeColorInventory();
    }
}
