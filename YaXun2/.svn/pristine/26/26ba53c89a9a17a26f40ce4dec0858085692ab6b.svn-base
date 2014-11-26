using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Protocols.YaXun2;

namespace Protocols.Yaxun2
{
    class Message
    {
        public byte Start=0x7e;//1byte
        private MessageHead Head;//消息头
        
        private byte[] bodyBytes;
        public byte CheckCode;//校验码 1个字节
        public byte End = 0x7e;

        public static Message Create()
        {
            return new Message();
        }
        public static Message Create(UInt16 mesId,string tel,UInt16 numSeq,byte[] body)
        {
            var mes=new Message();
            mes.MessageId = mesId;
            mes.Tel = tel;
            mes.NumSeq = numSeq;
            if (body != null)
            {
                mes.BodyBytes = body;
            }
           
            return mes;
        }
        public Message()
        {
            Head = new MessageHead();
           
        }
        public UInt16 NumSeq
        {
            get { return Head.NumSeq; }
            set { Head.NumSeq = value; }
        }
        public String Tel
        {
            get { return Head.Tel; }
            set { Head.Tel = value; }
        }

        public byte[] BodyBytes
        {
            get { return bodyBytes; }
            set { 
                    bodyBytes = value;
                    Head.contentLength = Convert.ToUInt16(bodyBytes.Length);
               }
        }


        //单次定位查询
        public UInt16 MessageId
        {
            get {return Head.MessageId;}
            set {Head.MessageId=value;}
        }

        public byte[] ToBytes()
        {
            MemoryStream mem = new MemoryStream();
            mem.WriteByte(Start);
            var hbytes = Head.ToBytes();
            var content = hbytes;
            if (bodyBytes != null && bodyBytes.Length > 0)
            {
                content = hbytes.Concat(bodyBytes).ToArray();
            }
            mem.Write(content,0,content.Length);
            mem.WriteByte(this.GetCheckCode(content));
            mem.WriteByte(End);
            return Convert7D(mem.ToArray());

        }
        private byte GetCheckCode(byte[] content)
        {
            byte result = content[0];
            for (var i = 0; i < content.Length-1; i++)
            {
                result = Convert.ToByte(result^content[i + 1]);
            }
            return result;
        }

        public static Message ToMes(byte[] bt)
        {
            if (bt.Length < 2)
            {
                return null;
            }
            MemoryStream ms = new MemoryStream();
            //转义 
            for (var i = 1; i < bt.Length; i++)//收尾0x7e 不处理
            {
                if (bt[i] == 0x7d && (i + 1) < bt.Length)
                {
                    var isAnd = false;
                    if (bt[i + 1] == 0x02)
                    {
                        ms.WriteByte(0x7e);
                        isAnd = true;
                    }
                    else if (bt[i + 1] == 0x01)
                    {
                        ms.WriteByte(0x7d);
                        isAnd = true;
                    }
                    if (isAnd)
                    {
                        i += 1;
                    }
                }
                else
                {
                    ms.WriteByte(bt[i]);
                }
                
            }
            ms.Seek(0, SeekOrigin.Begin);
            BitReader br=new BitReader(ms);
            var mes = new Message();
            mes.Head.MessageId = br.ReadUInt16();
            mes.Head.BodyProp = br.ReadBytes(2);
            mes.Head.Tel = br.ReadBCD();
           
            mes.Head.NumSeq = br.ReadUInt16();
            
            if (mes.Head.isLong)
            {
                mes.Head.MesPackNum = br.ReadBytes(4);
            }
            var content = br.ReadBytes(mes.Head.contentLength);
            mes.BodyBytes=content;
            return mes;
        }

        class MessageHead
        {
            public UInt16 MessageId;//消息ID   //0 起始索引
            private byte[] bodyProp = new Byte[2];//Convert.ToByte("01100001", 2);//消息体属性 //2 起始索引
            public bool isLong = false;//是否长消息
            public bool isRSA = false;//是否RSA加密
            public UInt16 contentLength = 0;

            public byte[] BodyProp
            {
                get { return bodyProp; }
                set
                {
                    bodyProp = value;
                    parseMessageBodyProperties();//解析
                }
            }
            public string Tel;//电话号  //4  起始索引  //占用6字节
            public UInt16 NumSeq=0;//消息流水号 //10 起始索引
            private byte[] mesPackNum;//消息包封装项 //12 起始索引

            public byte[] MesPackNum
            {
                get { return mesPackNum; }
                set { mesPackNum = value; parsePack(); }
            }
            private void parsePack()
            {
                
            }

            public void parseMessageBodyProperties()
            {
                var nbytes = BodyProp;
                var high = Convert.ToString(nbytes[0], 2);//高位
                var low = Convert.ToString(nbytes[1], 2); //低位
                var nhigh = high.PadLeft(8, '0');
                var nlow = low.PadLeft(8, '0');
                var last = nhigh.Substring(7, 1) + nlow;

                contentLength = Convert.ToUInt16(last, 2);//内容长度
                if (nhigh.Substring(2, 1) == "1")//第三位是1 表示长消息
                {
                    isLong = true;
                }
                if (nhigh.Substring(3, 3) == "000")
                {
                    isRSA = false;//加密
                }

            }
           
            public byte[] ToBytes()
            {
                var Idbytes = BitConverter.GetBytes(MessageId);
                Array.Reverse(Idbytes);//先传递高位

                var contentBody = BitConverter.GetBytes(contentLength);
                Array.Reverse(contentBody);//先传递高位 后传递低位

                var telbytes = BCD.str2Bcd(this.Tel);

                var numSeqBytes = BitConverter.GetBytes(this.NumSeq);
                Array.Reverse(numSeqBytes);

                return Idbytes.Concat(contentBody).Concat(telbytes).Concat(numSeqBytes).ToArray();
            }

        }

        /// <summary>
        /// 转义单包(发送数据转义)
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private byte[] Convert7D(byte[] command)
        {
            for (int i = 1; i < command.Length - 1; i++)
            {
                if (command[i] == 0x7E)
                {
                    Array.Resize(ref command, command.Length + 1);
                    Array.Copy(command, i + 1, command, i + 2, command.Length - i - 2);
                    command[i] = 0x7d;
                    command[++i] = 0x02;
                }
                else if (command[i] == 0x7d)
                {
                    Array.Resize(ref command, command.Length + 1);
                    Array.Copy(command, i + 1, command, i + 2, command.Length - i - 2);
                    //command[i] = 0x7d;
                    command[++i] = 0x01;
                }
            }
            return command;
        }
    }
}
