using System;

namespace BrunoMikoski.SpriteAuditor
{
    [Flags]
    public enum SpriteUsageFlags
    {
        None = 0,
        UsedBiggerThanSpriteRect = 1 << 0,
        UsedSmallerThanSpriteRect = 1 << 1,
        CantDiscoveryAllUsageSize = 1 << 2, // When for some reason cannot detect the size this image is used
        DefaultUnityAsset = 1 << 3,
        UsedOnDontDestroyOnLoadScene = 1 << 4,
        HasMultipleSpritesRect = 1 << 5,
    }
}
