namespace Database.Data.Models
{
    public class MafiaStats : GameStats
    {
        public int MurderGamesCount { get; set; }
        public int MurderWinsCount { get; set; }

        public float MurderRating
        {
            get => MurderGamesCount != 0 ? (float)MurderWinsCount / MurderGamesCount : 0;
            private set { }
        }


        public int DoctorMovesCount { get; set; }
        public int DoctorSuccessfullMovesCount { get; set; }

        public float DoctorRating
        {
            get => DoctorMovesCount != 0 ? (float)DoctorSuccessfullMovesCount / DoctorMovesCount : 0;
            private set { }
        }


        public int CommissionerMovesCount { get; set; }
        public int CommissionerSuccessfullMovesCount { get; set; }

        public float CommissionerRating
        {
            get => CommissionerMovesCount != 0 ? (float)CommissionerSuccessfullMovesCount / CommissionerMovesCount : 0;
            private set { }
        }


        public float TotalRating
        {
            get => (WinRate + MurderRating + DoctorRating + CommissionerRating) / 4;
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
