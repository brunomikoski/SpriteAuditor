using System;

namespace BrunoMikoski.SpriteAuditor
{
    [Flags]
    public enum ResultsFilter
    {
        UsedSmallerThanSpriteSize = 1 << 0,
        UsedBiggerThanSpriteSize = 1 << 1,
        UsedOnlyOnOneScenes = 1 << 2,
        UnableToDetectAllSizes = 1 << 3,
        SingleSprites = 1 << 4,
        MultipleSprites = 1 << 5,
        InsideAtlasSprites = 1 << 6,
        InsideScaledAtlasVariant = 1 << 7
    }
}
