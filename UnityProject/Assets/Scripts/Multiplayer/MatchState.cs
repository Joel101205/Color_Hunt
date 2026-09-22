public class MatchState
{
    public string activeGameDocId { get; set; }
    public string hexCode { get; set; }
    public PlayerIdentity opponentIdentity { get; set; }
    public MatchSettings activeMatchSettings { get; set; }

    public MatchState(string activeGameDocId)
    {
        this.activeGameDocId = activeGameDocId;
    }
}
