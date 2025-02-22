﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.model
{
    public class CommonTaskResponseInfo<T>
    {
        public string ErrorMsg { get; set; } = string.Empty;
        public T Data { get; set; } = default;
    }
}
