using Dapper;
using EasyNetQ;
using EasyNetQ.Topology;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSchedulerDemo
{
    class Program
    {

        private const int NumberOfConsume = 50;
        private static List<ConsumerClient> Clients = new List<ConsumerClient>();
        private static object LockingObject = new object();

        static void Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.WriteLine(System.Text.Encoding.GetEncoding("GB2312"));
            
            var advancedBus = AdvanceBusHelper.CreateInstance();
            

            if (!advancedBus.IsConnected)
            {

                Console.WriteLine("advancedBus Is Not Connected");
                advancedBus =
                        RabbitHutch.CreateBus(
                            "host=192.168.203.128;requestedHeartbeat=300;timeout=0;username=admin;password=admin")
                            .Advanced;
            }

            FluentScheduler.JobManager.AddJob(() =>
            {
                //GetMessage(advancedBus);
                ConsumeMessage(advancedBus);
            }, t =>
            {
                t.ToRunNow().AndEvery(120).Minutes();
            });

            Console.Read();
            
        }


        private static void ConsumeMessage(IAdvancedBus advancedBus)
        {
            try
            {
                var queueName = "ZNJB.Order";
                var routingKey = "ZNJB.*";
                var queue =advancedBus.QueueDeclare(queueName);
                var count = advancedBus.MessageCount(queue);
                if (count <= 0)
                {
                    Console.WriteLine("count=0");
                    advancedBus.Dispose();
                    return;
                }

                var exchange = advancedBus.ExchangeDeclare("ZNJB.exchange", ExchangeType.Topic);
                var binding = advancedBus.Bind(exchange, queue, routingKey);

                for (var message = 1; message <= NumberOfConsume; message++)
                {
                    var Client = new ConsumerClient();

                    lock (Clients)
                    {
                        Clients.Add(Client);
                    }


                    Task.Run(() =>
                    {
                        Client.StartClient(advancedBus, queue);

                        lock (Clients)
                        {
                            Clients.Remove(Client);
                            Monitor.PulseAll(Clients);
                        }
                    });

                    while (Clients.Count >= 500)
                    {
                        lock (Clients)
                        {
                            Monitor.Wait(Clients, 5000);
                        }
                    }
                }

                while (Clients.Count > 0)
                {
                    lock (Clients)
                    {
                        Monitor.Wait(Clients, 5000);
                    }
                }

                lock (LockingObject)
                {
                    Monitor.PulseAll(LockingObject);
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
            
        }






        private static void GetMessage(IAdvancedBus advancedBus)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                
                var queueName = "ZNJB.Order";
                var routingKey = "ZNJB.*";
                var queue = advancedBus.QueueDeclare(queueName);
                var count = advancedBus.MessageCount(queue);
                if (count <= 0)
                {
                    Console.WriteLine("count=0");
                    advancedBus.Dispose();
                    return;
                }
                
                var exchange = advancedBus.ExchangeDeclare("ZNJB.exchange", ExchangeType.Topic);
                var binding = advancedBus.Bind(exchange, queue, routingKey);

                //advancedBus.Consume(queue, x => x.Add<TraceableCodeSub>((message, info) => { HandleMessage(message); }))
                //    .SafeDispose();

                //{"Properties":{"ContentType":null,"ContentEncoding":null,"Headers":{},"DeliveryMode":0,"Priority":0,"CorrelationId":null,"ReplyTo":"my_reply_queue","Expiration":null,"MessageId":null,"Timestamp":0,"Type":null,"UserId":null,"AppId":"e2d1f4209ef5401f992313ba37fcae6c","ClusterId":null,"ContentTypePresent":false,"ContentEncodingPresent":false,"HeadersPresent":true,"DeliveryModePresent":false,"PriorityPresent":false,"CorrelationIdPresent":false,"ReplyToPresent":true,"ExpirationPresent":false,"MessageIdPresent":false,"TimestampPresent":false,"TypePresent":false,"UserIdPresent":false,"AppIdPresent":true,"ClusterIdPresent":false},
                //"MessageType":"ZNJB.Entitys.TraceableCodeSub, ZNJB.Entitys, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                //"Body":{
                //"Sid":"e2d1f4209ef5401f992313ba37fcae6c",
                //"Mid":"0cf59b07c5ae4cee8a8f9bdc0ccae4d4",
                //"OrderNo":"ZNJB37270155702","CodeType":1,
                //"TraceableCode":"11000301170008103075629506",
                //"CreateTime":"2017-03-07T16:49:15.7303122+08:00",
                //"OrgID":"edd45816-cda2-4f9f-9005-af39de8656a4"}}






                advancedBus.Consume(queue, (body, properties, info) =>
                {
                    Task t = Task.Factory.StartNew(() =>
                    {
                        var message = System.Text.Encoding.UTF8.GetString(body);
                        var success = InsertToDb(message);
                    });

                    t.Wait();
                });

                advancedBus.Consume(queue, (body, properties, info) => Task.Factory.StartNew(() =>
                {
                    //Console.WriteLine("ConsumerTag: {0}", info.ConsumerTag);
                    //Console.WriteLine("DeliverTag: {0}", info.DeliverTag);
                    //Console.WriteLine("Redelivered: {0}", info.Redelivered);
                    //Console.WriteLine("Exchange: {0}", info.Exchange);
                    //Console.WriteLine("RoutingKey: {0}", info.RoutingKey);

                    var message = System.Text.Encoding.UTF8.GetString(body);
                    InsertToDb(message);
                })
                .ContinueWith((o) => Task.Factory.StartNew(() =>
                {
                    if (o.Status == TaskStatus.Faulted)
                    {
                        Console.WriteLine("Faulted");
                    }
                }))
                );
                sw.Stop();
                //Console.WriteLine("客户端接收耗时：" + sw.ElapsedMilliseconds + "毫秒");
                //advancedBus.Dispose();
                //Console.ReadKey();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        //private Func<byte[] body,MessageProperties properties,MessageReceivedInfo info,Task> fun()

        //Func<byte[], MessageProperties, MessageReceivedInfo, Task> func => s= null;

        private static void HandleMessage(object message)
        {

            Console.WriteLine(message.ToString());
            //throw new NotImplementedException();
        }

        private static bool InsertToDb(string message)
        {
            return false;
            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(message);
                JToken a = jo["Body"];
                JObject obj = (JObject)a;
                var sub = JsonConvert.DeserializeObject<TraceableCodeSub>(obj.ToString());

                IDbConnection dbConnection = DbHelper.GetConnection();
                if (dbConnection.State != ConnectionState.Open)
                    dbConnection.Open();

                string sql = @"INSERT INTO [dbo].[TraceableCodeSub]
                           ([Sid]
                           ,[Mid]
                           ,[OrderNo]
                           ,[CodeType]
                           ,[TraceableCode]
                           ,[CreateTime]
                           ,[OrgId])
                     VALUES
                           (@SID
                           ,@Mid
                           ,@OrderNo
                           ,@CodeType
                           ,@TraceableCode
                           ,@CreateTime
                           ,@OrgId)";

                dbConnection.Execute(sql, sub);
                return true;

                //using (IDbConnection dbConnection = DbHelper.GetConnection())
                //{
                //    if (dbConnection.State != ConnectionState.Open)
                //        dbConnection.Open();

                //    string sql = @"INSERT INTO [dbo].[TraceableCodeSub]
                //           ([Sid]
                //           ,[Mid]
                //           ,[OrderNo]
                //           ,[CodeType]
                //           ,[TraceableCode]
                //           ,[CreateTime]
                //           ,[OrgId])
                //     VALUES
                //           (@SID
                //           ,@Mid
                //           ,@OrderNo
                //           ,@CodeType
                //           ,@TraceableCode
                //           ,@CreateTime
                //           ,@OrgId)";

                //    dbConnection.Execute(sql, sub);
                //}
                //Console.WriteLine("OrderNo:{0},TraceableCode:{1}", sub.OrderNo,sub.TraceableCode);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                //return false;
                throw e;
            }
        }

        private static void Test()
        {
            Console.WriteLine(DateTime.Now);
        }
    }
}