using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocols.YaXun2
{
    //高低位互换 因为终端数据要求先存储高位 .net读取是从低位开始读  所以用这个类处理
    //http://www.cnblogs.com/smark/p/3218753.html
    class Endian
    {
        //
        public static short SwapInt16(short v)
        {
            return (short)(((v & 0xff) << 8) | ((v >> 8) & 0xff));
        }
        public static ushort SwapUInt16(ushort v)
        {
            return (ushort)(((v & 0xff) << 8) | ((v >> 8) & 0xff));
        }
        public static int SwapInt32(int v)
        {
            return (int)(((SwapInt16((short)v) & 0xffff) << 0x10) |
                          (SwapInt16((short)(v >> 0x10)) & 0xffff));
        }
        public static uint SwapUInt32(uint v)
        {
            return (uint)(((SwapUInt16((ushort)v) & 0xffff) << 0x10) |
                           (SwapUInt16((ushort)(v >> 0x10)) & 0xffff));
        }
        public static long SwapInt64(long v)
        {
            return (long)(((SwapInt32((int)v) & 0xffffffffL) << 0x20) |
                           (SwapInt32((int)(v >> 0x20)) & 0xffffffffL));
        }
        public static ulong SwapUInt64(ulong v)
        {
            return (ulong)(((SwapUInt32((uint)v) & 0xffffffffL) << 0x20) |
                            (SwapUInt32((uint)(v >> 0x20)) & 0xffffffffL));
        }
    
    }
}
