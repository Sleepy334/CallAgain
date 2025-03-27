using CallAgain.Settings;
using ColossalFramework;
using HarmonyLib;
using System;
using UnityEngine;

namespace CallAgain.Patch
{
    [HarmonyPatch(typeof(CargoTruckAI), "SimulationStep", 
        new Type[] { typeof(ushort), typeof(Vehicle), typeof(Vector3)}, 
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
    public static class CargoTruckAISimulationStepPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
        {
            if (ModSettings.GetSettings().DespawnReturningCargoTrucks)
            {
                if ((data.m_flags & Vehicle.Flags.GoingBack) != 0 && data.m_sourceBuilding != 0)
                {
                    Building building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_sourceBuilding];
                    if (building.m_flags != 0 && building.Info != null && building.Info.GetAI() is CargoStationAI)
                    {
                        data.Unspawn(vehicleID);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PostVanAI), "SimulationStep", 
        new Type[] { typeof(ushort), typeof(Vehicle), typeof(Vehicle.Frame), typeof(ushort), typeof(Vehicle), typeof(int) }, 
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
    public static class PostVanAISimulationStepPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
        {
            if (ModSettings.GetSettings().DespawnReturningCargoTrucks)
            {
                if ((vehicleData.m_flags & Vehicle.Flags.GoingBack) != 0 && vehicleData.m_sourceBuilding != 0)
                {
                    Building building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[vehicleData.m_sourceBuilding];
                    if (building.m_flags != 0 && building.Info != null && building.Info.GetAI() is CargoStationAI)
                    {
                        vehicleData.Unspawn(vehicleID);
                    }
                }
            }
        }
    }
}
