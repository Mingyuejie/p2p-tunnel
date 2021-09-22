using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    /// <summary>
    /// 简单发布订阅，取代 “事件”
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimplePushSubHandler<T>
    {
        List<Action<T>> actions = new List<Action<T>>();

        public void Sub(Action<T> action)
        {
            if (!actions.Contains(action))
            {
                actions.Add(action);
            }
        }

        public void Push(T data)
        {
            for (int i = 0, len = actions.Count; i < len; i++)
            {
                actions[i].Invoke(data);
            }
        }

        public void Remove(Action<T> action)
        {
            actions.Remove(action);
        }
    }
}
