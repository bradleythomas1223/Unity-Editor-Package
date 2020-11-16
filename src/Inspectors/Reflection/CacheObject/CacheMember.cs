﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI;
using UnityExplorer.UI.Shared;
using UnityExplorer.Helpers;
#if CPP
using UnhollowerBaseLib;
#endif

namespace UnityExplorer.Inspectors.Reflection
{
    public abstract class CacheMember : CacheObjectBase
    {
        public override bool IsMember => true;

        public override Type FallbackType { get; }

        public MemberInfo MemInfo { get; set; }
        public Type DeclaringType { get; set; }
        public object DeclaringInstance { get; set; }
        public virtual bool IsStatic { get; private set; }

        public string ReflectionException { get; set; }

        public override bool CanWrite => m_canWrite ?? GetCanWrite();
        private bool? m_canWrite;

        public override bool HasParameters => ParamCount > 0;
        public virtual int ParamCount => m_arguments.Length;
        public override bool HasEvaluated => m_evaluated;
        public bool m_evaluated = false;
        public bool m_isEvaluating;
        public ParameterInfo[] m_arguments = new ParameterInfo[0];
        public string[] m_argumentInput = new string[0];

        public string NameForFiltering => m_nameForFilter ?? (m_nameForFilter = $"{MemInfo.DeclaringType.Name}.{MemInfo.Name}".ToLower());
        private string m_nameForFilter;

        public string RichTextName => m_richTextName ?? GetRichTextName();
        private string m_richTextName;

        public CacheMember(MemberInfo memberInfo, object declaringInstance)
        {
            MemInfo = memberInfo;
            DeclaringType = memberInfo.DeclaringType;
            DeclaringInstance = declaringInstance;
#if CPP
            if (DeclaringInstance != null)
                DeclaringInstance = DeclaringInstance.Il2CppCast(DeclaringType);
#endif
        }

        public static bool CanProcessArgs(ParameterInfo[] parameters)
        {
            foreach (var param in parameters)
            {
                var pType = param.ParameterType;

                if (pType.IsByRef && pType.HasElementType)
                    pType = pType.GetElementType();

                if (pType != null && (pType.IsPrimitive || pType == typeof(string)))
                    continue;
                else
                    return false;
            }
            return true;
        }

        public override void CreateIValue(object value, Type fallbackType)
        {
            IValue = InteractiveValue.Create(value, fallbackType);
            IValue.Owner = this;
            IValue.m_mainContentParent = this.m_rightGroup;
            IValue.m_subContentParent = this.m_subContent;
        }

        public override void UpdateValue()
        {
            if (!HasParameters || m_isEvaluating)
            {
                try
                {
#if CPP
                    if (!IsReflectionSupported())
                        throw new Exception("Type not supported with Reflection");
#endif
                    UpdateReflection();
#if CPP
                    if (IValue.Value != null)
                        IValue.Value = IValue.Value.Il2CppCast(ReflectionHelpers.GetActualType(IValue.Value));
#endif
                }
                catch (Exception e)
                {
                    ReflectionException = ReflectionHelpers.ExceptionToString(e, true);
                }
            }

            base.UpdateValue();
        }

        public abstract void UpdateReflection();

        public override void SetValue()
        {
            // no implementation for base class
        }

        public object[] ParseArguments()
        {
            if (m_arguments.Length < 1)
                return new object[0];

            var parsedArgs = new List<object>();
            for (int i = 0; i < m_arguments.Length; i++)
            {
                var input = m_argumentInput[i];
                var type = m_arguments[i].ParameterType;

                if (type.IsByRef)
                    type = type.GetElementType();

                if (!string.IsNullOrEmpty(input))
                {
                    if (type == typeof(string))
                    {
                        parsedArgs.Add(input);
                        continue;
                    }
                    else
                    {
                        try
                        {
                            var arg = type.GetMethod("Parse", new Type[] { typeof(string) })
                                          .Invoke(null, new object[] { input });

                            parsedArgs.Add(arg);
                            continue;
                        }
                        catch
                        {
                            ExplorerCore.Log($"Could not parse input '{input}' for argument #{i} '{m_arguments[i].Name}' ({type.FullName})");
                        }
                    }
                }

                // No input, see if there is a default value.
                if (m_arguments[i].IsOptional)
                {
                    parsedArgs.Add(m_arguments[i].DefaultValue);
                    continue;
                }

                // Try add a null arg I guess
                parsedArgs.Add(null);
            }

