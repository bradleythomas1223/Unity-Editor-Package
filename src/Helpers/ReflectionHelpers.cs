﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using MelonLoader;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using BF = System.Reflection.BindingFlags;
using ILType = Il2CppSystem.Type;

namespace Explorer
{
    public class ReflectionHelpers
    {
        public static BF CommonFlags = BF.Public | BF.Instance | BF.NonPublic | BF.Static;

        public static ILType GameObjectType => Il2CppType.Of<GameObject>();
        public static ILType TransformType => Il2CppType.Of<Transform>();
        public static ILType ObjectType => Il2CppType.Of<UnityEngine.Object>();
        public static ILType ComponentType => Il2CppType.Of<Component>();
        public static ILType BehaviourType => Il2CppType.Of<Behaviour>();

        private static readonly MethodInfo m_tryCastMethodInfo = typeof(Il2CppObjectBase).GetMethod("TryCast");

        public static object Il2CppCast(object obj, Type castTo)
        {
            if (!typeof(Il2CppSystem.Object).IsAssignableFrom(castTo)) return obj;

            return m_tryCastMethodInfo
                    .MakeGenericMethod(castTo)
                    .Invoke(obj, null);
        }

        public static bool IsEnumerable(Type t)
        {
            // Not needed for Il2Cpp at the moment. Don't want these to behave as Enumerables.
            //if (typeof(Transform).IsAssignableFrom(t)) return false;

            return typeof(IEnumerable).IsAssignableFrom(t);
        }

        // Checks for Il2Cpp List or HashSet.
        public static bool IsCppEnumerable(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() is Type g)
            {
                return typeof(Il2CppSystem.Collections.Generic.List<>).IsAssignableFrom(g)
                    || typeof(Il2CppSystem.Collections.Generic.IList<>).IsAssignableFrom(g)
                    || typeof(Il2CppSystem.Collections.Generic.HashSet<>).IsAssignableFrom(g);
            }
            else
            {
                return typeof(Il2CppSystem.Collections.IList).IsAssignableFrom(t);
            }
        }

        public static bool IsDictionary(Type t)
        {
            if (typeof(IDictionary).IsAssignableFrom(t))
            {
                return true;
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() is Type g)
            {
                return typeof(Il2CppSystem.Collections.Generic.Dictionary<,>).IsAssignableFrom(g)
                    || typeof(Il2CppSystem.Collections.Generic.IDictionary<,>).IsAssignableFrom(g);
            }
            else
            {
                return typeof(Il2CppSystem.Collections.IDictionary).IsAssignableFrom(t);
            }
        }

        public static Type GetTypeByName(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.TryGetTypes())
                {
                    if (type.FullName == fullName)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        public static Type GetActualType(object obj)
        {
            if (obj == null) return null;

            // Need to use GetIl2CppType for Il2CppSystem Objects
            if (obj is Il2CppSystem.Object ilObject)
            {
                // Prevent weird behaviour when inspecting an Il2CppSystem.Type object.
                if (ilObject is ILType)
                {
                    return typeof(ILType);
                }

                return Type.GetType(ilObject.GetIl2CppType().AssemblyQualifiedName) ?? obj.GetType();
            }

            // It's a normal object, this is fine
            return obj.GetType();
        }

        public static Type[] GetAllBaseTypes(object obj)
        {
            var list = new List<Type>();

            var type = GetActualType(obj);

            while (type != null)
            {
                list.Add(type);
                type = type.BaseType;
            }

            return list.ToArray();
        }

        public static bool LoadModule(string module)
        {
            var path = $@"MelonLoader\Managed\{module}";
            if (!File.Exists(path)) return false;

            try
            {
                Assembly.Load(File.ReadAllBytes(path));
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Log(e.GetType() + ", " + e.Message);
                return false;
            }
        }

        public static string ExceptionToString(Exception e)
        {
            if (IsFailedGeneric(e))
            {
                return "Unable to initialize this type.";
            }
            else if (IsObjectCollected(e))
            {
                return "Garbage collected in Il2Cpp.";
            }

            return e.GetType() + ", " + e.Message;
        }

        public static bool IsFailedGeneric(Exception e)
        {
            return IsExceptionOfType(e, typeof(TargetInvocationException)) && IsExceptionOfType(e, typeof(TypeLoadException));
        }

        public static bool IsObjectCollected(Exception e)
        {
            return IsExceptionOfType(e, typeof(ObjectCollectedException));
        }

        public static bool IsExceptionOfType(Exception e, Type t, bool strict = true, bool checkInner = true)
        {
            bool isType;

            if (strict)
                isType = e.GetType() == t;
            else
                isType = t.IsAssignableFrom(e.GetType());

            if (isType) return true;

            if (e.InnerException != null && checkInner)
                return IsExceptionOfType(e.InnerException, t, strict);
            else
                return false;
        }
    }
}
