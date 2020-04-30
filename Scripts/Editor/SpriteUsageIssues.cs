using System;

namespace BrunoMikoski.SpriteAuditor
{
    [Flags]
    public enum SpriteUsageIssues
    {
        UsedBiggerThanSpriteSize = 1,
        UsedSmallerThanSpriteSize = 2,
        CantDiscoveryUsageSize = 4 // When for some reason cannot detect the size this image is used
    }
}
