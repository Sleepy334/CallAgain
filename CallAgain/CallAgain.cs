using CallAgain.Settings;
using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static TransferManager;

namespace CallAgain
{
    public struct CallAgainData
    {
        public int m_iTimer;
        public int m_iRetries;
    }

    public class CallAgain
    {
        private Dictionary<ushort, CallAgainData> m_Healthcalls;
        private Dictionary<ushort, CallAgainData> m_Deathcalls;
        private Dictionary<ushort, CallAgainData> m_Goodscalls;
        private Dictionary<ushort, CallAgainData> m_Garbagecalls;
        TransferManagerCheckOffers m_checkOffers;
        Stopwatch m_watch;

        public CallAgain()
        {
            m_Healthcalls = new Dictionary<ushort, CallAgainData>();
            m_Deathcalls = new Dictionary<ushort, CallAgainData>();
            m_Goodscalls = new Dictionary<ushort, CallAgainData>();
            m_Garbagecalls = new Dictionary<ushort, CallAgainData>();
            m_checkOffers = new TransferManagerCheckOffers();
            m_watch = Stopwatch.StartNew();
        }

        public void Update()
        {
#if DEBUG
            long lStartTime = m_watch.ElapsedMilliseconds;
#endif
            Dictionary<TransferReason, List<TransferOffer>> issueOutgoingList = new Dictionary<TransferReason, List<TransferOffer>>();
            issueOutgoingList[TransferReason.Sick] = new List<TransferOffer>();
            issueOutgoingList[TransferReason.Dead] = new List<TransferOffer>();
            issueOutgoingList[TransferReason.Garbage] = new List<TransferOffer>();

            Dictionary<TransferReason, List<TransferOffer>> issueIncomingList = new Dictionary<TransferReason, List<TransferOffer>>();
            issueIncomingList[TransferReason.Goods] = new List<TransferOffer>();

            for (int i = 0; i < BuildingManager.instance.m_buildings.m_buffer.Length; i++)
            {
                Building building = BuildingManager.instance.m_buildings.m_buffer[i];
                
                // Outgoing
                issueOutgoingList[TransferReason.Sick].AddRange(CheckHealthTimer((ushort)i, building));
                issueOutgoingList[TransferReason.Dead].AddRange(CheckDeathTimer((ushort)i, building));
                issueOutgoingList[TransferReason.Garbage].AddRange(CheckGarbage((ushort)i, building, m_watch));

                // Incoming
                issueIncomingList[TransferReason.Goods].AddRange(CheckGoodsTimer((ushort)i, building));
            }

            AddOutgoingOffersCheckExisting(issueOutgoingList);
            AddIncomingOffersCheckExisting(issueIncomingList);
#if DEBUG
            long lStopTime = m_watch.ElapsedMilliseconds;
            Debug.Log("CallAgain - Execution Time: " + (lStopTime - lStartTime) + "ms");
#endif
        }

        public void AddOutgoingOffersCheckExisting(Dictionary<TransferReason, List<TransferOffer>> issues)
        {
            if (issues != null)
            {
#if DEBUG
                string sMessage = "";
#endif
                foreach (KeyValuePair<TransferReason, List<TransferOffer>> issue in issues)
                {
                    List<TransferOffer> offers = new List<TransferOffer>(issue.Value);
                    TransferReason material = issue.Key;
                    if (offers != null)
                    {
                        // Now check the transfer offers arent already in Transfer Manager before sending offers.
                        m_checkOffers.RemoveExisitingOutgoingOffers(issue.Key, ref offers);

                        // Now add them to TransferManager
                        foreach (TransferOffer offer in offers)
                        {
#if DEBUG
                            sMessage += $"\r\n{issue.Key} - {CallAgainUtils.DebugOffer(offer)}";
#endif
                            // Use AddAction so its thread safe
                            Singleton<SimulationManager>.instance.AddAction(() =>
                                {
                                    Singleton<TransferManager>.instance.AddOutgoingOffer(material, offer);
                                }
                            );
                            CallAgainStats.AddCall(issue.Key);
                        } 
                    }
                }
#if DEBUG
                if (sMessage.Length > 0)
                {
                    Debug.Log("CALL AGAIN - Adding transfer offers for: " + sMessage);
                }
#endif
            }
        }

