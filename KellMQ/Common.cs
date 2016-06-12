using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Management;
using System.Net.NetworkInformation;

namespace KellMQ
{
    /// <summary>
    /// 下线委托
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="client"></param>
    public delegate void OfflineHandler(object sender, IPEndPoint client);
    /// <summary>
    /// 接收委托
    /// </summary>
    /// <param name="client"></param>
    /// <param name="data"></param>
    public delegate void ReceiveHandler(IPEndPoint client, byte[] data);

    public delegate void ClonableSubscribeHandler(IPEndPoint client, ClonableObject msg);

    public delegate void SubscribeHandler(IPEndPoint client, BaseMessage msg);

    public enum ClientType : byte
    {
        Producer = 0,
        Consumer = 1,
        Other = 9
    }
    /// <summary>
    /// 共有类
    /// </summary>
    public class Common
    {
        public const int MsgIDSize = 16;
        public const int TypeInfoSize = 0;//10240;
        public const int TypeSize = 0;//4;
        /// <summary>
        /// 缓冲区的大小
        /// </summary>
        public const int BufferSize = 1024;
        /// <summary>
        /// 接收的超时时间
        /// </summary>
        public const int ReceiveTimeout = 2000;
        /// <summary>
        /// 出错标识
        /// </summary>
        public const string Error = "$Error$";
        /// <summary>
        /// 已收标识
        /// </summary>
        public const string Received = "$Received$";
        /// <summary>
        /// 已读标识
        /// </summary>
        public const string HadReaded = "$HadReaded$";
        /// <summary>
        /// 心跳标识
        /// </summary>
        public const string HeartBeat = "$HeartBeat$";
        //public const string EndPointBegin = "$[";
        //public const string EndPointEnd = "]$";
        //public const string LeaveMsg = "$LEAVE$";
        //public const string ExitMsg = "$EXIT$";

        //public static string GetTarget(string rawData, out byte[] data)
        //{
        //    string target = string.Empty;
        //    data = Array.ConvertAll<char, byte>(rawData.ToCharArray(), a => (byte)a);
        //    if (rawData.StartsWith(EndPointBegin))
        //    {
        //        int index = rawData.IndexOf(EndPointEnd);
        //        if (index > EndPointBegin.Length)
        //        {
        //            target = rawData.Substring(EndPointBegin.Length, index - EndPointBegin.Length);
        //            data = Array.ConvertAll<char, byte>(rawData.Substring(index + EndPointBegin.Length).ToCharArray(), a => (byte)a);
        //        }
        //    }
        //    return target;
        //}

        public static byte[] ParseHex(string text)
        {
            byte[] ret = new byte[text.Length / 2];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
            }
            return ret;
        }

        public static Guid GetGuid(byte[] data)
        {
            try
            {
                return new Guid(data);
            }
            catch
            {
                return Guid.Empty;
            }
        }
        /// <summary>
        /// 占用16字节
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static byte[] GetData(Guid guid)
        {
            return guid.ToByteArray();
        }
        /// <summary>
        /// 消息解包
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static T GetMsg<T>(object[] args = null) where T : BaseMessage
        {
            Type type = typeof(T);
            try
            {
                object o = type.Assembly.CreateInstance(type.FullName, true, System.Reflection.BindingFlags.CreateInstance | System.Reflection.BindingFlags.Instance, null, args, System.Globalization.CultureInfo.CurrentCulture, null);
                return o as T;
            }
            catch (Exception e)
            {
                //
            }
            return null;
        }
        /// <summary>
        /// 消息解包
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static T GetMsgClonable<T>(object[] args = null) where T : ClonableObject
        {
            Type type = typeof(T);
            try
            {
                object o = type.Assembly.CreateInstance(type.FullName, true, System.Reflection.BindingFlags.CreateInstance | System.Reflection.BindingFlags.Instance, null, args, System.Globalization.CultureInfo.CurrentCulture, null);
                return o as T;
            }
            catch (Exception e)
            {
                //
            }
            return null;
        }
        /// <summary>
        /// 消息解包
        /// </summary>
        /// <param name="data"></param>
        ///// <param name="type"></param>
        ///// <param name="clientType"></param>
        /// <returns></returns>
        public static object GetMsg(byte[] data)//, out Type type)//, out ClientType clientType)
        {
            //type = null;
            //clientType = ClientType.Other;
            if (data.Length < TypeInfoSize)
                return null;

