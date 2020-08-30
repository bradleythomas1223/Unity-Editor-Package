﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;

namespace Explorer
{
    public abstract class CacheObjectBase
    {
        public object Value;
        public string ValueType;

        // Reflection window only
        public MemberInfo MemberInfo { get; set; }
        public Type DeclaringType { get; set; }
        public object DeclaringInstance { get; set; }
        public string FullName => $"{MemberInfo.DeclaringType.Name}.{MemberInfo.Name}"; 
        public string ReflectionException;

        public bool CanWrite
        {
            get
            {
                if (MemberInfo is FieldInfo fi)
                {
                    return !(fi.IsLiteral && !fi.IsInitOnly);
                }
                else if (MemberInfo is PropertyInfo pi)
                {
                    return pi.CanWrite;
                }
                else
                {
                    return false;
                }
            }
        }

        // methods
        public virtual void Init() { }
        public abstract void DrawValue(Rect window, float width);

        public static CacheObjectBase GetCacheObject(object obj)
        {
            return GetCacheObject(obj, null, null);
        }

        /// <summary>
        /// Gets the CacheObject subclass for an object or MemberInfo
        /// </summary>
        /// <param name="obj">The current value (can be null if memberInfo is not null)</param>
        /// <param name="memberInfo">The MemberInfo (can be null if obj is not null)</param>
        /// <param name="declaringInstance">If MemberInfo is not null, the declaring class instance. Can be null if static.</param>
        /// <returns></returns>
        public static CacheObjectBase GetCacheObject(object obj, MemberInfo memberInfo, object declaringInstance)
        {
            //var type = ReflectionHelpers.GetActualType(obj) ?? (memberInfo as FieldInfo)?.FieldType ?? (memberInfo as PropertyInfo)?.PropertyType;

            Type type = null;

            if (obj != null)
            {
                type = ReflectionHelpers.GetActualType(obj);
            }
            else if (memberInfo != null)
            {
                if (memberInfo is FieldInfo fi)
                {
                    type = fi.FieldType;
                }
                else if (memberInfo is PropertyInfo pi)
                {
                    type = pi.PropertyType;
                }
                else if (memberInfo is MethodInfo mi)
                {
                    type = mi.ReturnType;
                }
            }

            if (type == null)
            {
                MelonLogger.Log("Could not get type for object or memberinfo!");
                if (memberInfo is MethodInfo)
                {
                    MelonLogger.Log("is it void?");
                }
                return null;
            }

            return GetCacheObject(obj, memberInfo, declaringInstance, type);
        }

        /// <summary>
        /// Gets the CacheObject subclass for an object or MemberInfo
        /// </summary>
        /// <param name="obj">The current value (can be null if memberInfo is not null)</param>
        /// <param name="memberInfo">The MemberInfo (can be null if obj is not null)</param>
        /// <param name="declaringInstance">If MemberInfo is not null, the declaring class instance. Can be null if static.</param>
        /// <param name="valueType">The type of the object or MemberInfo value.</param>
        /// <returns></returns>
        public static CacheObjectBase GetCacheObject(object obj, MemberInfo memberInfo, object declaringInstance, Type valueType)
        {
            CacheObjectBase holder;

            if (memberInfo is MethodInfo mi)
            {
                if (CacheMethod.CanEvaluate(mi))
                {
                    holder = new CacheMethod();
                }
                else
                {
                    return null;
                }
            }
            else if (valueType == typeof(GameObject) || valueType == typeof(Transform))
            {
                holder = new CacheGameObject();
            }
            else if (valueType.IsPrimitive || valueType == typeof(string))
            {
                holder = new CachePrimitive();
            }
            else if (valueType.IsEnum)
            {
                holder = new CacheEnum();
            }
            else if (ReflectionHelpers.IsArray(valueType) || ReflectionHelpers.IsList(valueType))
            {
                holder = new CacheList();
            }
            else if (ReflectionHelpers.IsDictionary(valueType))
            {
                holder = new CacheDictionary();
            }
            else
            {
                holder = new CacheOther();
            }

            holder.Value = obj;
            holder.ValueType = valueType.FullName;

            if (memberInfo != null)
            {
                holder.MemberInfo = memberInfo;
                holder.DeclaringType = memberInfo.DeclaringType;
                holder.DeclaringInstance = declaringInstance;

                holder.UpdateValue();
            }

            holder.Init();

            return holder;
        }

        public const float MAX_LABEL_WIDTH = 400f;

        public static void ClampLabelWidth(Rect window, ref float labelWidth)
        {
            float min = window.width * 0.37f;
            if (min > MAX_LABEL_WIDTH) min = MAX_LABEL_WIDTH;

            labelWidth = Mathf.Clamp(labelWidth, min, MAX_LABEL_WIDTH);
        }

        public void Draw(Rect window, float labelWidth = 215f)
        {
            if (labelWidth > 0)
            {
                ClampLabelWidth(window, ref labelWidth);
            }

            if (MemberInfo != null)
            {
                GUILayout.Label("<color=cyan>" + FullName + "</color>", new GUILayoutOption[] { GUILayout.Width(labelWidth) });
            }
            else
            {
                GUILayout.Space(labelWidth);
            }

            if (!string.IsNullOrEmpty(ReflectionException))
            {
                GUILayout.Label("<color=red>Reflection failed!</color> (" + ReflectionException + ")", null);
            }
            else if (Value == null && MemberInfo?.MemberType != MemberTypes.Method)
            {
                GUILayout.Label("<i>null (" + ValueType + ")</i>", null);
            }
            else
            {
                DrawValue(window, window.width - labelWidth - 90);
            }
        }

        public virtual void UpdateValue()
        {
            if (MemberInfo == null || !string.IsNullOrEmpty(ReflectionException))
            {
                return;
            }

            try
            {
                if (MemberInfo.MemberType == MemberTypes.Field)
                {
                    var fi = MemberInfo as FieldInfo;
                    Value = fi.GetValue(fi.IsStatic ? null : DeclaringInstance);
                }
                else if (MemberInfo.MemberType == MemberTypes.Property)
                {
                    var pi = MemberInfo as PropertyInfo;
                    bool isStatic = pi.GetAccessors()[0].IsStatic;
                    var target = isStatic ? null : DeclaringInstance;
                    Value = pi.GetValue(target, null);
                }
                //ReflectionException = null;
            }
            catch (Exception e)
            {
                ReflectionException = ReflectionHelpers.ExceptionToString(e);
            }
        }

        public void SetValue()
        {
            try
            {
                if (MemberInfo.MemberType == MemberTypes.Field)
                {
                    var fi = MemberInfo as FieldInfo;
                    fi.SetValue(fi.IsStatic ? null : DeclaringInstance, Value);
                }
                else if (MemberInfo.MemberType == MemberTypes.Property)
                {
                    var pi = MemberInfo as PropertyInfo;
                    pi.SetValue(pi.GetAccessors()[0].IsStatic ? null : DeclaringInstance, Value);
                }
            }
            catch (Exception e)
            {
                MelonLogger.LogWarning($"Error setting value: {e.GetType()}, {e.Message}");
            }
        }
    }
}
