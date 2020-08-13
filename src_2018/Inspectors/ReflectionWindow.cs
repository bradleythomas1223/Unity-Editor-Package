﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;
using Mono.CSharp;
using UnhollowerBaseLib;
using UnityEngine;

namespace Explorer
{
    public class ReflectionWindow : UIWindow
    {
        public override string Name { get => "Object Reflection"; set => Name = value; }

        public Type m_objectType;
        public object m_object;

        private List<FieldInfoHolder> m_FieldInfos;
        private List<PropertyInfoHolder> m_PropertyInfos;

        private bool m_autoUpdate = false;
        private string m_search = "";
        public MemberFilter m_filter = MemberFilter.Property;

        public enum MemberFilter
        {
            Both,
            Property,
            Field
        }

        public override void Init()
        {
            m_object = Target;

            m_FieldInfos = new List<FieldInfoHolder>();
            m_PropertyInfos = new List<PropertyInfoHolder>();

            var type = GetActualType(m_object);
            if (type == null)
            {
                MelonLogger.Log("could not get underlying type for object. ToString(): " + m_object.ToString());
                return;
            }

            try
            {
                m_objectType = type;
                GetFields(m_object);
                GetProperties(m_object);
            }
            catch { }

            UpdateValues(true);
        }

        public override void Update()
        {
            if (m_autoUpdate)
            {
                UpdateValues();
            }
        }

        private void UpdateValues(bool forceAll = false)
        {
            if (forceAll || m_filter == MemberFilter.Both || m_filter == MemberFilter.Field)
            {
                foreach (var holder in this.m_FieldInfos)
                {
                    if (m_search == "" || holder.fieldInfo.Name.ToLower().Contains(m_search.ToLower()))
                    {
                        holder.UpdateValue(m_object);
                    }
                }
            }

            if (forceAll || m_filter == MemberFilter.Both || m_filter == MemberFilter.Property)
            {
                foreach (var holder in this.m_PropertyInfos)
                {
                    if (m_search == "" || holder.propInfo.Name.ToLower().Contains(m_search.ToLower()))
                    {
                        holder.UpdateValue(m_object);
                    }
                }
            }
        }

        public override void WindowFunction(int windowID)
        {
            try
            {
                Header();

                GUILayout.BeginArea(new Rect(5, 25, m_rect.width - 10, m_rect.height - 35), GUI.skin.box);

                GUILayout.BeginHorizontal(null);
                GUILayout.Label("<b>Type:</b> <color=cyan>" + m_objectType.Name + "</color>", null);

                bool unityObj = m_object is UnityEngine.Object;

                if (unityObj)
                {
                    GUILayout.Label("Name: " + (m_object as UnityEngine.Object).name, null);
                }
                GUILayout.EndHorizontal();

                if (unityObj)
                {
                    GUILayout.BeginHorizontal(null);

                    GUILayout.Label("<b>Tools:</b>", new GUILayoutOption[] { GUILayout.Width(80) });

                    UIStyles.InstantiateButton((UnityEngine.Object)m_object);

                    if (m_object is Component comp && comp.gameObject is GameObject obj)
                    {
                        GUI.skin.label.alignment = TextAnchor.MiddleRight;
                        GUILayout.Label("GameObject:", null);
                        if (GUILayout.Button("<color=#00FF00>" + obj.name + "</color>", new GUILayoutOption[] { GUILayout.MaxWidth(m_rect.width - 350) }))
                        {
                            WindowManager.InspectObject(obj, out bool _);
                        }
                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    }

                    GUILayout.EndHorizontal();
                }

                UIStyles.HorizontalLine(Color.grey);

                GUILayout.BeginHorizontal(null);
                GUILayout.Label("<b>Search:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                m_search = GUILayout.TextField(m_search, null);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Label("<b>Filter:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                FilterToggle(MemberFilter.Both, "Both");
                FilterToggle(MemberFilter.Property, "Properties");
                FilterToggle(MemberFilter.Field, "Fields");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Label("<b>Values:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                if (GUILayout.Button("Update", new GUILayoutOption[] { GUILayout.Width(100) }))
                {
                    UpdateValues();
                }
                GUI.color = m_autoUpdate ? Color.green : Color.red;
                m_autoUpdate = GUILayout.Toggle(m_autoUpdate, "Auto-update?", new GUILayoutOption[] { GUILayout.Width(100) });
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                scroll = GUILayout.BeginScrollView(scroll, GUI.skin.scrollView);

                GUILayout.Space(10);

                if (m_filter == MemberFilter.Both || m_filter == MemberFilter.Field)
                {
                    UIStyles.HorizontalLine(Color.grey);

                    GUILayout.Label("<size=18><b><color=gold>Fields</color></b></size>", null);

                    foreach (var holder in this.m_FieldInfos)
                    {
                        if (m_search != "" && !holder.fieldInfo.Name.ToLower().Contains(m_search.ToLower()))
                        {
                            continue;
                        }

                        GUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.Height(25) });
                        holder.Draw(this);
                        GUILayout.EndHorizontal();
                    }
                }

                if (m_filter == MemberFilter.Both || m_filter == MemberFilter.Property)
                {
                    UIStyles.HorizontalLine(Color.grey);

                    GUILayout.Label("<size=18><b><color=gold>Properties</color></b></size>", null);

                    foreach (var holder in this.m_PropertyInfos)
                    {
                        if (m_search != "" && !holder.propInfo.Name.ToLower().Contains(m_search.ToLower()))
                        {
                            continue;
                        }

                        GUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.Height(25) });
                        holder.Draw(this);
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndScrollView();

                m_rect = WindowManager.ResizeWindow(m_rect, windowID);

                GUILayout.EndArea();
            }
            catch (Exception e)
            {
                MelonLogger.LogWarning("Exception on window draw. Message: " + e.Message);
                DestroyWindow();
                return;
            }
        }

