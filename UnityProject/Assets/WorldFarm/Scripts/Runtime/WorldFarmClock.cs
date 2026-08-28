using System;

namespace WorldFarm
{
    public static class WorldFarmClock
    {
        public static long NowUnixMillis()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            return now.ToUnixTimeMilliseconds();
        }
    }
}
