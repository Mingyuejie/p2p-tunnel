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
        private static long setTimeoutId = 0;
        private static readonly ConcurrentDictionary<long, System.Timers.Timer> setTimeoutCache = new();

        public static string GetArg(string[] args, string name)
        {
            if (args != null)
            {
                int argsLength = args.Length;
                for (int i = 0; i < argsLength; i++)
                {
                    if (args[i] == name)
                    {
                        if (args.Length > i + 1)
                        {
                            return args[i + 1];
                        }
                    }
                }
            }
            return null;
        }

        public static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);

            return Convert.ToInt64(ts.TotalMilliseconds);
        }

        public static long GetTimeStampSec()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);

            return Convert.ToInt64(ts.TotalSeconds);
        }

        public static long SetTimeout(Action action, double interval)
        {
            _ = Interlocked.Increment(ref setTimeoutId);
            long id = setTimeoutId;

            System.Timers.Timer t = new(interval);//实例化Timer类，设置间隔时间为10000毫秒；
            t.Elapsed += new ElapsedEventHandler((object source, ElapsedEventArgs e) =>
            {
                action();
                CloseTimeout(id);

            });//到达时间的时候执行事件；
            t.AutoReset = false;//设置是执行一次（false）还是一直执行(true)；
            t.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；
            t.Start(); //启动定时器

            setTimeoutCache.TryAdd(id, t);

            return id;

        }
        public static void CloseTimeout(long id)
        {
            if (setTimeoutCache.TryRemove(id, out System.Timers.Timer t) && t != null)
            {
                t.Close();
            }
        }
        public static long SetInterval(Action action, double interval)
        {
            _ = Interlocked.Increment(ref setTimeoutId);
            long id = setTimeoutId;

            System.Timers.Timer t = new(interval);//实例化Timer类，设置间隔时间为10000毫秒；
            t.Elapsed += new ElapsedEventHandler((object source, ElapsedEventArgs e) =>
            {
                action();
            });//到达时间的时候执行事件；
            t.AutoReset = true;//设置是执行一次（false）还是一直执行(true)；
            t.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；
            t.Start(); //启动定时器

            setTimeoutCache.TryAdd(id, t);

            return id;
        }

        public static void Sleep(int milliseconds)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            while (stopWatch.ElapsedMilliseconds < milliseconds)
            {
                int timeout = milliseconds - (int)stopWatch.ElapsedMilliseconds;
                Thread.Sleep(timeout >= 0 ? timeout : 0);
            }
            stopWatch.Stop();
        }

        /// <summary>
        /// 获取路由层数，自己与外网距离几个网关，用于发送一个对方网络收不到没有回应的数据包
        /// </summary>
        /// <returns></returns>
        public static int GetRouteLevel()
        {
            List<string> starts = new() { "10.", "100.", "192.168.", "172." };
            IEnumerable<IPAddress> list = GetTraceRoute("www.baidu.com");
            for (int i = 0; i < list.Count(); i++)
            {
                string ip = list.ElementAt(i).ToString();
                if (ip.StartsWith(starts[0]) || ip.StartsWith(starts[1]) || ip.StartsWith(starts[2]))
                {

                }
                else
                {
                    return i;
                }
            }
            return -1;
        }
        public static IEnumerable<IPAddress> GetTraceRoute(string hostNameOrAddress)
        {
            return GetTraceRoute(hostNameOrAddress, 1);
        }
        private static IEnumerable<IPAddress> GetTraceRoute(string hostNameOrAddress, int ttl)
        {
            Ping pinger = new();
            // 创建PingOptions对象
            PingOptions pingerOptions = new(ttl, true);
            int timeout = 100;
            byte[] buffer = Encoding.ASCII.GetBytes("11");
            // 创建PingReply对象
            // 发送ping命令
            PingReply reply = pinger.Send(hostNameOrAddress, timeout, buffer, pingerOptions);

            // 处理返回结果
            List<IPAddress> result = new();
            if (reply.Status == IPStatus.Success)
            {
                result.Add(reply.Address);
            }
            else if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut)
            {
                //增加当前这个访问地址
                if (reply.Status == IPStatus.TtlExpired)
                {
                    result.Add(reply.Address);
                }

                if (ttl <= 10)
                {
                    //递归访问下一个地址
                    IEnumerable<IPAddress> tempResult = GetTraceRoute(hostNameOrAddress, ttl + 1);
                    result.AddRange(tempResult);
                }
            }
            else
            {
                //失败
            }
            return result;
        }

        public static int GetRandomPort(List<int> usedPorts = null)
        {

            List<int> allPorts = GetUsedPort();
            if (usedPorts != null)
            {
                allPorts.AddRange(usedPorts);
            }
            Random rd = new();
            while (true)
            {
                int port = rd.Next(32000, 56000);
                if (!allPorts.Contains(port))
                {
                    return port;
                }
            }
        }
        public static List<int> GetUsedPort()
        {
            //获取本地计算机的网络连接和通信统计数据的信息
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            //返回本地计算机上的所有Tcp监听程序
            IPEndPoint[] ipsTCP = ipGlobalProperties.GetActiveTcpListeners();

            //返回本地计算机上的所有UDP监听程序
            IPEndPoint[] ipsUDP = ipGlobalProperties.GetActiveUdpListeners();

            //返回本地计算机上的Internet协议版本4(IPV4 传输控制协议(TCP)连接的信息。
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            List<int> allPorts = new();
            foreach (IPEndPoint ep in ipsTCP)
            {
                allPorts.Add(ep.Port);
            }

            foreach (IPEndPoint ep in ipsUDP)
            {
                allPorts.Add(ep.Port);
            }

            foreach (TcpConnectionInformation conn in tcpConnInfoArray)
            {
                allPorts.Add(conn.LocalEndPoint.Port);
            }
            return allPorts;

        }

        public static IPAddress GetDomainIp(string domain)
        {
            if (domain[0] >= 49 && domain[0] <= 50)
            {
                return IPAddress.Parse(domain);
            }
            else
            {
                return Dns.GetHostEntry(domain).AddressList[0];
            }
        }


        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);
        public static void FlushMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
            }
        }

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

        [DllImport("ws2_32.dll")]
        private static extern int inet_addr(string cp);
        [DllImport("IPHLPAPI.dll")]
        private static extern int SendARP(Int32 DestIP, Int32 SrcIP, ref Int64 pMacAddr, ref Int32 PhyAddrLen);
        public static string GetMacAddress(string hostip)
        {
            string mac;
            try
            {
                int ldest = inet_addr(hostip);
                long macinfo = new();
                int len = 6;
                _ = SendARP(ldest, 0, ref macinfo, ref len);
                string tmpMac = Convert.ToString(macinfo, 16).PadLeft(12, '0');
                mac = tmpMac.Substring(0, 2).ToUpper();
                for (int i = 2; i < tmpMac.Length; i += 2)
                {
                    mac = tmpMac.Substring(i, 2).ToUpper() + ":" + mac;
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
            return mac;
        }

        private static string[] fileSizeFormatArray = new string[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "BB" };
        public static string FileSizeFormat(long size)
        {
            float s = size;
            for (int i = 0; i < fileSizeFormatArray.Length; i++)
            {
                if (s < 1024)
                {
                    return $"{s:0.##}{fileSizeFormatArray[i]}";
                }
                s /= 1024;
            }
            return string.Empty;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    struct MIB_IPNETROW
    {
        [MarshalAs(UnmanagedType.U4)]
        public int dwIndex;
        [MarshalAs(UnmanagedType.U4)]
        public int dwPhysAddrLen;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac0;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac1;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac2;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac3;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac4;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac5;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac6;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac7;
        [MarshalAs(UnmanagedType.U4)]
        public int dwAddr;
        [MarshalAs(UnmanagedType.U4)]
        public int dwType;
    }


}
