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
        public static List<TransferOffer> RemoveExisitingOutgoingOffers(TransferReason material, List<TransferOffer> newOutgoingOffers)
        {
            if (newOutgoingOffers.Count > 0)
            {
                TransferManager manager = Singleton<TransferManager>.instance;

                // Reflect transfer offer fields.
                FieldInfo outgoingOfferField = typeof(TransferManager).GetField("m_outgoingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo outgoingCountField = typeof(TransferManager).GetField("m_outgoingCount", BindingFlags.NonPublic | BindingFlags.Instance);
                TransferOffer[] outgoingOffers = (TransferOffer[])outgoingOfferField.GetValue(manager);
                ushort[] outgoingCount = (ushort[])outgoingCountField.GetValue(manager);

                List<TransferOffer> existing = new List<TransferOffer>();

                int material_offset = (int)material * 8;
                int offer_offset;
                for (int priority = 7; priority >= 0; --priority)
                {
                    offer_offset = material_offset + priority;
                    for (int offerIndex = 0; offerIndex < outgoingCount[offer_offset]; offerIndex++)
                    {
                        TransferOffer offer = outgoingOffers[offer_offset * 256 + offerIndex];
                        if (offer.Citizen != 0)
                        {
                            // Check against list of new offers
                            foreach (TransferOffer offerSearch in newOutgoingOffers)
                            {
                                // Check if offer already exists
                                if (offerSearch.m_object == offer.m_object && !existing.Contains(offerSearch))
                                {
#if DEBUG
                                Debug.Log($"CALL AGAIN: Existing transfer offer {CallAgainUtils.DebugOffer(offer)} DETECTED");
#endif
                                    existing.Add(offerSearch);
                                }
                            }
                        }
                    }
                }

                return newOutgoingOffers.Except(existing).ToList();
            }
            else
            {
                return newOutgoingOffers;
            }
        }

        public static List<TransferOffer> RemoveExisitingIncomingOffers(TransferReason material, List<TransferOffer> newIncomingOffers)
        {
            if (newIncomingOffers.Count > 0)
            {
                TransferManager manager = Singleton<TransferManager>.instance;

                // Reflect transfer offer fields.
                FieldInfo incomingOfferField = typeof(TransferManager).GetField("m_incomingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo incomingCountField = typeof(TransferManager).GetField("m_incomingCount", BindingFlags.NonPublic | BindingFlags.Instance);
                TransferOffer[] incomingOffers = (TransferOffer[])incomingOfferField.GetValue(manager);
                ushort[] incomingCount = (ushort[])incomingCountField.GetValue(manager);

                List<TransferOffer> existing = new List<TransferOffer>();

                int material_offset = (int)material * 8;
                int offer_offset;
                for (int priority = 7; priority >= 0; --priority)
                {
                    offer_offset = material_offset + priority;
                    for (int offerIndex = 0; offerIndex < incomingCount[offer_offset]; offerIndex++)
                    {
                        TransferOffer offer = incomingOffers[offer_offset * 256 + offerIndex];
                        if (offer.Citizen != 0)
                        {
                            // Check against list of new offers
                            foreach (TransferOffer offerSearch in newIncomingOffers)
                            {
                                // Check if offer already exists
                                if (offerSearch.m_object == offer.m_object && !existing.Contains(offerSearch))
                                {
#if DEBUG
                                Debug.Log($"CALL AGAIN: Existing transfer offer {CallAgainUtils.DebugOffer(offer)} DETECTED");
#endif
                                    existing.Add(offerSearch);
                                }
                            }
                        }
                    }
                }

                return newIncomingOffers.Except(existing).ToList();
            }
            else
            {
                return newIncomingOffers;
            }
        }

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

