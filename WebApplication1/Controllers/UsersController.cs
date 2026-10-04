using Inventory.Models;
using Inventory.Services;
using Inventory.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;

    public UsersController(
        ApplicationDbContext context,
        PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }


        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.Role.RoleName)
                .ThenBy(u => u.FullName)
                .ToListAsync();

            return View(users);
        }


        
        // GET: Users/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            return Json(new
            {
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
                fullName = user.FullName,
                phone = user.Phone,
                role = user.Role.RoleName,
                isActive = user.IsActive,
                createdAt = user.CreatedAt.ToString("dd MMM yyyy"),
                updatedAt = user.UpdatedAt.HasValue
                    ? user.UpdatedAt.Value.ToString("dd MMM yyyy")
                    : null
            });
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View(new UserCreateViewModel());
        }


        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check username
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == model.Username);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    nameof(model.Username),
                    "This username is already in use.");

                return View(model);
            }


            // Check email
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This email address is already in use.");

                return View(model);
            }


            // Find Cashier role
            var cashierRole = await _context.Roles
                .FirstOrDefaultAsync(r =>
                    r.RoleName == "Cashier" &&
                    r.IsActive == true);

            if (cashierRole == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The Cashier role is not available.");

                return View(model);
            }


            // Create user
            var user = new User
            {
                Username = model.Username.Trim(),
                Email = model.Email.Trim(),
                FullName = model.FullName.Trim(),
                Phone = string.IsNullOrWhiteSpace(model.Phone)
                    ? null
                    : model.Phone.Trim(),

                PasswordHash =
                    _passwordService.HashPassword(model.Password),

                RoleId = cashierRole.RoleId,

                IsActive = true,

                CreatedAt = DateTime.Now,
                UpdatedAt = null
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Cashier account created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // GET: Users/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            // Admin profile cannot be edited here
            if (user.Role.RoleName == "Admin")
            {
                return Forbid();
            }

            return Json(new
            {
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
                fullName = user.FullName,
                phone = user.Phone
            });
        }


        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            UserEditViewModel model)
        {
            if (id != model.UserId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            // Admin profile is not edited through Cashier Management
            if (user.Role.RoleName == "Admin")
            {
                return Forbid();
            }


            // Check username belongs to another user
            var usernameExists = await _context.Users
                .AnyAsync(u =>
                    u.UserId != id &&
                    u.Username == model.Username);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    nameof(model.Username),
                    "This username is already in use.");

                return View(model);
            }


            // Check email belongs to another user
            var emailExists = await _context.Users
                .AnyAsync(u =>
                    u.UserId != id &&
                    u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This email address is already in use.");

                return View(model);
            }


            user.Username = model.Username.Trim();
            user.Email = model.Email.Trim();
            user.FullName = model.FullName.Trim();

            user.Phone = string.IsNullOrWhiteSpace(model.Phone)
                ? null
                : model.Phone.Trim();

            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Cashier profile updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // POST: Users/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            // Admin accounts cannot be disabled here
            if (user.Role.RoleName == "Admin")
            {
                return Forbid();
            }

            user.IsActive = !(user.IsActive ?? false);
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                user.IsActive == true
                    ? "Cashier account activated successfully."
                    : "Cashier account deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Users/ChangePassword/5
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            // Admin password cannot be changed through Cashier Management
            if (user.Role.RoleName == "Admin")
            {
                return Forbid();
            }

            return Json(new
            {
                userId = user.UserId,
                fullName = user.FullName,
                username = user.Username
            });
        }

        // POST: Users/ChangePassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            int id,
            UserPasswordViewModel model)
        {
            if (id != model.UserId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            // Admin password cannot be changed through Cashier Management
            if (user.Role.RoleName == "Admin")
            {
                return Forbid();
            }

            user.PasswordHash =
                _passwordService.HashPassword(model.NewPassword);

            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Cashier password changed successfully.";

            return RedirectToAction(nameof(Index));
        }
    }

}
