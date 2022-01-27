using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Events
{
    public class IntegrationEvent
    {
        //Sistem içerisinde üretilen eventleri tanımlamak için kullanacağız. 
        //RabbitMQ veya Azure Service Bus aracılığıyla diğer servislere haber ulaştırmak amacıyla üretilen eventlerdir.
        [JsonProperty]
        public Guid Id { get; private set; }
        [JsonProperty]
        public DateTime CreatedDate { get; private set; }
        [JsonConstructor]
        public IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreatedDate = DateTime.Now;
        }
        [JsonConstructor]
        public IntegrationEvent(Guid id, DateTime createdDate)
        {
            Id = id;
            CreatedDate = createdDate;
        }
    }
}
