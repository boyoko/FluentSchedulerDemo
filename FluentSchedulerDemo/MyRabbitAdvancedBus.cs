using EasyNetQ;
using System;
using System.Collections.Generic;
using System.Text;
using EasyNetQ.Consumer;
using EasyNetQ.Interception;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace FluentSchedulerDemo
{
    public class MyRabbitAdvancedBus : RabbitAdvancedBus
    {

        private readonly IClientCommandDispatcher clientCommandDispatcher;

        public MyRabbitAdvancedBus(IConnectionFactory connectionFactory, 
            IConsumerFactory consumerFactory, 
            IEasyNetQLogger logger,
            IClientCommandDispatcherFactory clientCommandDispatcherFactory,
            IPublishConfirmationListener confirmationListener,
            IEventBus eventBus, 
            IHandlerCollectionFactory handlerCollectionFactory,
            IContainer container, 
            ConnectionConfiguration connectionConfiguration,
            IProduceConsumeInterceptor produceConsumeInterceptor,
            IMessageSerializationStrategy messageSerializationStrategy, 
            IConventions conventions, 
            AdvancedBusEventHandlers advancedBusEventHandlers, 
            IPersistentConnectionFactory persistentConnectionFactory) : base(connectionFactory, consumerFactory, logger, clientCommandDispatcherFactory, confirmationListener, eventBus, handlerCollectionFactory, container, connectionConfiguration, produceConsumeInterceptor, messageSerializationStrategy, conventions, advancedBusEventHandlers, persistentConnectionFactory)
        {
        }


        public virtual IQueue QueueDeclare(
                        string name,
                        bool passive = false,
                        bool durable = true,
                        bool exclusive = false,
                        bool autoDelete = false,
                        int? perQueueMessageTtl = null,
                        int? expires = null,
                        int? maxPriority = null,
                        string deadLetterExchange = null,
                        string deadLetterRoutingKey = null,
                        int? maxLength = null,
                        int? maxLengthBytes = null,
                        bool lazyQueue = false)
        {

            var arguments = new Dictionary<string, object>();
            if (perQueueMessageTtl.HasValue)
            {
                arguments.Add("x-message-ttl", perQueueMessageTtl.Value);
            }
            if (expires.HasValue)
            {
                arguments.Add("x-expires", expires);
            }
            if (maxPriority.HasValue)
            {
                arguments.Add("x-max-priority", maxPriority.Value);
            }
            // Allow empty dead-letter-exchange as it represents the default rabbitmq exchange
            // and thus is a valid value. To dead-letter a message directly to a queue, you
            // would set dead-letter-exchange to empty and dead-letter-routing-key to name of the
            // queue since every queue has a direct binding with default exchange.
            if (deadLetterExchange != null)
            {
                arguments.Add("x-dead-letter-exchange", deadLetterExchange);
            }
            if (!string.IsNullOrEmpty(deadLetterRoutingKey))
            {
                arguments.Add("x-dead-letter-routing-key", deadLetterRoutingKey);
            }
            if (maxLength.HasValue)
            {
                arguments.Add("x-max-length", maxLength.Value);
            }
            if (maxLengthBytes.HasValue)
            {
                arguments.Add("x-max-length-bytes", maxLengthBytes.Value);
            }

            //lazy queue
            if (lazyQueue)
            {
                arguments.Add("x-queue-mode", "lazy");
            }

            clientCommandDispatcher.Invoke(x => x.QueueDeclare(name, durable, exclusive, autoDelete, arguments));
            //logger.DebugWrite("Declared Queue: '{0}', durable:{1}, exclusive:{2}, autoDelete:{3}, args:{4}", name, durable, exclusive, autoDelete, string.Join(", ", arguments.Select(kvp => String.Format("{0}={1}", kvp.Key, kvp.Value))));
            return new Queue(name, exclusive);
        }


    }
}