        public void AddIncomingOffersCheckExisting(Dictionary<TransferReason, List<TransferOffer>> issues)
        {
            if (issues != null)
            {
#if DEBUG
                string sMessage = "";
#endif
                foreach (KeyValuePair<TransferReason, List<TransferOffer>> issue in issues)
                {
                    List<TransferOffer> offers = issue.Value;
                    if (offers != null)
                    {
                        // Now check the transfer offers arent already in Transfer Manager before sending offers.
                        m_checkOffers.RemoveExisitingIncomingOffers(issue.Key, ref offers);
                        TransferReason material = issue.Key;

                        foreach (TransferOffer offer in offers)
                        {
#if DEBUG
                            sMessage += $"\r\n{issue.Key} - {CallAgainUtils.DebugOffer(offer)}";
#endif
                            // Use AddAction so its thread safe
                            Singleton<SimulationManager>.instance.AddAction(() =>
                            {
                                Singleton<TransferManager>.instance.AddIncomingOffer(material, offer);
                            });
                            CallAgainStats.AddCall(issue.Key);
                        }
                    }
                }
#if DEBUG
                if (sMessage.Length > 0)
                {
                    Debug.Log("CALL AGAIN - Adding transfer offers for: " + sMessage);
                }
#endif
            }
        }

        public List<TransferOffer> CheckHealthTimer(ushort usBuilding, Building building)
        {
            List<TransferOffer> list = new List<TransferOffer>();

            if (building.m_healthProblemTimer >= ModSettings.GetSettings().HealthcareThreshold)
            {
                List<uint> cimSick = CitiesUtils.GetSickCitizens(usBuilding, building);
                if (cimSick.Count > 0)
                {
                    // Only call again if there arent ambulances on the way? and we have passed call again rate
                    int iLastCallTimer = 0;
                    int iRetries = 0;
                    if (m_Healthcalls.ContainsKey(usBuilding))
                    {
                        iLastCallTimer = m_Healthcalls[usBuilding].m_iTimer;
                        iRetries = m_Healthcalls[usBuilding].m_iRetries;
                    }

                    // Call if no ambulances on the way and it has been HealthcareRate since last time we called
                    if ((building.m_healthProblemTimer - iLastCallTimer) > ModSettings.GetSettings().HealthcareRate && 
                        CitiesUtils.GetAmbulancesOnRoute(usBuilding).Count == 0)
                    {
                        // We only add 1 at a time so it is more realistic
                        TransferOffer offer = default(TransferOffer);
                        offer.Citizen = cimSick[0];
                        offer.Active = false;
                        offer.Amount = 1;
                        offer.Priority = building.m_healthProblemTimer * 7 / 128;
                        offer.Position = building.m_position;
                        list.Add(offer);

                        iLastCallTimer = building.m_healthProblemTimer;
                        iRetries++;

                        // Update call data
                        CallAgainData data = new CallAgainData();
                        data.m_iTimer = iLastCallTimer;
                        data.m_iRetries = iRetries;
                        m_Healthcalls[usBuilding] = data;
                    }
                }
            }
            else if (m_Healthcalls.ContainsKey(usBuilding))
            {
                m_Healthcalls.Remove(usBuilding);
            }

            return list;
        }

        public List<TransferOffer> CheckDeathTimer(ushort usBuilding, Building building)
        {
            List <TransferOffer> list = new List<TransferOffer>();

            if (building.m_deathProblemTimer >= ModSettings.GetSettings().DeathcareThreshold)
            {
                List<uint> cimDead = CitiesUtils.GetDeadCitizens(usBuilding, building);
                if (cimDead.Count > 0)
                {
                    // Only call again if there arent ambulances on the way? and we have passed call again rate
                    int iLastCallTimer = 0;
                    int iRetries = 0;
                    if (m_Deathcalls.ContainsKey(usBuilding))
                    {
                        iLastCallTimer = m_Deathcalls[usBuilding].m_iTimer;
                        iRetries = m_Deathcalls[usBuilding].m_iRetries;
                    }

                    // Call if no ambulances on the way and it has been DeathcareRate since last time we called
                    if ((building.m_deathProblemTimer - iLastCallTimer) > ModSettings.GetSettings().DeathcareRate && 
                        CitiesUtils.GetHearsesOnRoute(usBuilding).Count == 0)
                    {
                        // We only add 1 at a time so it is more realistic
                        TransferOffer offer = default(TransferOffer);
                        offer.Citizen = cimDead[0];
                        offer.Active = false;
                        offer.Amount = 1;
                        offer.Priority = building.m_deathProblemTimer * 7 / 128;
                        offer.Position = building.m_position;
                        list.Add(offer);

                        iLastCallTimer = building.m_deathProblemTimer;
                        iRetries++;

                        // Update data
                        CallAgainData data = new CallAgainData();
                        data.m_iTimer = iLastCallTimer;
                        data.m_iRetries = iRetries;
                        m_Deathcalls[usBuilding] = data;
                    }
                }
            }
            else if (m_Deathcalls.ContainsKey(usBuilding))
            {
                m_Deathcalls.Remove(usBuilding);
            }

            return list;
        }

