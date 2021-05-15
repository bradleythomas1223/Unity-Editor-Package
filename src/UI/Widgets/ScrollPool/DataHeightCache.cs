﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.Widgets
{
    public class DataViewInfo
    {
        public int dataIndex;
        public float height, startPosition;
        public int normalizedSpread;

        public static implicit operator float(DataViewInfo it) => it.height;
    }

    public class DataHeightCache<T> where T : ICell
    {
        private ScrollPool<T> ScrollPool { get; }

        public DataHeightCache(ScrollPool<T> scrollPool)
        {
            ScrollPool = scrollPool;
        }

        private readonly List<DataViewInfo> heightCache = new List<DataViewInfo>();

        public DataViewInfo this[int index]
        {
            get => heightCache[index];
            set => SetIndex(index, value);
        }

        public int Count => heightCache.Count;

        public float TotalHeight => totalHeight;
        private float totalHeight;

        public float DefaultHeight => m_defaultHeight ?? (float)(m_defaultHeight = ScrollPool.PrototypeHeight);
        private float? m_defaultHeight;

        /// <summary>
        /// Lookup table for "which data index first appears at this position"<br/>
        /// Index: DefaultHeight * index from top of data<br/>
        /// Value: the first data index at this position<br/>
        /// </summary>
        private readonly List<int> rangeCache = new List<int>();

        /// <summary>Same as GetRangeIndexOfPosition, except this rounds up to the next division if there was remainder from the previous cell.</summary>
        private int GetRangeCeilingOfPosition(float position) => (int)Math.Ceiling((decimal)position / (decimal)DefaultHeight);

        /// <summary>Get the first range (division of DefaultHeight) which the position appears in.</summary>
        private int GetRangeFloorOfPosition(float position) => (int)Math.Floor((decimal)position / (decimal)DefaultHeight);

        /// <summary>Get the data index at the specified position of the total height cache.</summary>
        public int GetFirstDataIndexAtPosition(float desiredHeight) => GetFirstDataIndexAtPosition(desiredHeight, out _);

        /// <summary>Get the data index and DataViewInfo at the specified position of the total height cache.</summary>
        public int GetFirstDataIndexAtPosition(float desiredHeight, out DataViewInfo cache)
        {
            cache = default;

            if (!heightCache.Any())
                return 0;

            int rangeIndex = GetRangeFloorOfPosition(desiredHeight);

            // probably shouldnt happen but just in case
            if (rangeIndex < 0)
                return 0;
            if (rangeIndex >= rangeCache.Count)
            {
                int idx = ScrollPool.DataSource.ItemCount - 1;
                cache = heightCache[idx];
                return idx;
            }

            int dataIndex = rangeCache[rangeIndex];
            cache = heightCache[dataIndex];

            // if the DataViewInfo is outdated, need to rebuild
            int expectedMin = GetRangeCeilingOfPosition(cache.startPosition);
            int expectedMax = expectedMin + cache.normalizedSpread - 1;
            if (rangeIndex < expectedMin || rangeIndex > expectedMax)
            {
                RecalculateStartPositions(Math.Max(dataIndex, expectedMax));

                rangeIndex = GetRangeFloorOfPosition(desiredHeight);
                dataIndex = rangeCache[rangeIndex];
                cache = heightCache[dataIndex];
            }

            return dataIndex;
        }

        /// <summary>
        /// Get the spread of the height, starting from the start position.<br/><br/>
        /// The "spread" begins at the start of the next interval of the DefaultHeight, then increases for
        /// every interval beyond that.
        /// </summary>
        private int GetRangeSpread(float startPosition, float height)
        {
            // get the remainder of the start position divided by min height
            float rem = startPosition % DefaultHeight;

            // if there is a remainder, this means the previous cell started in  our first cell and
            // they take priority, so reduce our height by (minHeight - remainder) to account for that.
            // We need to fill that gap and reach the next cell before we take priority.
            if (rem != 0.0f)
                height -= (DefaultHeight - rem);

            return (int)Math.Ceiling((decimal)height / (decimal)DefaultHeight);
        }

        /// <summary>Append a data index to the cache with the provided height value.</summary>
        public void Add(float value)
        {
            value = (float)Math.Floor(value);
            value = Math.Max(DefaultHeight, value);

            int spread = GetRangeSpread(totalHeight, value);

            heightCache.Add(new DataViewInfo()
            {
                height = value,
                startPosition = TotalHeight,
                normalizedSpread = spread,
            });

            int dataIdx = heightCache.Count - 1;
            for (int i = 0; i < spread; i++)
                rangeCache.Add(dataIdx);

            totalHeight += value;
        }

        /// <summary>Remove the last (highest count) index from the height cache.</summary>
        public void RemoveLast()
        {
            if (!heightCache.Any())
                return;

            var val = heightCache[heightCache.Count - 1];
            totalHeight -= val;            
            heightCache.RemoveAt(heightCache.Count - 1);

            int idx = heightCache.Count;
            while (rangeCache.Count > 0 && rangeCache[rangeCache.Count - 1] == idx)
                rangeCache.RemoveAt(rangeCache.Count - 1);
        }

        /// <summary>Set a given data index with the specified value.</summary>
        public void SetIndex(int dataIndex, float height)
        {
            height = (float)Math.Floor(height);
            height = Math.Max(DefaultHeight, height);

            // If the index being set is beyond the DataSource item count, prune and return.
            if (dataIndex >= ScrollPool.DataSource.ItemCount)
            {
                while (heightCache.Count > dataIndex)
                    RemoveLast();
                return;
            }

            // If the data index exceeds our cache count, fill the gap.
            // This is done by the ScrollPool when the DataSource sets its initial count, or the count increases.
            if (dataIndex >= heightCache.Count)
            {
                while (dataIndex > heightCache.Count)
                    Add(DefaultHeight);
                Add(height);
                return;
            }

            // We are actually updating an index. First, update the height and the totalHeight.
            var cache = heightCache[dataIndex];
            if (cache.height != height)
            {
                var diff = height - cache.height;
                totalHeight += diff;
                cache.height = height;
            }

            // update our start position using the previous cell (if it exists)
            if (dataIndex > 0)
            {
                var prev = heightCache[dataIndex - 1];
                cache.startPosition = prev.startPosition + prev.height;
            }

            // Get the normalized range index (actually ceiling) and spread based on our start position and height
            int rangeIndex = GetRangeCeilingOfPosition(cache.startPosition);
            int spread = GetRangeSpread(cache.startPosition, height);

            // If the previous item in the range cache is not the previous data index, there is a gap.
            if (dataIndex > 0 && rangeCache[rangeIndex - 1] != (dataIndex - 1))
            {
                // Recalculate start positions up to this index. The gap could be anywhere before here.
                RecalculateStartPositions(dataIndex + 1);
                // Get the range index and spread again after rebuilding
                rangeIndex = GetRangeCeilingOfPosition(cache.startPosition);
                spread = GetRangeSpread(cache.startPosition, height);
            }

            if (rangeCache.Count <= rangeIndex || rangeCache[rangeIndex] != dataIndex)
                throw new Exception("ScrollPool data height cache is corrupt or invalid, rebuild failed!");

            if (spread != cache.normalizedSpread)
            {
                int spreadDiff = spread - cache.normalizedSpread;
                cache.normalizedSpread = spread;

                SetSpread(dataIndex, rangeIndex, spreadDiff);
            }
        }

        private void SetSpread(int dataIndex, int rangeIndex, int spreadDiff)
        {
            if (spreadDiff > 0)
            {
                while (rangeCache[rangeIndex] == dataIndex && spreadDiff > 0)
                {
                    rangeCache.Insert(rangeIndex, dataIndex);
                    spreadDiff--;
                }
            }
            else
            {
                while (rangeCache[rangeIndex] == dataIndex && spreadDiff < 0)
                {
                    rangeCache.RemoveAt(rangeIndex);
                    spreadDiff++;
                }
                //for (int i = 0; i < -spreadDiff; i++)
                //    rangeCache.RemoveAt(rangeIndex);
            }
        }

        private void RecalculateStartPositions(int toIndex)
        {
            if (heightCache.Count <= 1)
                return;

            DataViewInfo cache;
            DataViewInfo prev = null;
            for (int i = 0; i <= toIndex && i < heightCache.Count; i++)
            {
                cache = heightCache[i];

                if (prev != null)
                    cache.startPosition = prev.startPosition + prev.height;
                else
                    cache.startPosition = 0;

                var origSpread = cache.normalizedSpread;
                cache.normalizedSpread = GetRangeSpread(cache.startPosition, cache.height);
                if (cache.normalizedSpread != origSpread)
                    SetSpread(i, GetRangeCeilingOfPosition(cache.startPosition), cache.normalizedSpread - origSpread);

                prev = cache;
            }
        }

        //private void HardRebuildRanges()
        //{
        //    var tempList = new List<float>();
        //    for (int i = 0; i < heightCache.Count; i++)
        //        tempList.Add(heightCache[i]);
        //
        //    heightCache.Clear();
        //    rangeCache.Clear();
        //    totalHeight = 0;
        //
        //    for (int i = 0; i < tempList.Count; i++)
        //        SetIndex(i, tempList[i]);
        //}
    }
}
