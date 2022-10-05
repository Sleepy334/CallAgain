using System.Collections.Generic;
using static TransferManager;

namespace CallAgain
{
    public class CallAgainStats
    {
        private static Dictionary<TransferReason, int> s_CallbackStats = new Dictionary<TransferReason, int>();

        public static void Init()
        {
            s_CallbackStats[TransferReason.Crime] = 0;
            s_CallbackStats[TransferReason.Sick] = 0;
            s_CallbackStats[TransferReason.Dead] = 0;
            s_CallbackStats[TransferReason.Goods] = 0;
            s_CallbackStats[TransferReason.Garbage] = 0;
        }

        public static void AddCall(TransferReason material)
        {
            if (s_CallbackStats != null)
            {
                s_CallbackStats[material]++;
            }
        }

        public static int GetCallCount(TransferReason material)
        {
            if (s_CallbackStats != null)
            {
                return s_CallbackStats[material];
            }
            return 0;
        }
    }
}