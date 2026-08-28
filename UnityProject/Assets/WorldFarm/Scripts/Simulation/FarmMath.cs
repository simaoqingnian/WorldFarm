using UnityEngine;

namespace WorldFarm
{
    public static class FarmMath
    {
        private static readonly EnvironmentAttributeType[] ScoredAttributes =
        {
            EnvironmentAttributeType.Moisture,
            EnvironmentAttributeType.Fertility,
            EnvironmentAttributeType.Temperature,
            EnvironmentAttributeType.Sunlight,
            EnvironmentAttributeType.Slope,
            EnvironmentAttributeType.Salinity
        };

        public static FarmBalanceResult Calculate(BiomeDefinition biome, CropVariantDefinition crop, float researchBonus = 0f)
        {
            float adaptation = CalculateAdaptation(biome, crop);
            float stress = Mathf.Max(0f, 1f - adaptation);
            float yieldMultiplier = Mathf.Clamp(adaptation, 0.12f, 1.35f);
            float yieldCap = crop != null ? crop.GetYieldCap(biome) : 1f;
            yieldMultiplier = Mathf.Min(yieldMultiplier, yieldCap);

            float growthMultiplier = 1f + stress * 0.6f;
            float mutationChance = CalculateMutationChance(biome, crop, stress, researchBonus);
            int baseYield = crop != null ? Mathf.Max(1, crop.BaseYield) : 1;

            return new FarmBalanceResult
            {
                adaptation = adaptation,
                stress = stress,
                yieldMultiplier = yieldMultiplier,
                growthMultiplier = growthMultiplier,
                mutationChance = mutationChance,
                minYield = Mathf.Max(1, Mathf.RoundToInt(baseYield * yieldMultiplier * 0.85f)),
                maxYield = Mathf.Max(1, Mathf.RoundToInt(baseYield * yieldMultiplier * 1.15f))
            };
        }

        public static float CalculateAdaptation(BiomeDefinition biome, CropVariantDefinition crop)
        {
            if (biome == null || crop == null)
            {
                return 0.05f;
            }

            float weightedScore = 0f;
            float totalWeight = 0f;

            for (int i = 0; i < ScoredAttributes.Length; i++)
            {
                EnvironmentAttributeType type = ScoredAttributes[i];
                float weight = crop.GetAttributeWeight(type);
                if (weight <= 0f)
                {
                    continue;
                }

                float plotValue = biome.GetAttribute(type);
                float idealValue = crop.GetIdealAttribute(type);
                float tolerance = GetDirectionalTolerance(crop.Tolerance, type, plotValue, idealValue);
                float distance = Mathf.Abs(plotValue - idealValue) / Mathf.Max(1f, tolerance);
                float score = Mathf.Clamp01(1f - distance);

                weightedScore += score * weight;
                totalWeight += weight;
            }

            float baseAdaptation = totalWeight > 0f ? weightedScore / totalWeight : 0.5f;
            float traitBonus = crop.GetTraitBonus(biome);
            float traitPenalty = crop.GetTraitPenalty(biome);
            return Mathf.Clamp(baseAdaptation + traitBonus - traitPenalty, 0.05f, 1.25f);
        }

        public static float CalculateMutationChance(BiomeDefinition biome, CropVariantDefinition crop, float stress, float researchBonus)
        {
            if (crop == null || !crop.NaturalMutationEnabled)
            {
                return 0f;
            }

            float biomeBonus = biome != null ? biome.MutationBonus : 0f;
            float chance = crop.BaseMutationChance + stress * stress * 0.08f + biomeBonus + researchBonus;
            return Mathf.Clamp(chance, 0f, 0.12f);
        }

        private static float GetDirectionalTolerance(EnvironmentToleranceProfile tolerance, EnvironmentAttributeType type, float plotValue, float idealValue)
        {
            switch (type)
            {
                case EnvironmentAttributeType.Moisture:
                    return plotValue < idealValue ? tolerance.drought : tolerance.waterlog;
                case EnvironmentAttributeType.Fertility:
                    return plotValue < idealValue ? tolerance.lowFertility : tolerance.highFertility;
                case EnvironmentAttributeType.Temperature:
                    return plotValue < idealValue ? tolerance.cold : tolerance.heat;
                case EnvironmentAttributeType.Sunlight:
                    return plotValue < idealValue ? tolerance.lowLight : tolerance.highLight;
                case EnvironmentAttributeType.Slope:
                    return tolerance.slope;
                case EnvironmentAttributeType.Salinity:
                    return tolerance.salinity;
                default:
                    return 35f;
            }
        }
    }
}
