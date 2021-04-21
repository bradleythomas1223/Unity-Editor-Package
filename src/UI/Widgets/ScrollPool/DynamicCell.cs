﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UnityExplorer.UI.Widgets
{
    public class DynamicCell : ICell
    {
        public DynamicCell(GameObject uiRoot)
        {
            this.uiRoot = uiRoot;
            m_enabled = uiRoot.activeSelf;
        }

        public bool Enabled => m_enabled;
        private bool m_enabled;

        public GameObject uiRoot;
        public InputField input;

        public void Disable()
        {
            m_enabled = false;
            uiRoot.SetActive(false);
        }

        public void Enable()
        {
            m_enabled = true;
            uiRoot.SetActive(true);
        }

        public static RectTransform CreatePrototypeCell(GameObject parent)
        {
            var prototype = UIFactory.CreateVerticalGroup(parent, "PrototypeCell", true, true, true, true, 0, new Vector4(1, 0, 0, 0),
                new Color(0.15f, 0.15f, 0.15f), TextAnchor.MiddleCenter);
            var rect = prototype.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(100, 30);
            //UIFactory.SetLayoutElement(prototype, minWidth: 100, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 9999);

            prototype.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            prototype.SetActive(false);

            return rect;
        }
    }
}
