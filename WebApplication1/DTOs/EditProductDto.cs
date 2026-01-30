using Microsoft.AspNetCore.Http;

namespace WebApplication1.DTOs
{
    public class EditProductDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public double? Price { get; set; }
        public string? Category { get; set; }
        public string? Image { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}
