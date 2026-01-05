using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTOs;
using WebApplication1.Models;
using System;
using System.Linq;
using System.IO;
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

        // =============================== FILE SERVICE ===============================

        [HttpGet("file/{id}")]
        public async Task<IActionResult> GetFile(Guid id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
                return NotFound();

            return File(file.Data, file.ContentType);
        }

        // =============================== PRODUCTS ===============================

        [HttpGet("get-creams")]
        public async Task<IActionResult> GetCreams()
        {
            var items = await _context.Medicines
                .AsNoTracking()
                .Where(x => x.Category == "Creams")
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("get-vitamins")]
        public async Task<IActionResult> GetVitamins()
        {
            var items = await _context.Medicines
                .AsNoTracking()
                .Where(x => x.Category == "Vitamins")
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("get-skincare")]
        public async Task<IActionResult> GetSkincare()
        {
            var items = await _context.Medicines
                .AsNoTracking()
                .Where(x => x.Category == "Skincare")
                .ToListAsync();

            return Ok(items);
        }

        // =============================== ADD PRODUCT ===============================

        [HttpPost("add-product")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddProduct([FromForm] CreateProductDto req)
        {
            if (req == null)
                return BadRequest("Invalid request");

            string imageUrl = req.Image;

            if (req.ImageFile != null && req.ImageFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await req.ImageFile.CopyToAsync(ms);

                var stored = new StoredFile
                {
                    Id = Guid.NewGuid(),
                    FileName = req.ImageFile.FileName,
                    ContentType = req.ImageFile.ContentType,
                    Data = ms.ToArray()
                };

                _context.Files.Add(stored);
                await _context.SaveChangesAsync();

                imageUrl = $"{Request.Scheme}://{Request.Host}/api/Api/file/{stored.Id}";
            }

            var product = new Api
            {
                Name = req.Name,
                Description = req.Description,
                Price = req.Price,
                Category = req.Category,
                Image = imageUrl
            };

            _context.Medicines.Add(product);
            await _context.SaveChangesAsync();

            return Ok(product);
        }

        // =============================== EDIT PRODUCT (FIXED) ===============================

        [HttpPut("edit-product/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> EditProduct(string id, [FromForm] EditProductDto req)
        {
            var product = await _context.Medicines.FindAsync(id);
            if (product == null)
                return NotFound("Product not found");

            if (!string.IsNullOrWhiteSpace(req.Name))
                product.Name = req.Name;

            if (!string.IsNullOrWhiteSpace(req.Description))
                product.Description = req.Description;

            if (req.Price != null)
                product.Price = req.Price.Value;

            if (!string.IsNullOrWhiteSpace(req.Category))
                product.Category = req.Category;

            // ===== IMAGE UPDATE =====
            if (req.ImageFile != null && req.ImageFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await req.ImageFile.CopyToAsync(ms);

                var stored = new StoredFile
                {
                    Id = Guid.NewGuid(),
                    FileName = req.ImageFile.FileName,
                    ContentType = req.ImageFile.ContentType,
                    Data = ms.ToArray()
                };

                _context.Files.Add(stored);
                await _context.SaveChangesAsync();

                product.Image = $"{Request.Scheme}://{Request.Host}/api/Api/file/{stored.Id}";
            }
            else if (!string.IsNullOrWhiteSpace(req.Image))
            {
                product.Image = req.Image;
            }

            await _context.SaveChangesAsync();
            return Ok(product);
        }

        // =============================== DELETE PRODUCT ===============================

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            var product = await _context.Medicines.FindAsync(id);
            if (product == null)
                return NotFound("Product not found");

            _context.Medicines.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted successfully" });
        }

        // =============================== AUTH ===============================

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin(LoginDto req)
        {
            var hasher = new PasswordHasher<User>();

            var user = new User
            {
                Email = req.Email,
                PasswordHash = hasher.HashPassword(null, req.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Admin created" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto req)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == req.Email);
            if (user == null)
                return BadRequest("Invalid credentials");

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);

            if (result != PasswordVerificationResult.Success)
                return BadRequest("Invalid credentials");

            return Ok(new
            {
                message = "Login successful",
                user = new { user.Id, user.Email }
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(LoginDto req)
        {
            if (await _context.Users.AnyAsync(x => x.Email == req.Email))
                return BadRequest("Email already exists");

            var hasher = new PasswordHasher<User>();

            var user = new User
            {
                Email = req.Email,
                PasswordHash = hasher.HashPassword(null, req.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registered", userId = user.Id });
        }

        // =============================== BANNER ===============================

        [HttpPost("upload-banner")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadBanner(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var stored = new StoredFile
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Data = ms.ToArray()
            };

            _context.Files.Add(stored);

            var url = $"{Request.Scheme}://{Request.Host}/api/Api/file/{stored.Id}";

            var setting = await _context.Settings.FirstOrDefaultAsync(x => x.Key == "MainBanner");
            if (setting == null)
                _context.Settings.Add(new Setting { Key = "MainBanner", Value = url });
            else
                setting.Value = url;

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

        // =============================== GRID ===============================

        [HttpGet("grid")]
        public async Task<IActionResult> GetGrid()
        {
            var grids = await _context.Settings
                .Where(x => x.Key.StartsWith("Grid"))
                .ToListAsync();

            string Get(string key) => grids.FirstOrDefault(x => x.Key == key)?.Value ?? "";

            return Ok(new
            {
                grid1 = Get("Grid1"),
                grid2 = Get("Grid2"),
                grid3 = Get("Grid3"),
                grid4 = Get("Grid4"),
                grid5 = Get("Grid5"),
                grid6 = Get("Grid6"),
                grid7 = Get("Grid7")
            });
        }

        [HttpPost("upload-grid/{slot}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadGrid(int slot, IFormFile file)
        {
            if (slot < 1 || slot > 7)
                return BadRequest("Slot must be 1–7");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var stored = new StoredFile
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Data = ms.ToArray()
            };

            _context.Files.Add(stored);

            var url = $"{Request.Scheme}://{Request.Host}/api/Api/file/{stored.Id}";
            var key = $"Grid{slot}";

            var setting = await _context.Settings.FirstOrDefaultAsync(x => x.Key == key);
            if (setting == null)
                _context.Settings.Add(new Setting { Key = key, Value = url });
            else
                setting.Value = url;

            await _context.SaveChangesAsync();
            return Ok(new { slot, url });
        }

        [HttpDelete("grid/{slot}")]
        public async Task<IActionResult> DeleteGrid(int slot)
        {
            var key = $"Grid{slot}";
            var setting = await _context.Settings.FirstOrDefaultAsync(x => x.Key == key);

            if (setting == null)
                return NotFound();

            _context.Settings.Remove(setting);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // =============================== ABOUT TEXTS ===============================

        [HttpGet("about-texts")]
        public async Task<IActionResult> GetAboutTexts()
        {
            var items = await _context.Settings
                .Where(x => x.Key.StartsWith("About_"))
                .ToListAsync();

            return Ok(items.ToDictionary(x => x.Key, x => x.Value ?? ""));
        }

        [HttpPost("about-texts")]
        public async Task<IActionResult> UpdateAboutTexts([FromBody] Dictionary<string, string> texts)
        {
            foreach (var kv in texts)
            {
                var setting = await _context.Settings.FirstOrDefaultAsync(x => x.Key == kv.Key);
                if (setting == null)
                    _context.Settings.Add(new Setting { Key = kv.Key, Value = kv.Value });
                else
                    setting.Value = kv.Value;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
