using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base
{
    public class EventBusConfig
    {
        public int ConnectionRetryCount { get; set; } = 5;//Rabbitmqya bağlanırken max 5 kez dene 
        public string DefaultTopicName { get; set; } = "SellingBuddyEventBus";//dışardan bir topic name verilmediği durumda bir hata oluşmaması için burda verildi
        public string EventBusConnectionString { get; set; } = String.Empty;
        public string SubscriberClientAppName { get; set; } = String.Empty;//kuyrukların ismini belirlemek için örn: notificationservice.ordercreated , orderservice.ordercreated çünkü aynı eventi birden fazla servis dinleyebilir
        public string EventNamePrefix { get; set; } = String.Empty;
        public string EventNameSuffix { get; set; } = "IntegrationEvent";
        public EventBusType EventBusType { get; set; } = EventBusType.RabbitMQ;//default olarak rabbitmqya bağlanılması için
        public object Connection { get; set; }//diğer message broker sistemlerinde de aynı config class'ını kullanabilmemiz için bir connection nesnesi ekledik. 
        public bool DeleteEventPrefix => !String.IsNullOrEmpty(EventNamePrefix);
        public bool DeleteEventSuffix => !String.IsNullOrEmpty(EventNameSuffix);

    }
    public enum EventBusType
    {
        RabbitMQ = 0,
        AzureServiceBus = 1
    }
}
