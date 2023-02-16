
using System;
using System.Threading;
using System.Configuration;
using Google.Apis.Logging;

namespace LoopService
{
    public abstract class BackgroundService
    {
        
        protected abstract int GetIntervalSeconds();


        protected virtual void Init() { }
        protected abstract void Execute();

        public void Loop()
        {
            Init();
            var interval = GetIntervalSeconds();

            while (true)
            {

                try
                {
                    Execute();
                }
                catch (Exception ex)
                {
                    
                }

                Thread.Sleep(interval * 1000);
            }
        }

        public static void Main(string[] args)
        {
#if DEBUG
            if (args.Length == 0)

            {
                args = new[] { "GoogleDriveSync" };
            }

#endif

            if (args.Length == 0)
            {
                return;
            }
            var serviceName = args[0];
            switch (serviceName)
            {
                 
                case "GoogleDriveSync":
                    new GoogleDriveSync().Loop();
                    break;
                default:
                    break;
            }
        }
    }
}