﻿//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using UnityEngine.UI;
//using UnityExplorer.UI.CacheObject;
//using UnityExplorer.UI.CacheObject.Views;
//using UnityExplorer.UI.Utility;
//using UnityExplorer.UI.Widgets;

//namespace UnityExplorer.UI.Inspectors
//{
//    // TODO
//    // - set fallback type from generic arguments
//    // - handle setting through IList
//    // - handle il2cpp lists

//    public class ListInspector : InspectorBase, IPoolDataSource<CacheListEntryCell>, ICacheObjectController
//    {
//        // TODO
//        public CacheObjectBase ParentCacheObject { get; set; }

//        public Type TargetType { get; private set; }

//        public bool CanWrite => RefIList != null && !RefIList.IsReadOnly;

//        public Type EntryType;
//        public IEnumerable RefIEnumerable;
//        public IList RefIList;

//        public int ItemCount => values.Count;
//        private readonly List<object> values = new List<object>();
//        private readonly List<CacheListEntry> cachedEntries = new List<CacheListEntry>();

//        public ScrollPool<CacheListEntryCell> ListScrollPool { get; private set; }

//        public LayoutElement ScrollPoolLayout;
//        public Text TopLabel;

//        public override void OnBorrowedFromPool(object target)
//        {
//            base.OnBorrowedFromPool(target);

//            var type = target.GetActualType();
//            if (type.IsGenericType)
//                EntryType = type.GetGenericArguments()[0];
//            else
//                EntryType = typeof(object);

//            // Set tab

//            Tab.TabText.text = $"[L] {SignatureHighlighter.ParseFullType(type, false)}";

//            // Get cache entries

//            CacheEntries(target);

//            TopLabel.text = $"{cachedEntries.Count} entries";

//            this.ListScrollPool.Refresh(true, false);
//        }

//        public override void OnReturnToPool()
//        {
//            base.OnReturnToPool();

//            values.Clear();

//            foreach (var entry in cachedEntries)
//                entry.ReleasePooledObjects();

//            cachedEntries.Clear();
//        }

//        public override void Update()
//        {
//            // ...
//        }

//        protected override void OnCloseClicked()
//        {
//            InspectorManager.ReleaseInspector(this);
//        }

//        private void CacheEntries(object value)
//        {
//            RefIEnumerable = value as IEnumerable;
//            RefIList = value as IList;

//            if (RefIEnumerable == null)
//            {
//                // todo il2cpp ...?
//            }

//            values.Clear();
//            int idx = 0;
//            foreach (var entry in RefIEnumerable)
//            {
//                values.Add(entry);

//                // If list count increased, create new cache entries
//                CacheListEntry cache;
//                if (idx >= cachedEntries.Count)
//                {
//                    cache = new CacheListEntry();
//                    cache.SetListOwner(this, idx);
//                    cachedEntries.Add(cache);
//                }
//                else
//                    cache = cachedEntries[idx];

//                cache.Initialize(this.EntryType);
//                cache.SetValueFromSource(entry);
//                idx++;
//            }

//            // Remove excess cached entries if list count decreased
//            if (cachedEntries.Count > values.Count)
//            {
//                for (int i = cachedEntries.Count - 1; i >= values.Count; i--)
//                {
//                    var cache = cachedEntries[i];
//                    if (cache.CellView != null)
//                    {
//                        cache.CellView.Occupant = null;
//                        cache.CellView = null;
//                    }
//                    cache.ReleasePooledObjects();
//                    cachedEntries.RemoveAt(i);
//                }
//            }
//        }

//        // List entry scroll pool

//        public void OnCellBorrowed(CacheListEntryCell cell)
//        {
//            cell.ListOwner = this;
//        }

//        public void SetCell(CacheListEntryCell cell, int index)
//        {
//            if (index < 0 || index >= cachedEntries.Count)
//            {
//                if (cell.Occupant != null)
//                {
//                    cell.Occupant.CellView = null;
//                    cell.Occupant = null;
//                }

//                cell.Disable();
//                return;
//            }

//            var entry = cachedEntries[index];

//            if (entry != cell.Occupant)
//            {
//                if (cell.Occupant != null)
//                {
//                    cell.Occupant.HideIValue();
//                    cell.Occupant.CellView = null;
//                    cell.Occupant = null;
//                }

//                cell.Occupant = entry;
//                entry.CellView = cell;
//            }

//            entry.SetCell(cell);
//        }

//        public override GameObject CreateContent(GameObject parent)
//        {
//            UIRoot = UIFactory.CreateVerticalGroup(parent, "InteractiveList", true, true, true, true, 2, new Vector4(4, 4, 4, 4),
//                new Color(0.05f, 0.05f, 0.05f));

//            UIFactory.SetLayoutElement(UIRoot, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 600);

//            // Entries label

//            TopLabel = UIFactory.CreateLabel(UIRoot, "EntryLabel", "not set", TextAnchor.MiddleLeft);

//            // entry scroll pool

//            ListScrollPool = UIFactory.CreateScrollPool<CacheListEntryCell>(UIRoot, "EntryList", out GameObject scrollObj,
//                out GameObject _, new Color(0.09f, 0.09f, 0.09f));
//            UIFactory.SetLayoutElement(scrollObj, minHeight: 25, flexibleHeight: 9999);
//            ListScrollPool.Initialize(this);
//            ScrollPoolLayout = scrollObj.GetComponent<LayoutElement>();

//            return UIRoot;
//        }
//    }
//}
