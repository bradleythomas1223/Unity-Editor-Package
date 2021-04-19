﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    public class InspectorTest : UIPanel
    {
        public override string Name => "Inspector";

        //public SimpleListSource<Component> ComponentList;

        public override void Update()
        {

        }

        public override void LoadSaveData()
        {
            ApplySaveData(ConfigManager.GameObjectInspectorData.Value);
        }

        public override void SaveToConfigManager()
        {
            ConfigManager.GameObjectInspectorData.Value = this.ToSaveData();
        }

        public override void OnFinishResize(RectTransform panel)
        {
            base.OnFinishResize(panel);
            RuntimeProvider.Instance.StartCoroutine(DelayedRefresh(panel));
        }

        private float previousRectHeight;

        private IEnumerator DelayedRefresh(RectTransform obj)
        {
            yield return null;

            if (obj.rect.height != previousRectHeight)
            {
                // height changed, hard refresh required.
                previousRectHeight = obj.rect.height;
                //scrollPool.ReloadData();
            }

            scrollPool.RefreshCells(true);
        }

        public override void SetDefaultPosAndAnchors()
        {
            mainPanelRect.localPosition = Vector2.zero; 
            mainPanelRect.pivot = new Vector2(0.5f, 0.5f);
            mainPanelRect.anchorMin = new Vector2(0.5f, 0);
            mainPanelRect.anchorMax = new Vector2(0.5f, 1);
            mainPanelRect.offsetMin = new Vector2(mainPanelRect.offsetMin.x, 100);  // bottom
            mainPanelRect.offsetMax = new Vector2(mainPanelRect.offsetMax.x, -50); // top
            mainPanelRect.sizeDelta = new Vector2(700f, mainPanelRect.sizeDelta.y);
            mainPanelRect.anchoredPosition = new Vector2(-150, 0);
        }

        private ScrollPool scrollPool;

        public override void ConstructPanelContent()
        {
            //UIRoot.GetComponent<Mask>().enabled = false;

            // temp debug
            scrollPool = UIFactory.CreateScrollPool(content, "Test", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(scrollContent, flexibleHeight: 9999);

            var test = new DynamicListTest(scrollPool, this);
            test.Init();

            var prototype = DynamicCell.CreatePrototypeCell(scrollContent);
            scrollPool.PrototypeCell = prototype.GetComponent<RectTransform>();

            dummyContentHolder = new GameObject("DummyHolder");
            dummyContentHolder.SetActive(false);

            GameObject.DontDestroyOnLoad(dummyContentHolder);
            for (int i = 0; i < 100; i++)
            {
                dummyContents.Add(CreateDummyContent());
            }

            previousRectHeight = mainPanelRect.rect.height;
        }

        internal GameObject dummyContentHolder;
        internal readonly List<GameObject> dummyContents = new List<GameObject>();

        private GameObject CreateDummyContent()
        {
            var obj = UIFactory.CreateVerticalGroup(dummyContentHolder, "Content", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            //UIFactory.SetLayoutElement(obj, minHeight: 25, flexibleHeight: 9999);
            obj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var label = UIFactory.CreateLabel(obj, "label", "Dummy " + dummyContents.Count, TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(label.gameObject, minHeight: 25, flexibleHeight: 0);

            //var input = UIFactory.CreateSrollInputField(obj, "input2", "...", out InputFieldScroller inputScroller);
            //UIFactory.SetLayoutElement(input, minHeight: 50, flexibleHeight: 9999);
            //input.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var inputObj = UIFactory.CreateInputField(obj, "input", "...", out var inputField);
            UIFactory.SetLayoutElement(inputObj, minHeight: 25, flexibleHeight: 9999);
            inputObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            inputField.lineType = InputField.LineType.MultiLineNewline;

            int numLines = UnityEngine.Random.Range(0, 10);
            inputField.text = "This field has " + numLines + " lines";
            for (int i = 0; i < numLines; i++)
                inputField.text += "\r\n";

            return obj;
        }
    }

    public class DynamicListTest : IPoolDataSource
    {
        internal ScrollPool Scroller;
        internal InspectorTest Inspector;

        public DynamicListTest(ScrollPool scroller, InspectorTest inspector) 
        {
            Scroller = scroller;
            Inspector = inspector;
        }

        public int ItemCount => Inspector.dummyContents.Count;

        public void Init()
        {

            Scroller.DataSource = this;
            Scroller.Initialize(this);
        }

        public ICell CreateCell(RectTransform cellTransform) => new DynamicCell(cellTransform.gameObject);

        public void SetCell(ICell icell, int index)
        {
            if (index < 0 || index >= ItemCount)
            {
                icell.Disable();
                return;
            }

            var root = (icell as DynamicCell).uiRoot;
            var content = Inspector.dummyContents[index];

            if (content.transform.parent.ReferenceEqual(root.transform))
                return;

            if (root.transform.Find("Content") is Transform existing)
                existing.transform.SetParent(Inspector.dummyContentHolder.transform, false);

            content.transform.SetParent(root.transform, false);
        }
    }
}
