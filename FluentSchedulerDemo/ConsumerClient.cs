using EasyNetQ;
using EasyNetQ.Topology;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;

namespace FluentSchedulerDemo
{
    public class ConsumerClient
    {
        private IDisposable _consumer;
        public object LockingObject = new object();
        //private TimeSpan _timeToLiveWhileIdleForQueue;
        private List<TraceableCodeSub> list = new List<TraceableCodeSub>();


        public void StartClient(IAdvancedBus advancedBus, IQueue queue)
        {
            this.ProcessMessage(advancedBus, queue);
        }

        //public void ProcessMessage(IAdvancedBus advancedBus)
        //{
        //    //var messageId = testMessage.Id.ToString(CultureInfo.InvariantCulture);
        //    //this._timeToLiveWhileIdleForQueue = new TimeSpan(0, 5, 0);
        //    //var queue = this._rabbitMqBus.Advanced.QueueDeclare(messageId, expires: (int)_timeToLiveWhileIdleForQueue.TotalMilliseconds);
        //    var queueName = "ZNJB.Order";
        //    var routingKey = "ZNJB.*";
        //    IQueue queue=null;
        //    //var queue = advancedBus.QueueDeclare(queueName, expires: (int)_timeToLiveWhileIdleForQueue.TotalMilliseconds);
        //    //var queue = advancedBus.QueueDeclare(queueName, expires: (int)_timeToLiveWhileIdleForQueue.TotalMilliseconds);
        //    try
        //    {
        //        queue = advancedBus.QueueDeclare(queueName);
        //    }
        //    catch(Exception e)
        //    {

        //    }
            

        //    //var exchange = this._rabbitMqBus.Advanced.ExchangeDeclare("RisResponse", ExchangeType.Topic);
        //    //var binding = this._rabbitMqBus.Advanced.Bind(exchange, queue, messageId);
        //    var exchange = advancedBus.ExchangeDeclare("ZNJB.exchange", ExchangeType.Topic);
        //    var binding = advancedBus.Bind(exchange, queue, routingKey);

        //    //this._rabbitMqBus.Send("RisRequest", testMessage);
        //    var count = advancedBus.MessageCount(queue);

        //    //Action<IMessage<TraceableCodeSub>, MessageReceivedInfo> processMessageResponse = this.ProcessMessageResponse;
        //    //this._consumer = advancedBus.Consume<TraceableCodeSub>(queue, processMessageResponse);

        //    Action<byte[], MessageProperties, MessageReceivedInfo> processMessage = this.ProcessMessage;
        //    this._consumer = advancedBus.Consume(queue, processMessage);

        //    lock (LockingObject)
        //    {
        //        Monitor.Wait(LockingObject);
        //    }

        //    /*This dispose will have it fail the quickest*/
        //    this._consumer.Dispose();
        //}

        public void ProcessMessage(IAdvancedBus advancedBus, IQueue queue)
        {
            try
            {
                Action<byte[], MessageProperties, MessageReceivedInfo> processMessage = this.ProcessMessage;
                this._consumer = advancedBus.Consume(queue, processMessage);
                lock (LockingObject)
                {
                    Monitor.Wait(LockingObject);
                }

                /*This dispose will have it fail the quickest*/
                this._consumer.Dispose();
            }
            catch(Exception e)
            {
                throw;
            }
            
        }

        public void ProcessMessage(byte[] body, MessageProperties properties, MessageReceivedInfo messageReceivedInfo)
        {
            var message = System.Text.Encoding.UTF8.GetString(body);
            //Console.WriteLine(message);
            JObject jo = (JObject)JsonConvert.DeserializeObject(message);
            JToken a = jo["Body"];
            JObject obj = (JObject)a;
            var sub = JsonConvert.DeserializeObject<TraceableCodeSub>(obj.ToString());
            lock (list)
            {
                list.Add(sub);
                Console.WriteLine("加入集合成功，集合现有{0}条数据！", list.Count);
            }
            while (list.Count >= 100000)
            {
                lock (list)
                {
                    //批量插入数据库
                    Console.WriteLine("成功插入数据库{0}条数据！",list.Count);
                    //list 清空
                    list.Clear();
                    Console.WriteLine("清空list，集合数量为{0}！", list.Count);

                }
            }

            lock (LockingObject)
            {
                Monitor.PulseAll(LockingObject);
            }

            /* If you uncomment this dipose and comment out the one above it will work a little better...if you use the sleep you can probably process over 10000 messages before it fails
            Thread.Sleep(50);
            this._consumer.Dispose();
            */
        }

        public void ProcessMessageResponse(IMessage<TraceableCodeSub> testMessage, MessageReceivedInfo messageReceivedInfo)
        {

            //插入数据库等操作***


            lock (LockingObject)
            {
                Monitor.PulseAll(LockingObject);
            }

            /* If you uncomment this dipose and comment out the one above it will work a little better...if you use the sleep you can probably process over 10000 messages before it fails
            Thread.Sleep(50);
            this._consumer.Dispose();
            */
        }
    }
}
