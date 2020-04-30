using System;

namespace BrunoMikoski.SpriteAuditor
{
    [Flags]
    internal enum SpriteDrawDetails
    {
        None = 1,
        UsageCount = 2,
        SizeDetails = 4,
        ReferencesPath = 8,
        SceneReferences = 16,
        All = UsageCount | SizeDetails | ReferencesPath | SceneReferences,
    }
}
