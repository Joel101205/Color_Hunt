using System;

public class MatchHistoryEntry
{
    public string gameId { get; set; }
    public string hexCode { get; set; }
    public PlayerIdentity opponentIdentity { get; set; }
    public DateTime matchDate { get; set; }

    public GameResult myResult { get; set; }
    public GameResult opponentResult { get; set; }

    public MatchHistoryEntry(string gameId)
    {
        this.gameId = gameId;
    }
}