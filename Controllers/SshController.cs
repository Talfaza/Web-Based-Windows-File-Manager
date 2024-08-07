﻿    using Microsoft.AspNetCore.Mvc;
    using Renci.SshNet;
    using System;
    using System.Collections.Generic;
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

            [HttpPost]
            public IActionResult Delete(List<string> items)
            {
                var path = Request.Form["currentPath"];
                using (var client = new SshClient(_currentModel.Ip, _currentModel.Username, _currentModel.Password))
                {
                    try
                    {
                        client.Connect();
                        if (client.IsConnected)
                        {
                            foreach (var item in items)
                            {
                                var fullPath = string.IsNullOrEmpty(path) ? item : $"{path}\\{item}";
                                var cmd = client.RunCommand($"del /q \"{fullPath}\" & rmdir /q /s \"{fullPath}\"");
                                cmd.Execute();
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

                var files = GetFilesAndDirectories(path);
                ViewBag.Files = files;
                ViewBag.CurrentPath = path;
                return View("Index", _currentModel);
            }

        [HttpPost]
        public IActionResult CompressModal(List<string> items)
        {
            ViewBag.ItemsToCompress = items;
            return PartialView("_CompressModal");
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
                        var cmdDel = $"cd \"{fullPath}\"  && del \"{itemsString}\"";

                        //  debugging information 
                        ViewBag.Command = cmdText;
                        ViewBag.ItemsString = itemsString;
                        ViewBag.FullPath = fullPath;

                        var cmd = client.RunCommand(cmdText);
                        var del = client.RunCommand(cmdDel);
                        cmd.Execute();
                        del.Execute();

                        ViewBag.CommandResult = cmd.Result;
                        ViewBag.CommandError = cmd.Error;

                  

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


            var files = GetFilesAndDirectories(path);
            ViewBag.Files = files;
            ViewBag.CurrentPath = path;
            return View("Index", _currentModel);
        }
        [HttpPost]
        public IActionResult Decompress(List<string> items)
        {
            var path = (string)Request.Form["currentPath"];
            using (var client = new SshClient(_currentModel.Ip, _currentModel.Username, _currentModel.Password))
            {
                try
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        var pathDir = string.IsNullOrEmpty(path) ? "." : path;
                        var fullPath = string.IsNullOrEmpty(path) ? "." : pathDir;

                        // Join all the items to be extracted
                        var itemsString = string.Join(" ", items.Select(item => $"\"{item}\""));

                        // Create the command to change directory and extract all items
                        var cmdText = $"cd \"{fullPath}\"  &&tar -xf {itemsString}";

                        var cmd = client.RunCommand(cmdText);
                        cmd.Execute();

                        // Debugging information
                        ViewBag.Command = cmdText;
                        ViewBag.CommandResult = cmd.Result;
                        ViewBag.CommandError = cmd.Error;

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
                            var cmd = client.RunCommand($"dir /b \"{fullPath}\"");
                            files.AddRange(cmd.Result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
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