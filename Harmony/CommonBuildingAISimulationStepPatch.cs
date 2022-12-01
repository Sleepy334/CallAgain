using CallAgain.Settings;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using ICities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using static TransferManager;

namespace CallAgain.Patch
{
    [HarmonyPatch]
    public static class CommonBuildingAISimulationStepPatch
    {
        [HarmonyPatch(typeof(CommonBuildingAI), "SimulationStep", new Type[] { typeof(ushort), typeof(Building), typeof(Building.Frame) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref })]
        [HarmonyPostfix]
        public static void SimulationStepPostFix(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
        {
            if (ModSettings.GetSettings().CallAgainEnabled)
            {
                // We use a randomizer so we don't check for issues every loop as it would kill performance.
                Randomizer randomizer = Singleton<SimulationManager>.instance.m_randomizer;

                // Call backs
                CheckDeathTimer(buildingID, buildingData, randomizer);
                CheckGarbage(buildingID, buildingData, randomizer);
                CheckGoodsTimer(buildingID, buildingData, randomizer);
            }
        }

        private static void CheckDeathTimer(ushort usBuilding, Building building, Randomizer randomizer)
        {
            // Dead citizens
            if (randomizer.Int32(5U) == 0 &&
                building.m_deathProblemTimer >= ModSettings.GetSettings().DeathcareThreshold &&
                Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.HealthCare))
            {
                List<uint> cimDead = CitiesUtils.GetDeadCitizens(usBuilding, building);
                if (cimDead.Count > 0)
                {
                    // Call if no hearses on the way
                    int count = 0;
                    int cargo = 0;
                    int capacity = 0;
                    int outside = 0;
                    CitiesUtils.CalculateGuestVehicles(usBuilding, ref building, TransferReason.Dead, ref count, ref cargo, ref capacity, ref outside);

                    if (count == 0)
                    {
                        // Select only 1 random dead citizen to request at a time so it is more realistic.
                        int iCitizen = randomizer.Int32((uint)cimDead.Count);
                        uint citizenId = cimDead[iCitizen];

                        TransferOffer offer = default(TransferOffer);
                        offer.Citizen = citizenId;
                        offer.Active = false;
                        offer.Amount = 1;
                        offer.Priority = building.m_deathProblemTimer * 7 / 128;
                        offer.Position = building.m_position;
#if DEBUG
                        Debug.Log($"Adding Dead offer for building: {usBuilding}");
#endif
                        // Remove exisiting offer if any
                        Singleton<TransferManager>.instance.RemoveOutgoingOffer(TransferReason.Dead, offer);

                        // Add offer
                        Singleton<TransferManager>.instance.AddOutgoingOffer(TransferReason.Dead, offer);

                        // Update stats
                        CallAgainStats.AddCall(TransferReason.Dead);
                    }
                }
            }
        }

        public static void CheckGarbage(ushort usBuilding, Building building, Randomizer randomizer)
        {
            if (randomizer.Int32(5U) == 0 &&
                building.m_garbageBuffer >= ModSettings.GetSettings().GarbageThreshold &&
                !CitiesUtils.IsPedestrianZone(building) &&
                Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.Garbage))
            {
                // Call if no garbage trucks on the way
                int count = 0;
                int cargo = 0;
                int capacity = 0;
                int outside = 0;
                CitiesUtils.CalculateGuestVehicles(usBuilding, ref building, TransferReason.Garbage, ref count, ref cargo, ref capacity, ref outside);

                if (count == 0)
                {
                    // Create outgoing offers for building
                    TransferOffer offer = default(TransferOffer);
                    offer.Building = usBuilding;
                    offer.Active = false;
                    offer.Amount = 1;
                    offer.Priority = building.m_garbageBuffer / 1000;
                    offer.Position = building.m_position;
#if DEBUG
                    Debug.Log($"Adding Garbage offer for building: {usBuilding}");
#endif
                    // Remove exisiting offer if any
                    Singleton<TransferManager>.instance.RemoveOutgoingOffer(TransferReason.Garbage, offer);

                    // Add offer
                    Singleton<TransferManager>.instance.AddOutgoingOffer(TransferReason.Garbage, offer);

                    // Update stats
                    CallAgainStats.AddCall(TransferReason.Garbage);
                }
            }
        }

        public static void CheckGoodsTimer(ushort usBuilding, Building building, Randomizer randomizer)
        {
            if (randomizer.Int32(5U) == 0 && 
                building.m_incomingProblemTimer >= ModSettings.GetSettings().GoodsThreshold &&
                !CitiesUtils.IsOutsideConnection(building) &&
                !CitiesUtils.IsPedestrianZone(building))
            {
                // Call if no cargo trucks on the way
                int count = 0;
                int cargo = 0;
                int capacity = 0;
                int outside = 0;
                CitiesUtils.CalculateGuestVehicles(usBuilding, ref building, TransferReason.Goods, ref count, ref cargo, ref capacity, ref outside);

                if (count == 0)
                {
                    // Create incoming offer for each
                    TransferOffer offer = default(TransferOffer);
                    offer.Building = usBuilding;
                    offer.Active = false;
                    offer.Amount = 1;
                    offer.Priority = 7; // Highest
                    offer.Position = building.m_position;
#if DEBUG
                    Debug.Log($"Adding Goods offer for building: {usBuilding}");
#endif
                    // Remove exisiting offer if any
                    Singleton<TransferManager>.instance.RemoveIncomingOffer(TransferReason.Goods, offer);

                    // Add offer
                    Singleton<TransferManager>.instance.AddIncomingOffer(TransferReason.Goods, offer);

                    // Update stats
                    CallAgainStats.AddCall(TransferReason.Goods);
                }
            }
        }
    }
}
