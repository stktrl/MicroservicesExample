using EventBus.Base.Abstraction;
using EventBus.Base.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.SubManagers
{
    public class InMemoryEventBusSubscriptionManager : IEventBusSubscriptionManager
    {
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;//handlerları tutmak için kullanırız.
        private readonly List<Type> _eventTypes;
        public bool IsEmpty => !_handlers.Keys.Any();// handlerımız boş mu kontrolü

        public Func<string, string> eventNameGetter;
        public event EventHandler<string> OnEventRemoverd;

        public InMemoryEventBusSubscriptionManager(Func<string, string> eventNameGetter)//bir fonksiyon parametre alıyoruz burda aldığımız eventname kısmını isteğimize göre manipüle edebilmek için
        {
            _handlers = new Dictionary<string, List<SubscriptionInfo>>();
            _eventTypes = new List<Type>();
            this.eventNameGetter = eventNameGetter;
        }

        public void AddSubscription<T, TH>()//dışarıdan bir event ve handler alıp bunu private fonsksiyona gönderiyoruz
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();//event ismi alıyoruz
            AddSubscription(typeof(TH), eventName);

            if (!_eventTypes.Contains(typeof(T)))
            {
                _eventTypes.Add(typeof(T));
            }
        }
        private void AddSubscription(Type handlerType, string eventName)
        {
            if (!HasSubscriptionsForEvent(eventName))//Dictionary'de böyle bir key var mı kontrolü yapılıyor
            {
                _handlers.Add(eventName, new List<SubscriptionInfo>());
            }
            if (_handlers[eventName].Any(s => s.HandlerType == handlerType))//Dictionary'de takip edilen eventin subsc.listesinde bu tipte bir event var mı 
            {
                throw new ArgumentException($"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
            }
            _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
        }

        public void Clear() => _handlers.Clear();


        public string GetEventKey<T>()
        {
            string eventName = typeof(T).Name;
            return eventNameGetter(eventName);
        }

        public Type GetEventTypeByName(string eventName) => _eventTypes.SingleOrDefault(t => t.Name == eventName);


        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent
        {
            var key = GetEventKey<T>();
            return GetHandlersForEvent(key);
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];

        public bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent
        {
            var key = GetEventKey<T>();

            return HasSubscriptionsForEvent(key);
        }

        public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);// dictionaryde event name parametresine göre bir kayıt var mı 


        public void RemoveSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var handlerToRemove = FindSubsriptionToRemove<T, TH>();
            var eventName = GetEventKey<T>();
            RemoveHandler(eventName, handlerToRemove);
        }

        private SubscriptionInfo FindSubsriptionToRemove<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            return FindSubsriptionToRemove(eventName, typeof(TH));
        }

        private SubscriptionInfo FindSubsriptionToRemove(string eventName, Type handlerType)
        {
            if (!HasSubscriptionsForEvent(eventName))
            {
                return null;
            }

            return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);
        }
        private void RemoveHandler(string eventName, SubscriptionInfo subsToRemove)
        {
            if (subsToRemove != null)
            {
                _handlers[eventName].Remove(subsToRemove);

                if (!_handlers[eventName].Any())
                {
                    _handlers.Remove(eventName);
                    var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
                    if (eventType != null)
                    {
                        _eventTypes.Remove(eventType);
                    }

                    RaiseOnEventRemoved(eventName);
                }
            }
        }

        private void RaiseOnEventRemoved(string eventName)// eventhandlerdan bir event silindiyse (unsubscribe) bu eventi kullananlara bildirim gönderilecek
        {
            var handler = OnEventRemoverd;
            handler?.Invoke(this, eventName);
        }
    }
}
