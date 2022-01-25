using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace common
{
    public struct NumberSpace
    {
        private ulong num;

        public NumberSpace(ulong defaultVal = 0)
        {
            num = defaultVal;
        }

        public ulong Get()
        {
            Interlocked.Increment(ref num);
            return num;
        }

        public void Reset()
        {
            Interlocked.Exchange(ref num, 0);
        }
    }

    public class NumberSpaceInt64
    {
        private long num = 0;

        public NumberSpaceInt64(long defaultVal = 0)
        {
            num = defaultVal;
        }

        public long Get()
        {
            Interlocked.Increment(ref num);
            return num;
        }

        public void Reset()
        {
            Interlocked.Exchange(ref num, 0);
        }
    }

    public class NumberSpaceInt32
    {
        private int num = 0;

        public NumberSpaceInt32(int defaultVal = 0)
        {
            num = defaultVal;
        }

        public int Get()
        {
            Interlocked.Increment(ref num);
            return num;
        }

        public void Decrement()
        {
            Interlocked.Decrement(ref num);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref num, 0);
        }
    }

    public class NumberSpaceUInt32
    {
        private uint num = 0;

        public NumberSpaceUInt32(uint defaultVal = 0)
        {
            num = defaultVal;
        }

        public uint Get()
        {
            Interlocked.Increment(ref num);
            return num;
        }

        public void Reset()
        {
            Interlocked.Exchange(ref num, 0);
        }
    }

    public struct BoolSpace
    {
        bool _default;
        private bool value;
        public BoolSpace(bool defaultVal = true)
        {
            _default = defaultVal;
            value = _default;
        }

        public bool Get()
        {
            return value;
        }

        public bool Reverse()
        {
            value = !_default;
            return value;
        }

        public void Reset()
        {
            value = _default;
        }
    }
}
