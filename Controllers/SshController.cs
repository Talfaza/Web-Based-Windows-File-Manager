using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;
using System;
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
        public IActionResult Navigate(string path, string currentPath)
        {
            currentPath = currentPath ?? string.Empty;
            path = path ?? string.Empty;

            var newPath = System.IO.Path.Combine(currentPath, path);

            // Ensure newPath is not null or empty
            if (string.IsNullOrWhiteSpace(newPath))
            {
                ViewBag.Message = "Invalid path.";
                return View("Index", _currentModel);
            }

            var files = GetFilesAndDirectories(newPath);
            ViewBag.Files = files;
            ViewBag.CurrentPath = newPath;
            return View("Index", _currentModel);
        }

        [HttpPost]
        public IActionResult NavigateUp(string currentPath)
        {
            // Ensure currentPath is not null or empty
            currentPath = currentPath ?? string.Empty;

            // Get the parent directory
            var parentPath = System.IO.Path.GetDirectoryName(currentPath);

            // Fetch files from the parent directory
            var files = GetFilesAndDirectories(parentPath);
            ViewBag.Files = files;
            ViewBag.CurrentPath = parentPath;
            return View("Index", _currentModel);
        }
        [HttpPost]
        [HttpPost]
        public IActionResult Delete(List<string> items, string currentPath)
        {
            // Ensure currentPath is not null or empty
            currentPath = currentPath ?? string.Empty;

            using (var client = new SshClient(_currentModel.Ip, _currentModel.Username, _currentModel.Password))
            {
                try
                {
                    client.Connect();
                    if (client.IsConnected)
   

                        foreach (var item in items)
                        {
                            // Combine path and item and escape backslashes for SSH
                            var fullPath = System.IO.Path.Combine(currentPath, item).Replace("\\", "\\\\");
   
                            var cmd = client.RunCommand($"del /q \"{fullPath}\" & rmdir /q /s \"{fullPath}\" ");
                            var result = cmd.Execute();


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

            // Fetch updated file list
            var files = GetFilesAndDirectories(currentPath);
            ViewBag.Files = files;
            ViewBag.CurrentPath = currentPath;

            return View("Index", _currentModel);
        }



        private List<string> GetFilesAndDirectories(string path)
        {
            var files = new List<string>();
            var fullPath = string.IsNullOrEmpty(path) ? "." : path;

            using (var client = new SshClient(_currentModel.Ip, _currentModel.Username, _currentModel.Password))
            {
                try
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        var cmd = client.RunCommand($" cmd /c dir /b \"{fullPath}\"");
                        files.AddRange(cmd.Result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries));
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
