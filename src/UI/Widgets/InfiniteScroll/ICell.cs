﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.UI.Widgets
{
    public interface ICell
    {
        bool Enabled { get; }

        void Enable();
        void Disable();
    }
}
