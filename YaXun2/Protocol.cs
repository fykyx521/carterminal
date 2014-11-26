using System;
using System.Collections.Generic;
using System.Text;

using DataLibrary;
using System.IO;
using Protocols.YaXun2;
using DataLibrary.Object;

namespace Protocols.Yaxun2
{
    /// <summary>
    /// 通用协议
    /// </summary>
    public class Protocol
    {
        
        
      

        #region 事件属性

        public override ProtocolType Type
        {
            get { return ProtocolType.General; }
        }

        public string Password
        {
            get { return string.Empty; }
            set { }
        }

        #endregion

        #region 下行指令组织
        class TerminalParam
        {
            public uint key;//占4字节
            public byte length;//占1字节
            public byte[] value;

            public TerminalParam(uint key, byte length, byte[] value)
            {
                this.key = key;
                this.length = length;
                this.value = value;
            }
            public Byte[] ToBytes()
            {
                var result = new byte[4 + 1 + length];
                var keyBytes=BitConverter.GetBytes(Endian.SwapUInt32(key));
                Array.Copy(keyBytes, result, 4);//key放进去
                result[4] = length;//放长度进去
                Array.Copy(value, 0, result, 5, value.Length);
                return result;
            }

        }

        private byte[] SetParamMessage(uint key, byte[] value)
        {
            var mes = Message.Create();
            mes.MessageId = 0x8103;
            mes.Tel = this.terminalid + "";
            mes.NumSeq = 0;
            mes.BodyBytes = this.SetParam(key,value);
            return mes.ToBytes(); 
        }
        private byte[] SetParamMessageByList(List<TerminalParam> ps)
        {
            var mes = Message.Create();
            mes.MessageId = 0x8103;
            mes.Tel = this.terminalid + "";
            mes.NumSeq = 0;
            mes.BodyBytes = this.SetParam(ps);
            return mes.ToBytes();
        }

        private byte[] SetParam(uint key, byte[] value)
        {
            var p = new TerminalParam(key, Convert.ToByte(value.Length), value);
            var list=new List<TerminalParam>();
            list.Add(p);
            return this.SetParam(list);
        }

        private byte[] SetParam(List<TerminalParam> ps)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            byte paramLength=Convert.ToByte(ps.Count);
            bw.Write(paramLength);
            foreach (var item in ps)
            {
                bw.Write(item.ToBytes());
            }
            return ms.ToArray();//发送参数信息
        }

       
        

        /// <summary>
        /// 调度信息
        /// 0 1：紧急
        ///    1 保留
        ///    2 1：终端显示器显示
        ///    3 1：终端TTS 播读
        ///    4 1：广告屏显示
        ///    5 0：中心导航信息，1：CAN 故障码信息
        ///    6-7 保留
        /// </summary>
        public override byte[] SendMessage(string msg)
        {
            var mes = Message.Create();
            mes.MessageId = 0x8300;
            mes.Tel = this.terminalid+"";
            mes.NumSeq = 0;
            byte state = Convert.ToByte("00000000", 2);
            byte[] mesBytes = Bit.StringToBytes(msg);
            if (mesBytes.Length >= 1000)
            {
                Array.Resize(ref mesBytes, 1000);
            }
           
            var newArray=new byte[mesBytes.Length+1];
            newArray[0]=state;
            Array.Copy(mesBytes,0,newArray,1,mesBytes.Length);
            mes.BodyBytes = newArray;
            return mes.ToBytes();

        }

        /// <summary>
        /// 发送用户数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override byte[] SendUserdata(string data)
        {
            return SendMessage(data);
        }

        /// <summary>
        /// 撤消报警
        /// </summary>
        public override byte[] QuashAlarm()
        {
            var mes = Message.Create(0x8203, this.terminalid + "", 0, null);
            var str = "0".PadLeft(32, '0');
            var charArray = str.ToCharArray();
            charArray[0] = '1';
            charArray[3] = '1';
            charArray[20] = '1';
            charArray[21] = '1';
            charArray[22] = '1';
            charArray[28] = '1';
            charArray[29] = '1';
            Array.Reverse(charArray);
            var nstr = new String(charArray, 0, charArray.Length);
            var uint32 = Endian.SwapUInt32(Convert.ToUInt32(nstr, 2));//高位在前,swap下
            var bytes=BitConverter.GetBytes(uint32);
            var body = new byte[1 + bytes.Length];
            body[0] = 0;
            Array.Copy(bytes, 0, body, 1,body.Length-1);
            mes.BodyBytes = body;
            return mes.ToBytes();
        }
       
