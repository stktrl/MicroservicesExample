using EventBus.Base;
using EventBus.Base.Events;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.AzureServiceBus
{
    public class EventBusServiceBus : BaseEventBus
    {
        private ITopicClient topicClient;
        private ManagementClient managementClient;
        private readonly EventBusConfig config;
        private ILogger logger;

        public EventBusServiceBus(IServiceProvider serviceProvider, EventBusConfig config) : base(serviceProvider, config)
        {
            logger = serviceProvider.GetService(typeof(ILogger<EventBusServiceBus>)) as ILogger<EventBusServiceBus>;
            managementClient = new ManagementClient(config.EventBusConnectionString);
            this.config = config;
            topicClient = createTopicClient();
        }

        private ITopicClient createTopicClient()
        {
            if (topicClient == null || topicClient.IsClosedOrClosing)
            {
                topicClient = new TopicClient(config.EventBusConnectionString, config.DefaultTopicName, RetryPolicy.Default);
            }
            //oluşturulan topiğin kontrolü yapılıyor
            if (!managementClient.TopicExistsAsync(config.DefaultTopicName).GetAwaiter().GetResult())
            {
                managementClient.CreateTopicAsync(config.DefaultTopicName).GetAwaiter().GetResult();
            }
            return topicClient;
        }
        public override void Publish(IntegrationEvent @event)//mesajı alıp azure service bus'a iletir.
        {
            var eventName = @event.GetType().Name;//örn: OrderCreatedIntegrationEvent 

            eventName = ProcessEventName(eventName);//örn: OrderCreated

            var eventStr= JsonConvert.SerializeObject(@event);
            var bodyArr = Encoding.UTF8.GetBytes(eventStr);
            var message = new Message()
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = bodyArr,
                Label=eventName
            };
            topicClient.SendAsync(message).GetAwaiter().GetResult();
        }
        public override void Subscribe<T, TH>()
        {
            //burdaki T bizim için Integration eventi temsil ediyor
            var eventName = typeof(T).Name;
            eventName = ProcessEventName(eventName);

            if (!SubManager.HasSubscriptionsForEvent(eventName))// böyle bir subs yok ise
            {
                var subscriptionClient = CreateSubscriptionClientIfNotExist(eventName);

                
                
            }
        }
        private void RegisterSubscriptionClientMessageHandler(ISubscriptionClient subscriptionClient)
        {
            subscriptionClient.RegisterMessageHandler(
                async (message, token) =>
                {
                    var eventName = $"{message.Label}";
                    var messageData = Encoding.UTF8.GetString(message.Body);

                    if(await ProcessEvent(ProcessEventName(eventName), messageData))
                    {
                        await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                    }
                },
                new MessageHandlerOptions(ExceptionReceivedHandler) { MaxConcurrentCalls=10, AutoComplete=false}
                );
        }
        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var ex = exceptionReceivedEventArgs.Exception;
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            logger.LogError(ex, "ERROR handling message :{ExceptionMessage} - Context {@ExceptionContext}", ex.Message, context);

            return Task.CompletedTask;
        }
        private ISubscriptionClient CreateSubscriptionClientIfNotExist(string eventName)
        {
            var subClient = createSubscriptionClient(eventName);

            var exist= managementClient.SubscriptionExistsAsync(config.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();

            if (!exist)
            {
                managementClient.CreateSubscriptionAsync(config.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();
                RemoveDefaultRule(subClient);
            }
            CreateRuleIfNotExists(eventName,subClient);
            return subClient;
        }
        private void CreateRuleIfNotExists(string eventName , ISubscriptionClient subscriptionClient)
        {
            //senaryoda eventname ve rule name aynı olacağı için bunun kontrolü yapılır

            bool ruleExist;
            try
            {
                var rule = managementClient.GetRuleAsync(config.DefaultTopicName, GetSubName(eventName), eventName).GetAwaiter().GetResult();
                ruleExist = rule != null;
            }
            catch (MessagingEntityNotFoundException)
            {

                ruleExist = false;
            }
            if (!ruleExist)
            {
                subscriptionClient.AddRuleAsync(new RuleDescription
                {
                    Filter = new CorrelationFilter { Label=eventName},
                    Name = eventName
                }).GetAwaiter().GetResult();//topic içerisinden geçen mesajlar bütün olayları kontrol eder burda ayrıştırmak için filter kullanılır.örn: orderCreated geldiğinde label değeri de orderCreated ise kkabul edecek. 
            }
        }
        private void RemoveDefaultRule(SubscriptionClient subscriptionClient)
        {
            try
            {
                subscriptionClient
                    .RemoveRuleAsync(RuleDescription.DefaultRuleName)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {

                logger.LogWarning("The mesaging entity {DefaultRuleName} Could not be found.", RuleDescription.DefaultRuleName);
            }
        }
        private SubscriptionClient createSubscriptionClient(string eventName)//azure tarafında subs. işleminin oluşturulması
        {
            return new SubscriptionClient(config.EventBusConnectionString, config.DefaultTopicName, GetSubName(eventName));
        }
        public override void UnSubscribe<T, TH>()
        {
            var eventName = typeof(T).Name;

            try
            {
                var subscriptionClient = createSubscriptionClient(eventName);

                subscriptionClient
                    .RemoveRuleAsync(eventName)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {

                logger.LogWarning("The messaging entity {eventNam} Could not be found.", eventName);
            }

            logger.LogInformation("Unsubscribing from event {EventName}", eventName);
            SubManager.RemoveSubscription<T, TH>();
        }
        public override void Dispose()
        {
            base.Dispose();
            topicClient.CloseAsync().GetAwaiter().GetResult();
            managementClient.CloseAsync().GetAwaiter().GetResult();
            topicClient = null;
            managementClient = null;
            
        }
    }
}
