using ColossalFramework;
using System.Collections.Generic;
using System.Reflection;
using static TransferManager;

namespace CallAgain
{
    public class TransferManagerCheckOffers
    {
        TransferOffer[]? m_outgoingOffers = null;
        TransferOffer[]? m_incomingOffers = null;
        ushort[]? m_outgoingCount = null;
        ushort[]? m_incomingCount = null;

        public TransferManagerCheckOffers()
        {
            TransferManager manager = Singleton<TransferManager>.instance;

            // Reflect transfer offer fields.
            FieldInfo outgoingOfferField = typeof(TransferManager).GetField("m_outgoingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo outgoingCountField = typeof(TransferManager).GetField("m_outgoingCount", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo incomingOfferField = typeof(TransferManager).GetField("m_incomingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo incomingCountField = typeof(TransferManager).GetField("m_incomingCount", BindingFlags.NonPublic | BindingFlags.Instance);

            m_outgoingOffers = (TransferOffer[])outgoingOfferField.GetValue(manager);
            m_incomingOffers = (TransferOffer[])incomingOfferField.GetValue(manager);
            m_outgoingCount = (ushort[])outgoingCountField.GetValue(manager);
            m_incomingCount = (ushort[])incomingCountField.GetValue(manager);
        }

        public void RemoveExisitingOutgoingOffers(TransferReason material, ref FastList<TransferOffer> offers)
        {
            if (m_outgoingCount != null && m_outgoingOffers != null && offers.m_size > 0)
            {
                List<TransferOffer> existing = new List<TransferOffer>();

                int material_offset = (int)material * 8;
                int offer_offset;
                for (int priority = 7; priority >= 0; --priority)
                {
                    offer_offset = material_offset + priority;
                    for (int offerIndex = 0; offerIndex < m_outgoingCount[offer_offset]; offerIndex++)
                    {
                        TransferOffer offer = m_outgoingOffers[offer_offset * 256 + offerIndex];
                        if (offer.Citizen != 0)
                        {
                            // Check against list of new offers
                            foreach (TransferOffer offerSearch in offers)
                            {
                                // Check if offer already exists
                                if (offerSearch.m_object == offer.m_object)
                                {
#if DEBUG
                                    Debug.Log($"CALL AGAIN: Existing transfer offer {CallAgainUtils.DebugOffer(offer)} DETECTED");
#endif
                                    offers.Remove(offer);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void RemoveExisitingIncomingOffers(TransferReason material, ref FastList<TransferOffer> offers)
        {
            if (m_incomingCount != null && m_incomingOffers != null && offers.m_size > 0)
            {
                int material_offset = (int)material * 8;
                int offer_offset;
                for (int priority = 7; priority >= 0; --priority)
                {
                    offer_offset = material_offset + priority;
                    for (int offerIndex = 0; offerIndex < m_incomingCount[offer_offset]; offerIndex++)
                    {
                        TransferOffer offer = m_incomingOffers[offer_offset * 256 + offerIndex];
                        if (offer.Citizen != 0)
                        {
                            // Check against list of new offers
                            foreach (TransferOffer offerSearch in offers)
                            {
                                // Check if offer already exists
                                if (offerSearch.m_object == offer.m_object)
                                {
#if DEBUG
                                    Debug.Log($"CALL AGAIN: Existing transfer offer {CallAgainUtils.DebugOffer(offer)} DETECTED");
#endif
                                    offers.Remove(offer);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}