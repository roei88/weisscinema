using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace Main
{
    public static class Program
    {
        const string WEB_FOLDER_NAME = "dist-ui";
        public static bool _running = true;

        static public void closeApplication(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("Quitting...");
            _running = false;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            Console.WriteLine("Caught Unhandled Exception - Program is" + (e.IsTerminating ? " " : " not ") + "Terminating:\n" + ex.ToString());
        }

        static public void Initializer(object args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(closeApplication);
            MessageHandlersManager messageHandlerManager = new MessageHandlersManager();
            bool success = messageHandlerManager.Start(9001);

            if (!success)
            {
                Console.WriteLine("Could not setup communication!");
            }

            string currentDirectory = Directory.GetCurrentDirectory();
            string webBuildDir = Path.Combine(Directory.GetParent(currentDirectory).FullName, "release", WEB_FOLDER_NAME);
            if (!Directory.Exists(webBuildDir))
            {
                string repoPath = Directory.GetParent(Directory.GetParent(currentDirectory).FullName).FullName;

                webBuildDir = Path.Combine(repoPath, "build", "web");
            }
            
            if (!Directory.Exists(webBuildDir))
            {
                Console.WriteLine("ERROR - CANNOT FIND WEB SOURCE FOLDER");
            }

            Utils.EmbeddedWebServer webServer = new Utils.EmbeddedWebServer(webBuildDir, 32323);

            while (_running)
            {
                Thread.Sleep(1000);
            }

            messageHandlerManager.Close();
            System.Environment.Exit(0);
        }
    }
}