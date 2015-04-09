using System;
using System.Collections.Generic;
using System.Threading;

using ICities;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using UnityEngine;

namespace EarlyDeath
{
    public class Killer : ThreadingExtensionBase
    {
        private Settings _settings;
        private Helper _helper;

        private SkylinesOverwatch.Data _data;

        private bool _initialized;
        private bool _terminated;

        private Randomizer _randomizer;

        public override void OnCreated(IThreading threading)
        {
            _settings = Settings.Instance;
            _helper = Helper.Instance;

            _initialized = false;
            _terminated = false;

            base.OnCreated(threading);
        }

        public override void OnBeforeSimulationTick()
        {
            try
            {
                if (!SkylinesOverwatch.Helper.Instance.GameLoaded)
                {
                    _initialized = false;
                    return;
                }
            }
            catch (Exception e)
            {
                _helper.Log("[ARIS] Skylines Overwatch not found. Unloading...");
                _terminated = true;
            }

            base.OnBeforeSimulationTick();
        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (_terminated) return;

            try
            {
                if (!SkylinesOverwatch.Helper.Instance.GameLoaded) return;

                if (!_initialized)
                {
                    try
                    {
                        SkylinesOverwatch.Settings.Instance.Enable.HumanMonitor     = true;
                        SkylinesOverwatch.Settings.Instance.Enable.Residents        = true;

                        _data = SkylinesOverwatch.Data.Instance;
                    }
                    catch (Exception e)
                    {
                        _helper.Log("[ARIS] Skylines Overwatch not found. Unloading...");
                        _terminated = true;
                    }

                    _randomizer = Singleton<SimulationManager>.instance.m_randomizer;

                    _initialized = true;

                    float probability = 1;

                    foreach (int i in _settings.DeathRate)
                        probability = probability * (1000 - i) / 1000;

                    _helper.Log(String.Format("Initialized with {0:P2} chance of surviving to the end", probability));
                }
                else if (_data.HumansUpdated.Length > 0)
                {
                    CitizenManager instance = Singleton<CitizenManager>.instance;

                    foreach (uint i in _data.HumansUpdated)
                    {
                        Citizen resident = instance.m_citizens.m_buffer[(int)i];

                        if (resident.Dead)
                            continue;

                        if ((resident.m_flags & Citizen.Flags.Created) == Citizen.Flags.None)
                            continue;

                        CitizenInfo info = resident.GetCitizenInfo(i);

                        if (info == null)
                            continue;

                        if (!(info.m_citizenAI is ResidentAI))
                            continue;

                        if (!Kill(resident))
                            continue;

                        resident.Sick = false;
                        resident.Dead = true;
                        resident.SetParkedVehicle(i, 0);

                        ushort home = resident.GetBuildingByLocation();

                        if (home == 0)
                            home = resident.m_homeBuilding;

                        if (home != 0)
                        {
                            DistrictManager dm = Singleton<DistrictManager>.instance;

                            Vector3 position = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)home].m_position;

                            byte district = dm.GetDistrict(position);

                            District[] buffer = dm.m_districts.m_buffer;
                            buffer[(int)district].m_deathData.m_tempCount = buffer[(int)district].m_deathData.m_tempCount + 1;
                        }

                        if (_randomizer.Int32(2) == 0)
                            instance.ReleaseCitizen(i);
                    }
                }
            }
            catch (Exception e)
            {
                string error = "Failed to initialize\r\n";
                error += String.Format("Error: {0}\r\n", e.Message);
                error += "\r\n";
                error += "==== STACK TRACE ====\r\n";
                error += e.StackTrace;

                _helper.Log(error);

                _terminated = true;
            }

            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }

        public override void OnReleased()
        {
            _initialized = false;
            _terminated = false;

            base.OnReleased();
        }

        private bool Kill(Citizen resident)
        {
            int bracket = resident.Age >> 4 & 31;

            /*
             * Handle "super seniors" whose age is not possible in the actual game,
             * but is made like this through slow aging mods. Let the game deal
             * with their death, since those mods are built assuming the game's 
             * kill mechanism. This is also the reason why we did & 31 instead of 
             * & 15 in the previous step.
             */
            if (bracket > 15)
                return false;

            int rate = _settings.DeathRate[bracket];

            rate += resident.BadHealth      * 10;
            rate += resident.NoElectricity  *  5;
            rate += resident.NoSewage       * 10;
            rate += resident.NoWater        * 10;
            rate += resident.Unemployed     *  1;

            if (resident.Education1)    rate -= 5;
            if (resident.Education2)    rate -= 3;
            if (resident.Education3)    rate -= 2;
            if (resident.Sick)          rate += 5;

            if (resident.WealthLevel == Citizen.Wealth.Medium)   rate -= 10;
            if (resident.WealthLevel == Citizen.Wealth.High)     rate -= 20;

            rate += _randomizer.Int32(-25, 25); // Luck

            if (rate <= 0)
                return false;

            return _randomizer.Int32((uint)(1000 / rate)) == 0;
        }
    }
}

