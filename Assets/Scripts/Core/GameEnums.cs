namespace BellyFull
{
    public enum GameState
    {
        WaitingForPlayers,
        NormalPlay,
        BellyAche,
        PreBlast,       // Hedgehogs hiding, countdown
        BallBlast,      // 8-second frenzy
        CrownAward,
        GameOver
    }

    public enum EquationType
    {
        Addition,
        Subtraction
    }

    public enum FieldObjectType
    {
        Ball,
        Hedgehog,
        Flower
    }

    public enum DodgeBehavior
    {
        Idle,           // Sitting calmly, edible
        Dodging,        // Fleeing from snake, uncatchable
        Frozen,         // During belly ache
        Hidden          // During ball blast (hedgehogs only)
    }

    public enum DifficultyTier
    {
        Tier1,  // Addition only, +1/+2, within 5
        Tier2,  // Addition & subtraction, within 8
        Tier3   // Mixed operations, larger steps, within 12
    }

    public enum PlayerIndex
    {
        Player1 = 0,
        Player2 = 1
    }
}
