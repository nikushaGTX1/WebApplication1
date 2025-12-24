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
        private readonly IWebHostEnvironment _env;

        public ApiController(DataContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ------------------ PRODUCTS ------------------

        [HttpGet("get-creams")]
        public async Task<ActionResult<List<Api>>> GetCreams()
        {
            var medicines = await _context.Medicines
                .AsNoTracking()
                .Where(m => m.Category == "Creams")
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
                Category = req.Category
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
            if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
                return BadRequest("Email and password are required.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
            if (user == null) return BadRequest("Invalid email or password.");

            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);

            if (result != PasswordVerificationResult.Success)
                return BadRequest("Invalid email or password.");

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
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully", userId = user.Id });
        }

        // ------------------ WISHLIST ------------------

        [HttpGet("wishlist/{userId}")]
        public async Task<ActionResult<List<WishlistItem>>> GetWishlist(string userId)
        {
            var wishlist = await _context.WishList
                .Include(w => w.Api)
                .Where(w => w.UserId == userId)
                .ToListAsync();

            return Ok(wishlist);
        }

        [HttpPost("wishlist")]
        public async Task<IActionResult> AddToWishlist([FromBody] WishlistItem item)
        {
            var exists = await _context.WishList
                .AnyAsync(w => w.UserId == item.UserId && w.MedicineId == item.MedicineId);

            if (exists) return BadRequest("Item already in wishlist");

            _context.WishList.Add(item);
            await _context.SaveChangesAsync();

            return Ok(item);
        }

        [HttpDelete("wishlist/{id}")]
        public async Task<IActionResult> RemoveFromWishlist(string id)
        {
            var item = await _context.WishList.FindAsync(id);
            if (item == null) return NotFound("Wishlist item not found!");

            _context.WishList.Remove(item);
            await _context.SaveChangesAsync();

            return Ok("Removed successfully");
        }

        // ------------------ BANNER SETTINGS ------------------

        [HttpPost("upload-banner")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadBanner(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            string uploadPath = Path.Combine(_env.WebRootPath, "uploads");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            string fileName = $"banner-{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string fullPath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string url = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";

            var banner = await _context.Settings.FirstOrDefaultAsync(x => x.Key == "MainBanner");

            if (banner == null)
            {
                banner = new Setting
                {
                    Key = "MainBanner",
                    Value = url
                };
                _context.Settings.Add(banner);
            }
            else
            {
                banner.Value = url;
            }

            await _context.SaveChangesAsync();

            return Ok(new { bannerUrl = url });
        }

        [HttpGet("banner")]
        public async Task<IActionResult> GetBanner()
        {
            var banner = await _context.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == "MainBanner");

            return Ok(new
            {
                bannerUrl = banner?.Value
            });
        }
    }
}
