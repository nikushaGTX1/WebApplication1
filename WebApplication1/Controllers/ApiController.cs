using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTOs;
using WebApplication1.Models;
using System.IO;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

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

        // ------------------ BANNER ------------------

        [HttpPost("upload-banner")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadBanner(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var root = _env.WebRootPath;
            if (string.IsNullOrEmpty(root))
                root = Path.Combine(_env.ContentRootPath, "wwwroot");

            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            var uploadPath = Path.Combine(root, "uploads");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            string fileName = $"banner-{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string fullPath = Path.Combine(uploadPath, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            string url = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";

            var banner = await _context.Settings.FirstOrDefaultAsync(x => x.Key == "MainBanner");

            if (banner == null)
                _context.Settings.Add(new Setting { Key = "MainBanner", Value = url });
            else
                banner.Value = url;

            await _context.SaveChangesAsync();

            return Ok(new { bannerUrl = url });
        }

        [HttpGet("banner")]
        public async Task<IActionResult> GetBanner()
        {
            var banner = await _context.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == "MainBanner");

            return Ok(new { bannerUrl = banner?.Value });
        }

        // ------------------ GRID ------------------

        [HttpGet("grid")]
        public async Task<IActionResult> GetGrid()
        {
            var items = await _context.Settings
                .Where(x => x.Key.StartsWith("Grid"))
                .ToListAsync();

            return Ok(new
            {
                grid1 = items.FirstOrDefault(x => x.Key == "Grid1")?.Value,
                grid2 = items.FirstOrDefault(x => x.Key == "Grid2")?.Value,
                grid3 = items.FirstOrDefault(x => x.Key == "Grid3")?.Value,
                grid4 = items.FirstOrDefault(x => x.Key == "Grid4")?.Value
            });
        }

        [HttpPost("upload-grid/{slot}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadGrid(int slot, IFormFile file)
        {
            if (slot < 1 || slot > 4)
                return BadRequest("Slot must be 1–4");

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var root = _env.WebRootPath;
            if (string.IsNullOrEmpty(root))
                root = Path.Combine(_env.ContentRootPath, "wwwroot");

            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            var uploadPath = Path.Combine(root, "uploads");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            string fileName = $"grid-{slot}-{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string fullPath = Path.Combine(uploadPath, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            string url = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";

            var key = $"Grid{slot}";
            var setting = await _context.Settings.FirstOrDefaultAsync(x => x.Key == key);

            if (setting == null)
                _context.Settings.Add(new Setting { Key = key, Value = url });
            else
                setting.Value = url;

            await _context.SaveChangesAsync();

            return Ok(new { slot, url });
        }

        // ------------------ ABOUT TEXTS ------------------

        [HttpGet("about-texts")]
        public async Task<IActionResult> GetAboutTexts()
        {
            var items = await _context.Settings
                .Where(x => x.Key.StartsWith("About_"))
                .ToListAsync();

            var dict = items.ToDictionary(x => x.Key, x => x.Value);

            return Ok(dict);
        }

        [HttpPost("about-texts")]
        public async Task<IActionResult> UpdateAboutTexts([FromBody] Dictionary<string, string> texts)
        {
            if (texts == null || texts.Count == 0)
                return BadRequest("No texts provided");

            foreach (var kv in texts)
            {
                var key = kv.Key;
                var value = kv.Value ?? string.Empty;

                var setting = await _context.Settings
                    .FirstOrDefaultAsync(x => x.Key == key);

                if (setting == null)
                {
                    _context.Settings.Add(new Setting
                    {
                        Key = key,
                        Value = value
                    });
                }
                else
                {
                    setting.Value = value;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "About texts updated" });
        }
    }
}
