using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace KnowledgeVault.API.Filters;

/// <summary>
/// Swagger operation filter to correctly handle file uploads with IFormFile
/// </summary>
public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if this action has UploadDocumentRequest with IFormFile
        var hasFileParameter = context.ApiDescription.ParameterDescriptions
            .Any(p => p.Type == typeof(UploadDocumentRequest));

        if (!hasFileParameter)
            return;

        // Remove the default parameters
        operation.Parameters = new List<OpenApiParameter>();

        // Add the file parameter as form data
        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Required = new HashSet<string> { "file" },
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["title"] = new OpenApiSchema
                            {
                                Type = "string",
                                Description = "Document title (optional, defaults to filename)",
                                Nullable = true
                            },
                            ["file"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary",
                                Description = "The file to upload"
                            }
                        }
                    }
                }
            }
        };
    }
}

// Add this class in the same file or a separate file
public class UploadDocumentRequest
{
    public string? Title { get; set; }
    public IFormFile File { get; set; } = null!;
}