using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public class ArrayCacheBuffer
    {
        private byte[] items { get; set; } = Array.Empty<byte>();
        private int size;
        public int Size
        {
            get
            {
                return size;
            }
            private set
            {
                if (value == 0)
                {
                    Array.Clear(items, 0, size);
                    items = Array.Empty<byte>();

                }
                else if (value > items.Length)
                {
                    byte[] newItems = new byte[value];
                    Array.Copy(items, newItems, items.Length);
                    items = newItems;
                }
            }
        }

        public Memory<byte> Memory
        {
            get
            {
                return items.AsMemory(0, size);
            }
        }

        public void AddRange(byte[] data)
        {
            BeResize(data.Length);
            Array.Copy(data, 0, items, size, data.Length);
            size += data.Length;
        }
        public void AddRange(Span<byte> data)
        {
            BeResize(data.Length);
            Array.Copy(data.ToArray(), 0, items, size, data.Length);
            size += data.Length;
        }

        public void RemoveRange(int index, int count)
        {
            if (index >= 0 && count > 0 && size - index >= count)
            {
                size -= count;
                if (index < size)
                {
                    Array.Copy(items, index + count, items, index, size - index);
                }
            }
        }

        public void Clear()
        {
            size = 0;
            Size = 0;
        }

        private void BeResize(int length)
        {
            int _size = size + length;
            if (size + length > items.Length)
            {
                int newsize = items.Length * 2;
                if (newsize < _size)
                {
                    newsize = _size;
                }
                Size = newsize;
            }
        }
    }
}
