using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stfrancisacademySchedular
{
    public class Pojo
    {
        public Pojo()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class HeadDetails
        {
            public string api { get; set; }
            public string source { get; set; }
        }

        public class MerchDetails
        {
            public string merchId { get; set; }
            public string password { get; set; }
            public string merchTxnId { get; set; }
            public string merchTxnDate { get; set; }
        }

        public class PayDetails
        {
            public double amount { get; set; }
            public string txnCurrency { get; set; }
            public string signature { get; set; }
        }

        public class PayInstrument
        {
            public HeadDetails headDetails { get; set; }
            public MerchDetails merchDetails { get; set; }
            public PayDetails payDetails { get; set; }
        }

        public class Root
        {
            public PayInstrument payInstrument { get; set; }
        }


    }
}