            return parsedArgs.ToArray();
        }

        private bool GetCanWrite()
        {
            if (MemInfo is FieldInfo fi)
                m_canWrite = !(fi.IsLiteral && !fi.IsInitOnly);
            else if (MemInfo is PropertyInfo pi)
                m_canWrite = pi.CanWrite;
            else
                m_canWrite = false;

            return (bool)m_canWrite;
        }

        private string GetRichTextName()
        {
            return m_richTextName = UISyntaxHighlight.ParseFullSyntax(MemInfo.DeclaringType, false, MemInfo);
        }

#if CPP
        internal bool IsReflectionSupported()
        {
            try
            {
                var baseType = ReflectionHelpers.GetActualType(IValue.Value) ?? IValue.FallbackType;

                var gArgs = baseType.GetGenericArguments();
                if (gArgs.Length < 1)
                    return true;

                foreach (var arg in gArgs)
                {
                    if (!Check(arg))
                        return false;
                }

                return true;

                bool Check(Type type)
                {
                    if (!typeof(Il2CppSystem.Object).IsAssignableFrom(type))
                        return true;

                    if (!ReflectionHelpers.Il2CppTypeNotNull(type, out IntPtr ptr))
                        return false;

                    return Il2CppSystem.Type.internal_from_handle(IL2CPP.il2cpp_class_get_type(ptr)) is Il2CppSystem.Type;
                }
            }
            catch
            {
                return false;
            }
        }
#endif

        #region UI 

        internal float GetMemberLabelWidth(RectTransform scrollRect)
        {
            var textGenSettings = m_memLabelText.GetGenerationSettings(m_topRowRect.rect.size);
            textGenSettings.scaleFactor = InputFieldScroller.canvasScaler.scaleFactor;

            var textGen = m_memLabelText.cachedTextGeneratorForLayout;
            float preferredWidth = textGen.GetPreferredWidth(RichTextName, textGenSettings);

            float max = scrollRect.rect.width * 0.4f;

            if (preferredWidth > max) preferredWidth = max;

            return preferredWidth < 125f ? 125f : preferredWidth;
        }

        internal void SetWidths(float labelWidth, float valueWidth)
        {
            m_leftLayout.preferredWidth = labelWidth;
            m_rightLayout.preferredWidth = valueWidth;
        }

        internal RectTransform m_topRowRect;
        internal Text m_memLabelText;
        internal GameObject m_leftGroup;
        internal LayoutElement m_leftLayout;
        internal GameObject m_rightGroup;
        internal LayoutElement m_rightLayout;

