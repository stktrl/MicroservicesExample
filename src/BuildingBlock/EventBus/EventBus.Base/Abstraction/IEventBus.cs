using EventBus.Base.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Abstraction
{
    //Servislerimizin subscibe işlemleri için kullanılacak 
    public interface IEventBus
    {
        void Publish(IntegrationEvent @event);//Event göndermek için kullanacak.
        void Subscribe<T, TH>() where T:IntegrationEvent where TH:IIntegrationEventHandler<T>;//Subscribe işlemleri için kullanılır.
        void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
    }
}
