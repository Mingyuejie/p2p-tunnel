using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyboard.hook
{
    internal class Demo
    {
        KeyboardHook hook = new KeyboardHook();

        public void Lock()
        {
            hook.Start();

            //控制台 消息泵
            //Hook.tagMSG msg = new Hook.tagMSG();
            //while (Hook.GetMessage(ref msg, 0, 0, 0) > 0)
            //{
            //    if (!isStart)
            //    {
            //        break;
            //    }
            //}
        }

        public void UnLock()
        {
            hook.Dispose();
        }
    }
}
