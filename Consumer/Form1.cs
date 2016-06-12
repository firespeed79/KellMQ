using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Configuration;

namespace Consumer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        KellMQ.ClonableConsumer mq;
        KellMQ.ClonableConsumer mq2;

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                textBox2.Text = ConfigurationManager.AppSettings["server"];
                mq = new KellMQ.ClonableConsumer(Convert.ToInt32(ConfigurationManager.AppSettings["port"]), ConfigurationManager.AppSettings["service"]);
                mq.Submited += new EventHandler<KellMQ.ClonableMessageArgs>(consumer_Submited);
                mq2 = new KellMQ.ClonableConsumer(Convert.ToInt32(ConfigurationManager.AppSettings["port2"]), ConfigurationManager.AppSettings["service2"]);
                mq2.Submited += new EventHandler<KellMQ.ClonableMessageArgs>(consumer2_Submited);
            }
            catch (Exception ex)
            {
                MessageBox.Show("已经创建过该信道：" + ex.Message);
            }
        }

        void consumer_Submited(object sender, KellMQ.ClonableMessageArgs e)
        {
            if (e.MsgId == 0)
                MessageBox.Show("订阅提交成功：找不到要订阅的消息！");
            else
                MessageBox.Show("订阅提交成功：消息[" + e.MsgId + "]被成功订阅！");
        }

        void consumer2_Submited(object sender, KellMQ.ClonableMessageArgs e)
        {
            if (e.MsgId == 0)
                MessageBox.Show("消费提交成功：找不到要消费的消息！");
            else
                MessageBox.Show("消费提交成功：消息[" + e.MsgId + "]被成功消费！");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (mq != null)
            {
                mq.SubscrMessage();
                mq.Query(KellMQ.Common.ClientId);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (mq != null)
                mq.CancelSubscribeMessage();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (mq2 != null)
                mq2.Query(KellMQ.Common.ClientId);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mq != null)
            {
                mq.CancelSubscribeMessage();
                mq.Dispose();
            }
            if (mq2 != null)
            {
                mq2.CancelSubscribeMessage();
                mq2.Dispose();
            }
        }
    }
}
