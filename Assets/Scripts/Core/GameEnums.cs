namespace BellyFull
{
    public enum GameState
    {
        TitleScreen,
        WaitingForPlayers,
        Countdown,
        NormalPlay,
        PreBlast,
        BallBlast,
        CrownAward,
        GameOver
    }

    public enum FieldObjectType
    {
        Hedgehog,
        Ball     // blast balls only
    }

    public enum HedgehogBehavior
    {
        Wandering,
        Fleeing,
        Hidden
    }

    public enum PlayerIndex
    {
        Player1 = 0,
        Player2 = 1
    }
}
