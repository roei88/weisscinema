using System;
using System.Threading;
using System.Diagnostics;

namespace WinMain
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Process currentProc = Process.GetCurrentProcess();
                Console.WriteLine("========== WinMain Started ===============  (" + currentProc.Id.ToString() + ")");

                Thread.CurrentThread.Name = "WinMain";

                Thread InitializerThread = new Thread(new ParameterizedThreadStart(global::Main.Program.Initializer));
                InitializerThread.Name = "Main";
                
                InitializerThread.IsBackground = true;
                InitializerThread.Start((object)args);

                // when not called from service, enter here in infinite loop to keep the process alive (when called by service, the service does it
                while (global::Main.Program._running)
                {
                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception " + ex.Message + " " + ex.StackTrace);
            }
        }
    }
}
