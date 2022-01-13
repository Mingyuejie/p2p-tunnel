using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Timers;

namespace common
{
    public static class Helper
    {
        public static string SeparatorString = ",";
        public static char SeparatorChar = ',';

        public static string GetStackTrace()
        {
            List<string> strs = new();
            StackTrace trace = new(true);
            foreach (StackFrame frame in trace.GetFrames())
            {
                strs.Add($"文件:{frame.GetFileName()},方法:{frame.GetMethod().Name},行:{frame.GetFileLineNumber()},列:{frame.GetFileColumnNumber()}");

            }
            return string.Join("\r\n", strs);
        }
    }
}