            //clientType = Deserialize<ClientType>(GetBytes(data, 0, 1));
            //int typeSize = BitConverter.ToInt32(GetBytes(data, 1, TypeSize), 0);
            //type = Deserialize<Type>(GetBytes(data, 1 + TypeSize, typeSize));
            object message = Deserialize(GetBytes(data, 1 + TypeSize + TypeInfoSize));
            return message;
        }
        /// <summary>
        /// 消息解包（注意：这里在服务器端执行会出错！一定要设法在客户端执行！）
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T GetMsg<T>(byte[] data) where T : BaseMessage
        {
            //type = null;
            //clientType = ClientType.Other;
            if (data.Length < TypeInfoSize)
                return default(T);

            //clientType = Deserialize<ClientType>(GetBytes(data, 0, 1));
            //int typeSize = BitConverter.ToInt32(GetBytes(data, 1, TypeSize), 0);
            //type = Deserialize<Type>(GetBytes(data, 1 + TypeSize, typeSize));
            T message = Deserialize<T>(GetBytes(data, 1 + TypeSize + TypeInfoSize));//注意：这里在服务器端执行会出错！一定要设法在客户端执行！
            return message;
        }
        /// <summary>
        /// 消息解包（注意：这里在服务器端执行会出错！一定要设法在客户端执行！）
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T GetMsgClonable<T>(byte[] data) where T : ClonableObject
        {
            //type = null;
            //clientType = ClientType.Other;
            if (data.Length < TypeInfoSize)
                return default(T);

            //clientType = Deserialize<ClientType>(GetBytes(data, 0, 1));
            //int typeSize = BitConverter.ToInt32(GetBytes(data, 1, TypeSize), 0);
            //type = Deserialize<Type>(GetBytes(data, 1 + TypeSize, typeSize));
            T message = Deserialize<T>(GetBytes(data, 1 + TypeSize + TypeInfoSize));//注意：这里在服务器端执行会出错！一定要设法在客户端执行！
            return message;
        }
        /// <summary>
        /// 消息封包
        /// </summary>
        /// <param name="clientType"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] GetData<T>(ClientType clientType, T msg) where T : BaseMessage
        {
            byte[] ctBytes = new byte[1];
            ctBytes[0] = (byte)clientType;
            //byte[] typeInfo = new byte[TypeInfoSize];
            //byte[] typeInfoBytes = Serialize<Type>(msg.Type);
            //byte[] typeSize = BitConverter.GetBytes(typeInfoBytes.Length);//typeSize为4个字节的数组
            //if (typeInfoBytes.Length > typeInfo.Length)
            //    throw new Exception("该类型[" + msg.Type.FullName + "]的信息占据空间大于" + TypeInfoSize + "字节！");
            //for (int i = 0; i < typeInfoBytes.Length; i++)
            //{
            //    typeInfo[i] = typeInfoBytes[i];
            //}
            byte[] message = Serialize<T>(msg);
            List<byte> buffer = new List<byte>(ctBytes);
            //buffer.AddRange(typeSize);
            //buffer.AddRange(typeInfo);
            buffer.AddRange(message);
            return buffer.ToArray();
        }
        /// <summary>
        /// 消息封包
        /// </summary>
        /// <param name="clientType"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] GetDataClonable<T>(ClientType clientType, T msg) where T : ClonableObject
        {
            byte[] ctBytes = new byte[1];
            ctBytes[0] = (byte)clientType;
            //byte[] typeInfo = new byte[TypeInfoSize];
            //byte[] typeInfoBytes = Serialize<Type>(msg.Type);
            //byte[] typeSize = BitConverter.GetBytes(typeInfoBytes.Length);//typeSize为4个字节的数组
            //if (typeInfoBytes.Length > typeInfo.Length)
            //    throw new Exception("该类型[" + msg.Type.FullName + "]的信息占据空间大于" + TypeInfoSize + "字节！");
            //for (int i = 0; i < typeInfoBytes.Length; i++)
            //{
            //    typeInfo[i] = typeInfoBytes[i];
            //}
            byte[] message = Serialize<T>(msg);
            List<byte> buffer = new List<byte>(ctBytes);
            //buffer.AddRange(typeSize);
            //buffer.AddRange(typeInfo);
            buffer.AddRange(message);
            return buffer.ToArray();
        }
        /// <summary>
        /// 消息封包
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] GetData<T>(T msg) where T : BaseMessage
        {
            //byte[] typeInfo = new byte[TypeInfoSize];
            //byte[] typeInfoBytes = Serialize<Type>(msg.Type);
            //byte[] typeSize = BitConverter.GetBytes(typeInfoBytes.Length);//typeSize为4个字节的数组
            //if (typeInfoBytes.Length > typeInfo.Length)
            //    throw new Exception("该类型[" + msg.Type.FullName + "]的信息占据空间大于" + TypeInfoSize + "字节！");
            //for (int i = 0; i < typeInfoBytes.Length; i++)
            //{
            //    typeInfo[i] = typeInfoBytes[i];
            //}
            byte[] message = Serialize<T>(msg);
            List<byte> buffer = new List<byte>();
            //buffer.AddRange(typeSize);
            //buffer.AddRange(typeInfo);
            buffer.AddRange(message);
            return buffer.ToArray();
        }
        /// <summary>
        /// 序列化消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] Serialize(object msg)
        {
            byte[] data = null;
            using (MemoryStream ms = new MemoryStream())
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, json);
                data = ms.ToArray();
            }
            return data;
        }
        /// <summary>
        /// 反序列化消息
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object Deserialize(byte[] data)
        {
            if (data == null)
                return null;

            object obj = null;
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter bf = new BinaryFormatter();
                string json = bf.Deserialize(ms).ToString();
                obj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            }
            return obj;
        }
        /// <summary>
        /// 序列化消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] Serialize<T>(T msg)
        {
            byte[] data = null;
            using (MemoryStream ms = new MemoryStream())
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, json);
                data = ms.ToArray();
            }
            return data;
        }
        /// <summary>
        /// 反序列化消息
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T Deserialize<T>(byte[] data)
        {
            if (data == null)
                return default(T);

            T obj = default(T);
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter bf = new BinaryFormatter();
                string json = bf.Deserialize(ms).ToString();
                obj = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            }
            return obj;
        }
        /// <summary>
        /// GetBytes
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] GetBytes(byte[] data, int index, int length)
        {
            if (index + length > data.Length)
                return null;

            List<byte> buffer = new List<byte>();
            for (int i = index; i < index + length; i++)
            {
                buffer.Add(data[i]);
            }
            return buffer.ToArray();
        }
        /// <summary>
        /// GetBytes
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] GetBytes(byte[] data, int offset)
        {
            if (offset > data.Length - 1)
                return null;

            List<byte> buffer = new List<byte>();
            for (int i = offset; i < data.Length; i++)
            {
                buffer.Add(data[i]);
            }
            return buffer.ToArray();
        }
        /// <summary>
        /// GetString
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <param name="encoder"></param>
        /// <returns></returns>
        public static string GetString(byte[] data, int index, int length, System.Text.Encoding encoder)
        {
            if (index + length > data.Length)
                return null;

            List<byte> buffer = new List<byte>();
            for (int i = index; i < index + length; i++)
            {
                if (data[i] != '\0')
                    buffer.Add(data[i]);
            }
            return encoder.GetString(buffer.ToArray(), 0, buffer.Count);
        }
        /// <summary>
        /// GetString
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="encoder"></param>
        /// <returns></returns>
        public static string GetString(byte[] data, int offset, System.Text.Encoding encoder)
        {
            if (offset > data.Length - 1)
                return null;

            List<byte> buffer = new List<byte>();
            for (int i = offset; i < data.Length; i++)
            {
                if (data[i] != '\0')
                    buffer.Add(data[i]);
            }
            return encoder.GetString(buffer.ToArray(), 0, buffer.Count);
        }
        /// <summary>
        /// CRC16
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dataLength"></param>
        /// <returns></returns>
        public static bool CRC16(string data, int dataLength)
        {
            byte[] input = System.Text.Encoding.UTF8.GetBytes(data);
            return CRC16(input, dataLength);
        }
        /// <summary>
        /// CRC16
        /// </summary>
        /// <param name="input"></param>
        /// <param name="dataLength"></param>
        /// <returns></returns>
        public static bool CRC16(byte[] input, int dataLength)
        {
            return CRC16Util.CRC16(input, dataLength);
        }

        public static byte[] GetBytes(string msg, Encoding encoding)
        {
            if (msg == null)
                return null;
            return encoding.GetBytes(msg);
        }

        public static bool IsValidHostOrAddress(string hostNameOrAddress, out bool isHostName)
        {
            isHostName = false;
            if (hostNameOrAddress != null)
            {
                try
                {
                    IPHostEntry host = Dns.GetHostEntry(hostNameOrAddress);
                    isHostName = host.HostName.Equals(hostNameOrAddress, StringComparison.InvariantCultureIgnoreCase);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        public static string ClientId
        {
            get
            {
                List<string> macs = GetMacByNetworkInterface();
                if (macs.Count > 0)
                    return macs[0];
                return GetCpuID();
            }
        }

        ///<summary>
        /// 通过WMI读取系统信息里的网卡MAC
        ///</summary>
        ///<returns></returns>
        public static List<string> GetMacByWMI()
        {
            List<string> macs = new List<string>();
            try
            {
                string mac = "";
                using (ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration"))
                {
                    using (ManagementObjectCollection moc = mc.GetInstances())
                    {
                        foreach (ManagementObject mo in moc)
                        {
                            if ((bool)mo["IPEnabled"])
                            {
                                mac = mo["MacAddress"].ToString();
                                macs.Add(mac);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            return macs;
        }

        ///返回描述本地计算机上的网络接口的对象(网络接口也称为网络适配器)。
        public static NetworkInterface[] NetCardInfo()
        {
            return NetworkInterface.GetAllNetworkInterfaces();
        }

        ///<summary>
        /// 通过NetworkInterface读取网卡Mac
        ///</summary>
        ///<returns></returns>
        public static List<string> GetMacByNetworkInterface()
        {
            List<string> macs = new List<string>();
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface ni in interfaces)
            {
                macs.Add(ni.GetPhysicalAddress().ToString());
            }
            return macs;
        }

        public static List<IPAddress> GetIPsV4()
        {
            List<IPAddress> ips = new List<IPAddress>();
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    ips.Add(ip);
            }
            return ips;
        }
        /// <summary>
        /// 取第一块CPU编号
        /// </summary>
        /// <returns></returns>   
        public static string GetCpuID()
        {
            try
            {
                string strCpuID = "";
                using (ManagementClass mc = new ManagementClass("Win32_Processor"))
                {
                    using (ManagementObjectCollection moc = mc.GetInstances())
                    {
                        foreach (ManagementObject mo in moc)
                        {
                            strCpuID = mo.Properties["ProcessorId"].Value.ToString();
                            break;
                        }
                    }
                }
                return strCpuID;
            }
            catch
            {
                return "";
            }
        } 

        /// <summary>
        /// 取第一块硬盘编号
        /// </summary>
        /// <returns></returns>   
        public static string GetHardDiskID()
        {
            try
            {
                string strHardDiskID = "";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        strHardDiskID = mo["SerialNumber"].ToString().Trim();
                        break;
                    }
                }
                return strHardDiskID;
            }
            catch
            {
                return "";
            }
        }
    }

    public class ClonableMessageArgs : EventArgs
    {
        int msgId;

        public int MsgId
        {
            get { return msgId; }
        }

        public ClonableMessageArgs(int msgId)
        {
            this.msgId = msgId;
        }
    }

    public class MessageArgs : EventArgs
    {
        Guid msgId;

        public Guid MsgId
        {
            get { return msgId; }
        }

        public MessageArgs(Guid msgId)
        {
            this.msgId = msgId;
        }
    }

    class CRC16Util
    {
        private static UInt16[] CRC16TABLE = new UInt16[256]
        {
        0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50a5, 0x60c6, 0x70e7, 
		0x8108, 0x9129, 0xa14a, 0xb16b, 0xc18c, 0xd1ad, 0xe1ce, 0xf1ef, 
		0x1231, 0x0210, 0x3273, 0x2252, 0x52b5, 0x4294, 0x72f7, 0x62d6, 
		0x9339, 0x8318, 0xb37b, 0xa35a, 0xd3bd, 0xc39c, 0xf3ff, 0xe3de, 
		0x2462, 0x3443, 0x0420, 0x1401, 0x64e6, 0x74c7, 0x44a4, 0x5485, 
		0xa56a, 0xb54b, 0x8528, 0x9509, 0xe5ee, 0xf5cf, 0xc5ac, 0xd58d, 
		0x3653, 0x2672, 0x1611, 0x0630, 0x76d7, 0x66f6, 0x5695, 0x46b4, 
		0xb75b, 0xa77a, 0x9719, 0x8738, 0xf7df, 0xe7fe, 0xd79d, 0xc7bc, 
		0x48c4, 0x58e5, 0x6886, 0x78a7, 0x0840, 0x1861, 0x2802, 0x3823, 
		0xc9cc, 0xd9ed, 0xe98e, 0xf9af, 0x8948, 0x9969, 0xa90a, 0xb92b, 
		0x5af5, 0x4ad4, 0x7ab7, 0x6a96, 0x1a71, 0x0a50, 0x3a33, 0x2a12, 
		0xdbfd, 0xcbdc, 0xfbbf, 0xeb9e, 0x9b79, 0x8b58, 0xbb3b, 0xab1a, 
		0x6ca6, 0x7c87, 0x4ce4, 0x5cc5, 0x2c22, 0x3c03, 0x0c60, 0x1c41, 
		0xedae, 0xfd8f, 0xcdec, 0xddcd, 0xad2a, 0xbd0b, 0x8d68, 0x9d49, 
		0x7e97, 0x6eb6, 0x5ed5, 0x4ef4, 0x3e13, 0x2e32, 0x1e51, 0x0e70, 
		0xff9f, 0xefbe, 0xdfdd, 0xcffc, 0xbf1b, 0xaf3a, 0x9f59, 0x8f78, 
		0x9188, 0x81a9, 0xb1ca, 0xa1eb, 0xd10c, 0xc12d, 0xf14e, 0xe16f, 
		0x1080, 0x00a1, 0x30c2, 0x20e3, 0x5004, 0x4025, 0x7046, 0x6067, 
		0x83b9, 0x9398, 0xa3fb, 0xb3da, 0xc33d, 0xd31c, 0xe37f, 0xf35e, 
		0x02b1, 0x1290, 0x22f3, 0x32d2, 0x4235, 0x5214, 0x6277, 0x7256, 
		0xb5ea, 0xa5cb, 0x95a8, 0x8589, 0xf56e, 0xe54f, 0xd52c, 0xc50d, 
		0x34e2, 0x24c3, 0x14a0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405, 
		0xa7db, 0xb7fa, 0x8799, 0x97b8, 0xe75f, 0xf77e, 0xc71d, 0xd73c, 
		0x26d3, 0x36f2, 0x0691, 0x16b0, 0x6657, 0x7676, 0x4615, 0x5634, 
		0xd94c, 0xc96d, 0xf90e, 0xe92f, 0x99c8, 0x89e9, 0xb98a, 0xa9ab, 
		0x5844, 0x4865, 0x7806, 0x6827, 0x18c0, 0x08e1, 0x3882, 0x28a3, 
		0xcb7d, 0xdb5c, 0xeb3f, 0xfb1e, 0x8bf9, 0x9bd8, 0xabbb, 0xbb9a, 
		0x4a75, 0x5a54, 0x6a37, 0x7a16, 0x0af1, 0x1ad0, 0x2ab3, 0x3a92, 
		0xfd2e, 0xed0f, 0xdd6c, 0xcd4d, 0xbdaa, 0xad8b, 0x9de8, 0x8dc9, 
		0x7c26, 0x6c07, 0x5c64, 0x4c45, 0x3ca2, 0x2c83, 0x1ce0, 0x0cc1, 
		0xef1f, 0xff3e, 0xcf5d, 0xdf7c, 0xaf9b, 0xbfba, 0x8fd9, 0x9ff8, 
        0x6e17, 0x7e36, 0x4e55, 0x5e74, 0x2e93, 0x3eb2, 0x0ed1, 0x1ef0
        };


        private static UInt16 CRC16_Table(byte[] pcrc, byte count)
        {
            UInt16 crc16 = 0;
            byte crcregister;

            //for( ; count > 0; count--)
            for (int i = 0; i < count; i++)
            {
                crcregister = (byte)(crc16 >> 8);
                crc16 <<= 8;
                crc16 ^= CRC16TABLE[crcregister ^ pcrc[i]];
                //	pcrc++;
            }
            return (crc16);
        }

        private static int CRC16NORMAL(byte[] puchMsg, int usDataLen)
        {

            int iTemp = 0xffff;

            for (int i = 0; i < usDataLen; i++)
            {
                iTemp ^= puchMsg[i];
                for (int j = 0; j < 8; j++)
                {
                    int flag = iTemp & 0x01;
                    iTemp >>= 1;
                    if (flag == 1)
                    {
                        iTemp ^= 0xa001;
                    }
                }
            }
            return iTemp;
        }

        public static bool CRC16(byte[] puchMsg, int usDataLen)
        {
            int iCRC = CRC16NORMAL(puchMsg, usDataLen);
            int iCRCHigh = (byte)(iCRC / 256);
            int iCRCLow = (byte)(iCRC % 256);
            if ((puchMsg[usDataLen] == iCRCHigh) && (puchMsg[usDataLen + 1] == iCRCLow))
                return true;
            else
                return false;
        }

        public static bool CRC16Table(byte[] puchMsg, byte usDataLen)
        {
            UInt16 iCRC = CRC16_Table(puchMsg, usDataLen);
            byte iCRCHigh = (byte)(iCRC / 256);
            byte iCRCLow = (byte)(iCRC % 256);
            if ((puchMsg[usDataLen] == iCRCHigh) && (puchMsg[usDataLen + 1] == iCRCLow))
                return true;
            else
                return false;
        }
    }
}
