namespace DustBot
{
    public sealed class EconomySystem
    {
        private readonly ProgressionSystem progression;

        public int Coins
        {
            get { return progression.Data.dustCoins; }
        }

        public EconomySystem(ProgressionSystem progression)
        {
            this.progression = progression;
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (progression.Data.dustCoins < amount)
            {
                return false;
            }

            progression.Data.dustCoins -= amount;
            return true;
        }

        public void Add(int amount)
        {
            progression.Data.dustCoins += System.Math.Max(0, amount);
        }
    }
}

