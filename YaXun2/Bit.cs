using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Protocols.Yaxun2;

namespace Protocols.YaXun2
{
    class Bit
    {
        public static byte[] StringToBytes(string value)
        {
            var bytes=Encoding.GetEncoding("GBK").GetBytes(value);
            Array.Reverse(bytes);//高位在前
            return bytes;
        }
        public static string BytesToString(byte[] value)
        {
            Array.Reverse(value);
            return Encoding.GetEncoding("GBK").GetString(value);
        }
    }
    /// <summary>
    /// 通用协议都是先传递高位再传递低位 所以需要swap
    /// </summary>
    class BitReader:BinaryReader
    {
        
        public BitReader(Stream input):base(input)
        {
            
        }
        public override ushort ReadUInt16()
        {
            return Endian.SwapUInt16(base.ReadUInt16());
        }
        public override uint ReadUInt32()
        {
            return Endian.SwapUInt32(base.ReadUInt32());
        }
        public byte[] ReadBCDBytes()
        {
            return base.ReadBytes(6);
        }
        public string ReadBCD()
        {
            var bytes=ReadBCDBytes();
            return BCD.bcd2Str(bytes);
        }
        /// <summary>
        /// YY-MM-DD HH24:mm:ss
        /// </summary>
        /// <returns></returns>
        public DateTime ReadDateTime()
        {
            try
            {
                var bytes = ReadBCDBytes();
                var time = BCD.bcd2Str(bytes);

                var year = int.Parse("20" + time.Substring(0, 2));
                var month = int.Parse(time.Substring(2, 2));
                var day = int.Parse(time.Substring(4, 2));
                var hour = int.Parse(time.Substring(6, 2));
                var min = int.Parse(time.Substring(8, 2));
                var sec = int.Parse(time.Substring(10, 2));
                var dtime = new DateTime(year, month, day, hour, min, sec);
                return dtime;

            }
            catch (Exception e)
            {
                throw e;
            }
            return DateTime.Now;
        }
            
        public string ReadString(int count)
        {
            var bytes = this.ReadBytes(count);
            return Bit.BytesToString(bytes);
        }
        /// <summary>
        /// YYMMDD
        /// </summary>
        /// <returns></returns>
        public DateTime ReadDate()
        {
            var bytes = this.ReadBytes(4);
            var time = BCD.bcd2Str(bytes);
            var year = int.Parse(time.Substring(0, 4));
            var month = int.Parse(time.Substring(4, 2));
            var day = int.Parse(time.Substring(6, 2));
            return new DateTime(year, month, day);
        }



    }
    class BitWriter : BinaryWriter
    {
        public BitWriter(Stream output): base(output)
        {
           
        }
        public override void Write(ushort value)
        {
            base.Write(Endian.SwapUInt16(value));//
        }
        public override void Write(uint value)
        {
            base.Write(Endian.SwapUInt32(value));
        }
        /// <summary>
        /// 写入BCD码
        /// </summary>
        /// <param name="bcd"></param>
        public void WriteBCD(String bcd)
        {
            base.Write(BCD.str2Bcd(bcd));
        }
    }
}
