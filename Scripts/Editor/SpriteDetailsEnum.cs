using System;

namespace BrunoMikoski.SpriteAuditor
{
    [Flags]
    public enum SpriteDrawDetails
    {
        None = 0,
        UsageCount = 1 << 0,
        SizeDetails = 1 << 1,
        ReferencesPath = 1 << 2,
        SceneReferences = 1 << 3,
        All = UsageCount | SizeDetails | ReferencesPath | SceneReferences,
    }
}