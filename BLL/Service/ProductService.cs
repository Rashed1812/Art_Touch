using BLL.Service_Abstraction;
using DAL.Data.Models;
using DAL.Repositories.Repository_Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _productRepository.GetAllAsync();
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            return await _productRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
        {
            var allProducts = await _productRepository.GetAllAsync();
            return allProducts.Where(p => p.CategoryId == categoryId && p.IsActive);
        }

        public async Task<IEnumerable<Product>> GetNewArrivalsAsync()
        {
            var allProducts = await _productRepository.GetAllAsync();
            return allProducts.Where(p => p.IsNewArrival && p.IsActive).Take(8);
        }

        public async Task<IEnumerable<Product>> GetBestSellersAsync()
        {
            var allProducts = await _productRepository.GetAllAsync();
            return allProducts.Where(p => p.IsBestseller && p.IsActive).Take(8);
        }

        public async Task AddAsync(Product product)
        {
            await _productRepository.AddAsync(product);
        }

        public async Task UpdateAsync(Product product)
        {
             _productRepository.Update(product);
        }

        public async Task DeleteAsync(Product product)
        {
             _productRepository.Delete(product);
        }
    }
}

