using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public class ReceiveDataBuffer
    {
        private Memory<byte> items { get; set; } = Array.Empty<byte>().AsMemory();
        private int size = 0;
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
                    items = Array.Empty<byte>();
                }
                else if (value > items.Length)
                {
                    Memory<byte> newItems = new byte[value].AsMemory();
                    items.CopyTo(newItems);
                    items = newItems;
                }
            }
        }

        public Memory<byte> Data
        {
            get
            {
                return items;
            }
        }

        public void AddRange(Memory<byte> data, int length)
        {
            BeResize(length);

            data.Slice(0, length).CopyTo(items.Slice(size, length));
            size += length;
        }

        public void RemoveRange(int index, int count)
        {
            if (index >= 0 && count > 0 && size - index >= count)
            {
                size -= count;
                if (index < size)
                {
                    items.Slice(index + count, size - index).CopyTo(items.Slice(index, size - index));
                }
            }
        }

        public void Clear(bool clearData = false)
        {
            size = 0;
            if (clearData)
            {
                Size = 0;
            }
        }

        private void BeResize(int length)
        {
            int _size = size + length;
            if (_size > items.Length)
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
