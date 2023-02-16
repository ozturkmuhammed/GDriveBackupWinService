using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LoopService
{

    public class TestService : BackgroundService
    {

        protected override void Execute()
        {
            ServerMetricsCollectors();
        }
        protected override int GetIntervalSeconds()
        {
            return 10;
        }
        private void ServerMetricsCollectors()
        {
            string path = @"C:\Muhammed\App_Pool_Close\test.txt";
            PathHelper.DirectoryYoksaOlustur(path);
            string path2 = @"C:\Muhammed\MuhammedServices\test.txt";
            PathHelper.DirectoryYoksaOlustur(path2);
            string[] dosyalar = Directory.GetFiles(Path.GetDirectoryName(path), "*.*", SearchOption.AllDirectories);
            for (int m = 0; m < dosyalar.Count(); m++)
            {
                string icerik = PathHelper.TxtReader(dosyalar[m]);
                try
                {
                    File.Delete(dosyalar[m]);
                }
                catch { }
                icerik = icerik.Trim();
                icerik = icerik.Replace("\n", "");
                icerik = icerik.Replace("\r", "");
                using (ServerManager manager = new ServerManager())
                {
                    for (int i = 0; i < manager.ApplicationPools.Count; i++)
                    {
                        if (manager.ApplicationPools[i].Name == icerik)
                        {
                            for (int k = 0; k < manager.ApplicationPools[i].WorkerProcesses.Count; k++)
                            {
                                int pid = manager.ApplicationPools[i].WorkerProcesses[k].ProcessId;
                                ProcessKill(pid);
                            }

                        }
                    }
                }
            }

            Console.WriteLine("Çalıştım..." + DateTime.Now.ToString());

        }

        private void ProcessKill(int pid)
        {
            Process[] prs = Process.GetProcesses();


            foreach (Process pr in prs)
            {
                if (pr.Id == pid)
                {

                    pr.Kill();

                }

            }
        }
    }
}
