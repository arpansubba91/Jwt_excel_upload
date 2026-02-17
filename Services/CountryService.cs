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
            Guid userId);  // Changed from int to Guid
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
            Guid userId)  // Changed from int to Guid
        {
            try
            {
                // Validate file
                var validationResult = ValidateFile(file);
                if (!validationResult.isValid)
                    return (false, validationResult.message, 0);

                // Parse Excel file
                var (countries, invalidRows) = await ParseExcelFileAsync(file, userId);

                // Check if any countries were found
                if (countries.Count == 0)
                {
                    return BuildErrorResponse(invalidRows);
                }

                // Save to database
                var saved = await _repository.AddCountriesAsync(countries);
                if (!saved)
                    return (false, "Error saving data to database", 0);

                // Log success
                _logger.LogInformation($"User {userId} uploaded {countries.Count} countries at {DateTime.UtcNow}");
                
                return BuildSuccessResponse(countries.Count, invalidRows);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing Excel upload: {ex.Message}");
                return (false, $"Error: {ex.Message}", 0);
            }
        }

        private (bool isValid, string message) ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (false, "File is empty");

            if (!IsValidExcelFile(file))
                return (false, "File must be an Excel file (.xlsx or .xls)");

            return (true, "");
        }

        private async Task<(List<Country> countries, List<string> invalidRows)> ParseExcelFileAsync(
            IFormFile file, 
            Guid userId)
        {
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
                        return (countries, invalidRows);

                    for (int row = 2; row <= worksheet.Dimension?.Rows; row++)
                    {
                        var countryData = ExtractCountryData(worksheet, row);
                        
                        if (string.IsNullOrEmpty(countryData.countryName))
                        {
                            invalidRows.Add($"Row {row}: No country name");
                            continue;
                        }

                        if (!IsValidCountry(countryData.countryName))
                        {
                            invalidRows.Add($"Row {row}: '{countryData.countryName}' is not a valid country");
                            continue;
                        }

                        var country = new Country
                        {
                            Id = Guid.NewGuid(),
                            Name = countryData.countryName,
                            Capital = countryData.capital,
                            Region = countryData.region,
                            Population = countryData.population,
                            UploadedByUserId = userId,
                            UploadDateTime = DateTime.UtcNow
                        };

                        countries.Add(country);
                    }
                }
            }

            return (countries, invalidRows);
        }

        private (string countryName, string capital, string region, string population) ExtractCountryData(
            ExcelWorksheet worksheet, 
            int row)
        {
            var countryName = worksheet.Cells[row, 1].Value?.ToString()?.Trim() ?? "";
            var capital = worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? "";
            var region = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "";
            var population = worksheet.Cells[row, 4].Value?.ToString()?.Trim() ?? "";

            return (countryName, capital, region, population);
        }

        private bool IsValidCountry(string countryName)
        {
            return _validCountries.Any(c => 
                c.Equals(countryName, StringComparison.OrdinalIgnoreCase));
        }

        private (bool success, string message, int count) BuildErrorResponse(List<string> invalidRows)
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

        private (bool success, string message, int count) BuildSuccessResponse(int uploadedCount, List<string> invalidRows)
        {
            string successMsg = $"Successfully uploaded {uploadedCount} countries";
            if (invalidRows.Count > 0)
            {
                successMsg += $". {invalidRows.Count} rows skipped (invalid or no country name)";
            }
            return (true, successMsg, uploadedCount);
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