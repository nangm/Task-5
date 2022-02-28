using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CartApi.Models
{
    public class Cart
    {
        public int CartID { get; set; }

        public int ProductID { get; set; }
        public int ProductPrice { get; set; }
        public double Total { get; set; }
        public string OrderStatus { get; set; }
        public int OrderID { get; set; }

    }
}
