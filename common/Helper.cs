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
                t.Stop();
                action();
                t.Start();
            });//到达时间的时候执行事件；
            t.AutoReset = true;//设置是执行一次（false）还是一直执行(true)；
            t.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；
            t.Start(); //启动定时器

            setTimeoutCache.TryAdd(id, t);

            return id;
        }


        public static long GetSequence(IPAddress ip, int port)
        {
            byte[] byts = ip.GetAddressBytes();
            byte[] byts1 = BitConverter.GetBytes(port);

            byte[] byts2 = new byte[byts.Length + byts1.Length];

            Array.Copy(byts, 0, byts2, 0, byts.Length);
            Array.Copy(byts1, 0, byts2, 4, byts1.Length);

            return BitConverter.ToInt64(byts2);
        }

        public static long GetSequence(string ip, int port)
        {
            return GetSequence(IPAddress.Parse(ip), port);
        }

        public static long GetSequence(IPEndPoint address)
        {
            return GetSequence(address.Address, address.Port);
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
                int port = rd.Next(22000, 56000);
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

        public static List<string> GetLocalIpAddress(string netType)
        {
            string hostName = Dns.GetHostName();    //获取主机名称  
            IPAddress[] addresses = Dns.GetHostAddresses(hostName); //解析主机IP地址  

            List<string> IPList = new();
            if (netType == string.Empty)
            {
                for (int i = 0; i < addresses.Length; i++)
                {
                    IPList.Add(addresses[i].ToString());
                }
            }
            else
            {
                //AddressFamily.InterNetwork表示此IP为IPv4,
                //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                for (int i = 0; i < addresses.Length; i++)
                {
                    if (addresses[i].AddressFamily.ToString() == netType)
                    {
                        IPList.Add(addresses[i].ToString());
                    }
                }
            }
            return IPList;
        }

        public static IPAddress GetDomainIp(string domain)
        {
            IPHostEntry hostinfo = Dns.GetHostEntry(domain);
            IPAddress[] aryIP = hostinfo.AddressList;
            return aryIP[0];
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

        public static string[] GetLocalIps()
        {
            IPHostEntry IpEntry = Dns.GetHostEntry(Dns.GetHostName());

            return IpEntry.AddressList.Where(c => c.AddressFamily == AddressFamily.InterNetwork).Select(c => c.ToString()).Reverse().ToArray();
        }


        public static string GetMd5Hash(string input)
        {
            MD5CryptoServiceProvider md5Hasher = new();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            StringBuilder sBuilder = new();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }


        public static string JsonSerializer<T>(T t)
        {
            return System.Text.Json.JsonSerializer.Serialize(t, options: new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All)
            });

        }
        public static T DeJsonSerializer<T>(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, options: new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
                PropertyNameCaseInsensitive = true
            });
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

        public static List<IPAddress> GetActiveIp()
        {
            List<IPAddress> IPAddressCollection = new(0);
            //IPAddress[] Collection = Dns.GetHostAddresses(Dns.GetHostName());
            NetworkInterface[] NetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface MIB2Interface in NetworkInterfaces)
            {
                IPInterfaceProperties IPProperties = MIB2Interface.GetIPProperties();
                UnicastIPAddressInformationCollection UnicastAddresses = IPProperties.UnicastAddresses;
                if (UnicastAddresses.Count > 0 && IPProperties.DhcpServerAddresses.Count > 0)
                {
                    foreach (UnicastIPAddressInformation Unicast in UnicastAddresses)
                    {
                        if (Unicast.Address.AddressFamily != AddressFamily.InterNetworkV6)
                        {
                            if (IPAddressCollection.IndexOf(Unicast.Address) < 0)
                            {
                                IPAddressCollection.Add(Unicast.Address);
                            }
                        }
                    }
                }
            }
            return IPAddressCollection;
        }



        [DllImport("IpHlpApi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern int GetIpNetTable(IntPtr pIpNetTable, [MarshalAs(UnmanagedType.U4)] ref int pdwSize, bool bOrder);

        public static Dictionary<IPAddress, PhysicalAddress> GetAllDevicesOnLAN()
        {
            Dictionary<IPAddress, PhysicalAddress> all = new()
            {
                // Add this PC to the list...
                { GetIPAddress(), GetMacAddress() }
            };
            int spaceForNetTable = 0;
            // Get the space needed
            // We do that by requesting the table, but not giving any space at all.
            // The return value will tell us how much we actually need.
            GetIpNetTable(IntPtr.Zero, ref spaceForNetTable, false);
            // Allocate the space
            // We use a try-finally block to ensure release.
            IntPtr rawTable = IntPtr.Zero;
            try
            {
                rawTable = Marshal.AllocCoTaskMem(spaceForNetTable);
                // Get the actual data
                int errorCode = GetIpNetTable(rawTable, ref spaceForNetTable, false);
                if (errorCode != 0)
                {
                    // Failed for some reason - can do no more here.
                    throw new Exception(string.Format(
                        "Unable to retrieve network table. Error code {0}", errorCode));
                }
                // Get the rows count
                int rowsCount = Marshal.ReadInt32(rawTable);
                IntPtr currentBuffer = new(rawTable.ToInt64() + Marshal.SizeOf(typeof(Int32)));
                // Convert the raw table to individual entries
                MIB_IPNETROW[] rows = new MIB_IPNETROW[rowsCount];
                for (int index = 0; index < rowsCount; index++)
                {
                    rows[index] = (MIB_IPNETROW)Marshal.PtrToStructure(new IntPtr(currentBuffer.ToInt64() +
                  (index * Marshal.SizeOf(typeof(MIB_IPNETROW)))
                 ),
                       typeof(MIB_IPNETROW));
                }
                // Define the dummy entries list (we can discard these)
                PhysicalAddress virtualMAC = new(new byte[] { 0, 0, 0, 0, 0, 0 });
                PhysicalAddress broadcastMAC = new(new byte[] { 255, 255, 255, 255, 255, 255 });
                foreach (MIB_IPNETROW row in rows)
                {
                    IPAddress ip = new(BitConverter.GetBytes(row.dwAddr));
                    byte[] rawMAC = new byte[] { row.mac0, row.mac1, row.mac2, row.mac3, row.mac4, row.mac5 };
                    PhysicalAddress pa = new(rawMAC);
                    if (!pa.Equals(virtualMAC) && !pa.Equals(broadcastMAC) && !IsMulticast(ip))
                    {
                        if (!all.ContainsKey(ip))
                        {
                            all.Add(ip, pa);
                        }
                    }
                }
            }
            finally
            {
                // Release the memory.
                Marshal.FreeCoTaskMem(rawTable);
            }
            return all;
        }

        public static IPAddress GetIPAddress()
        {
            String strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress[] addr = ipEntry.AddressList;
            foreach (IPAddress ip in addr)
            {
                if (!ip.IsIPv6LinkLocal)
                {
                    return ip;
                }
            }
            return addr.Length > 0 ? addr[0] : null;
        }

        public static PhysicalAddress GetMacAddress()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Only consider Ethernet network interfaces
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                    nic.OperationalStatus == OperationalStatus.Up)
                {
                    return nic.GetPhysicalAddress();
                }
            }
            return null;
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

        public static bool IsMulticast(IPAddress ip)
        {
            bool result = true;
            if (!ip.IsIPv6Multicast)
            {
                byte highIP = ip.GetAddressBytes()[0];
                if (highIP < 224 || highIP > 239)
                {
                    result = false;
                }
            }
            return result;
        }

        /*
        public static Dictionary<string, string> mimeTypes = new Dictionary<string, string> {
            {".323", "text/h323"},
            {".aaf", "application/octet-stream"},
            {".aca", "application/octet-stream"},
            {".accdb", "application/msaccess"},
            {".accde", "application/msaccess"},
            {".accdt", "application/msaccess"},{".acx", "application/internet-property-stream"},
            {".afm", "application/octet-stream"},
            {".ai", "application/postscript"},
            {".aif", "audio/x-aiff"},
            {".aifc", "audio/aiff"},
            {".aiff", "audio/aiff"},
            {".application", "application/x-ms-application"},
            {".art", "image/x-jg"},
            {".asd", "application/octet-stream"},
            {".asf", "video/x-ms-asf"},
            {".asi", "application/octet-stream"},
            {".asm", "text/plain"},{".asr", "video/x-ms-asf"},{".asx", "video/x-ms-asf"},
            {".atom", "application/atom+xml"},{".au", "audio/basic"},{".avi", "video/x-msvideo"},{".axs", "application/olescript"},
            {".bas", "text/plain"},{".bcpio", "application/x-bcpio"},{".bin", "application/octet-stream"},
            {".bmp", "image/bmp"},{".c", "text/plain"},{".cab", "application/octet-stream"},
            {".calx", "application/vnd.ms-office.calx"},{".cat", "application/vnd.ms-pki.seccat"},
            {".cdf", "application/x-cdf"},{".chm", "application/octet-stream"},{".class", "application/x-java-applet"},
            {".clp", "application/x-msclip"},{".cmx", "image/x-cmx"},{".cnf", "text/plain"},{".cod", "image/cis-cod"},
            {".cpio", "application/x-cpio"},{".cpp", "text/plain"},{".crd", "application/x-mscardfile"},
            {".crl", "application/pkix-crl"},{".crt", "application/x-x509-ca-cert"},{".csh", "application/x-csh"},
            {".css", "text/css"},{".csv", "application/octet-stream"},{".cur", "application/octet-stream"},
            {".dcr", "application/x-director"},{".deploy", "application/octet-stream"},{".der", "application/x-x509-ca-cert"},
            {".dib", "image/bmp"},{".dir", "application/x-director"},{".disco", "text/xml"},{".dll", "application/x-msdownload"},
            {".dll.config", "text/xml"},{".dlm", "text/dlm"},{".doc", "application/msword"},
            {".docm", "application/vnd.ms-word.document.macroEnabled.12"},
            {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
            {".dot", "application/msword"},{".dotm", "application/vnd.ms-word.template.macroEnabled.12"},
            {".dotx", "application/vnd.openxmlformats-officedocument.wordprocessingml.template"},
            {".dsp", "application/octet-stream"},{".dtd", "text/xml"},{".dvi", "application/x-dvi"},
            {".dwf", "drawing/x-dwf"},{".dwp", "application/octet-stream"},{".dxr", "application/x-director"},
            {".eml", "message/rfc822"},{".emz", "application/octet-stream"},{".eot", "application/octet-stream"},
            {".eps", "application/postscript"},{".etx", "text/x-setext"},{".evy", "application/envoy"},
            {".exe", "application/octet-stream"},{".exe.config", "text/xml"},{".fdf", "application/vnd.fdf"},
            {".fif", "application/fractals"},{".fla", "application/octet-stream"},{".flr", "x-world/x-vrml"},
            {".flv", "video/x-flv"},{".gif", "image/gif"},{".gtar", "application/x-gtar"},{".gz", "application/x-gzip"},
            {".h", "text/plain"},{".hdf", "application/x-hdf"},{".hdml", "text/x-hdml"},
            {".hhc", "application/x-oleobject"},{".hhk", "application/octet-stream"},
            {".hhp", "application/octet-stream"},{".hlp", "application/winhlp"},
            {".hqx", "application/mac-binhex40"},{".hta", "application/hta"},
            {".htc", "text/x-component"},{".htm", "text/html"},{".html", "text/html"},{".htt", "text/webviewhtml"},
            {".hxt", "text/html"},{".ico", "image/x-icon"},{".ics", "application/octet-stream"},{".ief", "image/ief"},
            {".iii", "application/x-iphone"},{".inf", "application/octet-stream"},{".ins", "application/x-internet-signup"},
            {".isp", "application/x-internet-signup"},{".IVF", "video/x-ivf"},{".jar", "application/java-archive"},
            {".java", "application/octet-stream"},{".jck", "application/liquidmotion"},{".jcz", "application/liquidmotion"},
            {".jfif", "image/pjpeg"},{".jpb", "application/octet-stream"},{".jpe", "image/jpeg"},{".jpeg", "image/jpeg"},
            {".jpg", "image/jpeg"},{".js", "application/x-javascript"},{".jsx", "text/jscript"},{".latex", "application/x-latex"},
            {".lit", "application/x-ms-reader"},{".lpk", "application/octet-stream"},{".lsf", "video/x-la-asf"},
            {".lsx", "video/x-la-asf"},{".lzh", "application/octet-stream"},{".m13", "application/x-msmediaview"},
            {".m14", "application/x-msmediaview"},{".m1v", "video/mpeg"},{".m3u", "audio/x-mpegurl"},
            {".man", "application/x-troff-man"},{".manifest", "application/x-ms-manifest"},{".map", "text/plain"},
            {".mdb", "application/x-msaccess"},{".mdp", "application/octet-stream"},{".me", "application/x-troff-me"},
            {".mht", "message/rfc822"},{".mhtml", "message/rfc822"},{".mid", "audio/mid"},{".midi", "audio/mid"},
            {".mix", "application/octet-stream"},{".mmf", "application/x-smaf"},{".mno", "text/xml"},
            {".mny", "application/x-msmoney"},{".mov", "video/quicktime"},{".movie", "video/x-sgi-movie"},
            {".mp2", "video/mpeg"},{".mp3", "audio/mpeg"},{".mpa", "video/mpeg"},{".mpe", "video/mpeg"},
            {".mpeg", "video/mpeg"},{".mpg", "video/mpeg"},{".mpp", "application/vnd.ms-project"},
            {".mpv2", "video/mpeg"},{".ms", "application/x-troff-ms"},{".msi", "application/octet-stream"},
            {".mso", "application/octet-stream"},{".mvb", "application/x-msmediaview"},{".mvc", "application/x-miva-compiled"},
            {".nc", "application/x-netcdf"},{".nsc", "video/x-ms-asf"},{".nws", "message/rfc822"},
            {".ocx", "application/octet-stream"},{".oda", "application/oda"},{".odc", "text/x-ms-odc"},
            {".ods", "application/oleobject"},{".one", "application/onenote"},{".onea", "application/onenote"},
            {".onetoc", "application/onenote"},{".onetoc2", "application/onenote"},{".onetmp", "application/onenote"},
            {".onepkg", "application/onenote"},{".osdx", "application/opensearchdescription+xml"},{".p10", "application/pkcs10"},
            {".p12", "application/x-pkcs12"},{".p7b", "application/x-pkcs7-certificates"},{".p7c", "application/pkcs7-mime"},
            {".p7m", "application/pkcs7-mime"},{".p7r", "application/x-pkcs7-certreqresp"},
            {".p7s", "application/pkcs7-signature"},{".pbm", "image/x-portable-bitmap"},
            {".pcx", "application/octet-stream"},{".pcz", "application/octet-stream"},{".pdf", "application/pdf"},
            {".pfb", "application/octet-stream"},{".pfm", "application/octet-stream"},{".pfx", "application/x-pkcs12"},
            {".pgm", "image/x-portable-graymap"},{".pko", "application/vnd.ms-pki.pko"},{".pma", "application/x-perfmon"},
            {".pmc", "application/x-perfmon"},{".pml", "application/x-perfmon"},{".pmr", "application/x-perfmon"},
            {".pmw", "application/x-perfmon"},{".png", "image/png"},{".pnm", "image/x-portable-anymap"},
            {".pnz", "image/png"},{".pot", "application/vnd.ms-powerpoint"},
            {".potm", "application/vnd.ms-powerpoint.template.macroEnabled.12"},
            {".potx", "application/vnd.openxmlformats-officedocument.presentationml.template"},
            {".ppam", "application/vnd.ms-powerpoint.addin.macroEnabled.12"},{".ppm", "image/x-portable-pixmap"},
            {".pps", "application/vnd.ms-powerpoint"},{".ppsm", "application/vnd.ms-powerpoint.slideshow.macroEnabled.12"},
            {".ppsx", "application/vnd.openxmlformats-officedocument.presentationml.slideshow"},
            {".ppt", "application/vnd.ms-powerpoint"},{".pptm", "application/vnd.ms-powerpoint.presentation.macroEnabled.12"},
            {".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
            {".prf", "application/pics-rules"},{".prm", "application/octet-stream"},{".prx", "application/octet-stream"},
            {".ps", "application/postscript"},{".psd", "application/octet-stream"},{".psm", "application/octet-stream"},
            {".psp", "application/octet-stream"},{".pub", "application/x-mspublisher"},{".qt", "video/quicktime"},
            {".qtl", "application/x-quicktimeplayer"},{".qxd", "application/octet-stream"},{".ra", "audio/x-pn-realaudio"},
            {".ram", "audio/x-pn-realaudio"},{".rar", "application/octet-stream"},{".ras", "image/x-cmu-raster"},
            {".rf", "image/vnd.rn-realflash"},{".rgb", "image/x-rgb"},{".rm", "application/vnd.rn-realmedia"},{".rmi", "audio/mid"},
            {".roff", "application/x-troff"},{".rpm", "audio/x-pn-realaudio-plugin"},{".rtf", "application/rtf"},{".rtx", "text/richtext"},
            {".scd", "application/x-msschedule"},{".sct", "text/scriptlet"},{".sea", "application/octet-stream"},
            {".setpay", "application/set-payment-initiation"},{".setreg", "application/set-registration-initiation"},{".sgml", "text/sgml"},
            {".sh", "application/x-sh"},{".shar", "application/x-shar"},{".sit", "application/x-stuffit"},
            {".sldm", "application/vnd.ms-powerpoint.slide.macroEnabled.12"},
            {".sldx", "application/vnd.openxmlformats-officedocument.presentationml.slide"},{".smd", "audio/x-smd"},
            {".smi", "application/octet-stream"},{".smx", "audio/x-smd"},{".smz", "audio/x-smd"},{".snd", "audio/basic"},
            {".snp", "application/octet-stream"},{".spc", "application/x-pkcs7-certificates"},{".spl", "application/futuresplash"},
            {".src", "application/x-wais-source"},{".ssm", "application/streamingmedia"},{".sst", "application/vnd.ms-pki.certstore"},
            {".stl", "application/vnd.ms-pki.stl"},{".sv4cpio", "application/x-sv4cpio"},{".sv4crc", "application/x-sv4crc"},
            {".swf", "application/x-shockwave-flash"},{".t", "application/x-troff"},{".tar", "application/x-tar"},{".tcl", "application/x-tcl"},
            {".tex", "application/x-tex"},{".texi", "application/x-texinfo"},{".texinfo", "application/x-texinfo"},
            {".tgz", "application/x-compressed"},{".thmx", "application/vnd.ms-officetheme"},
            {".thn", "application/octet-stream"},{".tif", "image/tiff"},{".tiff", "image/tiff"},{".toc", "application/octet-stream"},
            {".tr", "application/x-troff"},{".trm", "application/x-msterminal"},{".tsv", "text/tab-separated-values"},
            {".ttf", "application/octet-stream"},{".txt", "text/plain"},{".u32", "application/octet-stream"},{".uls", "text/iuls"},
            {".ustar", "application/x-ustar"},{".vbs", "text/vbscript"},{".vcf", "text/x-vcard"},{".vcs", "text/plain"},
            {".vdx", "application/vnd.ms-visio.viewer"},{".vml", "text/xml"},{".vsd", "application/vnd.visio"},
            {".vss", "application/vnd.visio"},{".vst", "application/vnd.visio"},{".vsto", "application/x-ms-vsto"},
            {".vsw", "application/vnd.visio"},{".vsx", "application/vnd.visio"},{".vtx", "application/vnd.visio"},{".wav", "audio/wav"},
            {".wax", "audio/x-ms-wax"},{".wbmp", "image/vnd.wap.wbmp"},{".wcm", "application/vnd.ms-works"},
            {".wdb", "application/vnd.ms-works"},{".wks", "application/vnd.ms-works"},{".wm", "video/x-ms-wm"},{".wma", "audio/x-ms-wma"},{".wmd", "application/x-ms-wmd"},{".wmf", "application/x-msmetafile"},{".wml", "text/vnd.wap.wml"},{".wmlc", "application/vnd.wap.wmlc"},{".wmls", "text/vnd.wap.wmlscript"},{".wmlsc", "application/vnd.wap.wmlscriptc"},{".wmp", "video/x-ms-wmp"},{".wmv", "video/x-ms-wmv"},{".wmx", "video/x-ms-wmx"},{".wmz", "application/x-ms-wmz"},{".wps", "application/vnd.ms-works"},{".wri", "application/x-mswrite"},{".wrl", "x-world/x-vrml"},{".wrz", "x-world/x-vrml"},{".wsdl", "text/xml"},{".wvx", "video/x-ms-wvx"},{".x", "application/directx"},{".xaf", "x-world/x-vrml"},{".xaml", "application/xaml+xml"},{".xap", "application/x-silverlight-app"},{".xbap", "application/x-ms-xbap"},{".xbm", "image/x-xbitmap"},{".xdr", "text/plain"},{".xla", "application/vnd.ms-excel"},{".xlam", "application/vnd.ms-excel.addin.macroEnabled.12"},{".xlc", "application/vnd.ms-excel"},{".xlm", "application/vnd.ms-excel"},{".xls", "application/vnd.ms-excel"},{".xlsb", "application/vnd.ms-excel.sheet.binary.macroEnabled.12"},{".xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12"},{".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},{".xlt", "application/vnd.ms-excel"},{".xltm", "application/vnd.ms-excel.template.macroEnabled.12"},{".xltx", "application/vnd.openxmlformats-officedocument.spreadsheetml.template"},{".xlw", "application/vnd.ms-excel"},{".xml", "text/xml"},{".xof", "x-world/x-vrml"},{".xpm", "image/x-xpixmap"},{".xps", "application/vnd.ms-xpsdocument"},{".xsd", "text/xml"},{".xsf", "text/xml"},{".xsl", "text/xml"},{".xslt", "text/xml"},{".xsn", "application/octet-stream"},{".xtp", "application/octet-stream"},{".xwd", "image/x-xwindowdump"},{".z", "application/x-compress"},{".zip", "application/x-zip-compressed" }
        };
        */

        public static string GetMD5HashFromFile(string filename)
        {
            try
            {
                //using FileStream file = new(filename, FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                //byte[] retVal = md5.ComputeHash(file);
                byte[] retVal = md5.ComputeHash(Encoding.Default.GetBytes(filename));
                // file.Close();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"{filename} MD5获取失败:{ex}");
                return string.Empty;
            }

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


        public static Encoding GetEncoding(FileStream stream, Encoding defaultEncoding)
        {
            Encoding targetEncoding = defaultEncoding;
            if (stream != null && stream.Length >= 2)
            {
                //保存文件流的前4个字节   
                byte byte1 = 0;
                byte byte2 = 0;
                byte byte3 = 0;
                byte byte4 = 0;
                //保存当前Seek位置   
                long origPos = stream.Seek(0, SeekOrigin.Begin);
                stream.Seek(0, SeekOrigin.Begin);

                int nByte = stream.ReadByte();
                byte1 = Convert.ToByte(nByte);
                byte2 = Convert.ToByte(stream.ReadByte());
                if (stream.Length >= 3)
                {
                    byte3 = Convert.ToByte(stream.ReadByte());
                }
                if (stream.Length >= 4)
                {
                    byte4 = Convert.ToByte(stream.ReadByte());
                }
                //根据文件流的前4个字节判断Encoding   
                //Unicode {0xFF, 0xFE};   
                //BE-Unicode {0xFE, 0xFF};   
                //UTF8 = {0xEF, 0xBB, 0xBF};   
                if (byte1 == 0xFE && byte2 == 0xFF)//UnicodeBe   
                {
                    targetEncoding = Encoding.BigEndianUnicode;
                }
                if (byte1 == 0xFF && byte2 == 0xFE && byte3 != 0xFF)//Unicode   
                {
                    targetEncoding = Encoding.Unicode;
                }
                if (byte1 == 0xEF && byte2 == 0xBB && byte3 == 0xBF)//UTF8   
                {
                    targetEncoding = Encoding.UTF8;
                }
                //恢复Seek位置         
                stream.Seek(origPos, SeekOrigin.Begin);
            }
            return targetEncoding;
        }

        /// <summary>
        /// 查询适合连接远程地址的合适接口地址
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <param name="socket"></param>
        /// <returns></returns>
        public static IPEndPoint RoutingInterfaceQuery(IPEndPoint remoteEndPoint, Socket socket)
        {
            SocketAddress address = remoteEndPoint.Serialize();
            byte[] remoteAddrBytes = new byte[address.Size];
            for (int i = 0; i < address.Size; i++)
            {
                remoteAddrBytes[i] = address[i];
            }
            byte[] outBytes = new byte[remoteAddrBytes.Length];
            socket.IOControl(IOControlCode.RoutingInterfaceQuery, remoteAddrBytes, outBytes);
            for (int i = 0; i < address.Size; i++)
            {
                address[i] = outBytes[i];
            }
            EndPoint ep = remoteEndPoint.Create(address);

            return (IPEndPoint)ep;
        }



        private static byte[] chunkedHeaderBytes = Encoding.ASCII.GetBytes("Transfer-Encoding: chunked");
        /// <summary>
        /// 判断是否分块传输
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static bool IsChunked(byte[] lines)
        {
            return HeaderEqual(lines, chunkedHeaderBytes);
        }

        /// <summary>
        /// \r\n\r\n结束 13 10 13 10
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static bool IsChunkedEnd(byte[] lines)
        {
            return lines.Length >= 4 && lines[^1] == 10 && lines[^2] == 13 && lines[^3] == 10 && lines[^4] == 13;
        }

        private static byte[] keepaliveHeaderBytes = Encoding.ASCII.GetBytes("Connection: keep-alive");
        /// <summary>
        /// 是否是keepalive
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static bool IsKeepAline(byte[] lines)
        {
            return HeaderEqual(lines, keepaliveHeaderBytes);
        }

        public static bool HeaderEqual(byte[] lines, byte[] headerBytes)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == 10 && lines[i - 1] == 13 && lines.Length - i >= headerBytes.Length)
                {
                    if (Enumerable.SequenceEqual(lines.Skip(i + 1).Take(headerBytes.Length), headerBytes))
                    {
                        return true;
                    }
                }

                //头结束
                if (lines[i] == 10 && lines[i - 1] == 13 && lines[i - 2] == 10 && lines[i - 3] == 13)
                {
                    return false;
                }
            }
            return false;
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
