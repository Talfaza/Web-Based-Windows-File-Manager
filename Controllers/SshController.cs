using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;
using System.Collections.Generic;
using WebBasedFileManager.Models;

namespace WebBasedFileManager.Controllers
{
    public class SshController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Connect(SshConnectionModel model)
        {
            var files = new List<string>();

            using (var client = new SshClient(model.Ip, model.Username, model.Password))
            {
                try
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        var cmd = client.RunCommand("dir /b");
                        files.AddRange(cmd.Result.Split('\n'));
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

            ViewBag.Files = files;
            return View("Index", model);
        }
    }
}