        public List<TransferOffer> CheckGoodsTimer(ushort usBuilding, Building building)
        {
            List<TransferOffer> list = new List<TransferOffer>();

            if (building.m_incomingProblemTimer >= ModSettings.GetSettings().GoodsThreshold)
            {
                
                // Only call again if there arent ambulances on the way? and we have passed call again rate
                int iLastCallTimer = 0;
                int iRetries = 0;
                if (m_Goodscalls.ContainsKey(usBuilding))
                {
                    iLastCallTimer = m_Goodscalls[usBuilding].m_iTimer;
                    iRetries = m_Goodscalls[usBuilding].m_iRetries;
                }

                // Call if no cargo trucks on the way and it has been GoodsRate since last time we called
                if ((building.m_incomingProblemTimer - iLastCallTimer) > ModSettings.GetSettings().GoodsRate &&
                    !CitiesUtils.IsOutsideConnection(building) &&
                    !CitiesUtils.IsPedestrianZone(building) &&
                    CitiesUtils.GetGoodsTrucksOnRoute(usBuilding).Count == 0)
                {
                    // Create incoming offers for each
                    TransferOffer offer = default(TransferOffer);
                    offer.Building = usBuilding;
                    offer.Active = false;
                    offer.Amount = 1;
                    offer.Priority = 7; // Highest
                    offer.Position = building.m_position;
                    list.Add(offer);

                    iLastCallTimer = building.m_incomingProblemTimer;
                    iRetries++;

                    CallAgainData data = new CallAgainData();
                    data.m_iTimer = iLastCallTimer;
                    data.m_iRetries = iRetries;
                    m_Goodscalls[usBuilding] = data;
                }
            }
            else if (m_Goodscalls.ContainsKey(usBuilding))
            {
                m_Goodscalls.Remove(usBuilding);
            }

            return list;
        }

        public List<TransferOffer> CheckGarbage(ushort usBuilding, Building building, Stopwatch watch)
        {
            List<TransferOffer> list = new List<TransferOffer>();

            if (building.m_garbageBuffer >= ModSettings.GetSettings().GarbageThreshold)
            {

                // Only call again if there arent ambulances on the way? and we have passed call again rate
                int iLastCallTimer = 0;
                int iRetries = 0;
                if (m_Garbagecalls.ContainsKey(usBuilding))
                {
                    iLastCallTimer = m_Garbagecalls[usBuilding].m_iTimer;
                    iRetries = m_Garbagecalls[usBuilding].m_iRetries;
                }

                // Call if no garbage trucks on the way and it has been GoodsRate since last time we called
                if ((watch.ElapsedMilliseconds - iLastCallTimer) > ModSettings.GetSettings().GarbageRate * 1000 &&
                    !CitiesUtils.IsPedestrianZone(building) &&
                    CitiesUtils.GetGarbageTrucksOnRoute(usBuilding).Count == 0)
                {
                    // Create outgoing offers for building
                    TransferOffer offer = default(TransferOffer);
                    offer.Building = usBuilding;
                    offer.Active = false;
                    offer.Amount = 1;
                    offer.Priority = building.m_garbageBuffer / 1000;
                    offer.Position = building.m_position;
                    list.Add(offer);

                    CallAgainData data = new CallAgainData();
                    data.m_iTimer = (int) watch.ElapsedMilliseconds;
                    data.m_iRetries = iRetries;
                    m_Garbagecalls[usBuilding] = data;
                }
            }
            else if (m_Garbagecalls.ContainsKey(usBuilding))
            {
                m_Garbagecalls.Remove(usBuilding);
            }

            return list;
        }
    }
}