        /// <summary>
        /// 单次定位
        /// </summary>
        public override byte[] CurrentPosition()
        {
            var mes = Message.Create();
            mes.MessageId = 0x8201;
            mes.Tel = this.terminalid+"";
            mes.NumSeq = 0;
            return mes.ToBytes();
        }

        /// <summary>
        /// 设置最高时速
        /// </summary>
        /// <param name="speed">速度 千米/时</param>
        public override byte[] MaxSpeed(int speed)
        {
           var nspeed = Endian.SwapUInt32(Convert.ToUInt32(speed/10));//因为终端先存取高位 所以swap下  /10文档中是 1/10km/h
           var bytes=this.SetParam(0x0055, BitConverter.GetBytes(nspeed));
           var mes = Message.Create();
           mes.MessageId = 0x8103;
           mes.Tel = this.terminalid + "";
           mes.NumSeq = 0;
           mes.BodyBytes = bytes;
           return mes.ToBytes(); 
        }

        //设置IP
        /// <summary>
        /// 0x0018 DWORD 服务器TCP 端口
        /// 0x0013 STRING 主服务器地址,IP 或域名
        /// </summary>
        /// <param name="apn"></param>
        /// <param name="type"></param>
        /// <param name="ips"></param>
        /// <param name="port"></param>
        /// <param name="span"></param>
        /// <returns></returns>
        public override byte[] SetNetConfigure(APNType apn, IpType type, byte[] ips, int port, int span)
        {
            uint ipId = 0x0018;
            uint portId = 0x0042;
            if (IpType.Udp == type)
            {
                portId = 0x0019;
            }
            string ip = string.Format("{0}.{1}.{2}.{3}", ips[0], ips[1], ips[2], ips[3]);
            var ipBytes = Bit.StringToBytes(ip);
            uint nport = Convert.ToUInt32(port);
            var portBytes = BitConverter.GetBytes(Endian.SwapUInt32(nport));
            var p = new TerminalParam(ipId, (byte)ipBytes.Length, ipBytes);
            var p2 = new TerminalParam(portId, (byte)portBytes.Length, portBytes);
            var list = new List<TerminalParam>();
            list.Add(p);
            list.Add(p2);
            return this.SetParamMessageByList(list);
        }

        /// <summary>
        /// 设置短信中心号
        /// </summary>
        /// <param name="tel"></param>
        /// <returns></returns>
        public override byte[] SetNumber(NumberType type, string tel)
        {
            byte[] temp = Bit.StringToBytes(tel);
            switch (type)
            {
                case NumberType.SmsCenter: return SetSmsCenter(temp);
                case NumberType.Help: return SetHelp(temp);
                case NumberType.Listen: return SetListen(temp);
                case NumberType.Control: return SetControl(temp);
                case NumberType.CallTel: return SetCallTel(tel);
                default: return null;
            }
        }


        private byte[] SetCallTel(string tel)
        {
            
        }

        /// <summary>
        /// 
        /// 0x0041 STRING 复位电话号码，可采用此电话号码拨打终端电话让终端复位
        ///    0x0042 STRING
        ///    恢复出厂设置电话号码，可采用此电话号码拨打终端电话让终端恢复
        ///    出厂设置
        /// </summary>
        private byte[] SetControl(byte[] tel)
        {
            var p = new TerminalParam(0x0041, (byte)tel.Length, tel);
            var p2 = new TerminalParam(0x0042, (byte)tel.Length, tel);
            var list=new List<TerminalParam>();
            list.Add(p);
            list.Add(p2);
            return this.SetParamMessageByList(list);
            
        }
        /// <summary>
        /// 设置监听电话号码
        /// </summary>
        private byte[] SetListen(byte[] tel)
        {
           return this.SetParamMessage(0x0048, tel);
        }
        /// <summary>
        /// 设置求助号码
        /// </summary>
        private byte[] SetHelp(byte[] tel)
        {
            
        }
        private byte[] SetSmsCenter(byte[] tel)
        {
            return this.SetParamMessage(0x0049, tel);
        }

