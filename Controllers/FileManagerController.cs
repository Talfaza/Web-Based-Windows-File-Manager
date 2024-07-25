using Microsoft.AspNetCore.Mvc;

namespace WebBasedFileManager.Controllers
{
    public class FileManagerController : Controller
    {
        public IActionResult FileManager()
        {
            return View();
        }
    }
}
