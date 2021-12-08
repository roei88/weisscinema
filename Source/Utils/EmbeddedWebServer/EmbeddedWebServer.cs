// MIT License - Copyright (c) 2016 Can Güney Aksakalli
// https://aksakalli.github.io/2014/02/24/simple-http-server-with-csparp.html

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Web;

namespace Utils
{
    public class EmbeddedWebServer
    {
        private readonly string[] _indexFiles = { 
            "index.html", 
            "index.htm", 
            "default.html", 
            "default.htm" 
        };

        private static IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
        #region extension to MIME type list
        {".asf", "video/x-ms-asf"},
        {".asx", "video/x-ms-asf"},
        {".avi", "video/x-msvideo"},
        {".bin", "application/octet-stream"},
        {".cco", "application/x-cocoa"},
        {".crt", "application/x-x509-ca-cert"},
        {".css", "text/css"},
        {".deb", "application/octet-stream"},
        {".der", "application/x-x509-ca-cert"},
        {".dll", "application/octet-stream"},
        {".dmg", "application/octet-stream"},
        {".ear", "application/java-archive"},
        {".eot", "application/octet-stream"},
        {".exe", "application/octet-stream"},
        {".flv", "video/x-flv"},
        {".gif", "image/gif"},
        {".hqx", "application/mac-binhex40"},
        {".htc", "text/x-component"},
        {".htm", "text/html"},
        {".html", "text/html"},
        {".ico", "image/x-icon"},
        {".img", "application/octet-stream"},
        {".iso", "application/octet-stream"},
        {".jar", "application/java-archive"},
        {".jardiff", "application/x-java-archive-diff"},
        {".jng", "image/x-jng"},
        {".jnlp", "application/x-java-jnlp-file"},
        {".jpeg", "image/jpeg"},
        {".jpg", "image/jpeg"},
        {".js", "application/x-javascript"},
        {".m3u8", "application/x-mpegURL"},
        {".mml", "text/mathml"},
        {".mng", "video/x-mng"},
        {".mov", "video/quicktime"},
        {".mp3", "audio/mpeg"},
        {".mpeg", "video/mpeg"},
        {".mpg", "video/mpeg"},
        {".msi", "application/octet-stream"},
        {".msm", "application/octet-stream"},
        {".msp", "application/octet-stream"},
        {".pdb", "application/x-pilot"},
        {".pdf", "application/pdf"},
        {".pem", "application/x-x509-ca-cert"},
        {".pl", "application/x-perl"},
        {".pm", "application/x-perl"},
        {".png", "image/png"},
        {".prc", "application/x-pilot"},
        {".ra", "audio/x-realaudio"},
        {".rar", "application/x-rar-compressed"},
        {".rpm", "application/x-redhat-package-manager"},
        {".rss", "text/xml"},
        {".run", "application/x-makeself"},
        {".sea", "application/x-sea"},
        {".shtml", "text/html"},
        {".sit", "application/x-stuffit"},
        {".swf", "application/x-shockwave-flash"},
        {".tcl", "application/x-tcl"},
        {".ts", "video/vnd.dlna.mpeg-tts"},
        {".tk", "application/x-tcl"},
        {".txt", "text/plain"},
        {".war", "application/java-archive"},
        {".wbmp", "image/vnd.wap.wbmp"},
        {".wmv", "video/x-ms-wmv"},
        {".xml", "text/xml"},
        {".xpi", "application/x-xpinstall"},
        {".zip", "application/zip"},
        #endregion
        };

        private string _rootDirectory;
        private HttpListener _listener;
        private int _port;
        private Dictionary<string, string> _virtualDirectories = new Dictionary<string, string>();

        public delegate void ReceiveWebRequestCallback(HttpListenerContext Context);
        public event ReceiveWebRequestCallback _receiveWebRequest;

        StreamWriter _accessLog = null;


        public int Port
        {
            get { return _port; }
            private set { }
        }

        /// <summary>
        /// Construct server with given port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        /// <param name="port">Port of the server.</param>
        public EmbeddedWebServer(string path, int port)
        {
            Console.WriteLine("Starting embedded web server at port " + port + " with path: " + path);

            string accessLogFilename = "EmbeddedWebServer_Access_log";
            string accessLogPath = accessLogFilename;

            try
            {
                int idx = 0;
                int max = 3;
                do
                {
                    var name = (idx > 0) ? accessLogPath + "_" + idx + "_.log" : accessLogPath + ".log";
                    _accessLog = File.AppendText(name);
                    
                    idx++;

                    if (_accessLog == null)
                        Thread.Yield();
                }
                while (_accessLog == null && idx < max );
            }
            catch (Exception e)
            {
                Console.WriteLine("exception in EmbeddedWebServer constructor: " + e.Message + " " + e.StackTrace);
            }

            this.Initialize(path, port);
        }

        ~EmbeddedWebServer()
        {
            Stop();

            if (_accessLog != null)
            {
                _accessLog.Close();
            }
        }

