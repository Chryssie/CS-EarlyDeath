using System;
using System.Collections.Generic;

namespace EarlyDeath
{
    public sealed class Settings
    {
        private Settings()
        {
            Tag = "[ARIS] Early Death";

            DeathRate = new Dictionary<int, int>();

            // Probability given in permille (per 1000)
            DeathRate.Add( 15,   5);
            DeathRate.Add( 30,  10); // teen
            DeathRate.Add( 45,  10);
            DeathRate.Add( 60,  30); // young
            DeathRate.Add( 75,  20);
            DeathRate.Add( 90,  15);
            DeathRate.Add(105,  10); // adult
            DeathRate.Add(120,   5);
            DeathRate.Add(135,   5);
            DeathRate.Add(150,  10);
            DeathRate.Add(165,  15);
            DeathRate.Add(180,  20);
            DeathRate.Add(195,  30); // senior
            DeathRate.Add(210,  45);
            DeathRate.Add(225,  60);
            DeathRate.Add(240,  80);
            DeathRate.Add(255,   1); // let game death rate take over
        }

        private static readonly Settings _Instance = new Settings();
        public static Settings Instance { get { return _Instance; } }

        public readonly string Tag;
        public readonly Dictionary<int, int> DeathRate;
    }
}