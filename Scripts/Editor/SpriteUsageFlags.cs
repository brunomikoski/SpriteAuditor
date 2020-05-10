using System;

namespace BrunoMikoski.SpriteAuditor
{
    [Flags]
    public enum SpriteUsageFlags
    {
        UsedBiggerThanSpriteRect = 1 << 0,
        UsedSmallerThanSpriteRect = 1 << 1,
        CantDiscoveryAllUsageSize = 1 << 2, // When for some reason cannot detect the size this image is used
        DefaultUnityAsset = 1 << 3,
        UsedOnDontDestroyOrUnknowScene = 1 << 4,
        UsingScaledAtlasSize = 1 << 5
    }
}