        /// <summary>
        /// Construct server with suitable port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        public EmbeddedWebServer(string path)
        {
            //get an empty port
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            this.Initialize(path, port);
        }


        private void WriteLog(string entry)
        {

            if (_accessLog != null)
            {
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + entry;
                Console.WriteLine(line);
                _accessLog.WriteLine(line);
                _accessLog.Flush();
            }

        }

        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            if (_listener != null && _listener.IsListening)
                _listener.Stop();
        }

        private void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
            _listener.Start();

            try
            {
                IAsyncResult result = _listener.BeginGetContext(new AsyncCallback(WebRequestCallback), _listener);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Context Error: " + ex.Message + " [" + ex.StackTrace + "]");
            }
        }

        protected void WebRequestCallback(IAsyncResult result)
        {
            if ((_listener == null) || (!_listener.IsListening))
                return;

            HttpListenerContext context = _listener.EndGetContext(result);
            _listener.BeginGetContext(new AsyncCallback(WebRequestCallback), _listener);

            if (_receiveWebRequest != null)
                _receiveWebRequest(context);

            Process(context);
        }

        private void Process(HttpListenerContext context)
        {
            string urlReferrer = (context.Request.UrlReferrer != null) ? HttpUtility.UrlDecode(context.Request.UrlReferrer.AbsolutePath) : "";
            string decodedPath = HttpUtility.UrlDecode(context.Request.Url.AbsolutePath);
            string filename = decodedPath;
            // Check if this path is from virtual directory
            string rootDirectory = _rootDirectory;
            string[] pathParts = filename.Split('/');
            string[] referrerParts = urlReferrer.Split('/');


            WriteLog(context.Request.RemoteEndPoint + " " +
                context.Request.LocalEndPoint + " " +
                context.Request.HttpMethod + " " +
                decodedPath + " - " +
                context.Response.StatusCode + " " +
                context.Request.UserHostName + " " +
                context.Request.UserAgent + " " +
                context.Request.Url + " " +
                urlReferrer);

            filename = filename.Substring(1);

            if (pathParts.Length > 1)
            {
                string key = pathParts[1];
                string referrerKey = (referrerParts.Length > 1) ? referrerParts[1] : "";
                int keyDetector = _virtualDirectories.ContainsKey(key) ? 1 : (_virtualDirectories.ContainsKey(referrerKey) ? 2 : 0);

                if (keyDetector > 0)
                {
                    string vdirKey = keyDetector == 1 ? key : referrerKey;
                    rootDirectory = _virtualDirectories[vdirKey];
                    filename = filename.Replace(vdirKey, "");
                }
            }
            if (!string.IsNullOrEmpty(filename) && filename[0] == '/')
                filename = filename.Substring(1);

            filename = filename.Replace("/", @"\");


            if (string.IsNullOrEmpty(filename))
            {
                foreach (string indexFile in _indexFiles)
                {
                    if (File.Exists(Path.Combine(rootDirectory, indexFile)))
                    {
                        filename = indexFile;
                        break;
                    }
                }
            }
            filename = Path.Combine(rootDirectory, filename);
            if (File.Exists(filename))
            {
                try
                {
                    Stream input = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

                    //Adding permanent http response headers
                    string mime;
                    context.Response.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime) ? mime : "application/octet-stream";
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filename).ToString("r"));

                    // context.Response.StatusDescription = "OK";
                    // context.Response.ProtocolVersion = new Version("1.1");
                    // context.Response.SendChunked = false;
                    // context.Response.KeepAlive = true;
                    // context.Response.AddHeader("Accept-Ranges", "bytes");
                    // context.Response.AddHeader("Connection", "Keep-Alive");
                    // context.Response.AddHeader("Keep-Alive", "timeout=5, max=100");
                    //CORS
                    // context.Response.AddHeader("Access-Control-Allow-Origin", "*");
                    // context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                    // context.Response.AddHeader("Access-Control-Allow-Headers", "X-Requested-With");
                    // context.Response.AddHeader("Access-Control-Max-Age", "86400");
                    byte[] buffer = new byte[1024 * 16];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.OutputStream.Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Process Error: " + ex.Message + " [" + ex.StackTrace + "]");
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }

            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            context.Response.OutputStream.Close();
        }

        private void Initialize(string path, int port)
        {
            this._rootDirectory = path;
            this._port = port;
            Listen();
        }

        public void AddVirtualDirectory(string name, string path)
        {
            if (_virtualDirectories.ContainsKey(name))
            {
                Console.WriteLine("Updating virtual directory \"" + name + "\" from: " + _virtualDirectories[name] + " to: " + path);
                _virtualDirectories[name] = path;
            }
            else
            {
                Console.WriteLine("Added virtual directory \"" + name + "\" to path: " + path);
                _virtualDirectories.Add(name, path);
            }
        }
    }
}
