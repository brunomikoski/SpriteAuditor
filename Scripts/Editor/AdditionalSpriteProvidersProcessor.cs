using System;
using System.Collections.Generic;
using System.Linq;
using BrunoMikoski.AtlasAudior.Serialization;
using UnityEditor;
using UnityEditor.Callbacks;

namespace BrunoMikoski.AtlasAudior
{
    public static class AdditionalSpriteProvidersProcessor
    {
        private const string SPRITE_REPORTER_TYPE_KEY = "SPRITE_REPORTERS_TYPE_KEY";

        [DidReloadScripts]
        public static void AfterScriptsReload()
        {
            EditorApplication.delayCall += OnUnityCompilationFinished;
        }

        private static void OnUnityCompilationFinished()
        {
            Type spriteReporterType = typeof(ISpriteReporter);
            List<Type> foundImplementations = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass && spriteReporterType.IsAssignableFrom(x)).ToList();

            if (foundImplementations.Count > 0)
            {
                EditorPrefs.SetString(SPRITE_REPORTER_TYPE_KEY, JsonWrapper.ToJson(foundImplementations));
            }
            else
            {
                EditorPrefs.DeleteKey(SPRITE_REPORTER_TYPE_KEY);
            }
        }
    }
}