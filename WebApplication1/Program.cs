using Microsoft.Extensions.FileProviders;

namespace KrepyshLocalServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // configure folder and port
            string contentRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Krepysh\\site");
            int port = 5500;
            bool useBrowser = false;

            (contentRoot, port, useBrowser) = ParseArgs(args, contentRoot, port, useBrowser);

            var provider = new PhysicalFileProvider(contentRoot);

            //configure webhost
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls($"http://localhost:{port}");

            var app = builder.Build();

            // directory browser will show up if needed
            if (useBrowser)
            {
                app.UseDirectoryBrowser(new DirectoryBrowserOptions
                {
                    FileProvider = provider,
                    RequestPath = ""
                });
            }

            app.UseStatusCodePagesWithReExecute("/404.html");

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = provider,
                DefaultFileNames = { "index.html" }
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = provider,
                RequestPath = ""
            });

            Console.WriteLine($"*SUC http://localhost:{port}");

            app.Run();
        }

        static (string folder, int port, bool useBrowser) ParseArgs(string[] args, string defaultFolder, int defaultPort, bool defaultBrowser)
        {
            string folder = defaultFolder;
            int port = defaultPort;
            bool useBrowser = defaultBrowser;

            for (int i = 0; i < args.Length - 1; i++)
            {
                switch (args[i])
                {
                    case "-folder":
                        folder = args[i + 1];
                        break;
                    case "-port":
                        if (int.TryParse(args[i + 1], out int parsedPort))
                            port = parsedPort;
                        break;
                    case "-browser":
                        useBrowser = true; 
                        break;
                }
            }

            return (folder, port, useBrowser);
        }

    }
}
