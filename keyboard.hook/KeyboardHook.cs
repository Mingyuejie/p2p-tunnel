using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace keyboard.hook
{
    /// <summary>
    ///      控制台程序没有消息泵，需要勾一勾消息
    ///      Hook.tagMSG msg = new Hook.tagMSG();
    ///      while (Hook.GetMessage(ref msg, 0, 0, 0) > 0)
    ///      {
    ///      }
    ///
    /// </summary>
    public class KeyboardHook : IDisposable
    {
        public delegate int HookProc(int nCode, int wParam, IntPtr lParam);
        static int hHook = 0;
        public const int WH_KEYBOARD_LL = 13;
        HookProc KeyBoardHookProcedure;
        [StructLayout(LayoutKind.Sequential)]
        public class KeyBoardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
        [DllImport("user32.dll")]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(int idHook);
        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string name);
        public void Start()
        {
            // 安装键盘钩子 
            if (hHook == 0)
            {
                KeyBoardHookProcedure = new HookProc(KeyBoardHookProc);
                hHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyBoardHookProcedure, GetModuleHandle(null), 0);
                //如果设置钩子失败. 
                if (hHook == 0)
                    Close();
                else
                {
                    try
                    {
                        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                        if (key == null)//如果该项不存在的话，则创建该项
                            key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
                        key.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                        key.Close();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
        public void Close()
        {
            bool retKeyboard = true;
            if (hHook != 0)
            {
                retKeyboard = UnhookWindowsHookEx(hHook);
                hHook = 0;
            }
            //如果去掉钩子失败. 
            //if (!retKeyboard) throw new Exception("UnhookWindowsHookEx failed.");
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                if (key != null)
                {
                    key.DeleteValue("DisableTaskMgr", false);
                    key.Close();
                }
            }
            catch (Exception)
            {
            }
        }
        public static int KeyBoardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KeyBoardHookStruct kbh = (KeyBoardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyBoardHookStruct));

                if (kbh.vkCode == 91) // 截获左win(开始菜单键) 
                    return 1;
                if (kbh.vkCode == 92)// 截获右win 
                    return 1;
                if (kbh.vkCode == (int)Keys.Escape && (int)Control.ModifierKeys == (int)Keys.Control) //截获Ctrl+Esc 
                    return 1;
                if (kbh.vkCode == (int)Keys.F4 && (int)Control.ModifierKeys == (int)Keys.Alt) //截获alt+f4 
                    return 1;
                if (kbh.vkCode == (int)Keys.Tab && (int)Control.ModifierKeys == (int)Keys.Alt) //截获alt+tab 
                    return 1;
                if (kbh.vkCode == (int)Keys.Escape && (int)Control.ModifierKeys == (int)Keys.Control + (int)Keys.Shift) //截获Ctrl+Shift+Esc 
                    return 1;
                if (kbh.vkCode == (int)Keys.Space && (int)Control.ModifierKeys == (int)Keys.Alt) //截获alt+空格 
                    return 1;
                if (kbh.vkCode == 241)                  //截获F1 
                    return 1; if (kbh.vkCode == (int)Keys.Control && kbh.vkCode == (int)Keys.Alt && kbh.vkCode == (int)Keys.Delete)
                    return 1;
                if ((int)Control.ModifierKeys == (int)Keys.Control + (int)Keys.Alt + (int)Keys.Delete)      //截获Ctrl+Alt+Delete 
                    return 1;
                if ((int)Control.ModifierKeys == (int)Keys.Control + (int)Keys.Shift)      //截获Ctrl+Shift 
                    return 1;
            }
            return CallNextHookEx(hHook, nCode, wParam, lParam);
        }
        #region IDisposable 成员
        public void Dispose()
        {
            Close();
        }

        public struct tagMSG
        {
            public int hwnd;
            public uint message;
            public int wParam;
            public long lParam;
            public uint time;
            public int pt;
        }
        [DllImport("user32.dll")]
        public static extern int GetMessage(ref tagMSG lpMsg, int a, int hwnd, int wMsgFilterMax);
        #endregion
    }
}