        /// <summary>
        /// 语音监听
        /// </summary>
        public override byte[] SoundListen(string tel)
        {
            
        }

        /// <summary>
        /// 初始化终端设备
        /// </summary>
        /// <returns></returns>
        public override byte[] SetDefault()
        {
           
        }

        /// <summary>
        /// 通话设置
        /// </summary>
        public override byte[] SetCall(CallType call)
        {
            
        }

        /// <summary>
        /// 设置频率 
        /// </summary>
        /// <param name="frequence">运动状态＋重车状态（点火）是否上传数据</param> 
        /// <param name="stopfrequence">静止状态＋空车状态（熄火）是否上传数据</param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override byte[] SetFrequency(int frequence, int stopfrequence, int count)
        {
            return null;
        }

        /// <summary>
        /// 定时回传
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public override byte[] SetTimereturn(int hour, int minute, int second)
        {

            var mes = Message.Create(0x8202, this.terminalid + "", 0, null);
            uint sec=(uint)(hour*60*60+minute*60+second);
            UInt16 span=10;// 默认10秒上传一次
            if((hour==minute)&&(minute==second)&&(second==0))
            {
                span=0;//停止跟踪
            }
            var spanBytes=BitConverter.GetBytes(Endian.SwapUInt16(span));
            var secBytes=BitConverter.GetBytes(Endian.SwapUInt32(sec));
            var bytes=new byte[6];
            Array.Copy(spanBytes,bytes,spanBytes.Length);
            Array.Copy(secBytes,0,bytes,spanBytes.Length,secBytes.Length);
            mes.BodyBytes = bytes;

            return mes.ToBytes();

        }

        /// <summary>
        /// 0x002C DWORD 缺省距离汇报间隔，单位为米（m），>0
        /// 定距回传
        ///   0x002E DWORD 休眠时汇报距离间隔，单位为米（m），>0
        /// </summary>
        /// <param name="distance">距离 单位米</param>
        /// <returns></returns>
        public override byte[] SetTrack(int distance)
        {
            uint dis = Convert.ToUInt32(distance);
            var bytes = BitConverter.GetBytes(Endian.SwapUInt32(dis));//高位在前

            var p = new TerminalParam(0x002C, (byte)bytes.Length, bytes);
            var p1 = new TerminalParam(0x002E, (byte)bytes.Length, bytes);
            var list = new List<TerminalParam>();
            list.Add(p);
            list.Add(p1);

            return this.SetParamMessageByList(list);
        }

        /// <summary>
        /// 设置黑匣子采样保存频率
        /// </summary>
        /// <param name="second"></param>
        /// <returns></returns>
        public override byte[] SetBlackbox(int second)
        {
            
        }

        /// <summary>
        /// 终端的链路维护
        /// </summary>
        /// <param name="minute"></param>
        /// <returns></returns>
        public override byte[] SetLink(int minute)
        {
      

            return null;
        }
        /*
         * 0 通道ID BYTE >0
1 拍摄命令 WORD
0 表示停止拍摄；0xFFFF 表示录像；其它表示拍
照张数
3 拍照间隔/录像时间 WORD 秒，0 表示按最小间隔拍照或一直录像
5 保存标志 BYTE
1：保存；
0：实时上传
6 分辨率a BYTE
0x01:320*240；
0x02:640*480；
0x03:800*600；
0x04:1024*768;
0x05:176*144;[Qcif];
0x06:352*288;[Cif];
0x07:704*288;[HALF D1];
0x08:704*576;[D1];
7 图像/视频质量 BYTE 1-10，1 代表质量损失最小，10 表示压缩比最大
8 亮度 BYTE 0-255
9 对比度 BYTE 0-127
10 饱和度 BYTE 0-127
11 色度 BYTE 0-255
         * 
         * */
        /// <summary>
        /// 中心抓拍图片
        /// </summary>
        /// <param name="ordinal"></param>
        /// <param name="size"></param>
        /// <param name="quality"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override byte[] Capture(uint ordinal, SizeType size, QualityType quality, byte count)
        {
            var mes = Message.Create(0x8801, this.terminalid + "", 0, null);
            var ms = new MemoryStream();
            BitWriter bw = new BitWriter(ms);
            bw.Write((byte)1);//通道一
            bw.Write((UInt16)count);//拍一张
            bw.Write((UInt16)0);//拍照间隔
            bw.Write((byte)0);//0实时上传 1保存
            byte pixel = 0x01;
            switch (size) //分辨率 图片大小
            {
                case SizeType.Big: pixel = 0x04; break;   //
                case SizeType.Normal: pixel = 0x08; break; //
                case SizeType.Small: pixel = 0x01; break;  //
            }
            bw.Write(pixel);//分辨率

            byte level = 1;
            switch (quality) //图片质量
            {
                case QualityType.Height: level = 1; break;
                case QualityType.Normal: level = 5; break;
                case QualityType.Low: level = 10; break;
            }
            bw.Write(level);//图片质量
            bw.Write((byte)127);//亮度
            bw.Write((byte)64);//对比度
            bw.Write((byte)64);//饱和度
            bw.Write((byte)127);//色度
            mes.BodyBytes = ms.ToArray();
            return mes.ToBytes();
        }

