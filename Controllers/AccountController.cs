using Microsoft.AspNetCore.Mvc;
using WebBasedFileManager.Data;
using WebBasedFileManager.Models;
using System.Linq;
using System.Threading.Tasks;

namespace WebBasedFileManager.Controllers
{
    public class AccountController : Controller
    {
        private readonly FileManagerContext _context;

        public AccountController(FileManagerContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction("Login");
            }
            return View(user);
        }

        [HttpGet]
        public IActionResult Login(bool success = false)
        {
            ViewBag.Success = success;
            return View();
        }

        [HttpPost]
        public IActionResult Login(User user)
        {
            var loggedInUser = _context.Users
                .FirstOrDefault(u => u.Username == user.Username && u.Password == user.Password);

            if (loggedInUser != null)
            {
                return RedirectToAction("FileManager", "FileManager");
            }
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(user);
        }
    }
}
