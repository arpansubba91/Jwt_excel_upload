using mySystem.Models;
using mySystem.Data;
using Microsoft.EntityFrameworkCore;

namespace mySystem.Repositories
{
    public interface ICountryRepository
    {
        Task AddCountriesAsync(List<Country> countries);
        Task<List<Country>> GetCountriesByUserIdAsync(Guid userId);
    }

    public class CountryRepository : ICountryRepository
    {
        private readonly ApplicationDbContext _context;

        public CountryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddCountriesAsync(List<Country> countries)
        {
            await _context.Countries.AddRangeAsync(countries);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Country>> GetCountriesByUserIdAsync(Guid userId)
        {
            return await _context.Countries
                .Where(c => c.UploadedByUserId == userId)
                .ToListAsync();
        }
    }
}