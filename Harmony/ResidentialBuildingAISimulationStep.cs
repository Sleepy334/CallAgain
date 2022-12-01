using CallAgain.Settings;
using ColossalFramework;
using HarmonyLib;
using SleepyCommon;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using static TransferManager;

namespace CallAgain.Patch
{
    [HarmonyPatch]
    public static class ResidentialBuildingAISimulationStepPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ResidentialBuildingAI), "SimulationStepActive")]
        public static void SimulationStepActive(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
        {
            if (ModSettings.GetSettings().CallAgainEnabled)
            {
                if (buildingData.m_healthProblemTimer > ModSettings.GetSettings().HealthcareThreshold &&
                    Singleton<SimulationManager>.instance.m_randomizer.Int32(5U) == 0 &&
                    Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.HealthCare))
                {
                    List<uint> cimSick = CitiesUtils.GetCitizens(buildingID, buildingData, Citizen.Flags.Sick);
                    AddSickOffers(buildingID, ref buildingData, cimSick);
                }
            }
        }

        public static void AddSickOffers(ushort buildingID, ref Building buildingData, List<uint> sickCitizens)
        {
            bool bNaturalDisasters = DependencyUtilities.IsNaturalDisastersDLC();
            int sickCount = sickCitizens.Count;
            int count = 0;
            int cargo = 0;
            int capacity = 0;
            int outside = 0;

            // Ambulances
            CitiesUtils.CalculateGuestVehicles(buildingID, ref buildingData, TransferReason.Sick, ref count, ref cargo, ref capacity, ref outside);
            sickCount -= capacity;

            // Medical helicopters
            if (bNaturalDisasters)
            {
                CitiesUtils.CalculateGuestVehicles(buildingID, ref buildingData, TransferReason.Sick2, ref count, ref cargo, ref capacity, ref outside);
                sickCount -= capacity;
            }

            if (sickCount > 0)
            {
                // Select only 1 random sick citizen to request at a time so it is more realistic.
                // Otherwise we end up with dozens of ambulances showing up at the same time which looks crap.
                int iCitizen = Singleton<SimulationManager>.instance.m_randomizer.Int32((uint)sickCitizens.Count);
                uint citizenId = sickCitizens[iCitizen];

                TransferOffer offer = default(TransferOffer);
                offer.Priority = buildingData.m_healthProblemTimer * 7 / 128;
                offer.Citizen = citizenId;
                offer.Position = buildingData.m_position;
                offer.Amount = 1;

                DistrictManager instance2 = Singleton<DistrictManager>.instance;
                byte district = instance2.GetDistrict(buildingData.m_position);
                DistrictPolicies.Services servicePolicies = instance2.m_districts.m_buffer[district].m_servicePolicies;

                TransferReason material;

                // Request helicopter or ambulance
                if (bNaturalDisasters && (servicePolicies & DistrictPolicies.Services.HelicopterPriority) != 0)
                {
                    instance2.m_districts.m_buffer[district].m_servicePoliciesEffect |= DistrictPolicies.Services.HelicopterPriority;
                    offer.Active = false;
                    material = TransferReason.Sick2;
                }
                else if ((buildingData.m_flags & Building.Flags.RoadAccessFailed) != 0)
                {
                    // No Road Access - request a helicopter or offer to walk 50/50
                    if (bNaturalDisasters && Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0)
                    {
                        // Request a helicopter
                        offer.Active = false;
                        material = TransferReason.Sick2;
                    }
                    else
                    {
                        // Offer to walk
                        offer.Active = true;
                        material = TransferReason.Sick;
                    }
                }
                else if (bNaturalDisasters && Singleton<SimulationManager>.instance.m_randomizer.Int32(20u) == 0)
                {
                    // Request a helicopter occasionally
                    offer.Active = false;
                    material = TransferReason.Sick2;
                }
                else
                {
                    // 80% of the time we ask for an ambulance as it is more fun than walking to hospital
                    // only occasionally offer walking incase their are no ambulances available
                    offer.Active = (Singleton<SimulationManager>.instance.m_randomizer.Int32(5u) == 0);
                    material = TransferReason.Sick;
                }
#if DEBUG
                Debug.Log($"Adding sick offer for building: {buildingID}");
#endif
                // Remove exisiting offer if any
                Singleton<TransferManager>.instance.RemoveOutgoingOffer(material, offer);

                // Add new offer
                Singleton<TransferManager>.instance.AddOutgoingOffer(material, offer);

                // Update stats
                CallAgainStats.AddCall(TransferReason.Sick);
            }
        }
    }
}
