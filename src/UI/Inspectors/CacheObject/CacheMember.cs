﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.Inspectors.CacheObject.Views;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Inspectors.CacheObject
{
    public abstract class CacheMember : CacheObjectBase
    {
        //public ReflectionInspector ParentInspector { get; internal set; }
        //public bool AutoUpdateWanted { get; internal set; }
        
        public abstract Type DeclaringType { get; }
        public string NameForFiltering { get; protected set; }

        public override bool HasArguments => Arguments?.Length > 0 || GenericArguments.Length > 0;
        public ParameterInfo[] Arguments { get; protected set; } = new ParameterInfo[0];
        public Type[] GenericArguments { get; protected set; } = new Type[0];
        public EvaluateWidget Evaluator { get; protected set; }
        public bool Evaluating => Evaluator != null && Evaluator.UIRoot.activeSelf;
        
        public virtual void SetInspectorOwner(ReflectionInspector inspector, MemberInfo member)
        {
            this.Owner = inspector;
            this.NameLabelText = SignatureHighlighter.ParseFullSyntax(member.DeclaringType, false, member);
            this.NameForFiltering = $"{member.DeclaringType.Name}.{member.Name}";
        }

        public override void ReleasePooledObjects()
        {
            base.ReleasePooledObjects();

            if (this.Evaluator != null)
            {
                this.Evaluator.OnReturnToPool();
                Pool<EvaluateWidget>.Return(this.Evaluator);
                this.Evaluator = null;
            }
        }

        internal override void HidePooledObjects()
        {
            base.HidePooledObjects();

            if (this.Evaluator != null)
                this.Evaluator.UIRoot.transform.SetParent(Pool<EvaluateWidget>.Instance.InactiveHolder.transform, false);
        }

        protected abstract object TryEvaluate();

        protected abstract void TrySetValue(object value);

        public void EvaluateAndSetCell()
        {
            Evaluate();
            if (CellView != null)
                SetCell(CellView);
        }

        /// <summary>
        /// Evaluate when first shown (if ShouldAutoEvaluate), or else when Evaluate button is clicked, or auto-updated.
        /// </summary>
        public void Evaluate()
        {
            SetValueFromSource(TryEvaluate());
        }

        public override void SetUserValue(object value)
        {
            // TODO unbox string, cast, etc

            TrySetValue(value);

            Evaluate();
        }

        protected override void SetValueState(CacheObjectCell cell, ValueStateArgs args)
        {
            base.SetValueState(cell, args);

            //var memCell = cell as CacheMemberCell;
            //memCell.UpdateToggle.gameObject.SetActive(ShouldAutoEvaluate);
        }

        private static readonly Color evalEnabledColor = new Color(0.15f, 0.25f, 0.15f);
        private static readonly Color evalDisabledColor = new Color(0.15f, 0.15f, 0.15f);

        protected override bool SetCellEvaluateState(CacheObjectCell objectcell)
        {
            var cell = objectcell as CacheMemberCell;

            cell.EvaluateHolder.SetActive(!ShouldAutoEvaluate);
            if (!ShouldAutoEvaluate)
            {
                //cell.UpdateToggle.gameObject.SetActive(false);
                cell.EvaluateButton.Button.gameObject.SetActive(true);
                if (HasArguments)
                {
                    if (!Evaluating)
                        cell.EvaluateButton.ButtonText.text = $"Evaluate ({Arguments.Length + GenericArguments.Length})";
                    else
                    {
                        cell.EvaluateButton.ButtonText.text = "Hide";
                        Evaluator.UIRoot.transform.SetParent(cell.EvaluateHolder.transform, false);
                        RuntimeProvider.Instance.SetColorBlock(cell.EvaluateButton.Button, evalEnabledColor, evalEnabledColor * 1.3f);
                    }
                }
                else
                    cell.EvaluateButton.ButtonText.text = "Evaluate";

                if (!Evaluating)
                    RuntimeProvider.Instance.SetColorBlock(cell.EvaluateButton.Button, evalDisabledColor, evalDisabledColor * 1.3f);
            }
            //else
            //{
            //    cell.UpdateToggle.gameObject.SetActive(true);
            //    cell.UpdateToggle.isOn = AutoUpdateWanted;
            //}

            if (State == ValueState.NotEvaluated && !ShouldAutoEvaluate)
            {
                // todo evaluate buttons etc
                SetValueState(cell, ValueStateArgs.Default);
                cell.RefreshSubcontentButton();

                return true;
            }

            if (State == ValueState.NotEvaluated)
                Evaluate();

            return false;
        }


        public void OnEvaluateClicked()
        {
            if (!HasArguments)
            {
                EvaluateAndSetCell();
            }
            else
            {
                if (Evaluator == null)
                {
                    this.Evaluator = Pool<EvaluateWidget>.Borrow();
                    Evaluator.OnBorrowedFromPool(this);
                    Evaluator.UIRoot.transform.SetParent((CellView as CacheMemberCell).EvaluateHolder.transform, false);
                    SetCellEvaluateState(CellView);
                }
                else
                {
                    if (Evaluator.UIRoot.activeSelf)
                        Evaluator.UIRoot.SetActive(false);
                    else
                        Evaluator.UIRoot.SetActive(true);

                    SetCellEvaluateState(CellView);
                }
            }
        }


        #region Cache Member Util

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

        public static List<CacheMember> GetCacheMembers(object inspectorTarget, Type _type, ReflectionInspector _inspector)
        {
            var list = new List<CacheMember>();
            var cachedSigs = new HashSet<string>();

            var types = ReflectionUtility.GetAllBaseTypes(_type);

            var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            if (!_inspector.StaticOnly)
                flags |= BindingFlags.Instance;

            var infos = new List<MemberInfo>();

            foreach (var declaringType in types)
            {
                var target = inspectorTarget;
                if (!_inspector.StaticOnly)
                    target = target.TryCast(declaringType);

                infos.Clear();
                infos.AddRange(declaringType.GetProperties(flags));
                infos.AddRange(declaringType.GetFields(flags));
                infos.AddRange(declaringType.GetMethods(flags));

                foreach (var member in infos)
                {
                    if (member.DeclaringType != declaringType)
                        continue;
                    TryCacheMember(member, list, cachedSigs, declaringType, _inspector);
                }
            }

            var typeList = types.ToList();

            var sorted = new List<CacheMember>();
            sorted.AddRange(list.Where(it => it is CacheProperty)
                             .OrderBy(it => typeList.IndexOf(it.DeclaringType))
                             .ThenBy(it => it.NameForFiltering));
            sorted.AddRange(list.Where(it => it is CacheField)
                             .OrderBy(it => typeList.IndexOf(it.DeclaringType))
                             .ThenBy(it => it.NameForFiltering));
            sorted.AddRange(list.Where(it => it is CacheMethod)
                             .OrderBy(it => typeList.IndexOf(it.DeclaringType))
                             .ThenBy(it => it.NameForFiltering));

            return sorted;
        }

        private static void TryCacheMember(MemberInfo member, List<CacheMember> list, HashSet<string> cachedSigs, 
            Type declaringType, ReflectionInspector _inspector, bool ignoreMethodBlacklist = false)
        {
            try
            {
                var sig = GetSig(member);

                if (IsBlacklisted(sig))
                    return;

                //ExplorerCore.Log($"Trying to cache member {sig}...");
                //ExplorerCore.Log(member.DeclaringType.FullName + "." + member.Name);

                CacheMember cached;
                Type returnType;
                switch (member.MemberType)
                {
                    case MemberTypes.Method:
                        {
                            var mi = member as MethodInfo;
                            if (!ignoreMethodBlacklist && IsBlacklisted(mi))
                                return;

                            var args = mi.GetParameters();
                            if (!CanProcessArgs(args))
                                return;

                            sig += AppendArgsToSig(args);
                            if (cachedSigs.Contains(sig))
                                return;

                            cached = new CacheMethod() { MethodInfo = mi };
                            returnType = mi.ReturnType;
                            break;
                        }

                    case MemberTypes.Property:
                        {
                            var pi = member as PropertyInfo;

                            var args = pi.GetIndexParameters();
                            if (!CanProcessArgs(args))
                                return;

                            if (!pi.CanRead && pi.CanWrite)
                            {
                                // write-only property, cache the set method instead.
                                var setMethod = pi.GetSetMethod(true);
                                if (setMethod != null)
                                    TryCacheMember(setMethod, list, cachedSigs, declaringType, _inspector, true);
                                return;
                            }

                            sig += AppendArgsToSig(args);
                            if (cachedSigs.Contains(sig))
                                return;

                            cached = new CacheProperty() { PropertyInfo = pi };
                            returnType = pi.PropertyType;
                            break;
                        }

                    case MemberTypes.Field:
                        {
                            var fi = member as FieldInfo;
                            cached = new CacheField() { FieldInfo = fi };
                            returnType = fi.FieldType;
                            break;
                        }

                    default: return;
                }

                cachedSigs.Add(sig);

                //cached.Initialize(_inspector, declaringType, member, returnType);
                cached.SetFallbackType(returnType);
                cached.SetInspectorOwner(_inspector, member);

                list.Add(cached);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"Exception caching member {member.DeclaringType.FullName}.{member.Name}!");
                ExplorerCore.Log(e.ToString());
            }
        }

        internal static string GetSig(MemberInfo member) => $"{member.DeclaringType.Name}.{member.Name}";

        internal static string AppendArgsToSig(ParameterInfo[] args)
        {
            string ret = " (";
            foreach (var param in args)
                ret += $"{param.ParameterType.Name} {param.Name}, ";
            ret += ")";
            return ret;
        }

        // Blacklists
        private static readonly HashSet<string> bl_typeAndMember = new HashSet<string>
        {
            // these cause a crash in IL2CPP
#if CPP
            "Type.DeclaringMethod",
            "Rigidbody2D.Cast",
            "Collider2D.Cast",
            "Collider2D.Raycast",
            "Texture2D.SetPixelDataImpl",
            "Camera.CalculateProjectionMatrixFromPhysicalProperties",
#endif
            // These were deprecated a long time ago, still show up in some games for some reason
            "MonoBehaviour.allowPrefabModeInPlayMode",
            "MonoBehaviour.runInEditMode",
            "Component.animation",
            "Component.audio",
            "Component.camera",
            "Component.collider",
            "Component.collider2D",
            "Component.constantForce",
            "Component.hingeJoint",
            "Component.light",
            "Component.networkView",
            "Component.particleSystem",
            "Component.renderer",
            "Component.rigidbody",
            "Component.rigidbody2D",
        };
        private static readonly HashSet<string> bl_methodNameStartsWith = new HashSet<string>
        {
            // these are redundant
            "get_",
            "set_",
        };

        internal static bool IsBlacklisted(string sig) => bl_typeAndMember.Any(it => sig.Contains(it));
        internal static bool IsBlacklisted(MethodInfo method) => bl_methodNameStartsWith.Any(it => method.Name.StartsWith(it));

        #endregion


    }
}
