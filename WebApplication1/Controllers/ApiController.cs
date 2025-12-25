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

        // -------------------------------- FILE SERVICE --------------------------------
        [HttpGet("file/{id}")]
        public async Task<IActionResult> GetFile(Guid id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
                return NotFound();

            return File(file.Data, file.ContentType);
        }


        // -------------------------------- PRODUCTS --------------------------------

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


        // ------------ ADD PRODUCT + IMAGE UPLOAD (DB STORAGE) -----------------

        [HttpPost("add-product")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto req)
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
                Price = req.Price,
                Image = imageUrl,
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



        // -------------------------------- AUTH --------------------------------

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



        // -------------------------------- BANNER (DB STORAGE) --------------------------------

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



        // -------------------------------- GRID (DB STORAGE) --------------------------------

        [HttpGet("grid")]
        public async Task<IActionResult> GetGrid()
        {
            try
            {
                var grids = await _context.Settings
                    .Where(x => x.Key.StartsWith("Grid"))
                    .ToListAsync();

                string Get(string key) =>
                    grids.FirstOrDefault(x => x.Key == key)?.Value ?? "";

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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Grid load failed",
                    error = ex.Message
                });
            }
        }


        [HttpPost("upload-grid/{slot}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadGrid(int slot, IFormFile file)
        {
            if (slot < 1 || slot > 7)
                return BadRequest("Slot must be between 1 and 7");

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
                return NotFound("Grid not found");

            _context.Settings.Remove(setting);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Grid {slot} deleted" });
        }



        // -------------------------------- ABOUT TEXTS --------------------------------

        [HttpGet("about-texts")]
        public async Task<IActionResult> GetAboutTexts()
        {
            try
            {
                var items = await _context.Settings
                    .Where(x => EF.Functions.Like(x.Key, "About_%"))
                    .ToListAsync();

                var dict = items.ToDictionary(x => x.Key, x => x.Value ?? "");

                string[] keys =
                {
                    "About_HeroTitle",
                    "About_HeroText",
                    "About_Who",
                    "About_WhoText",
                    "About_Philosophy",
                    "About_PhilosophyText",
                    "About_Mission",
                    "About_MissionText",
                    "About_Trust",
                    "About_ExploreBtn"
                };

                foreach (var k in keys)
                    if (!dict.ContainsKey(k)) dict[k] = "";

                return Ok(dict);
            }
            catch
            {
                return Ok(new { });
            }
        }


        [HttpPost("about-texts")]
        public async Task<IActionResult> UpdateAboutTexts([FromBody] Dictionary<string, string> texts)
        {
            if (texts == null || texts.Count == 0)
                return BadRequest("No texts provided");

            foreach (var kv in texts)
            {
                var setting = await _context.Settings.FirstOrDefaultAsync(x => x.Key == kv.Key);

                if (setting == null)
                    _context.Settings.Add(new Setting { Key = kv.Key, Value = kv.Value ?? "" });
                else
                    setting.Value = kv.Value ?? "";
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "About texts updated" });
        }
    }
}
