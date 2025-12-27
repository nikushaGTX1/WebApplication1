using Microsoft.AspNetCore.Http;

namespace WebApplication1.DTOs
{
    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }   // optional
        public string? Image { get; set; }         // optional

        public double Price { get; set; }
        public string Category { get; set; } = string.Empty;

        public IFormFile? ImageFile { get; set; }  // optional upload
    }
}
