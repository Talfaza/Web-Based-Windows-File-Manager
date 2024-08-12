using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WebBasedFileManager.Models;
using Microsoft.Extensions.Logging;
namespace WebBasedFileManager.Controllers
{
    public class SshController : Controller
    {
        private static SshConnectionModel _currentModel;
        private readonly ILogger<SshController> _logger;
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        public SshController(ILogger<SshController> logger)
        {
            _logger = logger;
        }
        [HttpPost]
        public IActionResult Connect(SshConnectionModel model)
        {
            _currentModel = model;
            var files = GetFilesAndDirectories("");

            ViewBag.Files = files;
            ViewBag.CurrentPath = "";
            ViewBag.DebugInfo = new List<string> { "Connected to server.", $"Current path: {ViewBag.CurrentPath}" };
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
                ViewBag.DebugInfo = new List<string> { "Navigate error: Invalid path." };
                return View("Index", _currentModel);
            }

            var files = GetFilesAndDirectories(newPath);
            ViewBag.Files = files;
            ViewBag.CurrentPath = newPath;
            ViewBag.DebugInfo = new List<string> { $"Navigated to: {newPath}" };
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
            ViewBag.DebugInfo = new List<string> { $"Navigated up to: {parentPath}" };
            return View("Index", _currentModel);
        }

        [HttpPost]
        public IActionResult Delete(List<string> items, string currentPath)
        {
            currentPath = currentPath ?? string.Empty;

            using (var client = new SshClient(_currentModel.Ip, _currentModel.Username, _currentModel.Password))
            {
                try
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        foreach (var item in items)
                        {
                            var fullPath = System.IO.Path.Combine(currentPath, item).Replace("\\", "\\\\");
                            var cmdText = $"del /q \"{fullPath}\" & rmdir /q /s \"{fullPath}\"";

                            // Debugging information
                            ViewBag.DebugInfo = new List<string> { $"Executing command: {cmdText}" };

                            var cmd = client.RunCommand(cmdText);
                            cmd.Execute();

                            if (!string.IsNullOrEmpty(cmd.Error))
                            {
                                ViewBag.Message = $"An error occurred during deletion: {cmd.Error}";
                                ViewBag.DebugInfo.Add($"Delete error: {cmd.Error}");
                            }
                        }
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
                    ViewBag.DebugInfo = new List<string> { $"Exception: {ex.Message}" };
                }
            }

            var files = GetFilesAndDirectories(currentPath);
            ViewBag.Files = files;
            ViewBag.CurrentPath = currentPath;

