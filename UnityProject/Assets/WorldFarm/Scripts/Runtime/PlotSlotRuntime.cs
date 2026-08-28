using System;
using UnityEngine;

namespace WorldFarm
{
    public sealed class PlotSlotRuntime : MonoBehaviour
    {
        [SerializeField] private string slotId;
        [SerializeField] private BiomeDefinition biome;
        [SerializeField] private CropVariantDefinition plantedCrop;
        [SerializeField] private PlotSlotState state = PlotSlotState.Empty;
        [SerializeField] private long plantedAtUnixMillis;
        [SerializeField] private long matureAtUnixMillis;

        public string SlotId { get { return slotId; } }
        public BiomeDefinition Biome { get { return biome; } }
        public CropVariantDefinition PlantedCrop { get { return plantedCrop; } }
        public PlotSlotState State { get { return state; } }
        public long MatureAtUnixMillis { get { return matureAtUnixMillis; } }

        public bool CanPlant
        {
            get { return state == PlotSlotState.Empty && biome != null; }
        }

        public FarmBalanceResult Plant(CropVariantDefinition crop, long nowUnixMillis)
        {
            if (!CanPlant)
            {
                throw new InvalidOperationException("Plot slot is not available for planting.");
            }

            if (crop == null)
            {
                throw new ArgumentNullException("crop");
            }

            FarmBalanceResult balance = FarmMath.Calculate(biome, crop);
            plantedCrop = crop;
            state = PlotSlotState.Growing;
            plantedAtUnixMillis = nowUnixMillis;
            matureAtUnixMillis = nowUnixMillis + Mathf.RoundToInt(crop.GrowthSeconds * balance.growthMultiplier) * 1000L;
            return balance;
        }

        public void Refresh(long nowUnixMillis)
        {
            if (state == PlotSlotState.Growing && nowUnixMillis >= matureAtUnixMillis)
            {
                state = PlotSlotState.Mature;
            }
        }

        public CropVariantDefinition Harvest()
        {
            if (state != PlotSlotState.Mature)
            {
                throw new InvalidOperationException("Crop is not mature.");
            }

            CropVariantDefinition harvested = plantedCrop;
            plantedCrop = null;
            state = PlotSlotState.Empty;
            plantedAtUnixMillis = 0L;
            matureAtUnixMillis = 0L;
            return harvested;
        }
    }
}
