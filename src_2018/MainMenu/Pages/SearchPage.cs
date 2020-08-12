﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using UnhollowerRuntimeLib;
using MelonLoader;
using UnhollowerBaseLib;

namespace Explorer
{
    public class SearchPage : MainMenu.WindowPage
    {
        public static SearchPage Instance;

        public override string Name { get => "Advanced Search"; set => base.Name = value; }

        private string m_searchInput = "";
        private string m_typeInput = "";
        private int m_limit = 100;

        public SceneFilter SceneMode = SceneFilter.Any;
        public TypeFilter TypeMode = TypeFilter.Object;

        public enum SceneFilter
        {
            Any,
            This,
            DontDestroy,
            None
        }

        public enum TypeFilter
        {
            Object,
            GameObject,
            Component,
            Custom
        }

        private List<object> m_searchResults = new List<object>();
        private Vector2 resultsScroll = Vector2.zero;

        public override void Init()
        {
            Instance = this;
        }

        public void OnSceneChange()
        {
            m_searchResults.Clear();
        }

        public override void Update()
        {
        }

        public override void DrawWindow()
        {
            try
            {
                // helpers
                GUILayout.BeginHorizontal(GUI.skin.box, null);
                GUILayout.Label("<b><color=orange>Helpers</color></b>", new GUILayoutOption[] { GUILayout.Width(70) });
                if (GUILayout.Button("Find Static Instances", new GUILayoutOption[] { GUILayout.Width(180) }))
                {
                    m_searchResults = GetInstanceClassScanner().ToList();
                }
                GUILayout.EndHorizontal();

                // search box
                SearchBox();

                // results
                GUILayout.BeginVertical(GUI.skin.box, null);

                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label("<b><color=orange>Results</color></b>", null);
                GUI.skin.label.alignment = TextAnchor.UpperLeft;

                resultsScroll = GUILayout.BeginScrollView(resultsScroll, GUI.skin.scrollView);

                var _temprect = new Rect(MainMenu.MainRect.x, MainMenu.MainRect.y, MainMenu.MainRect.width + 160, MainMenu.MainRect.height);

                if (m_searchResults.Count > 0)
                {
                    for (int i = 0; i < m_searchResults.Count; i++)
                    {
                        var obj = m_searchResults[i];

                        bool _ = false;
                        UIStyles.DrawValue(ref obj, _temprect, ref _);
                    }
                }
                else
                {
                    GUILayout.Label("<color=red><i>No results found!</i></color>", null);
                }

                GUILayout.EndScrollView();

                GUILayout.EndVertical();
            }
            catch
            {
                m_searchResults.Clear();
            }
        }

        private void SearchBox()
        {
            GUILayout.BeginVertical(GUI.skin.box, null);

            // ----- GameObject Search -----
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("<b><color=orange>Search</color></b>", null);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            GUILayout.BeginHorizontal(null);

            GUILayout.Label("Name Contains:", new GUILayoutOption[] { GUILayout.Width(100) });
            m_searchInput = GUILayout.TextField(m_searchInput, new GUILayoutOption[] { GUILayout.Width(200) });

            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            GUILayout.Label("Result limit:", new GUILayoutOption[] { GUILayout.Width(100) });
            var resultinput = m_limit.ToString();
            resultinput = GUILayout.TextField(resultinput, new GUILayoutOption[] { GUILayout.Width(55) });
            if (int.TryParse(resultinput, out int _i) && _i > 0)
            {
                m_limit = _i;
            }
            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(null);

            GUILayout.Label("Class Filter:", new GUILayoutOption[] { GUILayout.Width(100) });
            ClassFilterToggle(TypeFilter.Object, "Object");
            ClassFilterToggle(TypeFilter.GameObject, "GameObject");
            ClassFilterToggle(TypeFilter.Component, "Component");
            ClassFilterToggle(TypeFilter.Custom, "Custom");
            GUILayout.EndHorizontal();
            if (TypeMode == TypeFilter.Custom)
            {
                GUILayout.BeginHorizontal(null);
                GUI.skin.label.alignment = TextAnchor.MiddleRight;
                GUILayout.Label("Custom Class:", new GUILayoutOption[] { GUILayout.Width(250) });
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                m_typeInput = GUILayout.TextField(m_typeInput, new GUILayoutOption[] { GUILayout.Width(250) });
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal(null);
            GUILayout.Label("Scene Filter:", new GUILayoutOption[] { GUILayout.Width(100) });
            SceneFilterToggle(SceneFilter.Any, "Any", 60);
            SceneFilterToggle(SceneFilter.This, "This Scene", 100);
            SceneFilterToggle(SceneFilter.DontDestroy, "DontDestroyOnLoad", 140);
            SceneFilterToggle(SceneFilter.None, "No Scene", 80);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("<b><color=cyan>Search</color></b>", null))
            {
                Search();
            }

            GUILayout.EndVertical();
        }

