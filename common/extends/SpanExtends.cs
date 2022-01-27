using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common.extends
{
    public static class SpanExtends
    {
        public static ulong ToUInt64(this Span<byte> span)
        {
            return BitConverter.ToUInt64(span);
        }
        public static long ToInt64(this Span<byte> span)
        {
            return BitConverter.ToInt64(span);
        }
        public static ulong ToUInt64(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToUInt64(span);
        }
        public static long ToInt64(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToInt64(span);
        }
        public static long ToInt64(this Memory<byte> memory)
        {
            return BitConverter.ToInt64(memory.Span);
        }

        public static int ToInt32(this Span<byte> span)
        {
            return BitConverter.ToInt32(span);
        }
        public static int ToInt32(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToInt32(span);
        }


        public static short ToInt16(this Span<byte> span)
        {
            return BitConverter.ToInt16(span);
        }
        public static short ToInt16(this ReadOnlySpan<byte> span)
        {
            return BitConverter.ToInt16(span);
        }

        public static string GetString(this Span<byte> span)
        {
            return Encoding.UTF8.GetString(span);
        }
        public static string GetString(this ReadOnlySpan<byte> span)
        {
            return Encoding.UTF8.GetString(span);
        }

        public static bool ASCIIEquals(this Memory<byte> memory, Memory<byte> memory1)
        {
            return memory.Span.ASCIIEquals(memory1.Span);
        }

        public static bool ASCIIEquals(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> other)
        {
            if (span.Length != other.Length) return false;

            bool res = true;
            for (int i = 0; i < span.Length; i++)
            {
                byte spanI = span[i];
                byte otherI = other[i];

                if (spanI != 0x2F)
                {
                    res &= (spanI | 0X20) == (otherI | 0x20);
                }
                else
                {
                    res &= spanI == otherI;
                }
            }
            Console.WriteLine($"{span.GetString()}={other.GetString()},{res}");
            return res;
        }

        public static bool ASCIIEquals(this Span<byte> span, Span<byte> other)
        {
            if (span.Length != other.Length) return false;

            bool res = true;
            for (int i = 0; i < span.Length; i++)
            {
                byte spanI = span[i];
                byte otherI = other[i];

                if (spanI != 0x2F)
                {
                    res &= (spanI | 0X20) == (otherI | 0x20);
                }
                else
                {
                    res &= spanI == otherI;
                }
            }
            return res;
        }

    }
}
