using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Utility
{
    public class StripeSettings
    {
        public readonly string SecretKey;
        public readonly string PublishableKey;

        //public StripeSettings(IConfiguration config)
        //{
        //    SecretKey = config.GetSection("Stripe")["SecretKey"]!;
        //    PublishableKey = config.GetSection("Stripe")["PublishableKey"]!;
        //}

    }
}
