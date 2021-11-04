namespace Infrastructure.Data.Models
{
    public class MafiaStats : GameStats
    {
        public int MurderGamesCount { get; set; }
        public int MurderWinsCount { get; set; }

        public float MurderWinRate
        {
            get => MurderGamesCount != 0 ? (float)MurderWinsCount / MurderGamesCount : 0;
            private set { }
        }


        public int DoctorMovesCount { get; set; }
        public int DoctorSuccessfullMovesCount { get; set; }

        public float DoctorWinRate
        {
            get => DoctorMovesCount != 0 ? (float)DoctorSuccessfullMovesCount / DoctorMovesCount : 0;
            private set { }
        }


        public int CommissionerMovesCount { get; set; }
        public int CommissionerSuccessfullMovesCount { get; set; }

        public float CommissionerWinRate
        {
            get => CommissionerMovesCount != 0 ? (float)CommissionerSuccessfullMovesCount / CommissionerMovesCount : 0;
            private set { }
        }


        public float ExtraScores { get; set; }


        public float TotalWinRate
        {
            get => (WinRate + MurderWinRate + DoctorWinRate + CommissionerWinRate) / 4;
            private set { }
        }


        public float Scores
        {
            get => WinsCount + MurderWinsCount + DoctorSuccessfullMovesCount + CommissionerSuccessfullMovesCount + ExtraScores;
            private set { }
        }

        public float Rating
        {
            get => 100f * Scores / GamesCount * 0.25f * GamesCount;
            private set { }
        }


        public override void Reset()
        {
            base.Reset();

            MurderGamesCount = 0;
            MurderWinsCount = 0;

            DoctorMovesCount = 0;
            DoctorSuccessfullMovesCount = 0;

            CommissionerMovesCount = 0;
            CommissionerSuccessfullMovesCount = 0;
        }
    }
}
