﻿using EventBus.Base;
using EventBus.Base.Events;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.RabbitMQ
{
    public class EventBusRabbitMQ : BaseEventBus
    {
        private readonly IConnectionFactory connectionFactory;
        RabbitMQPersistentConnection persistentConnection;
        private readonly IModel consumerChannel;
        public EventBusRabbitMQ(IServiceProvider serviceProvider, EventBusConfig config) : base(serviceProvider, config)
        {
            if (config.Connection != null)
            {
                var connJson = JsonConvert.SerializeObject(EventBusConfig, new JsonSerializerSettings()
                {

                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                connectionFactory = JsonConvert.DeserializeObject<ConnectionFactory>(connJson);
            }
            else
            {
                connectionFactory = new ConnectionFactory();
            }
               

            persistentConnection = new RabbitMQPersistentConnection(connectionFactory, config.ConnectionRetryCount);
            consumerChannel = CreateConsumerChannel();
        }

        public override void Publish(IntegrationEvent @event)
        {
            if (!persistentConnection.isConnected)
            {
                persistentConnection.TryConnect();
            }

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(EventBusConfig.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                 {
                     //log
                 });

            var eventName = @event.GetType().Name;
            eventName = ProcessEventName(eventName);

            consumerChannel.ExchangeDeclare(exchange: EventBusConfig.DefaultTopicName, type: "direct");

            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() =>
            {
                var properties = consumerChannel.CreateBasicProperties();
                properties.DeliveryMode = 2;

                consumerChannel.QueueDeclare(queue: GetSubName(eventName),
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                consumerChannel.BasicPublish(exchange:EventBusConfig.DefaultTopicName,
                    routingKey:eventName,
                    mandatory:true,
                    basicProperties: properties,
                    body:body);
            });
        }

        public override void Subscribe<T, TH>()
        {
            var eventName = typeof(T).Name;
            eventName = ProcessEventName(eventName);
            if (!SubManager.HasSubscriptionsForEvent(eventName))
            {
                if (!persistentConnection.isConnected)
                {
                    persistentConnection.TryConnect();
                }

                consumerChannel.QueueDeclare(queue: GetSubName(eventName),
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);
                consumerChannel.QueueBind(queue: GetSubName(eventName),
                    exchange: EventBusConfig.DefaultTopicName,
                    routingKey: eventName);
            }

            SubManager.AddSubscription<T, TH>();
            StartBasicConsume(eventName);
            SubManager.OnEventRemoverd += SubManager_OnEventRemoverd;
        }

        private void SubManager_OnEventRemoverd(object sender, string eventName)
        {
            eventName = ProcessEventName(eventName);
            if (!persistentConnection.isConnected)
            {
                persistentConnection.TryConnect();
            }

            consumerChannel.QueueUnbind(queue: eventName,
                exchange: EventBusConfig.DefaultTopicName,
                routingKey: eventName);

            if (SubManager.IsEmpty)
            {
                consumerChannel.Close();
            }
        }

        private IModel CreateConsumerChannel()
        {
            if (!persistentConnection.isConnected)
            {
                persistentConnection.TryConnect();
            }
            var channel = persistentConnection.CreateModel();

            channel.ExchangeDeclare(exchange: EventBusConfig.DefaultTopicName,
                                    type: "direct");
            return channel;
        }

        public override void UnSubscribe<T, TH>()
        {
            SubManager.RemoveSubscription<T, TH>();
        }

        private void StartBasicConsume(string eventName)
        {
            if (consumerChannel != null)
            {
                var consumer = new EventingBasicConsumer(consumerChannel);

                consumer.Received += Consumer_Received;

                consumerChannel.BasicConsume(queue: GetSubName(eventName),
                    autoAck: false,
                    consumer: consumer);
            }
        }

        private async void Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            var eventName = e.RoutingKey;
            eventName = ProcessEventName(eventName);
            var message = Encoding.UTF8.GetString(e.Body.Span);

            try
            {
                await ProcessEvent(eventName, message);
            }
            catch (Exception ex)
            {

                //log
            }
            consumerChannel.BasicAck(e.DeliveryTag, multiple: false);
        }
    }
}
