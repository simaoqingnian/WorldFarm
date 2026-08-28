using UnityEngine;

namespace WorldFarm
{
    [CreateAssetMenu(menuName = "WorldFarm/Biome Definition", fileName = "BiomeDefinition")]
    public sealed class BiomeDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string countryId;
        [SerializeField] private int baseSlotCount = 2;
        [SerializeField] private int maxSlotCount = 6;
        [SerializeField] private BiomeTag[] tags;
        [SerializeField] private EnvironmentAttributeValue[] attributes;
        [SerializeField] private float mutationBonus;

        public string Id { get { return id; } }
        public string DisplayName { get { return displayName; } }
        public string CountryId { get { return countryId; } }
        public int BaseSlotCount { get { return baseSlotCount; } }
        public int MaxSlotCount { get { return maxSlotCount; } }
        public BiomeTag[] Tags { get { return tags; } }
        public float MutationBonus { get { return mutationBonus; } }

        public float GetAttribute(EnvironmentAttributeType type, float fallback = 50f)
        {
            if (attributes == null)
            {
                return fallback;
            }

            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i].type == type)
                {
                    return attributes[i].value;
                }
            }

            return fallback;
        }

        public bool HasTag(BiomeTag tag)
        {
            if (tags == null)
            {
                return false;
            }

            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == tag)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
