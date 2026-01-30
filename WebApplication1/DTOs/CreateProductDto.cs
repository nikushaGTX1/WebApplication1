using Microsoft.AspNetCore.Http;

namespace WebApplication1.DTOs
{
    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Price { get; set; }
        public string Category { get; set; } = string.Empty;

        // Image URL or path
        public string? Image { get; set; }

        // Uploaded image file
        public IFormFile? ImageFile { get; set; }
    }
}
