﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace We7
{
    public static class We7Helper
    {
        public static string ConvertTextToHtml(string text)
        {
            text.Replace("<", "&lt;");
            text.Replace(">", "&gt;");
            text.Replace("'", "\"");
            text.Replace(" ", "&nbsp;");
            text.Replace("\r\n", "<br/>");
            text.Replace("\r", "<br/>");
            text.Replace("\n", "<br/>");
            text.Replace("\"", "&quot;");

            return text;
        }
    }
}