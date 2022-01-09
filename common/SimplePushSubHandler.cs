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
        List<Func<T, Task>> funcs = new List<Func<T, Task>>();

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


        public void SubAsync(Func<T, Task> action)
        {
            if (!funcs.Contains(action))
            {
                funcs.Add(action);
            }
        }
        public async Task PushAsync(T data)
        {
            for (int i = 0, len = funcs.Count; i < len; i++)
            {
                await funcs[i].Invoke(data);
            }
        }
        public void RemoveAsync(Func<T, Task> action)
        {
            funcs.Remove(action);
        }
    }
}
