using System;

namespace WorldFarm
{
    [Serializable]
    public struct EnvironmentToleranceProfile
    {
        public float drought;
        public float waterlog;
        public float lowFertility;
        public float highFertility;
        public float cold;
        public float heat;
        public float lowLight;
        public float highLight;
        public float slope;
        public float salinity;

        public static EnvironmentToleranceProfile Default
        {
            get
            {
                return new EnvironmentToleranceProfile
                {
                    drought = 30f,
                    waterlog = 30f,
                    lowFertility = 35f,
                    highFertility = 45f,
                    cold = 30f,
                    heat = 30f,
                    lowLight = 35f,
                    highLight = 45f,
                    slope = 35f,
                    salinity = 20f
                };
            }
        }
    }
}
