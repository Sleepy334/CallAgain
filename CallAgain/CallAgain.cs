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
        private Dictionary<ushort, CallAgainData> m_Healthcalls = new Dictionary<ushort, CallAgainData>();
        private Dictionary<ushort, CallAgainData> m_Deathcalls = new Dictionary<ushort, CallAgainData>();
        private Dictionary<ushort, CallAgainData> m_Goodscalls = new Dictionary<ushort, CallAgainData>();
        private Dictionary<ushort, CallAgainData> m_Garbagecalls = new Dictionary<ushort, CallAgainData>();

        public CallAgain()
        {

        }

        public void Update(Stopwatch watch)
        {
#if DEBUG
            long lStartTime = watch.ElapsedMilliseconds;
#endif

            Dictionary<TransferReason, List<TransferOffer>> issueOutgoingList = new Dictionary<TransferReason, List<TransferOffer>>();
            issueOutgoingList[TransferReason.Sick] = new List<TransferOffer>();
            issueOutgoingList[TransferReason.Dead] = new List<TransferOffer>();
            issueOutgoingList[TransferReason.Garbage] = new List<TransferOffer>();

            Dictionary<TransferReason, List<TransferOffer>> issueIncomingList = new Dictionary<TransferReason, List<TransferOffer>>();
            issueIncomingList[TransferReason.Goods] = new List<TransferOffer>();


            bool bDespawnReturningCargoTrucks = ModSettings.GetSettings().DespawnReturningCargoTrucks;

            for (int i = 0; i < BuildingManager.instance.m_buildings.m_buffer.Length; i++)
            {
                Building building = BuildingManager.instance.m_buildings.m_buffer[i];

                // Outgoing
                issueOutgoingList[TransferReason.Sick].AddRange(CheckHealthTimer((ushort)i, building));
                issueOutgoingList[TransferReason.Dead].AddRange(CheckDeathTimer((ushort)i, building));
                issueOutgoingList[TransferReason.Garbage].AddRange(CheckGarbage((ushort)i, building, watch));

                // Incoming
                issueIncomingList[TransferReason.Goods].AddRange(CheckGoodsTimer((ushort)i, building));

                if (bDespawnReturningCargoTrucks)
                {
                    DespawnReturningCargoTrucks((ushort)i, building);
                }
            }

            AddOutgoingOffersCheckExisting(issueOutgoingList);
            AddIncomingOffersCheckExisting(issueIncomingList);
#if DEBUG
            long lStopTime = watch.ElapsedMilliseconds;
            Debug.Log("CallAgain - Execution Time: " + (lStopTime - lStartTime) + "ms");
#endif
        }

        public static void AddOutgoingOffersCheckExisting(Dictionary<TransferReason, List<TransferOffer>> issues)
        {
            // Now check the transfer offers arent already in Transfer Manager before sending offers.
#if DEBUG
            string sMessage = "";
#endif
            foreach (KeyValuePair<TransferReason, List<TransferOffer>> issue in issues)
            {
                
                List<TransferOffer> offers = CallAgainUtils.RemoveExisitingOutgoingOffers(issue.Key, issue.Value);
                foreach (TransferOffer offer in offers)
                {
#if DEBUG
                    sMessage += $"\r\n{issue.Key} - {CallAgainUtils.DebugOffer(offer)}";
#endif
                    TransferManager.instance.AddOutgoingOffer(issue.Key, offer);
                    CallAgainStats.s_CallbackStats[issue.Key]++;
                }

            }
#if DEBUG
            if (sMessage.Length > 0)
            {
                Debug.Log("CALL AGAIN - Adding transfer offers for: " + sMessage);
            }
#endif
        }

        public static void AddIncomingOffersCheckExisting(Dictionary<TransferReason, List<TransferOffer>> issues)
        {
            // Now check the transfer offers arent already in Transfer Manager before sending offers.
#if DEBUG
            string sMessage = "";
#endif
            foreach (KeyValuePair<TransferReason, List<TransferOffer>> issue in issues)
            {

                List<TransferOffer> offers = CallAgainUtils.RemoveExisitingIncomingOffers(issue.Key, issue.Value);
                foreach (TransferOffer offer in offers)
                {
#if DEBUG
                    sMessage += $"\r\n{issue.Key} - {CallAgainUtils.DebugOffer(offer)}";
#endif
                    TransferManager.instance.AddIncomingOffer(issue.Key, offer);
                    CallAgainStats.s_CallbackStats[issue.Key]++;
                }

            }
#if DEBUG
            if (sMessage.Length > 0)
            {
                Debug.Log("CALL AGAIN - Adding transfer offers for: " + sMessage);
            }
#endif
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
                    if ((building.m_healthProblemTimer - iLastCallTimer) > ModSettings.GetSettings().HealthcareRate && CitiesUtils.GetAmbulancesOnRoute(usBuilding).Count == 0)
                    {
                        // Create outgoing offers for each
                        foreach (uint cim in cimSick)
                        {
                            TransferOffer offer = default(TransferOffer);
                            offer.Citizen = cim;
                            offer.Active = false;
                            offer.Amount = 1;
                            offer.Priority = 7; // Highest
                            offer.Position = building.m_position;
                            list.Add(offer);
                        }

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

        public static int GetDeadPriority(int iTimer)
        {
            if (iTimer >= 128)
            {
                return 7;
            } 
            else if (iTimer >= 110)
            {
                return 6;
            }
            else if (iTimer >= 91)
            {
                return 5;
            }
            else if (iTimer >= 74)
            {
                return 4;
            }
            else if (iTimer >= 55)
            {
                return 3;
            }
            else if (iTimer >= 37)
            {
                return 2;
            }
            else if (iTimer >= 19)
            {
                return 1;
            }
            return 0;
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
                    if ((building.m_deathProblemTimer - iLastCallTimer) > ModSettings.GetSettings().DeathcareRate && CitiesUtils.GetHearsesOnRoute(usBuilding).Count == 0)
                    {
                        // Create outgoing offers for each
                        foreach (uint cim in cimDead)
                        {
                            TransferOffer offer = default(TransferOffer);
                            offer.Citizen = cim;
                            offer.Active = false;
                            offer.Amount = 1;
                            offer.Priority = GetDeadPriority(building.m_deathProblemTimer); // Highest
                            offer.Position = building.m_position;
                            list.Add(offer);
                        }

                        iLastCallTimer = building.m_healthProblemTimer;
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

        public static int GetGarbagePriority(int iGarbageBuffer)
        {
            if (iGarbageBuffer >= 7000)
            {
                return 7;
            }
            else if (iGarbageBuffer >= 6000)
            {
                return 6;
            }
            else if (iGarbageBuffer >= 5000)
            {
                return 5;
            }
            else if (iGarbageBuffer >= 4000)
            {
                return 4;
            }
            else if (iGarbageBuffer >= 3000)
            {
                return 3;
            }
            else if (iGarbageBuffer >= 2000)
            {
                return 2;
            }
            else if (iGarbageBuffer >= 1000)
            {
                return 1;
            }
            return 0;
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
                    CitiesUtils.GetGarbageTrucksOnRoute(usBuilding).Count == 0)
                {
                    // Create outgoing offers for building
                    TransferOffer offer = default(TransferOffer);
                    offer.Building = usBuilding;
                    offer.Active = false;
                    offer.Amount = 1;
                    offer.Priority = GetGarbagePriority(building.m_garbageBuffer); // Highest
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

        private void DespawnReturningCargoTrucks(ushort buildingId, Building building)
        {
            if (building.Info?.m_buildingAI is CargoStationAI)
            {
                // Build list of vehicles to despawn
                List<ushort> vehiclesToDespawn = new List<ushort>();
                List<ushort> vehicles = CitiesUtils.GetOwnVehiclesForBuilding(buildingId);
                foreach (ushort vehicleId in vehicles)
                {
                    Vehicle vehicle = VehicleManager.instance.m_vehicles.m_buffer[vehicleId];
                    if ((vehicle.m_flags & Vehicle.Flags.GoingBack) == Vehicle.Flags.GoingBack &&
                        (vehicle.Info != null && (vehicle.Info.m_vehicleAI is CargoTruckAI || vehicle.Info.m_vehicleAI is PostVanAI)))
                    {
                        vehiclesToDespawn.Add(vehicleId);
                    }
                }

                if (vehiclesToDespawn.Count > 0)
                {
                    // Now despawn them
                    var manager = Singleton<VehicleManager>.instance;
                    foreach (ushort vehicleId in vehiclesToDespawn)
                    {
                        try
                        {
                            // Try direct call.
                            manager.ReleaseVehicle(vehicleId);
                        }
                        catch (Exception ex)
                        {
                            // If we fail (because it is a target vehicle or similar), try adding using AddAction to despawn it at a later date.
                            Debug.LogError("Calling AddAction " + vehicleId + " Error:" + ex.Message);

                            // Add action so it is thread safe
                            Singleton<SimulationManager>.instance.AddAction(() => manager.ReleaseVehicle(vehicleId));
                        }
                    }
                }
            } 
        }
    }
}