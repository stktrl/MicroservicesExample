using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base
{
    public class SubscriptionInfo
    {
        public Type HandlerType { get; }//Gönderilen Integration eventin tipini tutacağız.
        public SubscriptionInfo(Type handlerType)
        {
            HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
        }
        //static olarak da tipi belirtmek istersek 
        public static SubscriptionInfo Typed(Type handlerType)
        {
            return new SubscriptionInfo(handlerType);
        }
    }
}
