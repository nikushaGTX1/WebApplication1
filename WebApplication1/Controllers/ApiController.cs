using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTOs;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly DataContext _context;

        public ApiController(DataContext context)
        {
            _context = context;
        }

        // ------------------ PRODUCTS ------------------

        [HttpGet("get-medicines")]
        public async Task<ActionResult<List<Api>>> GetMedicines()
        {
            var medicines = await _context.Medicines
                                          .AsNoTracking()
                                          .Where(m => m.Category == "Medicines")
                                          .ToListAsync();
            return Ok(medicines);
        }

        [HttpGet("get-vitamins")]
        public async Task<ActionResult<List<Api>>> GetVitamins()
        {
            var vitamins = await _context.Medicines
                                         .AsNoTracking()
                                         .Where(m => m.Category == "Vitamins")
                                         .ToListAsync();
            return Ok(vitamins);
        }

        [HttpGet("get-skincare")]
        public async Task<ActionResult<List<Api>>> GetSkincare()
        {
            var skincare = await _context.Medicines
                                         .AsNoTracking()
                                         .Where(m => m.Category == "Skincare")
                                         .ToListAsync();
            return Ok(skincare);
        }

        [HttpPost("add-product")]
        public async Task<IActionResult> CreateProduct(CreateProductDto req)
        {
            var product = new Api
            {
                Name = req.Name,
                Price = req.Price,
                Image = req.Image,
                Description = req.Description,
                Category = req.Category,
            };

            await _context.Medicines.AddAsync(product);
            await _context.SaveChangesAsync();

            return Ok(product);
        }

        [HttpPut("edit-product/{id}")]
        public async Task<IActionResult> EditProduct(string id, EditProductDto req)
        {
            var product = await _context.Medicines.FindAsync(id);
            if (product == null) return NotFound("Product not found!");

            if (!string.IsNullOrEmpty(req.Name)) product.Name = req.Name;
            if (!string.IsNullOrEmpty(req.Description)) product.Description = req.Description;
            if (!string.IsNullOrEmpty(req.Image)) product.Image = req.Image;
            if (req.Price != null) product.Price = (double)req.Price;

            await _context.SaveChangesAsync();
            return Ok(product);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            var product = await _context.Medicines.FindAsync(id);
            if (product == null) return NotFound("Product not found!");

            _context.Medicines.Remove(product);
            await _context.SaveChangesAsync();
            return Ok("Deleted successfully");
        }

        // ------------------ AUTH ------------------

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin(LoginDto req)
        {
            var passwordHasher = new PasswordHasher<User>();

            var user = new User
            {
                Email = req.Email,
                PasswordHash = passwordHasher.HashPassword(null, req.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Admin created" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginUser(LoginDto req)
        {
            // Basic validation
            if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
            {
                return BadRequest("Email and password are required.");
            }

            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user == null)
                return BadRequest("Invalid email or password.");

            // Verify password
            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);

            if (result != PasswordVerificationResult.Success)
                return BadRequest("Invalid email or password.");

            // Return basic user info (no roles)
            return Ok(new
            {
                message = "Login successful",
                user = new
                {
                    id = user.Id,
                    email = user.Email
                }
            });
        }
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser(LoginDto req)
        {
            if (await _context.Users.AnyAsync(u => u.Email == req.Email))
                return BadRequest("Email already exists");

            var passwordHasher = new PasswordHasher<User>();
            var user = new User
            {
                Email = req.Email,
                PasswordHash = passwordHasher.HashPassword(null, req.Password)
                // No admin role here
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully", userId = user.Id });
        }

        // ------------------ USERS ------------------

        [HttpGet("wishlist/{userId}")]
        public async Task<ActionResult<List<WishlistItem>>> GetWishlist(string userId)
        {
            var wishlist = await _context.WishList
                .Include(w => w.Api) // Include product details
                .Where(w => w.UserId == userId)
                .ToListAsync();

            return Ok(wishlist);
        }

        // POST: api/api/wishlist
        [HttpPost("wishlist")]
        public async Task<IActionResult> AddToWishlist([FromBody] WishlistItem item)
        {
            // Optional: prevent duplicates
            var exists = await _context.WishList
                .AnyAsync(w => w.UserId == item.UserId && w.MedicineId == item.MedicineId);

            if (exists) return BadRequest("Item already in wishlist");

            _context.WishList.Add(item);
            await _context.SaveChangesAsync();

            return Ok(item);
        }

        // DELETE: api/api/wishlist/{id}
        [HttpDelete("wishlist/{id}")]
        public async Task<IActionResult> RemoveFromWishlist(string id)
        {
            var item = await _context.WishList.FindAsync(id);
            if (item == null) return NotFound("Wishlist item not found!");

            _context.WishList.Remove(item);
            await _context.SaveChangesAsync();

            return Ok("Removed successfully");
        }
    }
}
