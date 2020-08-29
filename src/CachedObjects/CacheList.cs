﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public Type EntryType 
        { 
            get 
            {
                if (m_entryType == null)
                {
                    if (this.MemberInfo != null)
                    {
                        switch (this.MemberInfoType)
                        {
                            case MemberTypes.Field:
                                m_entryType = (MemberInfo as FieldInfo).FieldType.GetGenericArguments()[0];
                                break;
                            case MemberTypes.Property:
                                m_entryType = (MemberInfo as PropertyInfo).PropertyType.GetGenericArguments()[0];
                                break;
                        }
                    }
                    else if (Value != null)
                    {
                        m_entryType = Value.GetType().GetGenericArguments()[0];
                    }
                }
                return m_entryType;
            }
            set
            {
                m_entryType = value;
            } 
        }
        private Type m_entryType;

        public IEnumerable Enumerable
        {
            get
            {
                if (m_enumerable == null && Value != null)
                {
                    m_enumerable = Value as IEnumerable ?? CastValueFromList();
                }
                return m_enumerable;
            }
        }

        private IEnumerable m_enumerable;
        private CacheObjectBase[] m_cachedEntries;

        public MethodInfo GenericToArrayMethod
        {
            get
            {
                if (EntryType == null) return null;

                return m_genericToArray ?? 
                            (m_genericToArray = typeof(Il2CppSystem.Collections.Generic.List<>)
                            .MakeGenericType(new Type[] { this.EntryType })
                            .GetMethod("ToArray"));
            }
        }
        private MethodInfo m_genericToArray;

        private IEnumerable CastValueFromList()
        {
            return (Value == null) ? null : (IEnumerable)GenericToArrayMethod?.Invoke(Value, new object[0]);
        }

        public override void DrawValue(Rect window, float width)
        {
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

        /// <summary>
        /// Called only when the user presses the "Update" button, or if AutoUpdate is on.
        /// </summary>
        public override void UpdateValue()
        {
            base.UpdateValue();

            if (Value == null) return;

            var enumerator = Enumerable?.GetEnumerator();

            if (enumerator == null) return;

            var list = new List<CacheObjectBase>();
            while (enumerator.MoveNext())
            {
                list.Add(GetCacheObject(enumerator.Current, null, null, this.EntryType));
            }

            m_cachedEntries = list.ToArray();
        }
    }
}
