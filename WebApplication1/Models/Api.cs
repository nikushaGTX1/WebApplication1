namespace WebApplication1.Models;

public class Api
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Description { get; set; }
    public string Image { get; set; }
    public double Price { get; set; }
    public string Category { get; set; }


}
