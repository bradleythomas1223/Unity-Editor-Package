﻿#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace UnityExplorer.Core.Runtime.Il2Cpp
{
    public class Il2CppProvider : RuntimeProvider
    {
        public override void Initialize()
        {
            Reflection = new Il2CppReflection();
            TextureUtil = new Il2CppTextureUtil();
        }

        public override void SetupEvents()
        {
            Application.add_logMessageReceived(
                new Action<string, string, LogType>(ExplorerCore.Instance.OnUnityLog));

            //SceneManager.add_sceneLoaded(
            //    new Action<Scene, LoadSceneMode>(ExplorerCore.Instance.OnSceneLoaded1));

            //SceneManager.add_activeSceneChanged(
            //    new Action<Scene, Scene>(ExplorerCore.Instance.OnSceneLoaded2));
        }

        internal delegate IntPtr d_LayerToName(int layer);

        public override string LayerToName(int layer)
        {
            var iCall = ICallManager.GetICall<d_LayerToName>("UnityEngine.LayerMask::LayerToName");
            return IL2CPP.Il2CppStringToManaged(iCall.Invoke(layer));
        }

        internal delegate IntPtr d_FindObjectsOfTypeAll(IntPtr type);

        public override UnityEngine.Object[] FindObjectsOfTypeAll(Type type)
        {
            var iCall = ICallManager.GetICall<d_FindObjectsOfTypeAll>("UnityEngine.Resources::FindObjectsOfTypeAll");
            var cppType = Il2CppType.From(type);

            return new Il2CppReferenceArray<UnityEngine.Object>(iCall.Invoke(cppType.Pointer));
        }

        public override int GetSceneHandle(Scene scene)
            => scene.handle;

        //Scene.GetRootGameObjects();

        internal delegate void d_GetRootGameObjects(int handle, IntPtr list);

        public override GameObject[] GetRootGameObjects(Scene scene) => GetRootGameObjects(scene.handle);

        public static GameObject[] GetRootGameObjects(int handle)
        {
            if (handle == -1)
                return new GameObject[0];

            int count = GetRootCount(handle);

            if (count < 1)
                return new GameObject[0];

            var list = new Il2CppSystem.Collections.Generic.List<GameObject>(count);

            var iCall = ICallManager.GetICall<d_GetRootGameObjects>("UnityEngine.SceneManagement.Scene::GetRootGameObjectsInternal");

            iCall.Invoke(handle, list.Pointer);

            return list.ToArray();
        }

        //Scene.rootCount;

        internal delegate int d_GetRootCountInternal(int handle);

        public override int GetRootCount(Scene scene) => GetRootCount(scene.handle);

        public static int GetRootCount(int handle)
        {
            return ICallManager.GetICall<d_GetRootCountInternal>("UnityEngine.SceneManagement.Scene::GetRootCountInternal")
                   .Invoke(handle);
        }
    }
}

public static class UnityEventExtensions
{
    public static void AddListener(this UnityEvent action, Action listener)
    {
        action.AddListener(listener);
    }

    public static void AddListener<T>(this UnityEvent<T> action, Action<T> listener)
    {
        action.AddListener(listener);
    }
}

#endif