        /// <summary>
        /// 设置拍照黑匣子
        /// </summary>
        /// <param name="ordinal"></param>
        /// <param name="size"></param>
        /// <param name="quality"></param>
        /// <param name="count"></param>
        /// <param name="state"></param>
        /// <param name="ts"></param>
        /// <returns></returns>
        public override byte[] Capturebox(uint ordinal, SizeType size, QualityType quality, byte count, StateType state, TimeSpan ts, bool upload)
        {
            return null;
        }
        /// <summary>
        /// 取消黑匣子拍照
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public override byte[] Cencelcapbox(StateType state)
        {
            return null;
        }

        /// <summary>
        /// 提取黑匣子拍照
        /// </summary>
        /// <returns></returns>
        public override byte[] CaptureExt(int index)
        {
            return null;
        }

        /// <summary>
        /// 提取黑匣子拍照索引
        /// </summary>
        public override byte[] CaptureIndex(DateTime start, DateTime end, int maxCount)
        {
            return null;
        }

        /// <summary>
        /// 下发指令使终端升级
        /// </summary>
        /// <param name="apn"></param>
        /// <param name="iptype"></param>
        /// <param name="ip"></param>
        /// <param name="prot"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public override byte[] SetDownload(APNType apn, IpType iptype, byte[] ip, int prot, string version)
        {
            
        }

        /// <summary>
        /// 车台版本查询
        /// </summary>
        /// <returns></returns>
        public override byte[] SetVersion()
        {
            
            
        }

        #endregion

        #region  上行指令解析

        public override bool MySelf(ref byte[] command)
        {
            if (command.Length > 2)
            {
                return (command[0] == 0x7e) && (command[command.Length - 1] == 0x7e);//首尾标示符都是0x7e
            }
            return false;
        }

        public override long GetTerminalID(byte[] command)
        {
            
            try
            {    
                if (command.Length > 10)
                {
                    var bytes = new byte[6];
                    Array.Copy(command, 5, bytes, 0, 6);// 标识头 占一个字节 所以是5 
                    string num = BCD.bcd2Str(bytes);
                    this.terminalid=Convert.ToInt64(num);
                }


                return this.terminalid;
            }
            catch (Exception e)
            {
                throw new CommandException(command, e);
            }
        }

