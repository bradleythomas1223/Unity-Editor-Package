﻿#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using UnityExplorer.Core;
using CppType = Il2CppSystem.Type;
using BF = System.Reflection.BindingFlags;

namespace UnityExplorer
{
    public class Il2CppReflection : ReflectionUtility
    {
        public Il2CppReflection()
        {
            TryLoadGameModules();

            BuildDeobfuscationCache();
            OnTypeLoaded += TryCacheDeobfuscatedType;
        }

        #region IL2CPP Extern and pointers

        // Extern C++ methods 
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool il2cpp_class_is_assignable_from(IntPtr klass, IntPtr oklass);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_object_get_class(IntPtr obj);

        public static bool Il2CppTypeNotNull(Type type) => Il2CppTypeNotNull(type, out _);

        public static bool Il2CppTypeNotNull(Type type, out IntPtr il2cppPtr)
        {
            if (cppClassPointers.TryGetValue(type.AssemblyQualifiedName, out il2cppPtr))
                return il2cppPtr != IntPtr.Zero;

            il2cppPtr = (IntPtr)typeof(Il2CppClassPointerStore<>)
                    .MakeGenericType(new Type[] { type })
                    .GetField("NativeClassPtr", BF.Public | BF.Static)
                    .GetValue(null);

            cppClassPointers.Add(type.AssemblyQualifiedName, il2cppPtr);

            return il2cppPtr != IntPtr.Zero;
        }

        #endregion


        #region Deobfuscation cache

        private static readonly Dictionary<string, Type> DeobfuscatedTypes = new Dictionary<string, Type>();
        private static readonly Dictionary<string, string> reverseDeobCache = new Dictionary<string, string>();

