using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public class SimpleObjectPool<T> where T : new()
    {
        private ConcurrentStack<T> objectStack = new ConcurrentStack<T>();

        public T Get()
        {
            if (!objectStack.TryPop(out T t))
            {
                return new T();
            }
            return t;
        }

        public void Restore(T aObject)
        {
            if (aObject == null) return;
            objectStack.Push(aObject);
        }

        public void Clear()
        {
            objectStack.Clear();
        }
    }
}
