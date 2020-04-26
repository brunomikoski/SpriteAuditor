using System;

namespace BrunoMikoski.AtlasAudior
{
    [Flags]
    internal enum SpriteDetails
    {
        None = 1,
        UsageCount = 2,
        SizeDetails = 4,
        ReferencesPath = 8,
        SceneReferences = 16,
        All = UsageCount | SizeDetails | ReferencesPath | SceneReferences,
    }
}