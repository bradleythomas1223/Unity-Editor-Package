﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Helpers;
using UnityExplorer.UI;

namespace UnityExplorer.Inspectors.Reflection
{
    // WIP
    public enum IValueTypes
    {
        Any,
        Enumerable,
        Dictionary,
    }

    public class InteractiveValue
    {
        // ~~~~~~~~~ Static ~~~~~~~~~

        // WIP
        internal static Dictionary<IValueTypes, Type> s_typeDict = new Dictionary<IValueTypes, Type>
        {
            { IValueTypes.Any,          typeof(InteractiveValue) },
            { IValueTypes.Dictionary,   typeof(InteractiveDictionary) },
            { IValueTypes.Enumerable,   typeof(InteractiveEnumerable) },
        };

        // WIP
        public static IValueTypes GetIValueForType(Type type)
        {
            if (type.IsPrimitive || type == typeof(string))
                return IValueTypes.Any; // TODO Primitive
            else if (typeof(Transform).IsAssignableFrom(type))
                return IValueTypes.Any; // TODO Transform
            else if (ReflectionHelpers.IsDictionary(type))
                return IValueTypes.Dictionary;
            else if (ReflectionHelpers.IsEnumerable(type))
                return IValueTypes.Enumerable;
            else
                return IValueTypes.Any;
        }

        public static InteractiveValue Create(object value, Type fallbackType)
        {
            var type = ReflectionHelpers.GetActualType(value) ?? fallbackType;
            var iType = GetIValueForType(type);

            return (InteractiveValue)Activator.CreateInstance(s_typeDict[iType], new object[] { value, type });
        }

        // ~~~~~~~~~ Instance ~~~~~~~~~

        public InteractiveValue(object value, Type valueType)
        {
            this.Value = value;
            this.FallbackType = valueType;
        }

        public CacheObjectBase OwnerCacheObject;

        public object Value { get; set; }
        public readonly Type FallbackType;

        public virtual IValueTypes IValueType => IValueTypes.Any; 

        public virtual bool HasSubContent => false;
        public virtual bool WantInspectBtn => true;

        public string RichTextValue => m_richValue ?? GetLabelForValue();
        internal string m_richValue;
        internal string m_richValueType;

        public MethodInfo ToStringMethod => m_toStringMethod ?? GetToStringMethod();
        internal MethodInfo m_toStringMethod;

        public bool m_UIConstructed;

        public virtual void OnDestroy()
        {
            if (this.m_valueContent)
            {
                m_valueContent.transform.SetParent(null, false);
                m_valueContent.SetActive(false); 
                GameObject.Destroy(this.m_valueContent.gameObject);

                //if (OwnerCacheObject.m_subContent)
                //{
                //    var subTrans = this.OwnerCacheObject.m_subContent.transform;
                //    for (int i = subTrans.childCount - 1; i >= 0; i--)
                //    {
                //        var child = subTrans.GetChild(i);
                //        GameObject.Destroy(child.gameObject);
                //    }
                //}
            }
        }

        public virtual void OnValueUpdated()
        {
            if (!m_UIConstructed)
                ConstructUI(m_mainContentParent, m_subContentParent);

            if (OwnerCacheObject is CacheMember ownerMember && !string.IsNullOrEmpty(ownerMember.ReflectionException))
            {
                m_baseLabel.text = "<color=red>" + ownerMember.ReflectionException + "</color>";
                Value = null;
            }
            else
            {
                GetLabelForValue();
                m_baseLabel.text = RichTextValue;
            }

            bool shouldShowBtns = !Value.IsNullOrDestroyed();

            if (WantInspectBtn && m_inspectButton.activeSelf != shouldShowBtns)
                m_inspectButton.SetActive(shouldShowBtns);

            if (HasSubContent)
            {
                if (m_subExpandBtn.gameObject.activeSelf != shouldShowBtns)
                    m_subExpandBtn.gameObject.SetActive(shouldShowBtns);

                if (!shouldShowBtns && m_subContentParent.activeSelf)
                    ToggleSubcontent();
            }
        }

        public virtual void ConstructSubcontent() 
        {
            m_subContentConstructed = true;
        }

        public void ToggleSubcontent()
        {
            if (!this.m_subContentParent.activeSelf)
            {
                this.m_subContentParent.SetActive(true);
                this.m_subContentParent.transform.SetAsLastSibling();
                m_subExpandBtn.GetComponentInChildren<Text>().text = "▼";
            }
            else
            {
                this.m_subContentParent.SetActive(false);
                m_subExpandBtn.GetComponentInChildren<Text>().text = "▲";
            }

            OnToggleSubcontent(m_subContentParent.activeSelf);
        }

        internal virtual void OnToggleSubcontent(bool toggle)
        {
            if (!m_subContentConstructed)
                ConstructSubcontent();
        }

