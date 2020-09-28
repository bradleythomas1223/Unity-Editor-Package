﻿#if BIE
using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
#if CPP
using UnhollowerRuntimeLib;
using BepInEx.IL2CPP;
#endif

namespace Explorer
{
    [BepInPlugin(ExplorerCore.GUID, ExplorerCore.NAME, ExplorerCore.VERSION)]
#if CPP
    public class ExplorerBepInPlugin : BasePlugin
#else
    public class ExplorerBepInPlugin : BaseUnityPlugin
#endif
    {
        public static ExplorerBepInPlugin Instance;

        public static ManualLogSource Logging =>
#if CPP
                                        Instance?.Log;
#else
                                        Instance?.Logger;
#endif

        public static readonly Harmony HarmonyInstance = new Harmony(ExplorerCore.GUID);

#if CPP
        // temporary for Il2Cpp until scene change delegate works
        private static string lastSceneName;
#endif

        // Init
#if CPP
        public override void Load()
        {
#else
        internal void Awake()
        {
#endif
            Instance = this;

#if CPP
            ClassInjector.RegisterTypeInIl2Cpp<ExplorerBehaviour>();

            var obj = new GameObject(
                "ExplorerBehaviour",
                new Il2CppSystem.Type[]
                {
                    Il2CppType.Of<ExplorerBehaviour>()
                }
            );
            GameObject.DontDestroyOnLoad(obj);
#else
            SceneManager.activeSceneChanged += DoSceneChange;
#endif

            LoadMCS();

            new ExplorerCore();

            HarmonyInstance.PatchAll();
        }

        void LoadMCS()
        {
#if NET35
            var path = @"BepInEx\plugins\mcs.NET35.dll";
#else
            var path = @"BepInEx\plugins\mcs.dll";
#endif
            Assembly.Load(File.ReadAllBytes(path));
            ExplorerCore.Log("Loaded mcs!");
        }

        internal static void DoSceneChange(Scene arg0, Scene arg1)
        {
            ExplorerCore.OnSceneChange();
        }

#if CPP // BepInEx Il2Cpp mod class doesn't have monobehaviour methods yet, so wrap them in a dummy.
        public class ExplorerBehaviour : MonoBehaviour
        {
            public ExplorerBehaviour(IntPtr ptr) : base(ptr) { }

            internal void Awake()
            {
                Logging.LogMessage("ExplorerBehaviour.Awake");
            }

#endif
            internal void Update()
            {
                ExplorerCore.Update();

#if CPP
                var scene = SceneManager.GetActiveScene();
                if (scene.name != lastSceneName)
                {
                    lastSceneName = scene.name;
                    DoSceneChange(scene, scene);
                }
#endif
            }

            internal void OnGUI()
            {
                ExplorerCore.OnGUI();
            }
#if CPP
        }
#endif
    }
}
#endif
