﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace Explorer
{
    public partial class CacheList : CacheObjectBase
    {
        public bool IsExpanded { get; set; }
        public int ArrayOffset { get; set; }
        public int ArrayLimit { get; set; } = 20;

        public float WhiteSpace = 215f;
        public float ButtonWidthOffset = 290f;

        private CacheObjectBase[] m_cachedEntries;

        // Type of Entries in the Array
        public Type EntryType 
        {
            get => GetEntryType();
            set => m_entryType = value;
        }
        private Type m_entryType;

        // Cached IEnumerable object
        public IEnumerable Enumerable
        {
            get => GetEnumerable();
        }
        private IEnumerable m_enumerable;

        // Generic Type Definition for Lists
        public Type GenericTypeDef
        {
            get => GetGenericTypeDef();
        }
        private Type m_genericTypeDef;

        // Cached ToArray method for Lists
        public MethodInfo GenericToArrayMethod
        {
            get => GetGenericToArrayMethod();
        }
        private MethodInfo m_genericToArray;

        // Cached Item Property for ILists
        public PropertyInfo ItemProperty
        {
            get => GetItemProperty();
        }
        private PropertyInfo m_itemProperty;

        // ========== Methods ==========

        private IEnumerable GetEnumerable()
        {
            if (m_enumerable == null && Value != null)
            {
                m_enumerable = Value as IEnumerable ?? CastValueFromList();
            }
            return m_enumerable;
        }

        private Type GetGenericTypeDef()
        {
            if (m_genericTypeDef == null && Value != null)
            {
                var type = Value.GetType();
                if (type.IsGenericType)
                {
                    m_genericTypeDef = type.GetGenericTypeDefinition();
                }
            }
            return m_genericTypeDef;
        }

        private MethodInfo GetGenericToArrayMethod()
        {
            if (GenericTypeDef == null) return null;

            if (m_genericToArray == null)
            {
                m_genericToArray = GenericTypeDef
                                    .MakeGenericType(new Type[] { this.EntryType })
                                    .GetMethod("ToArray");
            }
            return m_genericToArray;
        }

        private PropertyInfo GetItemProperty()
        {
            if (m_itemProperty == null)
            {
                m_itemProperty = Value?.GetType().GetProperty("Item");
            }
            return m_itemProperty;
        }

        private IEnumerable CastValueFromList()
        {
            if (Value == null) return null;

            if (GenericTypeDef == typeof(Il2CppSystem.Collections.Generic.List<>))
            {
                return (IEnumerable)GenericToArrayMethod?.Invoke(Value, new object[0]);
            }
            else
            {
                return CastFromIList();
            }
        }

        private IList CastFromIList()
        {
            try
            {
                var genericType = typeof(List<>).MakeGenericType(new Type[] { this.EntryType });
                var list = (IList)Activator.CreateInstance(genericType);

                for (int i = 0; ; i++)
                {
                    try
                    {
                        var itm = ItemProperty.GetValue(Value, new object[] { i });
                        list.Add(itm);
                    }
                    catch { break; }
                }

                return list;
            }
            catch (Exception e)
            {
                MelonLogger.Log("Exception casting IList to Array: " + e.GetType() + ", " + e.Message);
                return null;
            }
        }

        private Type GetEntryType()
        {
            if (m_entryType == null)
            {
                if (this.MemberInfo != null)
                {
                    Type memberType = null;
                    switch (this.MemberInfo.MemberType)
                    {
                        case MemberTypes.Field:
                            memberType = (MemberInfo as FieldInfo).FieldType;
                            break;
                        case MemberTypes.Property:
                            memberType = (MemberInfo as PropertyInfo).PropertyType;
                            break;
                    }

                    if (memberType != null && memberType.IsGenericType)
                    {
                        m_entryType = memberType.GetGenericArguments()[0];
                    }
                }
                else if (Value != null)
                {
                    var type = Value.GetType();
                    if (type.IsGenericType)
                    {
                        m_entryType = type.GetGenericArguments()[0];
                    }
                }
            }

            // IList probably won't be able to get any EntryType.
            if (m_entryType == null)
            {
                m_entryType = typeof(object);
            }

            return m_entryType;
        }

        public override void UpdateValue()
        {
            base.UpdateValue();

            if (Value == null)
            {
                return;
            }

            var enumerator = Enumerable?.GetEnumerator();

            if (enumerator == null)
            {
                return;
            }

            var list = new List<CacheObjectBase>();
            while (enumerator.MoveNext())
            {
                var obj = enumerator.Current;
                var type = ReflectionHelpers.GetActualType(obj);

                if (obj is Il2CppSystem.Object iObj)
                {
                    obj = iObj.Il2CppCast(type);
                }

                var cached = GetCacheObject(obj, null, null, type);
                cached.UpdateValue();

                list.Add(cached);
            }
            m_cachedEntries = list.ToArray();
        }

        // ============= GUI Draw =============

        public override void DrawValue(Rect window, float width)
        {
            if (m_cachedEntries == null)
            {
                GUILayout.Label("m_cachedEntries is null!", null);
                return;
            }

            int count = m_cachedEntries.Length;

            if (!IsExpanded)
            {
                if (GUILayout.Button("v", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    IsExpanded = true;
                }
            }
            else
            {
                if (GUILayout.Button("^", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    IsExpanded = false;
                }
            }

            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            string btnLabel = "<color=yellow>[" + count + "] " + EntryType + "</color>";
            if (GUILayout.Button(btnLabel, new GUILayoutOption[] { GUILayout.MaxWidth(window.width - ButtonWidthOffset) }))
            {
                WindowManager.InspectObject(Value, out bool _);
            }
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;

            GUILayout.Space(5);

            if (IsExpanded)
            {
                float whitespace = WhiteSpace;
                
                if (whitespace > 0)
                {
                    ClampLabelWidth(window, ref whitespace);
                }

                if (count > ArrayLimit)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(null);

                    GUILayout.Space(whitespace);

                    int maxOffset = (int)Mathf.Ceil((float)(count / (decimal)ArrayLimit)) - 1;
                    GUILayout.Label($"Page {ArrayOffset + 1}/{maxOffset + 1}", new GUILayoutOption[] { GUILayout.Width(80) });
                    // prev/next page buttons
                    if (GUILayout.Button("< Prev", new GUILayoutOption[] { GUILayout.Width(60) }))
                    {
                        if (ArrayOffset > 0) ArrayOffset--;
                    }
                    if (GUILayout.Button("Next >", new GUILayoutOption[] { GUILayout.Width(60) }))
                    {
                        if (ArrayOffset < maxOffset) ArrayOffset++;
                    }
                    GUILayout.Label("Limit: ", new GUILayoutOption[] { GUILayout.Width(50) });
                    var limit = this.ArrayLimit.ToString();
                    limit = GUILayout.TextField(limit, new GUILayoutOption[] { GUILayout.Width(50) });
                    if (limit != ArrayLimit.ToString() && int.TryParse(limit, out int i))
                    {
                        ArrayLimit = i;
                    }

                    GUILayout.Space(5);
                }

                int offset = ArrayOffset * ArrayLimit;

                if (offset >= count)
                {
                    offset = 0;
                    ArrayOffset = 0;
                }

                for (int i = offset; i < offset + ArrayLimit && i < count; i++)
                {
                    var entry = m_cachedEntries[i];

                    //collapsing the BeginHorizontal called from ReflectionWindow.WindowFunction or previous array entry
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(null);

                    GUILayout.Space(whitespace);

                    if (entry.Value == null)
                    {
                        GUILayout.Label(i + "<i><color=grey> (null)</color></i>", null);
                    }
                    else
                    {
                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                        GUILayout.Label($"[{i}]", new GUILayoutOption[] { GUILayout.Width(30) });
                        entry.DrawValue(window, window.width - (whitespace + 85));
                    }
                }

                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
        }
    }
}
