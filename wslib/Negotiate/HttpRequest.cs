﻿using System;
using System.Collections.Generic;

namespace wslib.Negotiate
{
    public class HttpRequest
    {
        public Uri RequestUri { get; set; }
        public Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
    }
}