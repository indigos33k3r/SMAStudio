﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Language.Snippets
{
    public interface ISnippetsCollection
    {
        IList<CodeSnippet> Snippets { get; set; }
    }
}