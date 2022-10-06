using ColossalFramework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static TransferManager;

namespace CallAgain
{
    public class CallAgainUtils
    {   
        public static string DebugOffer(TransferOffer offer)
        {
            string sMessage = $"{offer.m_object.Type} {offer.m_object.Index}";
            sMessage += " Priority:" + offer.Priority;
            if (offer.Active)
            {
                sMessage += " Active";
            }
            else
            {
                sMessage += " Passive";
            }
            sMessage += " Exclude:" + offer.Exclude;
            sMessage += " Amount: " + offer.Amount;
            sMessage += " Park: " + offer.Park;
            return sMessage;
        }
    }
}

