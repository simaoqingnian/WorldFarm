using UnityEngine;

namespace WorldFarm
{
    [CreateAssetMenu(menuName = "WorldFarm/Crop Variant Definition", fileName = "CropVariantDefinition")]
    public sealed class CropVariantDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string speciesId;
        [SerializeField] private CropCategory category;
        [SerializeField] private int baseYield = 3;
        [SerializeField] private int growthSeconds = 300;
        [SerializeField] private float baseMutationChance = 0.002f;
        [SerializeField] private bool stableMutation;
        [SerializeField] private bool naturalMutationEnabled = true;
        [SerializeField] private string sourceVariantId;
        [SerializeField] private EnvironmentAttributeValue[] idealAttributes;
        [SerializeField] private EnvironmentAttributeWeight[] attributeWeights;
        [SerializeField] private EnvironmentToleranceProfile tolerance = EnvironmentToleranceProfile.Default;
        [SerializeField] private BiomeTagModifier[] traitBonus;
        [SerializeField] private BiomeTagModifier[] traitPenalty;
        [SerializeField] private BiomeTagModifier[] yieldCapByTag;

        public string Id { get { return id; } }
        public string DisplayName { get { return displayName; } }
        public string SpeciesId { get { return speciesId; } }
        public CropCategory Category { get { return category; } }
        public int BaseYield { get { return baseYield; } }
        public int GrowthSeconds { get { return growthSeconds; } }
        public float BaseMutationChance { get { return baseMutationChance; } }
        public bool StableMutation { get { return stableMutation; } }
        public bool NaturalMutationEnabled { get { return naturalMutationEnabled; } }
        public string SourceVariantId { get { return sourceVariantId; } }
        public EnvironmentToleranceProfile Tolerance { get { return tolerance; } }

        public float GetIdealAttribute(EnvironmentAttributeType type, float fallback = 50f)
        {
            return FindAttribute(idealAttributes, type, fallback);
        }

        public float GetAttributeWeight(EnvironmentAttributeType type)
        {
            if (attributeWeights == null)
            {
                return 0f;
            }

            for (int i = 0; i < attributeWeights.Length; i++)
            {
                if (attributeWeights[i].type == type)
                {
                    return attributeWeights[i].weight;
                }
            }

            return 0f;
        }

        public float GetTraitBonus(BiomeDefinition biome)
        {
            return SumMatchingModifiers(traitBonus, biome);
        }

        public float GetTraitPenalty(BiomeDefinition biome)
        {
            return SumMatchingModifiers(traitPenalty, biome);
        }

        public float GetYieldCap(BiomeDefinition biome)
        {
            if (yieldCapByTag == null || biome == null)
            {
                return 1f;
            }

            float cap = 1f;
            for (int i = 0; i < yieldCapByTag.Length; i++)
            {
                if (biome.HasTag(yieldCapByTag[i].tag))
                {
                    cap = Mathf.Min(cap, Mathf.Clamp01(yieldCapByTag[i].value));
                }
            }

            return cap;
        }

        private static float FindAttribute(EnvironmentAttributeValue[] values, EnvironmentAttributeType type, float fallback)
        {
            if (values == null)
            {
                return fallback;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].type == type)
                {
                    return values[i].value;
                }
            }

            return fallback;
        }

        private static float SumMatchingModifiers(BiomeTagModifier[] modifiers, BiomeDefinition biome)
        {
            if (modifiers == null || biome == null)
            {
                return 0f;
            }

            float total = 0f;
            for (int i = 0; i < modifiers.Length; i++)
            {
                if (biome.HasTag(modifiers[i].tag))
                {
                    total += modifiers[i].value;
                }
            }

            return total;
        }
    }
}
