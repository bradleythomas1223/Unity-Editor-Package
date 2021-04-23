﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExplorer.Core.Runtime;

namespace UnityExplorer.Core.Search
{
    public static class SearchProvider
    {
        internal static List<object> StaticClassSearch(string input)
        {
            var list = new List<object>();

            var nameFilter = "";
            if (!string.IsNullOrEmpty(input))
                nameFilter = input.ToLower();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.TryGetTypes().Where(it => it.IsSealed && it.IsAbstract))
                {
                    if (!string.IsNullOrEmpty(nameFilter) && !type.FullName.ToLower().Contains(nameFilter))
                        continue;

                    list.Add(type);
                }
            }

            return list;
        }

        internal static string[] s_instanceNames = new string[]
        {
            "m_instance",
            "m_Instance",
            "s_instance",
            "s_Instance",
            "_instance",
            "_Instance",
            "instance",
            "Instance",
            "<Instance>k__BackingField",
            "<instance>k__BackingField",
        };

        internal static List<object> SingletonSearch(string input)
        {
            var instances = new List<object>();

            var nameFilter = "";
            if (!string.IsNullOrEmpty(input))
                nameFilter = input.ToLower();

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Search all non-static, non-enum classes.
                foreach (var type in asm.TryGetTypes().Where(it => !(it.IsSealed && it.IsAbstract) && !it.IsEnum))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(nameFilter) && !type.FullName.ToLower().Contains(nameFilter))
                            continue;

                        ReflectionProvider.Instance.FindSingleton(s_instanceNames, type, flags, instances);
                    }
                    catch { }
                }
            }

            return instances;
        }

        private static bool Filter(Scene scene, SceneFilter filter)
        {
            switch (filter)
            {
                case SceneFilter.Any:
                    return true;
                case SceneFilter.DontDestroyOnLoad:
                    return scene == SceneHandler.DontDestroyScene;
                case SceneFilter.HideAndDontSave:
                    return scene == SceneHandler.AssetScene;
                case SceneFilter.ActivelyLoaded:
                    return scene != SceneHandler.DontDestroyScene && scene != SceneHandler.AssetScene;
                default:
                    return false;
            }
        }

        internal static List<object> UnityObjectSearch(string input, string customTypeInput, SearchContext context, 
            ChildFilter childFilter, SceneFilter sceneFilter)
        {
            var results = new List<object>();

            Type searchType = null;
            switch (context)
            {
                case SearchContext.GameObject:
                    searchType = typeof(GameObject); break;

                case SearchContext.Component:
                    searchType = typeof(Component); break;

                case SearchContext.Custom:
                    if (string.IsNullOrEmpty(customTypeInput))
                    {
                        ExplorerCore.LogWarning("Custom Type input must not be empty!");
                        return results;
                    }
                    if (ReflectionUtility.GetTypeByName(customTypeInput) is Type customType)
                    {
                        if (typeof(UnityEngine.Object).IsAssignableFrom(customType))
                            searchType = customType;
                        else
                            ExplorerCore.LogWarning($"Custom type '{customType.FullName}' is not assignable from UnityEngine.Object!");
                    }
                    else
                        ExplorerCore.LogWarning($"Could not find a type by the name '{customTypeInput}'!");
                    break;

                default:
                    searchType = typeof(UnityEngine.Object); break;
            }


            if (searchType == null)
                return results;

            var allObjects = RuntimeProvider.Instance.FindObjectsOfTypeAll(searchType);

            // perform filter comparers

            string nameFilter = null;
            if (!string.IsNullOrEmpty(input))
                nameFilter = input.ToLower();

            bool canGetGameObject = (sceneFilter != SceneFilter.Any || childFilter != ChildFilter.Any)
                && (context == SearchContext.GameObject || typeof(Component).IsAssignableFrom(searchType));

            if (!canGetGameObject)
            {
                if (context != SearchContext.UnityObject && (sceneFilter != SceneFilter.Any || childFilter != ChildFilter.Any))
                    ExplorerCore.LogWarning($"Type '{searchType}' cannot have Scene or Child filters applied to it");
            }

            foreach (var obj in allObjects)
            {
                // name check
                if (!string.IsNullOrEmpty(nameFilter) && !obj.name.ToLower().Contains(nameFilter))
                    continue;

                if (canGetGameObject)
                {
                    var go = context == SearchContext.GameObject
                            ? obj.TryCast<GameObject>()
                            : obj.TryCast<Component>().gameObject;

                    // scene check
                    if (sceneFilter != SceneFilter.Any)
                    {
                        if (!go)
                            continue;

                        switch (context)
                        {
                            case SearchContext.GameObject:
                            case SearchContext.Custom:
                            case SearchContext.Component:
                                if (!Filter(go.scene, sceneFilter))
                                    continue;
                                break;
                        }
                    }

                    if (childFilter != ChildFilter.Any)
                    {
                        if (!go)
                            continue;

                        // root object check (no parent)
                        if (childFilter == ChildFilter.HasParent && !go.transform.parent)
                            continue;
                        else if (childFilter == ChildFilter.RootObject && go.transform.parent)
                            continue;
                    }
                }

                results.Add(obj);
            }

            return results;
        }
    }
}
