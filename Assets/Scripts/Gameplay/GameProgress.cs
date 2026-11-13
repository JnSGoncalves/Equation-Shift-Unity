namespace EquationShift
{
    public static class GameProgress
    {
        public static void StartNewGame()
        {
            VillageSequence.ResetProgress();
            RoomLever.ResetActivations();
        }
    }
}
