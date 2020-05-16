using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace BrunoMikoski.SpriteAuditor
{
    public static class SpriteAuditorBatchAction
    {
        private enum State
        {
            None,
            WithSelectedItems,
            MovingToAnotherAtlas
        }
        
        private static bool initialized;
        private static bool moveToAnotherAtlas;
        private static bool removeFromAtlas;
        private static SpriteAtlas targetNewAtlas;

        private static State currrentState = State.None;

        public static void DrawBatchActions()
        {
            UpdateCurrentState();

            if (currrentState == State.None)
                return;


            if (currrentState == State.WithSelectedItems)
            {
                DrawOptions();
            }
            else if (currrentState == State.MovingToAnotherAtlas)
            {
                DrawMoveSelectedObjectsToAnotherAtlas();
            }
            
        }

        private static void DrawOptions()
        {
            int selectedObjectsCount = SpriteAuditorUtility.SelectedObjects.Count;

            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("Batch Actions", EditorStyles.toolbarDropDown);

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button($"Select {selectedObjectsCount} objects",EditorStyles.miniButton) )
                    {
                        UnitySelectSelectedObjects();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                {
                    moveToAnotherAtlas |= GUILayout.Button($"Move {selectedObjectsCount} To Another Atlas",
                        EditorStyles.miniButton);


                    if (GUILayout.Button($"Remove {selectedObjectsCount} from Atlas",
                        EditorStyles.miniButton))
                    {
                        MoveSpritesToAtlas(null);
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button($"Try to fix Size of {selectedObjectsCount} selected objects",
                        EditorStyles.miniButton))
                    {
                        TryToFixSizeOfSelectedObjects();
                        MoveSpritesToAtlas(null);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }

        private static void TryToFixSizeOfSelectedObjects()
        {
            foreach (Object selectedObject in SpriteAuditorUtility.SelectedObjects)
            {
                if (selectedObject is Sprite sprite)
                {
                    if (SpriteAuditorWindow.GetWindowInstance().SpriteDatabase
                        .TryGetSpriteDataBySprite(sprite, out SpriteData spriteData))
                    {
                        if (SpriteAuditorUtility.CanFixSpriteData(spriteData))
                        {
                            SpriteAuditorUtility.SetBestSizeForTexture(spriteData);
                        }
                    }
                }
            }
        }

        private static void UpdateCurrentState()
        {
            if (!SpriteAuditorUtility.HasSelectedItems)
            {
                currrentState = State.None;
            }
            else
            {
                currrentState = State.WithSelectedItems;

                if (moveToAnotherAtlas)
                {
                    currrentState = State.MovingToAnotherAtlas;
                }
            }
        }

        private static void DrawMoveSelectedObjectsToAnotherAtlas()
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("Move Sprites to Another Atlas", EditorStyles.toolbarDropDown);
                targetNewAtlas = (SpriteAtlas) EditorGUILayout.ObjectField("Target Atlas", targetNewAtlas, typeof(SpriteAtlas), false);

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Cancel", EditorStyles.miniButtonLeft))
                    {
                        moveToAnotherAtlas = false;
                    }

                    EditorGUI.BeginDisabledGroup(targetNewAtlas == null);
                    {
                        if (GUILayout.Button("Move", EditorStyles.miniButtonRight))
                        {
                            MoveSpritesToAtlas(targetNewAtlas);

                            moveToAnotherAtlas = false;
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();

            }
            EditorGUILayout.EndVertical();
        }

        private static void MoveSpritesToAtlas(SpriteAtlas newAtlas)
        {
            foreach (Object selectedObject in SpriteAuditorUtility.SelectedObjects)
            {
                if (selectedObject is Sprite targetSprite)
                {
                    TryToRemoveSpriteFromAnyAtlasReference(targetSprite);

                    if (newAtlas == null) 
                        continue;
                    
                    Debug.Log($"Added {targetSprite} to {newAtlas}");
                    newAtlas.Add(new[] {selectedObject});
                    EditorUtility.SetDirty(newAtlas);
                }
            }

            SpriteAuditorUtility.ClearSelection();
            SpriteAuditorUtility.SetAllDirty();
        }

        private static bool TryToRemoveSpriteFromAnyAtlasReference(Sprite targetSprite)
        {
            bool anyReferenceChanged = false;
            if (AtlasCacheUtility.TryGetAtlasesForSprite(targetSprite,
                out List<SpriteAtlas> spriteAtlases))
            {
                for (int i = 0; i < spriteAtlases.Count; i++)
                {
                    SpriteAtlas spriteAtlas = spriteAtlases[i];
                    if (!AtlasCacheUtility.TryRemoveSpriteFromAtlas(targetSprite, spriteAtlas))
                        anyReferenceChanged = true;
                }
            }

            return anyReferenceChanged;
        }


        private static void UnitySelectSelectedObjects()
        {
            Selection.objects = SpriteAuditorUtility.SelectedObjects.ToArray();
        }


        private static void AddSelectedItemsToAtlas()
        {
            
        }

        private static void RemoveSelectedItemsFromAtlas()
        {
            
        }
    }
}