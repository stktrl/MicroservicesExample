using EventBus.Base.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Abstraction
{
    //Eventlerin handle edilmesi için kullanılır.Dinamik bir interface olacak.
    //Bu interface sadece tipi IntegrationEvent olan tiplere çalışır.
    public interface IIntegrationEventHandler<TIntegrationEvent>:IntegrationEventHandler where TIntegrationEvent:IntegrationEvent
    {
        Task Handle(TIntegrationEvent @event);
    }
    //Bu interface sadece markup amaçlı kullanılır.
    public interface IntegrationEventHandler
    {

    }
}
