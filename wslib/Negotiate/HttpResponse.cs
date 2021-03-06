﻿using System;
using System.Collections.Generic;
using System.Net;

namespace wslib.Negotiate
{
    public class HttpResponse
    {
        public HttpStatusCode Status = HttpStatusCode.SwitchingProtocols;
        public readonly Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}