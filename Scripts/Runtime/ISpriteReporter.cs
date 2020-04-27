using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrunoMikoski.SpriteAuditor
{
    public interface ISpriteReporter
    {
        void ReportSprites(ref Dictionary<Sprite, Scene> spriteToScene);
    }
}