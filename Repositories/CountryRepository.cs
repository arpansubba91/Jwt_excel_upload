using mySystem.Models;
using mySystem.Data;
using Microsoft.EntityFrameworkCore;

namespace mySystem.Repositories
{
    public interface ICountryRepository
    {
        Task<bool> AddCountriesAsync(List<Country> countries);
        Task<List<Country>> GetAllCountriesAsync();
        Task<List<Country>> GetCountriesByUserIdAsync(int userId);
    }

    public class CountryRepository : ICountryRepository
    {
        private readonly ApplicationDbContext _context;

        public CountryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddCountriesAsync(List<Country> countries)
        {
            try
            {
                await _context.Countries.AddRangeAsync(countries);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<List<Country>> GetAllCountriesAsync()
        {
            return await _context.Countries.ToListAsync();
        }

        public async Task<List<Country>> GetCountriesByUserIdAsync(int userId)
        {
            return await _context.Countries
                .Where(c => c.UploadedByUserId == userId)
                .ToListAsync();
        }
    }
}