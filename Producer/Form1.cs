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

namespace Producer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        KellMQ.ClonableProducer mq;
        KellMQ.ClonableProducer mq2;

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                textBox2.Text = ConfigurationManager.AppSettings["server"];
                //producer.Submited += new EventHandler<KellMQ.ClonableMessageArgs>(producer_Submited);
                mq = new KellMQ.ClonableProducer(Convert.ToInt32(ConfigurationManager.AppSettings["port"]), ConfigurationManager.AppSettings["service"]);
                mq2 = new KellMQ.ClonableProducer(Convert.ToInt32(ConfigurationManager.AppSettings["port"]), ConfigurationManager.AppSettings["service2"]);
            }
            catch (Exception ex)
            {
                MessageBox.Show("已经创建过该信道：" + ex.Message);
            }
        }

        //void producer_Submited(object sender, KellMQ.ClonableMessageArgs e)
        //{
        //    if (e.MsgId == Guid.Empty)
        //        MessageBox.Show("提交成功：消息保存失败！");
        //    else
        //        MessageBox.Show("提交成功：消息[" + e.MsgId + "]保存成功！");
        //}

        private void button1_Click(object sender, EventArgs e)
        {
            if (mq != null)
            {
                MyClass my = new MyClass(1, "kell");
                KellMQ.ClonableObject msg = new MyMessageClonable<MyClass>(my);
                toolStripStatusLabel1.Text = msg.GetHashCode().ToString();
                textBox1.Text = msg.GetHashCode().ToString();
                AddEvent(msg);
            }
        }

        public void AddEvent(KellMQ.ClonableObject msg)
        {
            if (mq != null)
            {
                try
                {
                    if (mq.SendMsg<KellMQ.ClonableObject>(msg))
                    {
                        MessageBox.Show("发送消息成功！");
                    }
                    else
                    {
                        MessageBox.Show("发送消息失败！");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("发送订阅型消息出错：" + ex.Message);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MyClass my = new MyClass(2, "bill");
            KellMQ.ClonableObject msg = new MyMessageClonable<MyClass>(my);
            toolStripStatusLabel1.Text = msg.GetHashCode().ToString();
            textBox1.Text = msg.GetHashCode().ToString();
            AddEvent(msg);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mq != null)
                mq.Dispose();
            if (mq2 != null)
                mq2.Dispose();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            MyClass my = new MyClass(1, "kellpush");
            KellMQ.ClonableObject msg = new MyMessageClonable<MyClass>(my);
            toolStripStatusLabel1.Text = msg.GetHashCode().ToString();
            textBox1.Text = msg.GetHashCode().ToString();
            try
            {
                if (mq2.SendMsg<KellMQ.ClonableObject>(msg))
                {
                    MessageBox.Show("发送消息成功！");
                }
                else
                {
                    MessageBox.Show("发送消息失败！");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送消费型消息1出错：" + ex.Message);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            MyClass my = new MyClass(2, "billpush");
            KellMQ.ClonableObject msg = new MyMessageClonable<MyClass>(my);
            toolStripStatusLabel1.Text = msg.GetHashCode().ToString();
            textBox1.Text = msg.GetHashCode().ToString();
            try
            {
                if (mq2.SendMsg<KellMQ.ClonableObject>(msg))
                {
                    MessageBox.Show("发送消息成功！");
                }
                else
                {
                    MessageBox.Show("发送消息失败！");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送消费型消息2出错：" + ex.Message);
            }
        }
    }
}
