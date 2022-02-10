using EventBus.Base.Abstraction;
using EventBus.Base.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Events
{
    //2. vidyo 25 den sonra izlenecek
    public abstract class BaseEventBus : IEventBus
    {
        public readonly IServiceProvider ServiceProvider;
        public readonly IEventBusSubscriptionManager SubManager;
        public EventBusConfig EventBusConfig { get; set; }


        protected BaseEventBus(IServiceProvider serviceProvider, EventBusConfig config)
        {
            ServiceProvider = serviceProvider;
            SubManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
            this.EventBusConfig = config;
        }
        public async Task<bool> ProcessEvent(string eventName, string message)
        {
            eventName = ProcessEventName(eventName);
            var processed = false;

            if (SubManager.HasSubscriptionsForEvent(eventName))
            {
                var substriptions = SubManager.GetHandlersForEvent(eventName);
                using (var scope = ServiceProvider.CreateScope())
                {
                    foreach (var subscription in substriptions)
                    {
                        var handler = ServiceProvider.GetService(subscription.HandlerType);
                        if (handler == null)
                        {
                            continue;
                        }
                        var eventType = SubManager.GetEventTypeByName($"{EventBusConfig.EventNamePrefix}{eventName}{EventBusConfig.EventNameSuffix}");
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                    }
                }

                processed = true;

            }
            return processed;
        }
        public virtual string ProcessEventName(string eventName)//örn OrderCreatedIntegrationEvent ----> OrderCreated
        {
            if (EventBusConfig.DeleteEventPrefix)
            {
                eventName = eventName.TrimStart(EventBusConfig.EventNamePrefix.ToArray());
            }
            if (EventBusConfig.DeleteEventSuffix)
            {
                eventName = eventName.TrimEnd(EventBusConfig.EventNameSuffix.ToArray());
            }
            return eventName;
        }
        public virtual string GetSubName(string eventName)
        {
            return $"{EventBusConfig.SubscriberClientAppName}.{ProcessEventName(eventName)}";//örn: notificationService.OrderCreatedIntegrationEvent---->notificationService.OrderCreated aynı zamanda queue name ismine denk gelir
        }//Sub. durumunun ismini getirmek için kullanılır
        public virtual void Dispose()
        {
            EventBusConfig = null;
            SubManager.Clear();

        }
        public abstract void Publish(IntegrationEvent @event);

        public abstract void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        public abstract void UnSubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;
    }

}
