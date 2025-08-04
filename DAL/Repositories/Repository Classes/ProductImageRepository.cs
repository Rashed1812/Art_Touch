using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Data;
using DAL.Data.Models;
using DAL.Repositories.Repository_Interfaces;

namespace DAL.Repositories.Repository_Classes
{
    public class ProductImageRepository : GenriceRepository<ProductImage>, IProductImageRepository
    {
        public ProductImageRepository(StoreDbContext context) : base(context)
        {
        }
    }
}
