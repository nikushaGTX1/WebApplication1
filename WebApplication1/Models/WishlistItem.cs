using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

public class WishlistItem
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; }
    public User User { get; set; }

    [Required]
    public string MedicineId { get; set; }
    public Api Api { get; set; }
}
