using System;

namespace BrunoMikoski.SpriteAuditor
{
    [Flags]
    public enum ResultsFilter
    {
        SizeWarnings = 1 << 0,
        UsedOnlyOnOneScenes = 1 << 1,
        UnableToDetectAllSizes = 1 << 2,
        SingleSprites = 1 << 3,
        MultipleSprites = 1 << 4,
        InsideAtlasSprites = 1 << 5,
    }
}
