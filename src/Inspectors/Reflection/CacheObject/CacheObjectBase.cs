﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityExplorer.UI;
using UnityExplorer.UI.Shared;
using UnityExplorer.Helpers;
using UnityEngine.UI;

namespace UnityExplorer.Inspectors.Reflection
{
    public class CacheObjectBase
    {
        public InteractiveValue IValue;

        public virtual bool CanWrite => false;
        public virtual bool HasParameters => false;
        public virtual bool IsMember => false;
        public virtual bool HasEvaluated => true;

        // TODO
        public virtual void InitValue(object value, Type valueType)
        {
            if (valueType == null && value == null)
            {
                return;
            }

            // TEMP
            IValue = new InteractiveValue
            {
                OwnerCacheObject = this,
                ValueType = ReflectionHelpers.GetActualType(value) ?? valueType,
            };
            UpdateValue();
        }

        public virtual void Enable()
        {
            if (!m_constructedUI)
            {
                ConstructUI();
                UpdateValue();
            }

            m_mainContent.SetActive(true);
        }

        public virtual void Disable()
        {
            m_mainContent.SetActive(false);
        }

        public virtual void UpdateValue()
        {
            IValue.UpdateValue();
        }

        public virtual void SetValue() => throw new NotImplementedException();

        #region UI CONSTRUCTION

        internal bool m_constructedUI;
        internal GameObject m_parentContent;
        internal GameObject m_mainContent;

        // Make base UI holder for CacheObject, this doesnt actually display anything.
        internal virtual void ConstructUI()
        {
            m_constructedUI = true;

            m_mainContent = UIFactory.CreateVerticalGroup(m_parentContent, new Color(0.1f, 0.1f, 0.1f));
            var rowGroup = m_mainContent.GetComponent<VerticalLayoutGroup>();
            rowGroup.childForceExpandWidth = true;
            rowGroup.childControlWidth = true;
            rowGroup.childForceExpandHeight = false;
            rowGroup.childControlHeight = true;
            var rowLayout = m_mainContent.AddComponent<LayoutElement>();
            rowLayout.minHeight = 25;
            rowLayout.flexibleHeight = 500;
            rowLayout.minWidth = 200;
            rowLayout.flexibleWidth = 5000;
        }

        #endregion

    }
}
