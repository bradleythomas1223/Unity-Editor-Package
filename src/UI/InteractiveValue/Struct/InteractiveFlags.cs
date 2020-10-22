﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Explorer.UI.Shared;
using Explorer.CacheObject;

namespace Explorer.UI
{
    public class InteractiveFlags : InteractiveEnum, IExpandHeight
    {
        public bool[] m_enabledFlags = new bool[0];

        public bool IsExpanded { get; set; }
        public float WhiteSpace { get; set; } = 215f;

        public override void Init()
        {
            base.Init();

            UpdateValue();
        }

        public override void UpdateValue()
        {
            base.UpdateValue();

            if (Value == null) return;

            try
            {
                var enabledNames = Value.ToString().Split(',').Select(it => it.Trim());

                m_enabledFlags = new bool[EnumNames.Length];

                for (int i = 0; i < EnumNames.Length; i++)
                {
                    m_enabledFlags[i] = enabledNames.Contains(EnumNames[i]);
                }
            }
            catch (Exception e)
            {
                ExplorerCore.Log(e.ToString());
            }
        }

        public override void DrawValue(Rect window, float width)
        {
            if (OwnerCacheObject.CanWrite)
            {
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
            }

            GUILayout.Label(Value.ToString() + "<color=#2df7b2><i> (" + ValueType + ")</i></color>", new GUILayoutOption[0]);

            if (IsExpanded)
            {
                GUILayout.EndHorizontal();

                var whitespace = CalcWhitespace(window);

                for (int i = 0; i < EnumNames.Length; i++)
                {
                    GUIHelper.BeginHorizontal(new GUILayoutOption[0]);
                    GUIHelper.Space(whitespace);

                    m_enabledFlags[i] = GUILayout.Toggle(m_enabledFlags[i], EnumNames[i], new GUILayoutOption[0]);

                    GUILayout.EndHorizontal();
                }

                GUIHelper.BeginHorizontal(new GUILayoutOption[0]);
                GUIHelper.Space(whitespace);
                if (GUILayout.Button("<color=lime>Apply</color>", new GUILayoutOption[] { GUILayout.Width(155) }))
                {
                    SetFlagsFromInput();
                }
                GUILayout.EndHorizontal();

                GUIHelper.BeginHorizontal(new GUILayoutOption[0]);
            }
        }

        public void SetFlagsFromInput()
        {
            string val = "";
            for (int i = 0; i < EnumNames.Length; i++)
            {
                if (m_enabledFlags[i])
                {
                    if (val != "") val += ", ";
                    val += EnumNames[i];
                }
            }
            Value = Enum.Parse(ValueType, val);
            OwnerCacheObject.SetValue();
        }
    }
}
