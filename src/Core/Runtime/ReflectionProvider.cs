﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.Core.Runtime
{
    public abstract class ReflectionProvider
    {
        public static ReflectionProvider Instance;

        public ReflectionProvider()
        {
            Instance = this;
        }

        public abstract Type GetActualType(object obj);

        public abstract object Cast(object obj, Type castTo);

        public abstract bool IsAssignableFrom(Type toAssignTo, Type toAssignFrom);

        public abstract bool IsReflectionSupported(Type type);

        public abstract string ProcessTypeNameInString(Type type, string theString, ref string typeName);

        public abstract bool LoadModule(string module);
    }
}
