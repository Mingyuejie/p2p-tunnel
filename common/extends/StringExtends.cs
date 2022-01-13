using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace common.extends
{
    public static class StringExtends
    {
        public static string SubStr(this string str, int start, int maxLength)
        {
            if (maxLength + start > str.Length)
            {
                maxLength = str.Length - start;
            }
            return str.Substring(start, maxLength);
        }

        public static string Md5(this string input)
        {
            MD5CryptoServiceProvider md5Hasher = new();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            StringBuilder sBuilder = new();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        public static T Convert<T>(this string input)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter != null)
                {
                    return (T)converter.ConvertFromString(input);
                }
                return default;
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static object Convert(this string input, Type type)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(type);
                if (converter != null)
                {
                    return converter.ConvertFromString(input);
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static int ToInt(this string input, int defaultValue = 0)
        {
            if (int.TryParse(input, out int res) == false)
            {
                res = defaultValue;
            }

            return res;
        }

        public static float ToFloat(this string input, float defaultValue = 0)
        {
            if (float.TryParse(input, out float res) == false)
            {
                res = defaultValue;
            }

            return res;
        }

        public static double ToDouble(this string input, double defaultValue = 0)
        {
            if (double.TryParse(input, out double res) == false)
            {
                res = defaultValue;
            }

            return res;
        }

        public static int[] ToIntArray(this string input)
        {
            return Array.ConvertAll(input.Split(Helper.SeparatorChar), c => int.Parse(c));
        }
    }
}
