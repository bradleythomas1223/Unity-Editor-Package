﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer
{
    interface IExpandHeight
    {
        bool IsExpanded { get; set; }

        float WhiteSpace { get; set; }
        float ButtonWidthOffset { get; set; }
    }
}