        public string GetLabelForValue()
        {
            var valueType = Value?.GetType() ?? this.FallbackType;

            m_richValueType = UISyntaxHighlight.ParseFullSyntax(valueType, true);

            if (OwnerCacheObject is CacheMember cm && !cm.HasEvaluated)
                return $"<i><color=grey>Not yet evaluated</color> ({m_richValueType})</i>";

            if (Value.IsNullOrDestroyed())
            {
                return $"<color=grey>null</color> ({m_richValueType})";
            }

            string label;

            if (valueType == typeof(TextAsset) && Value is TextAsset textAsset)
            {
                label = textAsset.text;

                if (label.Length > 10)
                    label = $"{label.Substring(0, 10)}...";

                label = $"\"{label}\" {textAsset.name} ({m_richValueType})";
            }
            else if (valueType == typeof(EventSystem))
            {
                label = m_richValueType;
            }
            else
            {
                var toString = (string)ToStringMethod.Invoke(Value, null);

                var fullnametemp = valueType.ToString();
                if (fullnametemp.StartsWith("Il2CppSystem"))
                    fullnametemp = fullnametemp.Substring(6, fullnametemp.Length - 6);

                var temp = toString.Replace(fullnametemp, "").Trim();

                if (string.IsNullOrEmpty(temp))
                {
                    label = m_richValueType;
                }
                else
                {
                    if (toString.Length > 200)
                        toString = toString.Substring(0, 200) + "...";

                    label = toString;

                    var unityType = $"({valueType.FullName})";
                    if (Value is UnityEngine.Object && label.Contains(unityType))
                        label = label.Replace(unityType, $"({m_richValueType})");
                    else
                        label += $" ({m_richValueType})";
                }
            }

            return m_richValue = label;
        }

        private MethodInfo GetToStringMethod()
        {
            try
            {
                m_toStringMethod = ReflectionHelpers.GetActualType(Value).GetMethod("ToString", new Type[0])
                                   ?? typeof(object).GetMethod("ToString", new Type[0]);

                // test invoke
                m_toStringMethod.Invoke(Value, null);
            }
            catch
            {
                m_toStringMethod = typeof(object).GetMethod("ToString", new Type[0]);
            }
            return m_toStringMethod;
        }

        #region UI CONSTRUCTION

        internal GameObject m_mainContentParent;
        internal GameObject m_subContentParent;

        internal GameObject m_valueContent;
        internal GameObject m_inspectButton;
        internal Text m_baseLabel;

        internal Button m_subExpandBtn;
        internal bool m_subContentConstructed;

        public virtual void ConstructUI(GameObject parent, GameObject subGroup)
        {
            m_UIConstructed = true;

            m_valueContent = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            m_valueContent.name = "InteractiveValue.ValueContent";
            var mainRect = m_valueContent.GetComponent<RectTransform>();
            mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);
            var mainGroup = m_valueContent.GetComponent<HorizontalLayoutGroup>();
            mainGroup.childForceExpandWidth = false;
            mainGroup.childControlWidth = true;
            mainGroup.childForceExpandHeight = false;
            mainGroup.childControlHeight = true;
            mainGroup.spacing = 4;
            mainGroup.childAlignment = TextAnchor.UpperLeft;
            var mainLayout = m_valueContent.AddComponent<LayoutElement>();
            mainLayout.flexibleWidth = 9000;
            mainLayout.minWidth = 175;
            mainLayout.minHeight = 25;
            mainLayout.flexibleHeight = 0;

            // subcontent expand button TODO
            if (HasSubContent)
            {
                var subBtnObj = UIFactory.CreateButton(m_valueContent, new Color(0.3f, 0.3f, 0.3f));
                var btnLayout = subBtnObj.AddComponent<LayoutElement>();
                btnLayout.minHeight = 25;
                btnLayout.minWidth = 25;
                btnLayout.flexibleWidth = 0;
                btnLayout.flexibleHeight = 0;
                var btnText = subBtnObj.GetComponentInChildren<Text>();
                btnText.text = "▲";
                m_subExpandBtn = subBtnObj.GetComponent<Button>();
                m_subExpandBtn.onClick.AddListener(() =>
                {
                    ToggleSubcontent();
                });
            }

            // inspect button

            m_inspectButton = UIFactory.CreateButton(m_valueContent, new Color(0.3f, 0.3f, 0.3f, 0.2f));
            var inspectLayout = m_inspectButton.AddComponent<LayoutElement>();
            inspectLayout.minWidth = 60;
            inspectLayout.minHeight = 25;
            inspectLayout.flexibleHeight = 0;
            inspectLayout.flexibleWidth = 0;
            var inspectText = m_inspectButton.GetComponentInChildren<Text>();
            inspectText.text = "Inspect";
            var inspectBtn = m_inspectButton.GetComponent<Button>();

            inspectBtn.onClick.AddListener(OnInspectClicked);
            void OnInspectClicked()
            {
                if (!Value.IsNullOrDestroyed(false))
                    InspectorManager.Instance.Inspect(this.Value);
            }

            m_inspectButton.SetActive(false);

            // value label

            var labelObj = UIFactory.CreateLabel(m_valueContent, TextAnchor.MiddleLeft);
            m_baseLabel = labelObj.GetComponent<Text>();
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 9000;
            labelLayout.minHeight = 25;

            m_subContentParent = subGroup;
        }

#endregion
    }
}
