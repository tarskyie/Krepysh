using Microsoft.Extensions.FileProviders;

namespace WebApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            // configure folder and port
            string contentRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Krepysh\\site");
            int port = 5500;

            (contentRoot, port) = ParseArgs(args, contentRoot, port);

            var provider = new PhysicalFileProvider(contentRoot);

            //configure webhost
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls($"http://localhost:{port}");

            var app = builder.Build();

            // directory browser will show up if no 
            app.UseDirectoryBrowser(new DirectoryBrowserOptions
            {
                FileProvider = provider,
                RequestPath = ""
            });

            app.UseStatusCodePagesWithReExecute("404.html");

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

            app.Run();
        }

        static (string folder, int port) ParseArgs(string[] args, string defaultFolder, int defaultPort)
        {
            string folder = defaultFolder;
            int port = defaultPort;

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
                }
            }

            return (folder, port);
        }

    }
}
