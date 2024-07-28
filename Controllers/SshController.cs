using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;
using WebBasedFileManager.Models;

namespace WebBasedFileManager.Controllers
{
    public class SshController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Connect(SshConnectionModel model)
        {
            using (var client = new SshClient(model.Ip, model.Hostname, model.Password))
            {
                try
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        ViewBag.Message = "Successfully connected to the server.";
                        client.Disconnect();
                    }
                    else
                    {
                        ViewBag.Message = "Failed to connect to the server.";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Message = $"Error: {ex.Message}";
                }
            }

            return View("Index");
        }
    }
}