        private static void BuildDeobfuscationCache()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.TryGetTypes())
                    TryCacheDeobfuscatedType(type);
            }

            if (DeobfuscatedTypes.Count > 0)
                ExplorerCore.Log($"Built deobfuscation cache, count: {DeobfuscatedTypes.Count}");
        }

        private static void TryCacheDeobfuscatedType(Type type)
        {
            try
            {
                // Thanks to Slaynash for this
                if (type.CustomAttributes.Any(it => it.AttributeType.Name == "ObfuscatedNameAttribute"))
                {
                    var cppType = Il2CppType.From(type);

                    if (!DeobfuscatedTypes.ContainsKey(cppType.FullName))
                    {
                        DeobfuscatedTypes.Add(cppType.FullName, type);
                        reverseDeobCache.Add(type.FullName, cppType.FullName);
                    }
                }
            }
            catch { }
        }

        internal override string Internal_ProcessTypeInString(string theString, Type type)
        {
            if (reverseDeobCache.TryGetValue(type.FullName, out string obName))
                return theString.Replace(obName, type.FullName);

            return theString;
        }

        #endregion


        // Get type by name

        internal override Type Internal_GetTypeByName(string fullName)
        {
            if (DeobfuscatedTypes.TryGetValue(fullName, out Type deob))
                return deob;

            return base.Internal_GetTypeByName(fullName);
        }

        #region Get actual type

        internal override Type Internal_GetActualType(object obj)
        {
            if (obj == null)
                return null;

            var type = obj.GetType();

            try
            {
                if (IsString(obj))
                    return typeof(string);

                if (IsIl2CppPrimitive(type))
                    return il2cppPrimitivesToMono[type.FullName];

                if (obj is Il2CppSystem.Object cppObject)
                {
                    var cppType = cppObject.GetIl2CppType();

                    // check if type is injected
                    IntPtr classPtr = il2cpp_object_get_class(cppObject.Pointer);
                    if (RuntimeSpecificsStore.IsInjected(classPtr))
                    {
                        // Note: This will fail on injected subclasses.
                        // - {Namespace}.{Class}.{Subclass} would be {Namespace}.{Subclass} when injected.
                        // Not sure on solution yet.
                        return GetTypeByName(cppType.FullName) ?? type;
                    }

                    if (AllTypes.TryGetValue(cppType.FullName, out Type primitive) && primitive.IsPrimitive)
                        return primitive;

                    return GetUnhollowedType(cppType) ?? type;
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception in IL2CPP GetActualType: " + ex);
            }

            return type;
        }

        public static Type GetUnhollowedType(CppType cppType)
        {
            var fullname = cppType.FullName;

            if (DeobfuscatedTypes.TryGetValue(fullname, out Type deob))
                return deob;

            if (fullname.StartsWith("System."))
                fullname = $"Il2Cpp{fullname}";

            AllTypes.TryGetValue(fullname, out Type monoType);
            return monoType;
        }

        #endregion


        #region Casting

        private static readonly Dictionary<string, IntPtr> cppClassPointers = new Dictionary<string, IntPtr>();

        internal override object Internal_TryCast(object obj, Type castTo)
        {
            if (obj == null)
                return null;

            var type = obj.GetType();

            if (type == castTo)
                return obj;

            // from structs
            if (type.IsValueType)
            {
                // from il2cpp primitive to system primitive
                if (IsIl2CppPrimitive(type) && castTo.IsPrimitive)
                {
                    return MakeMonoPrimitive(obj);
                }
                // from system primitive to il2cpp primitive
                else if (IsIl2CppPrimitive(castTo))
                {
                    return MakeIl2CppPrimitive(castTo, obj);
                }
                // from other structs to il2cpp object
                else if (typeof(Il2CppSystem.Object).IsAssignableFrom(castTo))
                {
                    return BoxIl2CppObject(obj);
                }
                else
                    return obj;
            }

            // from string to il2cpp.Object / il2cpp.String
            if (obj is string && typeof(Il2CppSystem.Object).IsAssignableFrom(castTo))
            {
                return BoxStringToType(obj, castTo);
            }

            // from il2cpp objects...

            if (!(obj is Il2CppSystem.Object cppObj))
                return obj;

            // from Il2CppSystem.Object to a struct
            if (castTo.IsValueType)
                return UnboxCppObject(cppObj, castTo);
            // or to system string
            else if (castTo == typeof(string))
                return UnboxString(obj);

            // Casting from il2cpp object to il2cpp object...

            if (!Il2CppTypeNotNull(castTo, out IntPtr castToPtr))
                return obj;

            IntPtr castFromPtr = il2cpp_object_get_class(cppObj.Pointer);

            if (!il2cpp_class_is_assignable_from(castToPtr, castFromPtr))
                return null;

            if (RuntimeSpecificsStore.IsInjected(castToPtr))
            {
                var injectedObj = UnhollowerBaseLib.Runtime.ClassInjectorBase.GetMonoObjectFromIl2CppPointer(cppObj.Pointer);
                return injectedObj ?? obj;
            }

            try
            {
                return Activator.CreateInstance(castTo, cppObj.Pointer);
            }
            catch
            {
                return obj;
            }
        }

        #endregion


        #region Boxing and unboxing ValueTypes

        // cached il2cpp unbox methods
        internal static readonly Dictionary<string, MethodInfo> unboxMethods = new Dictionary<string, MethodInfo>();

        // Unbox an il2cpp object to a struct or System primitive.
        public object UnboxCppObject(Il2CppSystem.Object cppObj, Type toType)
        {
            if (!toType.IsValueType)
                return null;

            try
            {
                if (toType.IsEnum)
                    return Enum.Parse(toType, cppObj.ToString());

                var name = toType.AssemblyQualifiedName;

                if (!unboxMethods.ContainsKey(name))
                {
                    unboxMethods.Add(name, typeof(Il2CppObjectBase)
                                                .GetMethod("Unbox")
                                                .MakeGenericMethod(toType));
                }

                return unboxMethods[name].Invoke(cppObj, ArgumentUtility.EmptyArgs);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception Unboxing Il2Cpp object to struct: " + ex);
                return null;
            }
        }

        private static Il2CppSystem.Object BoxIl2CppObject(object cppStruct, Type structType)
        {
            return GetMethodInfo(structType, "BoxIl2CppObject", ArgumentUtility.EmptyTypes)
                   .Invoke(cppStruct, ArgumentUtility.EmptyArgs)
                   as Il2CppSystem.Object;
        }

        public Il2CppSystem.Object BoxIl2CppObject(object value)
        {
            if (value == null)
                return null;

            try
            {
                var type = value.GetType();
                if (!type.IsValueType)
                    return null;

                if (type.IsEnum)
                    return Il2CppSystem.Enum.Parse(Il2CppType.From(type), value.ToString());

                if (type.IsPrimitive && AllTypes.TryGetValue($"Il2Cpp{type.FullName}", out Type cppType))
                    return BoxIl2CppObject(MakeIl2CppPrimitive(cppType, value), cppType);

                return BoxIl2CppObject(value, type);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception in BoxIl2CppObject: " + ex);
                return null;
            }
        }

        // Helpers for Il2Cpp primitive <-> Mono

        internal static readonly Dictionary<string, Type> il2cppPrimitivesToMono = new Dictionary<string, Type>
        {
            { "Il2CppSystem.Boolean", typeof(bool) },
            { "Il2CppSystem.Byte",    typeof(byte) },
            { "Il2CppSystem.SByte",   typeof(sbyte) },
            { "Il2CppSystem.Char",    typeof(char) },
            { "Il2CppSystem.Double",  typeof(double) },
            { "Il2CppSystem.Single",  typeof(float) },
            { "Il2CppSystem.Int32",   typeof(int) },
            { "Il2CppSystem.UInt32",  typeof(uint) },
            { "Il2CppSystem.Int64",   typeof(long) },
            { "Il2CppSystem.UInt64",  typeof(ulong) },
            { "Il2CppSystem.Int16",   typeof(short) },
            { "Il2CppSystem.UInt16",  typeof(ushort) },
            { "Il2CppSystem.IntPtr",  typeof(IntPtr) },
            { "Il2CppSystem.UIntPtr", typeof(UIntPtr) }
        };

        public static bool IsIl2CppPrimitive(object obj) => IsIl2CppPrimitive(obj.GetType());

        public static bool IsIl2CppPrimitive(Type type) => il2cppPrimitivesToMono.ContainsKey(type.FullName);

        public object MakeMonoPrimitive(object cppPrimitive)
        {
            return GetFieldInfo(cppPrimitive.GetType(), "m_value").GetValue(cppPrimitive);
        }

        public object MakeIl2CppPrimitive(Type cppType, object monoValue)
        {
            var cppStruct = Activator.CreateInstance(cppType);
            GetFieldInfo(cppType, "m_value").SetValue(cppStruct, monoValue);
            return cppStruct;
        }

        #endregion


        #region String boxing/unboxing

        private const string IL2CPP_STRING_FULLNAME = "Il2CppSystem.String";
        private const string STRING_FULLNAME = "System.String";

        public bool IsString(object obj)
        {
            if (obj is string || obj is Il2CppSystem.String)
                return true;
        
            if (obj is Il2CppSystem.Object cppObj)
            {
                var type = cppObj.GetIl2CppType();
                return type.FullName == IL2CPP_STRING_FULLNAME || type.FullName == STRING_FULLNAME;
            }

            return false;
        }

        public object BoxStringToType(object value, Type castTo)
        {
            if (castTo == typeof(Il2CppSystem.String))
                return (Il2CppSystem.String)(value as string);
            else
                return (Il2CppSystem.Object)(value as string);
        }

        public string UnboxString(object value)
        {
            if (value is string s)
                return s;

            s = null;
            if (value is Il2CppSystem.Object cppObject)
                s = cppObject.ToString();
            else if (value is Il2CppSystem.String cppString)
                s = cppString;

            return s;
        }

        #endregion



        #region Singleton finder

        internal override void Internal_FindSingleton(string[] possibleNames, Type type, BF flags, List<object> instances)
        {
            PropertyInfo pi;
            foreach (var name in possibleNames)
            {
                pi = type.GetProperty(name, flags);
                if (pi != null)
                {
                    var instance = pi.GetValue(null, null);
                    if (instance != null)
                    {
                        instances.Add(instance);
                        return;
                    }
                }
            }

            base.Internal_FindSingleton(possibleNames, type, flags, instances);
        }

        #endregion


        #region Force-loading game modules

        // Helper for IL2CPP to try to make sure the Unhollowed game assemblies are actually loaded.

        internal void TryLoadGameModules()
        {
            Internal_LoadModule("Assembly-CSharp");
            Internal_LoadModule("Assembly-CSharp-firstpass");
        }

        internal override bool Internal_LoadModule(string moduleName)
        {
            if (!moduleName.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase))
                moduleName += ".dll";
#if ML
            var path = Path.Combine("MelonLoader", "Managed", $"{moduleName}");
#else
            var path = Path.Combine("BepInEx", "unhollowed", $"{moduleName}");
#endif
            return DoLoadModule(path);
        }

        internal bool DoLoadModule(string fullPath)
        {
            if (!File.Exists(fullPath))
                return false;

            try
            {
                Assembly.Load(File.ReadAllBytes(fullPath));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetType() + ", " + e.Message);
            }

            return false;
        }

        #endregion




        #region Il2cpp reflection blacklist

        public override string DefaultReflectionBlacklist => string.Join(";", defaultIl2CppBlacklist);

        // These methods currently cause a crash in most il2cpp games,
        // even from doing "GetParameters()" on the MemberInfo.
        // Blacklisting until the issue is fixed in Unhollower.
        public static HashSet<string> defaultIl2CppBlacklist = new HashSet<string>
        {
            // These were deprecated a long time ago, still show up in some IL2CPP games for some reason
            "UnityEngine.MonoBehaviour.allowPrefabModeInPlayMode",
            "UnityEngine.MonoBehaviour.runInEditMode",
            "UnityEngine.Component.animation",
            "UnityEngine.Component.audio",
            "UnityEngine.Component.camera",
            "UnityEngine.Component.collider",
            "UnityEngine.Component.collider2D",
            "UnityEngine.Component.constantForce",
            "UnityEngine.Component.hingeJoint",
            "UnityEngine.Component.light",
            "UnityEngine.Component.networkView",
            "UnityEngine.Component.particleSystem",
            "UnityEngine.Component.renderer",
            "UnityEngine.Component.rigidbody",
            "UnityEngine.Component.rigidbody2D",
            "UnityEngine.Light.flare",
            // These can cause a crash in IL2CPP
            "Il2CppSystem.Type.DeclaringMethod",
            "Il2CppSystem.RuntimeType.DeclaringMethod",
            "Unity.Jobs.LowLevel.Unsafe.JobsUtility.CreateJobReflectionData",
            "Unity.Profiling.ProfilerRecorder.CopyTo",
            "Unity.Profiling.ProfilerRecorder.StartNew",
            "UnityEngine.Analytics.Analytics.RegisterEvent",
            "UnityEngine.Analytics.Analytics.SendEvent",
            "UnityEngine.Analytics.ContinuousEvent+ConfigureEventDelegate.Invoke",
            "UnityEngine.Analytics.ContinuousEvent.ConfigureEvent",
            "UnityEngine.Animations.AnimationLayerMixerPlayable.Create",
            "UnityEngine.Animations.AnimationLayerMixerPlayable.CreateHandle",
            "UnityEngine.Animations.AnimationMixerPlayable.Create",
            "UnityEngine.Animations.AnimationMixerPlayable.CreateHandle",
            "UnityEngine.AssetBundle.RecompressAssetBundleAsync",
            "UnityEngine.Audio.AudioMixerPlayable.Create",
            "UnityEngine.BoxcastCommand.ScheduleBatch",
            "UnityEngine.Camera.CalculateProjectionMatrixFromPhysicalProperties",
            "UnityEngine.CapsulecastCommand.ScheduleBatch",
            "UnityEngine.Collider2D.Cast",
            "UnityEngine.Collider2D.Raycast",
            "UnityEngine.ComputeBuffer+BeginBufferWriteDelegate.Invoke",
            "UnityEngine.ComputeBuffer+EndBufferWriteDelegate.Invoke",
            "UnityEngine.ComputeBuffer.BeginBufferWrite",
            "UnityEngine.ComputeBuffer.EndBufferWrite",
            "UnityEngine.Cubemap+SetPixelDataImplArrayDelegate.Invoke",
            "UnityEngine.Cubemap+SetPixelDataImplDelegate.Invoke",
            "UnityEngine.Cubemap.SetPixelDataImpl",
            "UnityEngine.Cubemap.SetPixelDataImplArray",
            "UnityEngine.CubemapArray+SetPixelDataImplArrayDelegate.Invoke",
            "UnityEngine.CubemapArray+SetPixelDataImplDelegate.Invoke",
            "UnityEngine.CubemapArray.SetPixelDataImpl",
            "UnityEngine.CubemapArray.SetPixelDataImplArray",
            "UnityEngine.Experimental.Playables.MaterialEffectPlayable.Create",
            "UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure+AddInstanceDelegate.Invoke",
            "UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure+AddInstance_Procedural_InjectedDelegate.Invoke",
            "UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure.AddInstance",
            "UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure.AddInstance_Procedural",
            "UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure.AddInstance_Procedural_Injected",
            "UnityEngine.Experimental.Rendering.RayTracingShader+DispatchDelegate.Invoke",
            "UnityEngine.Experimental.Rendering.RayTracingShader.Dispatch",
            "UnityEngine.Experimental.Rendering.RenderPassAttachment.Clear",
            "UnityEngine.GUI.DoButtonGrid",
            "UnityEngine.GUI.Slider",
            "UnityEngine.GUI.Toolbar",
            "UnityEngine.Graphics.DrawMeshInstancedIndirect",
            "UnityEngine.Graphics.DrawMeshInstancedProcedural",
            "UnityEngine.Graphics.DrawProcedural",
            "UnityEngine.Graphics.DrawProceduralIndirect",
            "UnityEngine.Graphics.DrawProceduralIndirectNow",
            "UnityEngine.Graphics.DrawProceduralNow",
            "UnityEngine.LineRenderer+BakeMeshDelegate.Invoke",
            "UnityEngine.LineRenderer.BakeMesh",
            "UnityEngine.Mesh.GetIndices",
            "UnityEngine.Mesh.GetTriangles",
            "UnityEngine.Mesh.SetIndices",
            "UnityEngine.Mesh.SetTriangles",
            "UnityEngine.Physics2D.BoxCast",
            "UnityEngine.Physics2D.CapsuleCast",
            "UnityEngine.Physics2D.CircleCast",
            "UnityEngine.PhysicsScene.BoxCast",
            "UnityEngine.PhysicsScene.CapsuleCast",
            "UnityEngine.PhysicsScene.OverlapBox",
            "UnityEngine.PhysicsScene.OverlapCapsule",
            "UnityEngine.PhysicsScene.SphereCast",
            "UnityEngine.PhysicsScene2D.BoxCast",
            "UnityEngine.PhysicsScene2D.CapsuleCast",
            "UnityEngine.PhysicsScene2D.CircleCast",
            "UnityEngine.PhysicsScene2D.GetRayIntersection",
            "UnityEngine.PhysicsScene2D.Linecast",
            "UnityEngine.PhysicsScene2D.OverlapArea",
            "UnityEngine.PhysicsScene2D.OverlapBox",
            "UnityEngine.PhysicsScene2D.OverlapCapsule",
            "UnityEngine.PhysicsScene2D.OverlapCircle",
            "UnityEngine.PhysicsScene2D.OverlapCollider",
            "UnityEngine.PhysicsScene2D.OverlapPoint",
            "UnityEngine.PhysicsScene2D.Raycast",
            "UnityEngine.Playables.Playable.Create",
            "UnityEngine.Profiling.CustomSampler.Create",
            "UnityEngine.RaycastCommand.ScheduleBatch",
            "UnityEngine.RemoteConfigSettings+QueueConfigDelegate.Invoke",
            "UnityEngine.RemoteConfigSettings.QueueConfig",
            "UnityEngine.RenderTexture.GetTemporaryImpl",
            "UnityEngine.Rendering.AsyncGPUReadback.Request",
            "UnityEngine.Rendering.AttachmentDescriptor.ConfigureClear",
            "UnityEngine.Rendering.BatchRendererGroup+AddBatch_InjectedDelegate.Invoke",
            "UnityEngine.Rendering.BatchRendererGroup.AddBatch",
            "UnityEngine.Rendering.BatchRendererGroup.AddBatch_Injected",
            "UnityEngine.Rendering.CommandBuffer+Internal_DispatchRaysDelegate.Invoke",
            "UnityEngine.Rendering.CommandBuffer.DispatchRays",
            "UnityEngine.Rendering.CommandBuffer.DrawMeshInstancedProcedural",
            "UnityEngine.Rendering.CommandBuffer.Internal_DispatchRays",
            "UnityEngine.Rendering.CommandBuffer.ResolveAntiAliasedSurface",
            "UnityEngine.Rendering.ScriptableRenderContext.BeginRenderPass",
            "UnityEngine.Rendering.ScriptableRenderContext.BeginScopedRenderPass",
            "UnityEngine.Rendering.ScriptableRenderContext.BeginScopedSubPass",
            "UnityEngine.Rendering.ScriptableRenderContext.BeginSubPass",
            "UnityEngine.Rendering.ScriptableRenderContext.SetupCameraProperties",
            "UnityEngine.Rigidbody2D.Cast",
            "UnityEngine.Scripting.GarbageCollector+CollectIncrementalDelegate.Invoke",
            "UnityEngine.Scripting.GarbageCollector.CollectIncremental",
            "UnityEngine.SpherecastCommand.ScheduleBatch",
            "UnityEngine.Texture2D+SetPixelDataImplArrayDelegate.Invoke",
            "UnityEngine.Texture2D+SetPixelDataImplDelegate.Invoke",
            "UnityEngine.Texture2D.SetPixelDataImpl",
            "UnityEngine.Texture2D.SetPixelDataImplArray",
            "UnityEngine.Texture2DArray+SetPixelDataImplArrayDelegate.Invoke",
            "UnityEngine.Texture2DArray+SetPixelDataImplDelegate.Invoke",
            "UnityEngine.Texture2DArray.SetPixelDataImpl",
            "UnityEngine.Texture2DArray.SetPixelDataImplArray",
            "UnityEngine.Texture3D+SetPixelDataImplArrayDelegate.Invoke",
            "UnityEngine.Texture3D+SetPixelDataImplDelegate.Invoke",
            "UnityEngine.Texture3D.SetPixelDataImpl",
            "UnityEngine.Texture3D.SetPixelDataImplArray",
            "UnityEngine.TrailRenderer+BakeMeshDelegate.Invoke",
            "UnityEngine.TrailRenderer.BakeMesh",
            "UnityEngine.WWW.LoadFromCacheOrDownload",
            "UnityEngine.XR.InputDevice.SendHapticImpulse",
        };

        #endregion
    }
}

#endif