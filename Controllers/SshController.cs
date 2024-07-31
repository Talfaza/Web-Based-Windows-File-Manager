using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;
using System.Collections.Generic;
using System.Linq;
using WebBasedFileManager.Models;

namespace WebBasedFileManager.Controllers
{
    public class SshController : Controller
    {
        private static SshConnectionModel _currentModel;

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Connect(SshConnectionModel model)
        {
            _currentModel = model;
            var files = GetFilesAndDirectories("");

            ViewBag.Files = files;
            ViewBag.CurrentPath = "";
            return View("Index", model);
        }

        [HttpPost]
        public IActionResult Navigate(string path)
        {
            var files = GetFilesAndDirectories(path);
            ViewBag.Files = files;
            ViewBag.CurrentPath = path;
            return View("Index", _currentModel);
        }

        private List<string> GetFilesAndDirectories(string path)
        {
            var files = new List<string>();
            var fullPath = string.IsNullOrEmpty(path) ? "" : path + "\\";

            using (var client = new SshClient(_currentModel.Ip, _currentModel.Username, _currentModel.Password))
            {
                try
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        var cmd = client.RunCommand($"dir /b {fullPath}");
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

            return files;
        }
    }
}