            return View("Index", _currentModel);
        }

        [HttpPost]
        public IActionResult Compress(List<string> items, string archiveName)
        {
            var path = (string)Request.Form["currentPath"];

            using (var client = new SshClient(_currentModel.Ip, _currentModel.Username, _currentModel.Password))
            {
                try
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        var itemsString = string.Join(" ", items.Select(item => $"\"{item}\""));
                        var fullPath = string.IsNullOrEmpty(path) ? "." : path;
                        var archivePath = $"{archiveName}.rar";

                        if (fullPath.EndsWith("\\"))
                        {
                            fullPath = fullPath.TrimEnd('\\');
                        }

                        var cmdText = $"cd \"{fullPath}\" && tar -cf \"{archivePath}\" {itemsString}";

                        // Debugging information
                        ViewBag.DebugInfo = new List<string>
                        {
                            $"Compressing files: {itemsString}",
                            $"Archive path: {archivePath}",
                            $"Command: {cmdText}"
                        };

                        var cmd = client.RunCommand(cmdText);
                        cmd.Execute();

                        ViewBag.CommandResult = cmd.Result;
                        ViewBag.CommandError = cmd.Error;

                        if (!string.IsNullOrEmpty(cmd.Error))
                        {
                            ViewBag.Message = "An error occurred during compression.";
                        }
                        else
                        {
                            ViewBag.Message = $"Successfully compressed files into {archiveName}.tar";
                        }

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
                    ViewBag.DebugInfo = new List<string> { $"Exception: {ex.Message}" };
                }
            }

            var files = GetFilesAndDirectories(path);
            ViewBag.Files = files;
            ViewBag.CurrentPath = path;
            return View("Index", _currentModel);
        }
        [HttpPost]
        public IActionResult Decompress(string archiveName, string currentPath)
        {
            using (var client = new SshClient(_currentModel.Ip, _currentModel.Username, _currentModel.Password))
            {
                try
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        var fullPath = System.IO.Path.Combine(currentPath, archiveName).Replace("\\", "\\\\");
                        var archiveDir = System.IO.Path.GetFileNameWithoutExtension(archiveName);
                        var cmdText = $"cd \"{currentPath}\"  && mkdir \"{archiveDir}\" && move \"{archiveName}\" \"{archiveDir}\" && cd \"{archiveDir}\" && tar xvf \"{archiveName}\" && del /q \"{archiveName}\" ";
             
                      

                        var cmd = client.RunCommand(cmdText);
                        cmd.Execute();

                        if (!string.IsNullOrEmpty(cmd.Error))
                        {
                            ViewBag.Message = "An error occurred during decompression.";
                        }
                        else
                        {
                            ViewBag.Message = $"Successfully decompressed {archiveName}.";
                        }

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

            var files = GetFilesAndDirectories(currentPath);
            ViewBag.Files = files;
            ViewBag.CurrentPath = currentPath;
            return View("Index", _currentModel);
        }


        [HttpPost]
        public IActionResult Move(List<string> items, string currentPath, string destinationPath)
        {
            currentPath = currentPath ?? string.Empty;

            ViewBag.DebugInfo = new List<string>
    {
        $"Current Path: {currentPath}",
        $"Destination Path: {destinationPath}",
        $"Files to Move: {string.Join(", ", items)}"
    };

            using (var client = new SshClient(_currentModel.Ip, _currentModel.Username, _currentModel.Password))
            {
                try
                {
                    client.Connect();
                    if (client.IsConnected)
                    {

                        foreach (var item in items)
                        {
                            var sourceFilePath = System.IO.Path.Combine(currentPath, item);
                            var destinationFilePath = System.IO.Path.Combine(destinationPath);

                            var cmdCommand = $"move /y \"{sourceFilePath}\" \"{destinationFilePath}\"";

                            var cmd = client.RunCommand(cmdCommand);
                             cmd.Execute();
                            if (!string.IsNullOrEmpty(cmd.Error))
                            {
                                ViewBag.Message = "An error occurred during moving.";
                            }
                            else
                            {
                                ViewBag.Message = $"Successfully decompressed {item}.";
                            }

                           
                        }
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

            var files = GetFilesAndDirectories(currentPath);
            ViewBag.Files = files;
            ViewBag.CurrentPath = currentPath;
            return View("Index", _currentModel);
        }
        [HttpPost]
        public IActionResult UploadFiles(List<IFormFile> uploadedFiles, string currentPath)
        {
            currentPath = currentPath ?? string.Empty;

            using (var client = new SshClient(_currentModel.Ip, _currentModel.Username, _currentModel.Password))
            {
                try
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        var sftp = new SftpClient(client.ConnectionInfo);

                        sftp.Connect();

                        foreach (var file in uploadedFiles)
                        {
                            if (file.Length > 0)
                            {
                                var filePath = System.IO.Path.Combine(currentPath, file.FileName);
                                using (var stream = file.OpenReadStream())
                                {
                                    sftp.UploadFile(stream, filePath);
                                }
                            }
                        }

                        sftp.Disconnect();

                        ViewBag.Message = "Files uploaded successfully.";
                    }
                    else
                    {
                        ViewBag.Message = "Failed to connect to the server.";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Message = $"Error: {ex.Message}";
                    ViewBag.DebugInfo = new List<string> { $"Exception: {ex.Message}" };
                }
            }

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
                        var cmd = client.RunCommand($"cmd /c dir /b \"{fullPath}\"");
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
