using ProtoBuf;
using System;
using System.Collections.Generic;
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
    }
}
