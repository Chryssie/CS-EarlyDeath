using System;
using System.Collections.Generic;

namespace EarlyDeath
{
    public sealed class Settings
    {
        private Settings()
        {
            #if DEBUG

            IsDebugBuild   = true;
            IsPTRBuild     = false;
            IsReleaseBuild = false;

            Flair         += "ARIS LOCAL";

            #elif PTR

            IsDebugBuild   = false;
            IsPTRBuild     = true;
            IsReleaseBuild = false;

            Flair         += "ARIS PTR";

            #else

            IsDebugBuild   = false;
            IsPTRBuild     = false;
            IsReleaseBuild = true;

            Flair         += "ARIS";

            #endif

            Tag = String.Format("[{0}] Early Death", Flair);

            DeathRate = new int[16];

            // Probability given in permille (per 1000)
            DeathRate[ 0] =  5;
            DeathRate[ 1] = 10; // teen
            DeathRate[ 2] = 10;
            DeathRate[ 3] = 30; // young
            DeathRate[ 4] = 20;
            DeathRate[ 5] = 15;
            DeathRate[ 6] = 10; // adult
            DeathRate[ 7] =  5;
            DeathRate[ 8] =  5;
            DeathRate[ 9] = 10;
            DeathRate[10] = 15;
            DeathRate[11] = 20;
            DeathRate[12] = 30; // senior
            DeathRate[13] = 45;
            DeathRate[14] = 60;
            DeathRate[15] = 80;
        }

        private static readonly Settings _Instance = new Settings();
        public static Settings Instance { get { return _Instance; } }

        public readonly string Flair;
        public readonly string Tag;
        public readonly int[] DeathRate;

        public readonly bool IsDebugBuild;
        public readonly bool IsPTRBuild;
        public readonly bool IsReleaseBuild;
    }
}