        internal override void ConstructUI()
        {
            base.ConstructUI();

            var topGroupObj = UIFactory.CreateHorizontalGroup(m_mainContent, new Color(1, 1, 1, 0));
            m_topRowRect = topGroupObj.GetComponent<RectTransform>();
            var topLayout = topGroupObj.AddComponent<LayoutElement>();
            topLayout.minHeight = 25;
            topLayout.flexibleHeight = 0;
            topLayout.minWidth = 300;
            topLayout.flexibleWidth = 5000;
            var topGroup = topGroupObj.GetComponent<HorizontalLayoutGroup>();
            topGroup.childForceExpandHeight = false;
            topGroup.childForceExpandWidth = false;
            topGroup.childControlHeight = true;
            topGroup.childControlWidth = true;
            topGroup.spacing = 10;
            topGroup.padding.left = 3;
            topGroup.padding.right = 3;
            topGroup.padding.top = 0;
            topGroup.padding.bottom = 0;

            // left group

            m_leftGroup = UIFactory.CreateHorizontalGroup(topGroupObj, new Color(1, 1, 1, 0));
            var leftLayout = m_leftGroup.AddComponent<LayoutElement>();
            leftLayout.minHeight = 25;
            leftLayout.flexibleHeight = 0;
            leftLayout.minWidth = 125;
            leftLayout.flexibleWidth = 200;
            var leftGroup = m_leftGroup.GetComponent<HorizontalLayoutGroup>();
            leftGroup.childForceExpandHeight = true;
            leftGroup.childForceExpandWidth = false;
            leftGroup.childControlHeight = true;
            leftGroup.childControlWidth = true;
            leftGroup.spacing = 4;

            // member label

            var labelObj = UIFactory.CreateLabel(m_leftGroup, TextAnchor.MiddleLeft);
            var leftRect = labelObj.GetComponent<RectTransform>();
            leftRect.anchorMin = Vector2.zero;
            leftRect.anchorMax = Vector2.one;
            leftRect.offsetMin = Vector2.zero;
            leftRect.offsetMax = Vector2.zero;
            leftRect.sizeDelta = Vector2.zero;
            m_leftLayout = labelObj.AddComponent<LayoutElement>();
            m_leftLayout.preferredWidth = 125;
            m_leftLayout.minHeight = 25;
            m_leftLayout.flexibleHeight = 100;
            var labelFitter = labelObj.AddComponent<ContentSizeFitter>();
            labelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            labelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            m_memLabelText = labelObj.GetComponent<Text>();
            m_memLabelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            m_memLabelText.text = this.RichTextName;

            // right group

            m_rightGroup = UIFactory.CreateVerticalGroup(topGroupObj, new Color(1, 1, 1, 0));
            m_rightLayout = m_rightGroup.AddComponent<LayoutElement>();
            m_rightLayout.minHeight = 25;
            m_rightLayout.flexibleHeight = 480;
            m_rightLayout.minWidth = 125;
            m_rightLayout.flexibleWidth = 5000;
            var rightGroup = m_rightGroup.GetComponent<VerticalLayoutGroup>();
            rightGroup.childForceExpandHeight = true;
            rightGroup.childForceExpandWidth = false;
            rightGroup.childControlHeight = true;
            rightGroup.childControlWidth = true;
            rightGroup.spacing = 2;
            rightGroup.padding.top = 4;
            rightGroup.padding.bottom = 2;

            ConstructArgInput(out GameObject argsHolder);

            ConstructEvaluateButtons(argsHolder);

            IValue.m_mainContentParent = m_rightGroup;
        }

        internal void ConstructArgInput(out GameObject argsHolder)
        {
            argsHolder = null;

            if (HasParameters)
            {
                argsHolder = UIFactory.CreateVerticalGroup(m_rightGroup, new Color(1, 1, 1, 0));
                var argsGroup = argsHolder.GetComponent<VerticalLayoutGroup>();
                argsGroup.spacing = 4;

                if (this is CacheMethod cm && cm.GenericArgs.Length > 0)
                {
                    cm.ConstructGenericArgInput(argsHolder);
                }

                // todo normal args

                if (m_arguments.Length > 0)
                {
                    var titleObj = UIFactory.CreateLabel(argsHolder, TextAnchor.MiddleLeft);
                    var titleText = titleObj.GetComponent<Text>();
                    titleText.text = "<b>Arguments:</b>";

                    for (int i = 0; i < m_arguments.Length; i++)
                    {
                        AddArgRow(i, argsHolder);
                    }
                }

                argsHolder.SetActive(false);
            }
        }

        internal void AddArgRow(int i, GameObject parent)
        {
            var arg = m_arguments[i];

            var rowObj = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var rowLayout = rowObj.AddComponent<LayoutElement>();
            rowLayout.minHeight = 25;
            rowLayout.flexibleWidth = 5000;
            var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childForceExpandHeight = false;
            rowGroup.childForceExpandWidth = true;
            rowGroup.spacing = 4;

            var argLabelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            var argLabelLayout = argLabelObj.AddComponent<LayoutElement>();
            argLabelLayout.minHeight = 25;
            var argText = argLabelObj.GetComponent<Text>();
            var argTypeTxt = UISyntaxHighlight.ParseFullSyntax(arg.ParameterType, false);
            argText.text = $"{argTypeTxt} <color={UISyntaxHighlight.Local}>{arg.Name}</color>";

            var argInputObj = UIFactory.CreateInputField(rowObj, 14, (int)TextAnchor.MiddleLeft, 1);
            var argInputLayout = argInputObj.AddComponent<LayoutElement>();
            argInputLayout.flexibleWidth = 1200;
            argInputLayout.preferredWidth = 150;
            argInputLayout.minWidth = 20;
            argInputLayout.minHeight = 25;
            argInputLayout.flexibleHeight = 0;
            //argInputLayout.layoutPriority = 2;

            var argInput = argInputObj.GetComponent<InputField>();
            argInput.onValueChanged.AddListener((string val) => { m_argumentInput[i] = val; });

            if (arg.IsOptional)
            {
                var phInput = argInput.placeholder.GetComponent<Text>();
                phInput.text = " = " + arg.DefaultValue?.ToString() ?? "null";
            }
        }