        private void FilterToggle(MemberFilter mode, string label)
        {
            if (m_filter == mode)
            {
                GUI.color = Color.green;
            }
            else
            {
                GUI.color = Color.white;
            }
            if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Width(100) }))
            {
                m_filter = mode;
            }
            GUI.color = Color.white;
        }

        // ============ HELPERS =============== 

        public Type GetActualType(object m_object)
        {
            if (m_object is Il2CppSystem.Object ilObject)
            {
                var iltype = ilObject.GetIl2CppType();
                return Type.GetType(iltype.AssemblyQualifiedName);
            }
            else
            {
                return m_object.GetType();
            }
        }

        public Type[] GetAllBaseTypes(object m_object)
        {
            var list = new List<Type>();

            if (m_object is Il2CppSystem.Object ilObject)
            {
                var ilType = ilObject.GetIl2CppType();
                if (Type.GetType(ilType.AssemblyQualifiedName) is Type ilTypeToManaged)
                {
                    list.Add(ilTypeToManaged);

                    while (ilType.BaseType != null)
                    {
                        ilType = ilType.BaseType;
                        if (Type.GetType(ilType.AssemblyQualifiedName) is Type ilBaseTypeToManaged)
                        {
                            list.Add(ilBaseTypeToManaged);
                        }
                    }
                }
            }
            else
            {
                var type = m_object.GetType();
                list.Add(type);
                while (type.BaseType != null)
                {
                    type = type.BaseType;
                    list.Add(type);
                }
            }

            return list.ToArray();
        }

        public static bool IsList(Type t)
        {
            return t.IsGenericType
                && t.GetGenericTypeDefinition() is Type typeDef
                && (typeDef.IsAssignableFrom(typeof(List<>)) || typeDef.IsAssignableFrom(typeof(Il2CppSystem.Collections.Generic.List<>)));
        }

        private void GetProperties(object m_object, List<string> names = null)
        {
            if (names == null)
            {
                names = new List<string>();
            }

            var types = GetAllBaseTypes(m_object);

            foreach (var type in types)
            {
                PropertyInfo[] propInfos = new PropertyInfo[0];

                try
                {
                    propInfos = type.GetProperties(At.flags);
                }
                catch (TypeLoadException)
                {
                    MelonLogger.Log($"Couldn't get Properties for Type '{type.Name}', it may not support Il2Cpp Reflection at the moment.");
                }

                foreach (var pi in propInfos)
                {
                    // this member causes a crash when inspected, so just skipping it for now.
                    if (pi.Name == "Il2CppType")
                    {
                        continue;
                    }

                    if (names.Contains(pi.Name))
                    {
                        continue;
                    }
                    names.Add(pi.Name);

                    var piHolder = new PropertyInfoHolder(type, pi);
                    m_PropertyInfos.Add(piHolder);
                }
            }
        }

        private void GetFields(object m_object, List<string> names = null)
        {
            if (names == null)
            {
                names = new List<string>();
            }

            var types = GetAllBaseTypes(m_object);

            foreach (var type in types)
            {
                foreach (var fi in type.GetFields(At.flags))
                {
                    if (names.Contains(fi.Name))
                    {
                        continue;
                    }
                    names.Add(fi.Name);

                    var fiHolder = new FieldInfoHolder(type, fi);
                    m_FieldInfos.Add(fiHolder);
                }
            }
        }
    }
}
