using mySystem.Models;
using mySystem.Repositories;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace mySystem.Services
{
    public interface ICountryService
    {
        Task<(bool success, string message, int count)> ProcessExcelUploadAsync(
            IFormFile file, 
            int userId);
    }

    public class CountryService : ICountryService
    {
        private readonly ICountryRepository _repository;
        private readonly ILogger<CountryService> _logger;

        // List of valid countries
        private readonly List<string> _validCountries = new List<string>
        {
            "India", "USA", "Japan", "Germany", "France", "Brazil", "Canada", 
            "Australia", "Mexico", "UK", "China", "Russia", "South Korea", 
            "Italy", "Spain", "Netherlands", "Switzerland", "Sweden", "Norway",
            "Denmark", "Belgium", "Austria", "Poland", "Greece", "Portugal"
        };

        public CountryService(ICountryRepository repository, ILogger<CountryService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<(bool success, string message, int count)> ProcessExcelUploadAsync(
            IFormFile file, 
            int userId)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return (false, "File is empty", 0);

                if (!IsValidExcelFile(file))
                    return (false, "File must be an Excel file (.xlsx or .xls)", 0);

                var countries = new List<Country>();
                var invalidRows = new List<string>();

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                            return (false, "No worksheet found in Excel file", 0);

                        for (int row = 2; row <= worksheet.Dimension?.Rows; row++)
                        {
                            var countryName = worksheet.Cells[row, 1].Value?.ToString()?.Trim() ?? "";
                            var capital = worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? "";
                            var region = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "";
                            var population = worksheet.Cells[row, 4].Value?.ToString()?.Trim() ?? "";

                            // Check if country name exists
                            if (string.IsNullOrEmpty(countryName))
                            {
                                invalidRows.Add($"Row {row}: No country name");
                                continue;
                            }

                            // Check if it's a valid country (case-insensitive)
                            bool isValidCountry = _validCountries.Any(c => 
                                c.Equals(countryName, StringComparison.OrdinalIgnoreCase));

                            if (!isValidCountry)
                            {
                                invalidRows.Add($"Row {row}: '{countryName}' is not a valid country");
                                continue;
                            }

                            // Valid country - add to list
                            countries.Add(new Country
                            {
                                Name = countryName,
                                Capital = capital,
                                Region = region,
                                Population = population,
                                UploadedByUserId = userId,
                                UploadDateTime = DateTime.UtcNow
                            });
                        }
                    }
                }

                // If no valid countries found
                if (countries.Count == 0)
                {
                    string errorMsg = "No valid country data found in Excel file. ";
                    if (invalidRows.Count > 0)
                    {
                        errorMsg += $"Invalid rows: {string.Join(", ", invalidRows.Take(5))}";
                        if (invalidRows.Count > 5)
                            errorMsg += $" and {invalidRows.Count - 5} more...";
                    }
                    return (false, errorMsg, 0);
                }

                // Save valid countries to database
                var saved = await _repository.AddCountriesAsync(countries);
                if (!saved)
                    return (false, "Error saving data to database", 0);

                string successMsg = $"Successfully uploaded {countries.Count} countries";
                if (invalidRows.Count > 0)
                {
                    successMsg += $". {invalidRows.Count} rows skipped (invalid or no country name)";
                }

                _logger.LogInformation($"User {userId} uploaded {countries.Count} countries at {DateTime.UtcNow}");
                return (true, successMsg, countries.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing Excel upload: {ex.Message}");
                return (false, $"Error: {ex.Message}", 0);
            }
        }

        private bool IsValidExcelFile(IFormFile file)
        {
            return file.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
                   file.ContentType == "application/vnd.ms-excel" ||
                   file.FileName.EndsWith(".xlsx") ||
                   file.FileName.EndsWith(".xls");
        }
    }
}