using System;
using System.Collections.Generic;
using System.Threading;

using ICities;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using UnityEngine;

namespace EarlyDeath
{
    public class Killer : ThreadingExtensionBase
    {
        private Settings _settings;
        private Helper _helper;

        private bool _initialized;
        private bool _terminated;

        private Randomizer _randomizer;

        protected bool IsOverwatched()
        {
            #if DEBUG

            return true;

            #else

            foreach (var plugin in PluginManager.instance.GetPluginsInfo())
            {
                if (plugin.publishedFileID.AsUInt64 == 583538182)
                    return true;
            }

            return false;

            #endif
        }

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
            if (_terminated) return;

            if (!_helper.GameLoaded)
            {
                _initialized = false;
                return;
            }

            base.OnBeforeSimulationTick();
        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (_terminated) return;

            if (!_helper.GameLoaded) return;

            try
            {
                if (!_initialized)
                {
                    if (!IsOverwatched())
                    {
                        _helper.NotifyPlayer("Skylines Overwatch not found. Terminating...");
                        _terminated = true;

                        return;
                    }

                    SkylinesOverwatch.Settings.Instance.Enable.HumanMonitor = true;

                    _randomizer = Singleton<SimulationManager>.instance.m_randomizer;

                    _initialized = true;

                    float probability = 1;

                    foreach (int i in _settings.DeathRate)
                        probability = probability * (1000 - i) / 1000;

                    _helper.NotifyPlayer(String.Format("Initialized with {0:P2} chance of surviving to the end", probability));
                }
                else
                {
                    ProcessHumansUpdated();
                }
            }
            catch (Exception e)
            {
                string error = String.Format("Failed to {0}\r\n", !_initialized ? "initialize" : "update");
                error += String.Format("Error: {0}\r\n", e.Message);
                error += "\r\n";
                error += "==== STACK TRACE ====\r\n";
                error += e.StackTrace;

                _helper.Log(error);

                if (!_initialized)
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

        private void ProcessHumansUpdated()
        {
            SkylinesOverwatch.Data data = SkylinesOverwatch.Data.Instance;
            uint[] humans = data.HumansUpdated;

            if (humans.Length == 0) return;

            CitizenManager instance = Singleton<CitizenManager>.instance;

            foreach (uint i in humans)
            {
                if (!data.IsResident(i))
                    continue;

                Citizen[] residents = instance.m_citizens.m_buffer;
                Citizen resident = residents[(int)i];

                if (resident.Dead)
                    continue;

                if ((resident.m_flags & Citizen.Flags.Created) == Citizen.Flags.None)
                    continue;

                if ((resident.m_flags & Citizen.Flags.DummyTraffic) != Citizen.Flags.None)
                    continue;

                if (!Kill(resident))
                    continue;

                residents[(int)i].Sick = false;
                residents[(int)i].Dead = true;
                residents[(int)i].SetParkedVehicle(i, 0);

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

                SkylinesOverwatch.Helper.Instance.RequestHumanRemoval(i);
            }
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

