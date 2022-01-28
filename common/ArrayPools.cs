using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public static class ArrayPools
    {
        public static MemoryPool<byte> memoryPool = MemoryPool<byte>.Shared;
        public static ArrayPool<byte> arrayPool = ArrayPool<byte>.Create(64 * 1024, 100000);
    }
}
