using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EventBus.Base.Abstraction;
using EventBus.Factory;
using EventBus.Base;
using RabbitMQ.Client;
using EventBus.UnitTest.Events.Events;
using EventBus.UnitTest.Events.EventHandlers;

namespace EventBus.UnitTest
{
    [TestClass]
    public class EventBusTests
    {
        private ServiceCollection services;
        
        public EventBusTests()
        {
            services = new ServiceCollection();
            services.AddLogging(configure=>configure.AddConsole());
        }
        [TestMethod]
        public void subscribe_event_on_rabbitmq_test()
        {
            services.AddSingleton<IEventBus>(sp =>
            {
                return EventBusFactory.Create(GetRabbitMQConfig(), sp);
            });
            var sp = services.BuildServiceProvider();

            var eventBus = sp.GetRequiredService<IEventBus>();


            eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();
            
            eventBus.UnSubscribe<OrderCreatedIntegrationEvent,OrderCreatedIntegrationEventHandler>();
        }
        [TestMethod]
        public void sendMessageToRabbitMQ()
        {
            services.AddSingleton<IEventBus>(sp =>
            {
                return EventBusFactory.Create(GetRabbitMQConfig(), sp);
            });
            var sp = services.BuildServiceProvider();

            var eventBus = sp.GetRequiredService<IEventBus>();

            eventBus.Publish(new OrderCreatedIntegrationEvent(1));
        }
       
        private EventBusConfig GetRabbitMQConfig()
        {
            var config = new EventBusConfig()
            {
                ConnectionRetryCount = 5,
                DefaultTopicName = "SellingBuddyTopicName",
                SubscriberClientAppName = "EventBus.UnitTest",
                EventBusType = EventBusType.RabbitMQ,
                EventNameSuffix = "IntegrationEvent",
                Connection = new ConnectionFactory()
                {
                    //HostName ="localhost",
                    //Port = 5672,
                    //UserName = "ElronYzlm",
                    //Password = "159753"
                }
            };
            return config;
        }
    }
}
