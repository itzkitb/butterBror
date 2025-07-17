
namespace butterBror.Models
{
    public class StatisticItem
    {
        private int PerSecond = 0;
        private int Total = 0;

        private DateTime LastUpdate = DateTime.UtcNow;

        public int Get()
        {
            if ((DateTime.UtcNow - LastUpdate).TotalSeconds >= 1)
            {
                LastUpdate = DateTime.UtcNow;
                PerSecond = Total;
                Total = 0;
            }

            return PerSecond;
        }

        public void Add(int count = 1)
        {
            Total += count;

            if ((DateTime.UtcNow - LastUpdate).TotalSeconds >= 1)
            {
                LastUpdate = DateTime.UtcNow;
                PerSecond = Total;
                Total = 0;
            }
        }
    }
}
