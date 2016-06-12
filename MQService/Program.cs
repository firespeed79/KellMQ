using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KellMQ;
using System.Threading;
using System.Net;

namespace MQService
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            KellMQ.CommunicationService.Received += new ReceiveHandler(Receiver_Received);
            KellMQ.CommunicationService.Received2 += new ReceiveHandler(Receiver_Received2);
            //KellMQ.CommunicationService.ClientOffline += new OfflineHandler(Receiver_ClientOffline);
            bool flag = KellMQ.CommunicationService.Start();
            if (flag)
            {
                Logs.Create("服务启动成功！");
                //Console.WriteLine("服务启动成功！");
                while (true)
                {
                    Console.WriteLine("停止服务请键入（Y）...");
                    string yn = Console.ReadLine().Trim();
                    if (yn.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
                    {
                        KellMQ.CommunicationService.Stop();
                        break;
                    }
                }
                Console.Write("退出请回车...");
                Console.Read();
            }
            else
            {
                Logs.Create("服务启动失败！");
                Console.WriteLine("服务启动失败！");
                Console.Write("退出请回车...");
                Console.Read();
            }
        }

        //static void Receiver_ClientOffline(object sender, System.Net.IPEndPoint client)
        //{
        //    Console.WriteLine("客户端[" + client.ToString() + "]离线...");
        //}

        static void Receiver_Received(System.Net.IPEndPoint client, byte[] data)
        {
            if (data != null && data.Length > 0)
                Console.WriteLine("收到来自订阅型客户端[" + client.ToString() + "]的数据共" + data.Length + "字节。");
            else
                Console.WriteLine("收到来自订阅型客户端[" + client.ToString() + "]的消息，无数据。");
        }

        static void Receiver_Received2(System.Net.IPEndPoint client, byte[] data)
        {
            if (data != null && data.Length > 0)
                Console.WriteLine("收到来自消费型客户端[" + client.ToString() + "]的数据共" + data.Length + "字节。");
            else
                Console.WriteLine("收到来自消费型客户端[" + client.ToString() + "]的消息，无数据。");
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string str = string.Format("服务程序错误:{0}，程序状态：{1}", e.ExceptionObject.ToString(), (e.IsTerminating ? "终止" : "未终止"));
            Logs.Create("MQService " + str);
        }
    }
}
