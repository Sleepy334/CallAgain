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

        private Dictionary<TransferReason, FastList<TransferOffer>> m_issueOutgoingList;
        private Dictionary<TransferReason, FastList<TransferOffer>> m_issueIncomingList;

        TransferManagerCheckOffers m_checkOffers;
        Stopwatch m_watch;

        public CallAgain()
        {
            m_Healthcalls = new Dictionary<ushort, CallAgainData>();
            m_Deathcalls = new Dictionary<ushort, CallAgainData>();
            m_Goodscalls = new Dictionary<ushort, CallAgainData>();
            m_Garbagecalls = new Dictionary<ushort, CallAgainData>();
            m_checkOffers = new TransferManagerCheckOffers();

            // Create arrays to store offers
            m_issueOutgoingList = new Dictionary<TransferReason, FastList<TransferOffer>>();
            m_issueIncomingList = new Dictionary<TransferReason, FastList<TransferOffer>>();
            m_issueOutgoingList[TransferReason.Sick] = new FastList<TransferOffer>();
            m_issueOutgoingList[TransferReason.Dead] = new FastList<TransferOffer>();
            m_issueOutgoingList[TransferReason.Garbage] = new FastList<TransferOffer>();
            m_issueIncomingList[TransferReason.Goods] = new FastList<TransferOffer>();

            m_watch = Stopwatch.StartNew();
        }

        public void Update()
        {
#if DEBUG
            long lStartTime = m_watch.ElapsedMilliseconds;
#endif
            // Clear arrays for next update.
            m_issueOutgoingList[TransferReason.Sick].Clear();
            m_issueOutgoingList[TransferReason.Dead].Clear();
            m_issueOutgoingList[TransferReason.Garbage].Clear();
            m_issueIncomingList[TransferReason.Goods].Clear();

            for (int i = 0; i < BuildingManager.instance.m_buildings.m_buffer.Length; i++)
            {
                Building building = BuildingManager.instance.m_buildings.m_buffer[i];

                // Outgoing
                FastList<TransferOffer> sickList = m_issueOutgoingList[TransferReason.Sick];
                CheckHealthTimer((ushort)i, building, ref sickList);

                FastList<TransferOffer> deathList = m_issueOutgoingList[TransferReason.Dead];
                CheckDeathTimer((ushort)i, building, ref deathList);

                FastList<TransferOffer> garbageList = m_issueOutgoingList[TransferReason.Garbage];
                CheckGarbage((ushort)i, building, m_watch, ref garbageList);

                // Incoming
                FastList<TransferOffer> goodsList = m_issueIncomingList[TransferReason.Goods];
                CheckGoodsTimer((ushort)i, building, ref goodsList);
            }

            AddOutgoingOffersCheckExisting(m_issueOutgoingList);
            AddIncomingOffersCheckExisting(m_issueIncomingList);
#if DEBUG
            long lStopTime = m_watch.ElapsedMilliseconds;
            Debug.Log("CallAgain - Execution Time: " + (lStopTime - lStartTime) + "ms");
#endif
        }

        public void AddOutgoingOffersCheckExisting(Dictionary<TransferReason, FastList<TransferOffer>> issues)
        {
            if (issues.Count > 0)
            {
#if DEBUG
                string sMessage = "";
#endif
                foreach (KeyValuePair<TransferReason, FastList<TransferOffer>> issue in issues)
                {
                    FastList<TransferOffer> offers = issue.Value;
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

        public void AddIncomingOffersCheckExisting(Dictionary<TransferReason, FastList<TransferOffer>> issues)
        {
            if (issues.Count > 0)
            {
#if DEBUG
                string sMessage = "";
#endif
                foreach (KeyValuePair<TransferReason, FastList<TransferOffer>> issue in issues)
                {
                    FastList<TransferOffer> offers = issue.Value;
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

        public void CheckHealthTimer(ushort usBuilding, Building building, ref FastList<TransferOffer> list)
        {
            if (building.m_healthProblemTimer >= ModSettings.GetSettings().HealthcareThreshold && 
                Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.HealthCare))
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
        }

        public void CheckDeathTimer(ushort usBuilding, Building building, ref FastList<TransferOffer> list)
        {
            if (building.m_deathProblemTimer >= ModSettings.GetSettings().DeathcareThreshold &&
                Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.HealthCare))
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
        }

        public void CheckGoodsTimer(ushort usBuilding, Building building, ref FastList<TransferOffer> list)
        {
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
        }

        public void CheckGarbage(ushort usBuilding, Building building, Stopwatch watch, ref FastList<TransferOffer> list)
        {
            if (building.m_garbageBuffer >= ModSettings.GetSettings().GarbageThreshold &&
                Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.Garbage))
            {
                // Only call again if there arent ambulances on the way? and we have passed call again rate
                int iLastCallTimer = 0;
                int iRetries = 0;
                if (m_Garbagecalls.ContainsKey(usBuilding))
                {
                    iLastCallTimer = m_Garbagecalls[usBuilding].m_iTimer;
                    iRetries = m_Garbagecalls[usBuilding].m_iRetries;
                }

                // Call if no garbage trucks on the way and it has been GarbageRate since last time we called
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
        }
    }
}