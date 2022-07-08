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
                                // Currently just checking Citizen
                                if (offerSearch.Citizen == offer.Citizen && !existing.Contains(offerSearch))
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
                                // Currently just checking Citizen
                                if (offerSearch.Citizen == offer.Citizen && !existing.Contains(offerSearch))
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
            string sMessage = "";
            sMessage += "Priority:" + offer.Priority;
            sMessage += "Active:" + offer.Active;
            sMessage += "Exclude:" + offer.Exclude;
            sMessage += " Amount: " + offer.Amount;
            if (offer.Building > 0 && offer.Building < BuildingManager.instance.m_buildings.m_size)
            {
                var instB = default(InstanceID);
                instB.Building = offer.Building;
                sMessage += " (" + offer.Building + ")" + BuildingManager.instance.m_buildings.m_buffer[offer.Building].Info?.name + "(" + InstanceManager.instance.GetName(instB) + ")";
            }
            if (offer.Vehicle > 0 && offer.Vehicle < VehicleManager.instance.m_vehicles.m_size)
            {
                sMessage += " (" + offer.Vehicle + ")" + VehicleManager.instance.m_vehicles.m_buffer[offer.Vehicle].Info?.name;
            }
            if (offer.Citizen > 0)
            {
                sMessage += $" Citizen:{offer.Citizen}";
                Citizen oCitizen = CitizenManager.instance.m_citizens.m_buffer[offer.Citizen];
                sMessage += $" Building:{oCitizen.GetBuildingByLocation()}";
            }
            if (offer.NetSegment > 0)
            {
                sMessage += $" NetSegment={offer.NetSegment}";
            }
            if (offer.TransportLine > 0)
            {
                sMessage += $" TransportLine={offer.TransportLine}";
            }
            if (sMessage.Length == 0)
            {
                sMessage = " unknown";
            }
            return sMessage;
        }
    }
}