        private void ClassFilterToggle(TypeFilter mode, string label)
        {
            if (TypeMode == mode)
            {
                GUI.color = Color.green;
            }
            else
            {
                GUI.color = Color.white;
            }
            if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Width(100) }))
            {
                TypeMode = mode;
            }
            GUI.color = Color.white;
        }

        private void SceneFilterToggle(SceneFilter mode, string label, float width)
        {
            if (SceneMode == mode)
            {
                GUI.color = Color.green;
            }
            else
            {
                GUI.color = Color.white;
            }
            if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Width(width) }))
            {
                SceneMode = mode;
            }
            GUI.color = Color.white;
        }


        // -------------- ACTUAL METHODS (not Gui draw) ----------------- //

        // credit: ManlyMarco (RuntimeUnityEditor)
        public static IEnumerable<object> GetInstanceClassScanner()
        {
            var query = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.FullName.StartsWith("Mono"))
                .SelectMany(GetTypesSafe)
                .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters);

            foreach (var type in query)
            {
                object obj = null;
                try
                {
                    obj = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null, null);
                }
                catch
                {
                    try
                    {
                        obj = type.GetField("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null);
                    }
                    catch
                    {
                    }
                }
                if (obj != null && !obj.ToString().StartsWith("Mono"))
                {
                    yield return obj;
                }
            }
        }

        public static IEnumerable<Type> GetTypesSafe(Assembly asm)
        {
            try { return asm.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(x => x != null); }
            catch { return Enumerable.Empty<Type>(); }
        }

        // ======= search functions =======

        private void Search()
        {
            m_searchResults = FindAllObjectsOfType(m_searchInput, m_typeInput);
        }

        private List<object> FindAllObjectsOfType(string _search, string _type)
        {
            Il2CppSystem.Type type = null;

            if (TypeMode == TypeFilter.Custom)
            {
                try
                {
                    var findType = CppExplorer.GetType(_type);
                    type = Il2CppSystem.Type.GetType(findType.AssemblyQualifiedName);
                }
                catch (Exception e)
                {
                    MelonLogger.Log("Exception: " + e.GetType() + ", " + e.Message + "\r\n" + e.StackTrace);
                }
            }
            else if (TypeMode == TypeFilter.Object)
            {
                type = Il2CppType.Of<Object>();
            }
            else if (TypeMode == TypeFilter.GameObject)
            {
                type = Il2CppType.Of<GameObject>();
            }
            else if (TypeMode == TypeFilter.Component)
            {
                type = Il2CppType.Of<Component>();
            }

            if (!Il2CppType.Of<Object>().IsAssignableFrom(type))
            {
                MelonLogger.LogError("Your Class Type must inherit from UnityEngine.Object! Leave blank to default to UnityEngine.Object");
                return new List<object>();
            }

            var matches = new List<object>();
            int added = 0;

            //MelonLogger.Log("Trying to get IL Type. ASM name: " + type.Assembly.GetName().Name + ", Namespace: " + type.Namespace + ", name: " + type.Name);

            //var asmName = type.Assembly.GetName().Name;
            //if (asmName.Contains("UnityEngine"))
            //{
            //    asmName = "UnityEngine";
            //}

            //var intPtr = IL2CPP.GetIl2CppClass(asmName, type.Namespace, type.Name);
            //var ilType = Il2CppType.TypeFromPointer(intPtr);

            foreach (var obj in Resources.FindObjectsOfTypeAll(type))
            {
                if (added == m_limit)
                {
                    break;
                }

                if (_search != "" && !obj.name.ToLower().Contains(_search.ToLower()))
                {
                    continue;
                }

                if (SceneMode != SceneFilter.Any)
                {
                    if (SceneMode == SceneFilter.None)
                    {
                        if (!NoSceneFilter(obj, obj.GetType()))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        GameObject go;

                        var objtype = obj.GetType();
                        if (objtype == typeof(GameObject))
                        {
                            go = obj as GameObject;
                        }
                        else if (typeof(Component).IsAssignableFrom(objtype))
                        {
                            go = (obj as Component).gameObject;
                        }
                        else { continue; }

                        if (!go) { continue; }

                        if (SceneMode == SceneFilter.This)
                        {
                            if (go.scene.name != CppExplorer.ActiveSceneName || go.scene.name == "DontDestroyOnLoad")
                            {
                                continue;
                            }
                        }
                        else if (SceneMode == SceneFilter.DontDestroy)
                        {
                            if (go.scene.name != "DontDestroyOnLoad")
                            {
                                continue;
                            }
                        }
                    }
                }

                if (!matches.Contains(obj))
                {
                    matches.Add(obj);
                    added++;
                }
            }

            return matches;
        }

        public static bool ThisSceneFilter(object obj, Type type)
        {
            if (type == typeof(GameObject) || typeof(Component).IsAssignableFrom(type))
            {
                var go = obj as GameObject ?? (obj as Component).gameObject;

                if (go != null && go.scene.name == CppExplorer.ActiveSceneName && go.scene.name != "DontDestroyOnLoad")
                {
                    return true;
                }
            }

            return false;
        }

        public static bool DontDestroyFilter(object obj, Type type)
        {
            if (type == typeof(GameObject) || typeof(Component).IsAssignableFrom(type))
            {
                var go = obj as GameObject ?? (obj as Component).gameObject;

                if (go != null && go.scene.name == "DontDestroyOnLoad")
                {
                    return true;
                }
            }

            return false;
        }

        public static bool NoSceneFilter(object obj, Type type)
        {
            if (type == typeof(GameObject))
            {
                var go = obj as GameObject;

                if (go.scene.name != CppExplorer.ActiveSceneName && go.scene.name != "DontDestroyOnLoad")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (typeof(Component).IsAssignableFrom(type))
            {
                var go = (obj as Component).gameObject;

                if (go == null || (go.scene.name != CppExplorer.ActiveSceneName && go.scene.name != "DontDestroyOnLoad"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
    }
}
