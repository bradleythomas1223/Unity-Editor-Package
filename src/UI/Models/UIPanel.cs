﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Input;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Models
{
    public abstract class UIPanel : UIBehaviourModel
    {
        // STATIC

        public static event Action OnPanelsReordered;

        public static void UpdateFocus()
        {
            if (InputManager.GetMouseButtonDown(0) || InputManager.GetMouseButtonDown(1))
            {
                int count = UIManager.PanelHolder.transform.childCount;
                var mousePos = InputManager.MousePosition;
                for (int i = count - 1; i >= 0; i--)
                {
                    var transform = UIManager.PanelHolder.transform.GetChild(i);
                    if (transformToPanelDict.TryGetValue(transform.GetInstanceID(), out UIPanel panel))
                    {
                        var pos = panel.mainPanelRect.InverseTransformPoint(mousePos);
                        if (panel.Enabled && panel.mainPanelRect.rect.Contains(pos))
                        {
                            if (transform.GetSiblingIndex() != count - 1)
                            {
                                transform.SetAsLastSibling();
                                OnPanelsReordered?.Invoke();
                            }
                            break;
                        }
                    }
                }
            }
        }

        private static readonly List<UIPanel> instances = new List<UIPanel>();
        private static readonly Dictionary<int, UIPanel> transformToPanelDict = new Dictionary<int, UIPanel>();

        // INSTANCE

        public UIPanel()
        {
            instances.Add(this);
        }

        public abstract UIManager.Panels PanelType { get; }

        public abstract string Name { get; }

        public virtual bool ShouldSaveActiveState => true;

        public override GameObject UIRoot => uiRoot;
        protected GameObject uiRoot;
        protected RectTransform mainPanelRect;
        public GameObject content;
        public PanelDragger dragger;

        public abstract void ConstructPanelContent();

        public virtual void OnFinishResize(RectTransform panel)
        {
            SaveToConfigManager();
        }

        public virtual void OnFinishDrag(RectTransform panel)
        {
            SaveToConfigManager();
        }

        public override void Destroy()
        {
            instances.Remove(this);
            base.Destroy();
        }

        public override void ConstructUI(GameObject parent)
        {
            // create core canvas 
            uiRoot = UIFactory.CreatePanel(Name, out GameObject panelContent);
            mainPanelRect = this.uiRoot.GetComponent<RectTransform>();
            content = panelContent;

            transformToPanelDict.Add(this.uiRoot.transform.GetInstanceID(), this);

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(this.uiRoot, true, true, true, true, 0, 0, 0, 0, 0, TextAnchor.UpperLeft);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(content, true, true, true, true, 2, 2, 2, 2, 2, TextAnchor.UpperLeft);

            // always apply default pos and anchors (save data may only be partial)
            SetDefaultPosAndAnchors();

            // Title bar
            var titleGroup = UIFactory.CreateHorizontalGroup(content, "TitleBar", false, true, true, true, 2,
                new Vector4(2, 2, 2, 2), new Color(0.09f, 0.09f, 0.09f));
            UIFactory.SetLayoutElement(titleGroup, minHeight: 25, flexibleHeight: 0);

            // Title text

            var titleTxt = UIFactory.CreateLabel(titleGroup, "TitleBar", Name, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(titleTxt.gameObject, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // close button

            var closeBtn = UIFactory.CreateButton(titleGroup, "CloseButton", "X", () =>
            {
                UIManager.SetPanelActive(this.PanelType, false);
            });
            UIFactory.SetLayoutElement(closeBtn.gameObject, minHeight: 25, minWidth: 25, flexibleWidth: 0);
            RuntimeProvider.Instance.SetColorBlock(closeBtn, new Color(0.63f, 0.32f, 0.31f),
                new Color(0.81f, 0.25f, 0.2f), new Color(0.6f, 0.18f, 0.16f));

            // Panel dragger

            dragger = new PanelDragger(titleTxt.GetComponent<RectTransform>(), mainPanelRect);
            dragger.OnFinishResize += OnFinishResize;
            dragger.OnFinishDrag += OnFinishDrag;

            // content (abstract)

            ConstructPanelContent();

            // apply panel save data or revert to default
            try
            {
                LoadSaveData();
                dragger.OnEndResize();
            }
            catch (Exception ex)
            {
                ExplorerCore.Log($"Exception loading panel save data: {ex}");
                SetDefaultPosAndAnchors();
            }

            // simple listener for saving enabled state
            this.OnToggleEnabled += (bool val) =>
            {
                SaveToConfigManager();
            };
        }
        
        // SAVE DATA

        public abstract void SaveToConfigManager();

        public abstract void SetDefaultPosAndAnchors();

        public abstract void LoadSaveData();

        public virtual string ToSaveData()
        {
            try
            {
                return $"{(ShouldSaveActiveState ? Enabled : false)}" +
                $"|{mainPanelRect.RectAnchorsToString()}" +
                $"|{mainPanelRect.RectPositionToString()}";
            }
            catch
            {
                return "";
            }
        }

        public virtual void ApplySaveData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            var split = data.Split('|');

            try
            {
                mainPanelRect.SetAnchorsFromString(split[1]);
                mainPanelRect.SetPositionFromString(split[2]);
                UIManager.SetPanelActive(this.PanelType, bool.Parse(split[0]));
            }
            catch 
            {
                ExplorerCore.LogWarning("Invalid or corrupt panel save data! Restoring to default.");
                SetDefaultPosAndAnchors();
            }
        }
    }

    public static class RectSaveExtensions
    {
        #region WINDOW ANCHORS / POSITION HELPERS

        // Window Anchors helpers

        internal static CultureInfo _enCulture = new CultureInfo("en-US");

        internal static string RectAnchorsToString(this RectTransform rect)
        {
            if (!rect)
                throw new ArgumentNullException("rect");

            return string.Format(_enCulture, "{0},{1},{2},{3}", new object[]
            {
                rect.anchorMin.x,
                rect.anchorMin.y,
                rect.anchorMax.x,
                rect.anchorMax.y
            });
        }

        internal static void SetAnchorsFromString(this RectTransform panel, string stringAnchors)
        {
            if (string.IsNullOrEmpty(stringAnchors))
                throw new ArgumentNullException("stringAnchors");

            var split = stringAnchors.Split(',');

            if (split.Length != 4)
                throw new Exception($"stringAnchors split is unexpected length: {split.Length}");

            Vector4 anchors;
            anchors.x = float.Parse(split[0], _enCulture);
            anchors.y = float.Parse(split[1], _enCulture);
            anchors.z = float.Parse(split[2], _enCulture);
            anchors.w = float.Parse(split[3], _enCulture);

            panel.anchorMin = new Vector2(anchors.x, anchors.y);
            panel.anchorMax = new Vector2(anchors.z, anchors.w);
        }

        internal static string RectPositionToString(this RectTransform rect)
        {
            if (!rect)
                throw new ArgumentNullException("rect");

            return string.Format(_enCulture, "{0},{1}", new object[]
            {
                rect.localPosition.x, rect.localPosition.y
            });
        }

        internal static void SetPositionFromString(this RectTransform rect, string stringPosition)
        {
            var split = stringPosition.Split(',');

            if (split.Length != 2)
                throw new Exception($"stringPosition split is unexpected length: {split.Length}");

            Vector3 vector = rect.localPosition;
            vector.x = float.Parse(split[0], _enCulture);
            vector.y = float.Parse(split[1], _enCulture);
            rect.localPosition = vector;
        }

        #endregion
    }
}
