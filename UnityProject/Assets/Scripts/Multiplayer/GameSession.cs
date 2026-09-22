using UnityEngine;

public static class GameSession
{ 
    public static PlayerIdentity LocalPlayer { get; set; } 
    public static MatchState CurrentMatch { get; set; } 
    public static GameResult OwnResult { get; set; } 
    public static GameResult OpponentResult { get; set; }
    public static string PendingRematchOpponentId { get; set; } = "";
    public static string LastOpponentId { get; set; } = "";
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // so that we don't need Reload Domain
    private static void ResetStatics()
    {
        LocalPlayer = null;
        CurrentMatch = null;
        OwnResult = null;
        OpponentResult = null;
        PendingRematchOpponentId = "";
        LastOpponentId = "";
        CanvasUIManager.selectedColor = Color.white;
    }
    
    public static void ClearMatch()
    {
        if (CurrentMatch?.opponentIdentity != null && !string.IsNullOrEmpty(CurrentMatch.opponentIdentity.firebasePlayerId))
        {
            LastOpponentId = CurrentMatch.opponentIdentity.firebasePlayerId;
        }
        CurrentMatch = null;
        OwnResult = null;
        OpponentResult = null;
    }

    public static void ClearAll()
    {
        LocalPlayer = null;
        ClearMatch();
        PendingRematchOpponentId = "";
        LastOpponentId = "";
    }
}
