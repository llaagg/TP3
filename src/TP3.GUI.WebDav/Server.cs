using System.Collections.Specialized;
using Common.Logging;
using WebDAVSharp.Server;
using WebDAVSharp.Server.Stores.DiskStore;

namespace TP3.GUI.WebDav;


public class Server
{
    public void Start()
    {
        // create properties
        NameValueCollection properties = new NameValueCollection();
        properties["showDateTime"] = "true";
        // set Adapter
        LogManager.Adapter = new Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter(properties);


        WebDavServer server = new WebDavServer(new WebDavDiskStore("C:\\work"));
        server.Listener.Prefixes.Add("http://localhost/");
        server.Start();
    }
}
