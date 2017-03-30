using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RabbitMqPublisherDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.WriteLine(System.Text.Encoding.GetEncoding("GB2312"));
            
            try
            {
                //从工厂中拿到实例 本地host、用户admin
                var factory = new ConnectionFactory()
                {
                    UserName = "admin",
                    Password = "admin",
                    HostName = "192.168.203.128"
                };
                //创建连接
                using (var connection = factory.CreateConnection())
                {
                    //创建返回一个新的频道
                    using (var channel = connection.CreateModel())
                    {
                        //声明队列
                        //channel.QueueDeclare("firstTest", true, false, false, null);
                        channel.QueueDeclare("lazyqueue", true, false, false, new Dictionary<string, object>
                        {
                            { "x-queue-mode","lazy"}
                        });
                        //
                        channel.ConfirmSelect();
                        var sw = new Stopwatch();
                        sw.Start();
                        for (var i = 1; i <= 10000000; i++)
                        {
                            MessageA mesA = new MessageA
                            {
                                Sid = Guid.NewGuid().ToString(),
                                Mid = Guid.NewGuid().ToString(),
                                CodeType = i,
                                CreateTime = DateTime.Now,
                                OrgID = Guid.NewGuid(),
                                TraceableCode = Guid.NewGuid().ToString(),
                                OrderNo = Guid.NewGuid().ToString("N")
                            };
                            //byte[] 传输
                            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mesA));
                            //var msg = Encoding.UTF8.GetBytes("Hello RabbitMQ -- :" + i);
                            //发布消息
                            var props = channel.CreateBasicProperties();
                            //props.Persistent = true;
                            //props.Persistent = true; 和 props.DeliveryMode = 2; 效果相同
                            //消息持久化到Disk
                            props.DeliveryMode = 2;
                            channel.BasicPublish(string.Empty, routingKey: "lazyqueue", basicProperties: props, body: body);
                        }

                        //批量confirm模式(batch)
                        bool isok = channel.WaitForConfirms();
                        if (isok)
                        {
                            Console.WriteLine("OK");
                        }
                        else
                        {
                            Console.WriteLine("消息确认失败，需要重新发送！");
                        }
                        sw.Stop();
                        Console.WriteLine("耗时{0}毫秒",sw.ElapsedMilliseconds);

                    }
                }
                Console.ReadKey();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error:"+ ex.Message);
                throw;
            }
            
        }
    }
}