        /// <summary>
        /// 链路维护
        /// </summary>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //var mes = Message.Create();
            //mes.MessageId = 0x8001;
            //mes.Tel = this.terminalid+"";
            //mes.NumSeq = 0;
            //this.NormalMes(mes, 0);
        }

        public override void Dispose()
        {
            if (this.timer != null)
            {
                this.timer.Stop();
                this.timer.Close();
                this.timer.Dispose();
            }
        }

        /// <summary>
        /// 终端数据解析
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public override int Parse(byte[] command)
        {
            try
            {

                
                if (command.Length > 2) //大于2 
                {
                    //var mbytes = new byte[] { command[1], command[2] };
                    //var mesId = Endian.SwapUInt16(BitConverter.ToUInt16(mbytes,0));

                    //var bytes = new byte[6];
                    //Array.Copy(command, 5, bytes, 0, 6);// 标识头 占一个字节 所以是5 
                    //string num = BCD.bcd2Str(bytes);
                    //this.terminalid = Convert.ToInt64(num);


                    var mes=Message.ToMes(command);

                    switch (mes.MessageId)
                    {
                        case 0x0001: TerNormal(mes); break;//终端通用应答 
                        case 0x0002: NormalMes(mes, 0); break;//心跳数据 使用平台通用应答
                        case 0x0100: Register(mes); break;//终端注册
                        case 0x0102: CheckAuth(mes);  break;//终端鉴权
                        case 0x0200: Gps(mes);  break;//读取GpS位置信息
                        case 0x0702: Driver(mes); break;//驾驶员信息上报
                        case 0x0201: SingleLocation(mes); break;//调用定位后的终端回答
                        case 0x0704: GpsAppend(mes);  break;//位置信息 补录
                        default: this.NormalMes(mes, 0); break;//默认恢复平台通用信息
                        
                    }
                    return command.Length;
                }
                
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return 0;
        }

        /// <summary>
        /// 终端通用应答
        /// </summary>
        /// <param name="mes"></param>
        private void TerNormal(Message mes)
        {
            var body = mes.BodyBytes;
            var ms = new MemoryStream(body);
            var read = new BitReader(ms);
            var numseq = read.ReadUInt16();//应答流水号
            var fromNum = read.ReadUInt16();//平台ID号
            var result = read.ReadByte();//结果
            Console.WriteLine(result);
        }
        /// <summary>
        /// 单次定位应答
        /// </summary>
        /// <param name="mes"></param>
        private void SingleLocation(Message mes)
        {
            var body = mes.BodyBytes;
            var newBody = new byte[body.Length - 2];//把应答序列号排除
            Array.Copy(body, 2, newBody, 0, newBody.Length);
            this.ReadGPS(newBody);
            this.NormalMes(mes, 0);
        }

        //注册回复
        private void Register(Message from)
        {
            var mes = Message.Create();
            mes.MessageId = 0x8100;
            mes.Tel=from.Tel;
            mes.NumSeq=from.NumSeq;//应答流水号
            MemoryStream ms = new MemoryStream();
            BitWriter bw = new BitWriter(ms);
            bw.Write(from.NumSeq);
            byte result = 0;//0：成功；1：车辆已被注册；2：数据库中无该车辆；3：终端已被注册；4：数据库中无该终端
            bw.Write(result);
            bw.Write(Guid.NewGuid().ToByteArray());
            
            mes.BodyBytes = ms.ToArray();
            var sendBytes = mes.ToBytes();
            EventRespons(sendBytes);
            
        }
        //平台通用应答
        private void NormalMes(Message from,byte res)
        {
            var mes = Message.Create();
            mes.MessageId = 0x8001;
            mes.Tel = from.Tel;
            mes.NumSeq = from.NumSeq;//应答流水号

            MemoryStream ms = new MemoryStream();
            BitWriter bw = new BitWriter(ms);
            bw.Write(from.NumSeq);
            bw.Write(from.MessageId);
            bw.Write(res);
           
            
            mes.BodyBytes = ms.ToArray();
            var sendBytes = mes.ToBytes();
            EventRespons(sendBytes);
        }

        //终端鉴权  也是通用应答
        private void CheckAuth(Message from)
        {
            NormalMes(from, 0);
        }
        /// <summary>
        /// GPS补录
        /// </summary>
        /// <param name="from"></param>
        private void GpsAppend(Message from)
        {
            var ms = new MemoryStream(from.BodyBytes);
            var read = new BitReader(ms);
            var count = read.ReadUInt16();//位置信息个数
            var type = read.ReadByte();//0：正常位置批量汇报，1：盲区补报
            for (var i = 0; i < count; i++)
            {
                var gpsLength = read.ReadUInt16();
                var bytes = read.ReadBytes(gpsLength);
                this.ReadGPS(bytes);
            }
            this.NormalMes(from, 0);
        }

        //发送过来的Gps信息
        private void Gps(Message from)
        {
            
            var body = from.BodyBytes;
            ReadGPS(body);
            NormalMes(from, 0);
            
        }
        private void ReadGPS(byte[] bytes)
        {
            MemoryStream ms = new MemoryStream(bytes);
            BitReader br = new BitReader(ms);

            var alarmType = br.ReadUInt32();//告警类型
            var gpsState = br.ReadUInt32();//gps状态
            var lat = br.ReadUInt32();//纬度
            var lon = br.ReadUInt32();//经度
            var height = br.ReadUInt16();//高度
            var speed = br.ReadUInt16();//速度
            var direction = br.ReadUInt16();//方向
            var dtime = DateTime.Now;
            try
            {
                dtime = br.ReadDateTime();// 日期
            }
            catch (Exception e)
            {
                throw e;
            }


            double lati = lat/1000000.0;
            double loni = lon/1000000.0;



            var type = Convert.ToString(alarmType, 2);
            type = type.PadLeft(32, '0');//
            var typeArray = type.ToCharArray();
            Array.Reverse(typeArray);//反转一下 好与文档中对应

            List<AlarmType> alarmlist = new List<AlarmType>();
            if (typeArray[1] == '1')
            {
                alarmlist.Add(AlarmType.超速报警);
            }
            if (typeArray[7] == '1')
            {
                alarmlist.Add(AlarmType.电瓶欠压);
            }

            var state = Convert.ToString(alarmType, 2);
            state = state.PadLeft(32, '0');//
            var stateArray = state.ToCharArray();
            Array.Reverse(stateArray);//反转一下 好与文档中对应
            var gState = false;
            if (stateArray[1] == '1')
            {
                gState = true;
            }
            var accState = false;
            if (stateArray[0] == '1')
            {
                accState = true;
            }
            if (alarmlist.Count == 0)
            {
                alarmlist.Add(AlarmType.无报警);
            }
            CarPoint point = new CarPoint(dtime,loni, lati, Convert.ToInt16(height));
            point.Alarms=alarmlist;
            point.Direction=Convert.ToInt16(direction);
            point.Speed = Convert.ToInt16(speed);

            point.GpsState = gState;  
            point.AccState = accState;
            ACC acc = new ACC()
            {
                ACCState = accState,
                point = point,
                Terminalid = this.terminalid
            };
            this.EventACC(acc);
            
            
            EventPointer(point);
            //foreach (var alarm in alarmlist)
            //{
                
            //    dtime = dtime.AddMilliseconds(1);
            //}
        }
        /*
                * 
                * 0x00：IC 卡读卡成功；
            0x01：读卡失败，原因为卡片密钥认证未通过；
            0x02：读卡失败，原因为卡片已被锁定；
            0x03：读卡失败，原因为卡片被拔出；
            0x04：读卡失败，原因为数据校验错误。
                
                * 
                * 
                * */
        private void Driver(Message from)
        {
            var body = from.BodyBytes;
            MemoryStream ms = new MemoryStream(body);
            BitReader br = new BitReader(ms);
            var state=br.ReadByte();
            var driver = new Driver();
            var time = br.ReadDateTime();//读取打卡时间
            driver.state = 0;
            if (state == 0x01)//终端时间未校准 使用服务器时间
            {
                time = DateTime.Now;
            }
           
               
                var ic = br.ReadByte();
               
                if (ic.Equals(0x00))//读卡成功
                {
                    //只有certificate  序列号读到
                    var driverNameLength = br.ReadByte();
                    var driverName = br.ReadString(driverNameLength);
                    var certBytes = br.ReadBytes(20);//从业资格证
                    var nBytes=new byte[4];
                    Array.Copy(certBytes,16,nBytes,0,4);
                    var certificate = BitConverter.ToUInt32(nBytes,0)+"";
                    //certificate
                    var licenceLength = br.ReadByte();//发证机关名称长度 
                    var licenceName = br.ReadString(licenceLength);//发证机关名称
                    var certificateVaDate = DateTime.Now; //br.ReadDate();//证件有效期 读不到
                    
                    driver.driverName=driverName;
                    driver.certificate = certificate;
                    driver.licenceName=licenceName;
                    driver.certificateVaDate=certificateVaDate;

                }
                
            
            
            driver.time = time;
            this.EventDriver(driver);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private void LoginReturn()
        {
            this.timer = new System.Timers.Timer();
            this.timer.Interval = LinkTime * 1000; //下发链路维护时间间隔
            this.timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            this.timer.Start();
        }

       
        #endregion

    }
}
