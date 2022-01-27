using EventBus.Base.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Abstraction
{
    public interface IEventBusSubscriptionManager
    {
        bool IsEmpty { get; }// herhangi bir event dinleniyor mu
        event EventHandler<string> OnEventRemoverd;//Event silindiğinde bu eventi de tetikliyiceğiz.
        void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
        void RemoveSubscription<T, TH>() where TH : IIntegrationEventHandler<T> where T : IntegrationEvent;
        bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent; //dışardan bir event geldiğinde zaten bu eventin dinlenip dinlenmediğinin kontrolü yapılır
        bool HasSubscriptionsForEvent(string eventName);
        Type GetEventTypeByName(string eventName);//event ismi gönderildiğinde tipi dönülecek örneğin ordercreated ismi gönderildiğinde ordercreatedintegration tipi döneceğiz
        void Clear();//liste silinecek bütün subscriptionlar silinir
        IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent;//bir eventin bütün handler ve subscriptionları geri dönülür
        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);
        string GetEventKey<T>();
    }
}