        internal void ConstructEvaluateButtons(GameObject argsHolder)
        {
            if (HasParameters)
            {
                var evalGroupObj = UIFactory.CreateHorizontalGroup(m_rightGroup, new Color(1, 1, 1, 0));
                var evalGroup = evalGroupObj.GetComponent<HorizontalLayoutGroup>();
                evalGroup.childForceExpandWidth = false;
                evalGroup.childForceExpandHeight = false;
                evalGroup.spacing = 5;
                var evalGroupLayout = evalGroupObj.AddComponent<LayoutElement>();
                evalGroupLayout.minHeight = 25;
                evalGroupLayout.flexibleHeight = 0;
                evalGroupLayout.flexibleWidth = 5000;

                var evalButtonObj = UIFactory.CreateButton(evalGroupObj, new Color(0.4f, 0.4f, 0.4f));
                var evalLayout = evalButtonObj.AddComponent<LayoutElement>();
                evalLayout.minWidth = 100;
                evalLayout.minHeight = 22;
                evalLayout.flexibleWidth = 0;
                var evalText = evalButtonObj.GetComponentInChildren<Text>();
                evalText.text = $"Evaluate ({ParamCount})";

                var evalButton = evalButtonObj.GetComponent<Button>();
                var colors = evalButton.colors;
                colors.highlightedColor = new Color(0.4f, 0.7f, 0.4f);
                evalButton.colors = colors;

                var cancelButtonObj = UIFactory.CreateButton(evalGroupObj, new Color(0.3f, 0.3f, 0.3f));
                var cancelLayout = cancelButtonObj.AddComponent<LayoutElement>();
                cancelLayout.minWidth = 100;
                cancelLayout.minHeight = 22;
                cancelLayout.flexibleWidth = 0;
                var cancelText = cancelButtonObj.GetComponentInChildren<Text>();
                cancelText.text = "Close";

                cancelButtonObj.SetActive(false);

                evalButton.onClick.AddListener(() =>
                {
                    if (!m_isEvaluating)
                    {
                        argsHolder.SetActive(true);
                        m_isEvaluating = true;
                        evalText.text = "Evaluate";
                        colors = evalButton.colors;
                        colors.normalColor = new Color(0.3f, 0.6f, 0.3f);
                        evalButton.colors = colors;

                        cancelButtonObj.SetActive(true);
                    }
                    else
                    {
                        if (this is CacheMethod cm)
                            cm.Evaluate();
                        else
                            UpdateValue();
                    }
                });

                var cancelButton = cancelButtonObj.GetComponent<Button>();
                cancelButton.onClick.AddListener(() =>
                {
                    cancelButtonObj.SetActive(false);
                    argsHolder.SetActive(false);
                    m_isEvaluating = false;

                    evalText.text = $"Evaluate ({ParamCount})";
                    colors = evalButton.colors;
                    colors.normalColor = new Color(0.4f, 0.4f, 0.4f);
                    evalButton.colors = colors;
                });
            }
            else if (this is CacheMethod)
            {
                // simple method evaluate button

                var evalButtonObj = UIFactory.CreateButton(m_rightGroup, new Color(0.3f, 0.6f, 0.3f));
                var evalLayout = evalButtonObj.AddComponent<LayoutElement>();
                evalLayout.minWidth = 100;
                evalLayout.minHeight = 22;
                evalLayout.flexibleWidth = 0;
                var evalText = evalButtonObj.GetComponentInChildren<Text>();
                evalText.text = "Evaluate";

                var evalButton = evalButtonObj.GetComponent<Button>();
                var colors = evalButton.colors;
                colors.highlightedColor = new Color(0.4f, 0.7f, 0.4f);
                evalButton.colors = colors;

                evalButton.onClick.AddListener(OnMainEvaluateButton);
                void OnMainEvaluateButton()
                {
                    (this as CacheMethod).Evaluate();
                }
            }
        }

        #endregion
    }
}
