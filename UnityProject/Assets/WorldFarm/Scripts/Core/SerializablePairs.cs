using System;

namespace WorldFarm
{
    [Serializable]
    public struct EnvironmentAttributeValue
    {
        public EnvironmentAttributeType type;
        public float value;
    }

    [Serializable]
    public struct EnvironmentAttributeWeight
    {
        public EnvironmentAttributeType type;
        public float weight;
    }

    [Serializable]
    public struct BiomeTagModifier
    {
        public BiomeTag tag;
        public float value;
    }
}
