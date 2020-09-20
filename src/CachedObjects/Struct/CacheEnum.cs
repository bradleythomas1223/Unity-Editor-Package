﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace Explorer
{
    public class CacheEnum : CacheObjectBase
    {
        // public Type EnumType;
        public string[] EnumNames;

        public override void Init()
        {
            if (ValueType == null && Value != null)
            {
                ValueType = Value.GetType();
            }

            if (ValueType != null)
            {
                EnumNames = Enum.GetNames(ValueType);
            }
            else
            {
                ReflectionException = "Unknown, could not get Enum names.";
            }
        }

        public override void DrawValue(Rect window, float width)
        {
            if (CanWrite)
            {
                if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    SetEnum(ref Value, -1);
                    SetValue();
                }
                if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    SetEnum(ref Value, 1);
                    SetValue();
                }
            }

            GUILayout.Label(Value.ToString() + "<color=#2df7b2><i> (" + ValueType + ")</i></color>", null);
        }

        public void SetEnum(ref object value, int change)
        {
            var names = EnumNames.ToList();

            int newindex = names.IndexOf(value.ToString()) + change;

            if ((change < 0 && newindex >= 0) || (change > 0 && newindex < names.Count))
            {
                value = Enum.Parse(ValueType, names[newindex]);
            }
        }
    }
}
