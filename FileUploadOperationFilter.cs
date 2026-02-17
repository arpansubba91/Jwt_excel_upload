using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParams = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile))
            .ToList();

        if (!fileParams.Any())
            return;

        if (operation.RequestBody == null)
            return;

        if (!operation.RequestBody.Content.ContainsKey("multipart/form-data"))
            return;

        var formDataContent = operation.RequestBody.Content["multipart/form-data"];

        // Clear existing properties
        formDataContent.Schema.Properties.Clear();

        // Add only file property
        formDataContent.Schema.Properties.Add("file", new OpenApiSchema()
        {
            Type = "string",
            Format = "binary",
            Description = "Select Excel file"
        });

        // Remove required fields to prevent checkbox
        formDataContent.Schema.Required.Clear();
    }
}