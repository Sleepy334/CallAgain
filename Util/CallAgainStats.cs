using System.Collections.Generic;
using static TransferManager;

namespace CallAgain
{
    public class CallAgainStats
    {
        public static Dictionary<TransferReason, int> s_CallbackStats = new Dictionary<TransferReason, int>();

        public static void Init()
        {
            s_CallbackStats[TransferReason.Sick] = 0;
            s_CallbackStats[TransferReason.Dead] = 0;
            s_CallbackStats[TransferReason.Goods] = 0;
            s_CallbackStats[TransferReason.Garbage] = 0;
        }
    }
}