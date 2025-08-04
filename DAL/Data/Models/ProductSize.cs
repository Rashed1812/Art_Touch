using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Data.Models
{
    public class ProductSize
    {
        public int Id { get; set; }
        public string Size { get; set; } = string.Empty;
        public int QuantityInStock { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
