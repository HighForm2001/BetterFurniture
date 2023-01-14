using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BetterFurniture.Data;
namespace BetterFurniture.Models.Repositories
{
    public class FurnitureRepository
    {
        private readonly FurnitureContext _context;

        public FurnitureRepository(FurnitureContext context)
        {
            _context = context;
        }
        public List<Furniture> GetAll() { return _context.furnitures.ToList(); }
        public async Task<Furniture> GetByID(int id)
        {
            return await _context.furnitures.FindAsync(id);
        }
        public Furniture GetByName(string name)
        {
            List<Furniture> furnitures = GetAll();
            foreach(var furniture in furnitures)
            {
                if (furniture.Name.Equals(name))
                {
                    return furniture;
                }
            }
            return null;
        }
        public void Add(Furniture furniture)
        {
            _context.furnitures.Add(furniture);
            _context.SaveChanges();
        }
        public void Update(Furniture furniture)
        {
            _context.furnitures.Update(furniture);
            _context.SaveChanges();
        }
        public void Delete(int id)
        {
            var product = _context.furnitures.Find(id);
            if (product == null){
                return;
            }
            _context.furnitures.Remove(product);
            _context.SaveChanges();
        }
        public async Task<int> GetQuantity(int id)
        {
            Furniture furniture = await GetByID(id);
            return furniture.Quantity;
        }
    }
}
