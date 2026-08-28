using UnityEngine;

namespace WorldFarm
{
    [CreateAssetMenu(menuName = "WorldFarm/Mutation Rule Definition", fileName = "MutationRuleDefinition")]
    public sealed class MutationRuleDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private CropVariantDefinition sourceCrop;
        [SerializeField] private CropVariantDefinition resultCrop;
        [SerializeField] private BiomeTag[] triggerTags;
        [SerializeField] private float requiredAdaptationMax = 0.55f;
        [SerializeField] private float chanceMultiplier = 1f;
        [SerializeField] private int stabilizeSuccessCount = 3;

        public string Id { get { return id; } }
        public CropVariantDefinition SourceCrop { get { return sourceCrop; } }
        public CropVariantDefinition ResultCrop { get { return resultCrop; } }
        public float ChanceMultiplier { get { return chanceMultiplier; } }
        public int StabilizeSuccessCount { get { return stabilizeSuccessCount; } }

        public bool CanTrigger(CropVariantDefinition crop, BiomeDefinition biome, float adaptation)
        {
            if (crop == null || biome == null || sourceCrop == null)
            {
                return false;
            }

            if (!crop.NaturalMutationEnabled || crop != sourceCrop)
            {
                return false;
            }

            if (adaptation > requiredAdaptationMax)
            {
                return false;
            }

            if (triggerTags == null || triggerTags.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < triggerTags.Length; i++)
            {
                if (biome.HasTag(triggerTags[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
