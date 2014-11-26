using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocols.Yaxun2
{
    /// <summary>
    /// http://bbs.csdn.net/topics/340103961 BCD
    /// http://www.cnblogs.com/windthunder/archive/2011/09/06/2169269.html
    /// http://www.cnblogs.com/xugang/archive/2012/04/23/2466510.html
    /// </summary>
    class BCD
    {
        /// <summary>
        /// BCD码转为10进制串(阿拉伯数据) 
        /// </summary>
        /// <param name="bytes">BCD码 </param>
        /// <returns>10进制串 </returns>
        public static String bcd2Str(byte[] bytes)
        {
            StringBuilder temp = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
            {
                temp.Append((byte)((bytes[i] & 0xf0) >> 4));
                temp.Append((byte)(bytes[i] & 0x0f));
            }
            return temp.ToString().Substring(0, 1).Equals("0") ? temp.ToString().Substring(1) : temp.ToString();
        }

        /// <summary>
        /// 10进制串转为BCD码 
        /// </summary>
        /// <param name="asc">10进制串 </param>
        /// <returns>BCD码 </returns>
        public static byte[] str2Bcd(String asc)
        {
            int len = asc.Length;
            int mod = len % 2;

            if (mod != 0)
            {
                asc = "0" + asc;
                len = asc.Length;
            }

            byte[] abt = new byte[len];
            if (len >= 2)
            {
                len = len / 2;
            }

            byte[] bbt = new byte[len]; 
            abt = System.Text.Encoding.ASCII.GetBytes(asc);
            int j, k;

            for (int p = 0; p < asc.Length / 2; p++)
            {
                if ((abt[2 * p] >= '0') && (abt[2 * p] <= '9'))
                {
                    j = abt[2 * p] - '0';
                }
                else if ((abt[2 * p] >= 'a') && (abt[2 * p] <= 'z'))
                {
                    j = abt[2 * p] - 'a' + 0x0a;
                }
                else
                {
                    j = abt[2 * p] - 'A' + 0x0a;
                }

                if ((abt[2 * p + 1] >= '0') && (abt[2 * p + 1] <= '9'))
                {
                    k = abt[2 * p + 1] - '0';
                }
                else if ((abt[2 * p + 1] >= 'a') && (abt[2 * p + 1] <= 'z'))
                {
                    k = abt[2 * p + 1] - 'a' + 0x0a;
                }
                else
                {
                    k = abt[2 * p + 1] - 'A' + 0x0a;
                }

                int a = (j << 4) + k;
                byte b = (byte)a;
                bbt[p] = b;
            }
            return bbt;
        }
    }
}
