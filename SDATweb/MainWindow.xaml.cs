using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using WinRT.Interop;

namespace SDATweb
{
    public sealed partial class MainWindow : Window
    {
        private const string edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
        private WebsiteDataModel websiteDataModel = new WebsiteDataModel();
        private FilePickerService filePickerService = new FilePickerService();
        private HttpRequestSender requestSender = new HttpRequestSender();
        private SystemProcessLauncher processLauncher = new SystemProcessLauncher();
        private const string systemPrompt = "Only answer in html, do not comment. Always start with <html> and end with </html>. Do not format it as markdown. Do NOT include in reply anything else than html markup code. Available assets: ";
        private string apiKey = "";
        private string apiUrl = "http://127.0.0.1:8080/v1/chat/completions";
        private string appName = "My Website";
        private string deployFolder = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Krepysh\\site";
        private string assetsFolder = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Krepysh\\site\\assets";
        private int appPort = 5500;
        private bool useLocalServer = false;
        private Process? serverProcess;

        public MainWindow()
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] == "-key" && i + 1 < args.Length) { apiKey = args[i + 1]; }
                if (args[i] == "-url" && i + 1 < args.Length) { apiUrl = args[i + 1]; }
                if (args[i] == "-name" && i + 1 < args.Length)
                {
                    appName = args[i + 1];
                }
                if (args[i] == "-path" && i + 1 < args[i].Length)
                {
                    deployFolder = args[i + 1];
                    assetsFolder = Path.Combine(deployFolder, "assets");
                }
            }

            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            keyBox.Password = apiKey;
            urlBox.Text = apiUrl;
            nameBox.Text = appName;

            useLocalServer = HasLocalServer();

            _ = LoadConfig();

            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            StopServer();
        }

        private void NewPage(object sender, RoutedEventArgs e)
        {
            string contentOfThePage = """
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Sample Page</title>
                </head>
                <body>
                    <h1>Welcome to My HTML Page</h1>
                    <p>This is a sample paragraph with proper line breaks.</p>
                </body>
                </html>
                """;
            string nameOfThePage = "Sample Page";

            // add to data model first
            websiteDataModel.AddNewPage(contentOfThePage, nameOfThePage);

            // add actual data object instead of placeholder
            lb_pages.Items.Add(new PageItem { Name = nameOfThePage, Content = contentOfThePage });
        }

        private void RemovePage(object sender, RoutedEventArgs e)
        {
            if (lb_pages.SelectedIndex != -1)
            {
                int idx = lb_pages.SelectedIndex;
                // remove from the data model as well
                websiteDataModel.RemovePage(idx);
                lb_pages.Items.RemoveAt(idx);
            }
        }

        private async void SendRequest(object sender, RoutedEventArgs e)
        {
            // Robustly find the related textboxes for the ListBox item that contains the clicked button.
            if (sender is not Button sendButton)
                return;

            // Find the ListBoxItem container
            DependencyObject current = sendButton;
            while (current != null && current is not ListBoxItem)
            {
                current = VisualTreeHelper.GetParent(current);
            }

            if (current is not ListBoxItem listBoxItem)
            {
                // fallback: try to find within the whole listbox selected item
                if (lb_pages.SelectedItem is PageItem selectedPage)
                {
                    // send request using the selected page's content textbox if possible
                    string prompt = string.Empty;
                    string response = await requestSender.SendHTTP(urlBox.Text, keyBox.Password, prompt, systemPrompt + FileNames());
                    // no UI textbox to update in this fallback
                }
                return;
            }

            // Find all TextBox descendants inside the ListBoxItem
            var textBoxes = FindDescendants<TextBox>(listBoxItem);

            TextBox? promptBox = null;
            TextBox? contentBox = null;

            // Heuristic: contentBox in template has Height=160, so pick that as contentBox
            foreach (var tb in textBoxes)
            {
                if (Math.Abs(tb.Height - 160) < 0.1)
                {
                    contentBox = tb;
                }
                else if (string.Equals(tb.PlaceholderText, "Enter AI prompt...", StringComparison.OrdinalIgnoreCase))
                {
                    promptBox = tb;
                }
            }

            // fallback assignments
            if (contentBox == null)
            {
                // try to pick the largest textbox by ActualHeight
                foreach (var tb in textBoxes)
                {
                    if (contentBox == null || tb.ActualHeight > contentBox.ActualHeight)
                        contentBox = tb;
                }
            }

            if (promptBox == null)
            {
                // pick the first textbox that is not the contentBox
                foreach (var tb in textBoxes)
                {
                    if (tb != contentBox)
                    {
                        promptBox = tb;
                        break;
                    }
                }
            }

            if (promptBox == null || contentBox == null)
                return;

            contentBox.Text = "Waiting for response...";
            try
            {
                contentBox.Text = await requestSender.SendHTTP(urlBox.Text, keyBox.Password, promptBox.Text, systemPrompt + FileNames());
            }
            catch (Exception ex)
            {
                contentBox.Text = $"Error: {ex.Message}";
            }
        }

        // Helper to find descendants of a specific type
        private static List<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
        {
            var results = new List<T>();
            if (root == null) return results;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int childCount = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (child is T t)
                        results.Add(t);
                    queue.Enqueue(child);
                }
            }

            return results;
        }

        private async void BuildWebsite(object sender, RoutedEventArgs e)
        {
            clearSite();
            copyAssets();

            try
            {
                Directory.CreateDirectory(deployFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating folder {deployFolder}: {ex.Message}");
                return;
            }

            File.WriteAllText(Path.Combine(deployFolder, "styles.css"), cssBox.Text);

            // a navigation menu listing all pages
            var navLinks = new System.Text.StringBuilder();
            navLinks.AppendLine("<nav>");
            for (int i = 0; i < websiteDataModel.PagesContent.Count; i++)
            {
                navLinks.AppendLine($"<a href='{websiteDataModel.PagesName[i].Replace(" ", "")}{i}.html'>{websiteDataModel.PagesName[i]}</a>");
            }
            navLinks.AppendLine("</nav>");
            string navHtml = navLinks.ToString();

            // write each page with the navigation menu injected
            for (int i = 0; i < websiteDataModel.PagesContent.Count; i++)
            {
                string pageHtml = websiteDataModel.PagesContent[i];

                int headIndex = pageHtml.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
                if (headIndex >= 0)
                {
                    headIndex += "<head>".Length;
                    pageHtml = pageHtml.Insert(headIndex, "<link rel='icon' type='image/png' href='assets/icon.png'> <link rel=\"stylesheet\" href=\"styles.css\">");
                }
                else
                {
                    pageHtml += "<link rel='icon' type='image/png' href='assets/icon.png'> <link rel=\"stylesheet\" href=\"styles.css\">";
                }

                int bodyIndex = pageHtml.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);
                if (bodyIndex >= 0)
                {
                    bodyIndex += "<body>".Length;
                    pageHtml = pageHtml.Insert(bodyIndex, navHtml);
                }
                else
                {
                    pageHtml += navHtml;
                }

                string pageFileName = Path.Combine(deployFolder, $"{websiteDataModel.PagesName[i].Replace(" ", "")}{i}.html");
                try
                {
                    File.WriteAllText(pageFileName, pageHtml);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving file {pageFileName}: {ex.Message}");
                }
            }

            // Create an index.html that serves as the landing page with a list of links
            var indexContent = new System.Text.StringBuilder();
            if (indexToggle.IsChecked == false || websiteDataModel.PagesContent.Count == 0)
            {
                indexContent.AppendLine("<!DOCTYPE html>");
                indexContent.AppendLine("<html>");
                indexContent.AppendLine($"<head><title>{nameBox.Text} Home</title><link rel='icon' type='image/png' href='assets/icon.png'><link rel=\"stylesheet\" href=\"styles.css\"></head>");
                indexContent.AppendLine("<body>");
                indexContent.AppendLine(navHtml);
                indexContent.AppendLine($"<h1>Welcome to the {nameBox.Text} Home Page</h1>");
                indexContent.AppendLine("<ul>");
                for (int i = 0; i < websiteDataModel.PagesContent.Count; i++)
                {
                    indexContent.AppendLine($"<li><a href='{websiteDataModel.PagesName[i].Replace(" ", "")}{i}.html'>{websiteDataModel.PagesName[i]}</a></li>");
                }
                indexContent.AppendLine("</ul>");
                indexContent.AppendLine("</body>");
                indexContent.AppendLine("</html>");
            }
            else
            {
                indexContent.AppendLine("<!DOCTYPE html>");
                indexContent.AppendLine("<html>");
                indexContent.AppendLine($"<head><title>{nameBox.Text} Home</title><link rel='icon' type='image/png' href='assets/icon.png'><meta http-equiv='refresh' content=\"0; url='{websiteDataModel.PagesName[0].Replace(" ", "")}0.html'\" /></head>");
                indexContent.AppendLine("<body>");
                indexContent.AppendLine(navHtml);
                indexContent.AppendLine($"<h1>Welcome to the {nameBox.Text} Home Page</h1>");
                indexContent.AppendLine("</body>");
                indexContent.AppendLine("</html>");
            }

            string indexFileName = Path.Combine(deployFolder, "index.html");
            try
            {
                File.WriteAllText(indexFileName, indexContent.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving file {indexFileName}: {ex.Message}");
            }

            // Create a 404.html page for handling not found routes
            var notFoundContent = new System.Text.StringBuilder();
            notFoundContent.AppendLine("<!DOCTYPE html>");
            notFoundContent.AppendLine("<html>");
            notFoundContent.AppendLine("<head><title>404 Not Found</title><link rel='icon' type='image/png' href='assets/icon.png'></head>");
            notFoundContent.AppendLine("<body>");
            notFoundContent.AppendLine(navHtml);
            notFoundContent.AppendLine("<h1>404 - Page Not Found</h1>");
            notFoundContent.AppendLine("<p>The page you are looking for does not exist.</p>");
            notFoundContent.AppendLine("</body>");
            notFoundContent.AppendLine("</html>");

            string notFoundFileName = Path.Combine(deployFolder, "404.html");
            try
            {
                File.WriteAllText(notFoundFileName, notFoundContent.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving file {notFoundFileName}: {ex.Message}");
            }

            // save current work
            try
            {
                await SaveConfig();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        private void copyAssets()
        {
            try
            {
                Directory.CreateDirectory(assetsFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating folder {assetsFolder}: {ex.Message}");
                return;
            }
            foreach (var asset in websiteDataModel.Assets)
            {
                string destPath = Path.Combine(assetsFolder, asset.Name);
                try
                {
                    File.Copy(asset.Path, destPath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error copying file {asset.Name}: {ex.Message}");
                }
            }
            // copy icon
            try
            {
                File.Copy(iconBox.Text, Path.Combine(assetsFolder, "icon.png"), true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying file {iconBox.Text}: {ex.Message}");
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                DependencyObject parent = textBox;
                while (parent != null && parent is not ListBoxItem)
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }

                if (parent is ListBoxItem listBoxItem)
                {
                    ListBox listBox = (ListBox)ItemsControl.ItemsControlFromItemContainer(listBoxItem);

                    if (listBox != null)
                    {
                        int index = listBox.ItemContainerGenerator.IndexFromContainer(listBoxItem);

                        // Update the PageItem object
                        if (listBox.Items[index] is PageItem pageItem)
                        {
                            pageItem.Content = textBox.Text;
                        }

                        // Update the data model
                        if (index < websiteDataModel.PagesContent.Count)
                        {
                            websiteDataModel.UpdatePageContent(index, textBox.Text);
                        }
                        else
                        {
                            websiteDataModel.PagesContent.Add(textBox.Text);
                        }
                    }
                }
            }
        }

        private void TextBox_NameChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                DependencyObject parent = textBox;
                while (parent != null && parent is not ListBoxItem)
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }

                if (parent is ListBoxItem listBoxItem)
                {
                    ListBox listBox = (ListBox)ItemsControl.ItemsControlFromItemContainer(listBoxItem);

                    if (listBox != null)
                    {
                        int index = listBox.ItemContainerGenerator.IndexFromContainer(listBoxItem);

                        // Update the PageItem object
                        if (listBox.Items[index] is PageItem pageItem)
                        {
                            pageItem.Name = textBox.Text;
                        }

                        // Update the data model
                        if (index < websiteDataModel.PagesName.Count)
                        {
                            websiteDataModel.UpdatePageName(index, textBox.Text);
                        }
                        else
                        {
                            websiteDataModel.PagesName.Add(textBox.Text);
                        }
                    }
                }
            }
        }

        private void clearSite()
        {
            try
            {
                if (Directory.Exists(deployFolder))
                {
                    // it should delete contents of the folder, not itself. fix tomorrow.
                    Directory.Delete(deployFolder, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting folder {deployFolder}: {ex.Message}");
            }
        }

        private void ClearItems(object sender, RoutedEventArgs e)
        {
            clearSite();
            websiteDataModel.ClearAll();
            lb_pages.Items.Clear();
        }

        private void OpenDirectory(object sender, RoutedEventArgs e)
        {
            processLauncher.GenericStartProcess("explorer.exe", deployFolder);
        }

        private async void OpenIndex(object sender, RoutedEventArgs e)
        {
            BuildWebsite(sender, e);

            if (useLocalServer)
            {
                string index = await StartServer();

                processLauncher.GenericStartProcess(edgePath, index);
            }
            else
            {
                processLauncher.GenericStartProcess(edgePath, $"{deployFolder}/index.html");
            }
        }

        private async void SelectIcon(object sender, RoutedEventArgs e)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            StorageFile file = await filePickerService.SelectFile([".png"], hWnd);
            if (file != null)
            {
                iconBox.Text = file.Path;
            }
        }

        private void ClearAssets(object sender, RoutedEventArgs e)
        {
            lb_assets.Items.Clear();
            websiteDataModel.ClearAssets();
        }

        private async void AddAsset(object sender, RoutedEventArgs e)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            StorageFile file = await filePickerService.SelectFile(["*"], hWnd);
            if (file != null)
            {
                lb_assets.Items.Add(file.Name);
                websiteDataModel.AddAsset(file);
            }
        }

        private string FileNames()
        {
            string res = String.Empty;

            foreach (var item in lb_assets.Items)
                res += "assets\\" + item.ToString() + ";";

            return res;
        }

        private bool HasLocalServer()
        {
            if (File.Exists("LocalServer\\KrepyshLocalServer.exe"))
                return true;

            return false;
        }

        private async Task<string> StartServer()
        {
            StopServer();

            serverProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "LocalServer\\KrepyshLocalServer.exe",
                    Arguments = $"-port {appPort} -folder {deployFolder}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            serverProcess.Exited += (s, e) =>
            {
                Debug.WriteLine("Server process exited unexpectedly");
                serverProcess = null;
            };

            serverProcess.Start();

            string? response = await serverProcess.StandardOutput.ReadLineAsync();

            if (response != null && response.StartsWith("*SUC "))
            {
                return response.Substring(5);
            }

            StopServer();
            return "failed";
        }

        private void StopServer()
        {
            if (serverProcess != null)
            {
                try
                {
                    if (!serverProcess.HasExited)
                    {
                        serverProcess.Kill();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error stopping server: {ex.Message}");
                }
                finally
                {
                    serverProcess?.Dispose();
                    serverProcess = null;
                }
            }
        }

        private async Task SaveConfig()
        {
            var cfg = new ConfigModel
            {
                AppName = nameBox?.Text ?? appName,
                ApiKey = keyBox?.Password ?? apiKey,
                ApiUrl = urlBox?.Text ?? apiUrl,
                DeployFolder = deployFolder,
                AssetsFolder = assetsFolder,
                AppPort = appPort,
                UseLocalServer = useLocalServer,
                IndexToggle = indexToggle?.IsChecked ?? false,
                Css = cssBox?.Text ?? "",
                IconPath = iconBox?.Text ?? "",
                Pages = new List<PageInfo>(),
                Assets = new List<AssetInfo>()
            };

            for (int i = 0; i < websiteDataModel.PagesName.Count && i < websiteDataModel.PagesContent.Count; i++)
            {
                cfg.Pages.Add(new PageInfo { Name = websiteDataModel.PagesName[i], Content = websiteDataModel.PagesContent[i] });
            }

            foreach (var asset in websiteDataModel.Assets)
            {
                cfg.Assets.Add(new AssetInfo { Name = asset.Name, Path = asset.Path });
            }

            try
            {
                Directory.CreateDirectory(deployFolder);
                string configPath = Path.Combine(deployFolder, "config.json");
                string json = JsonSerializer.Serialize(cfg, SourceGenerationContext.Default.ConfigModel);
                await File.WriteAllTextAsync(configPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveConfig error: {ex.Message}");
                urlBox.Text = ex.Message;
                throw;
            }
        }

        // Load config from deployFolder\config.json and apply to UI/model
        public async Task LoadConfig()
        {
            try
            {
                string configPath = Path.Combine(deployFolder, "config.json");
                if (!File.Exists(configPath))
                {
                    Debug.WriteLine($"{configPath} does not exist");
                    return;
                }

                string json = await File.ReadAllTextAsync(configPath);
                var cfg = JsonSerializer.Deserialize<ConfigModel>(json, SourceGenerationContext.Default.ConfigModel);
                if (cfg == null)
                    return;

                // apply core settings
                deployFolder = string.IsNullOrWhiteSpace(cfg.DeployFolder) ? deployFolder : cfg.DeployFolder;
                assetsFolder = string.IsNullOrWhiteSpace(cfg.AssetsFolder) ? assetsFolder : cfg.AssetsFolder;
                appPort = cfg.AppPort;
                useLocalServer = cfg.UseLocalServer;

                try
                {
                    if (nameBox != null) nameBox.Text = cfg.AppName ?? nameBox.Text;
                    if (urlBox != null) urlBox.Text = cfg.ApiUrl ?? urlBox.Text;
                    if (keyBox != null) keyBox.Password = cfg.ApiKey ?? keyBox.Password;
                    if (cssBox != null) cssBox.Text = cfg.Css ?? cssBox.Text;
                    if (iconBox != null) iconBox.Text = cfg.IconPath ?? iconBox.Text;
                    if (indexToggle != null) indexToggle.IsChecked = cfg.IndexToggle;

                    // clear existing pages/assets and load from config
                    websiteDataModel.ClearAll();
                    lb_pages.Items.Clear();
                    lb_assets.Items.Clear();

                    // Add pages with actual data from JSON
                    foreach (var p in cfg.Pages)
                    {
                        websiteDataModel.AddNewPage(p.Content ?? "", p.Name ?? "Page");
                        lb_pages.Items.Add(new PageItem
                        {
                            Name = p.Name ?? "Page",
                            Content = p.Content ?? ""
                        });
                    }

                    foreach (var a in cfg.Assets)
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(a.Path) && File.Exists(a.Path))
                            {
                                StorageFile file = await StorageFile.GetFileFromPathAsync(a.Path);
                                if (file != null)
                                {
                                    websiteDataModel.AddAsset(file);
                                    lb_assets.Items.Add(file.Name);
                                }
                            }
                        }
                        catch (Exception exAsset)
                        {
                            Debug.WriteLine($"LoadConfig: failed to add asset '{a.Path}': {exAsset.Message}");
                        }
                    }
                }
                catch (Exception exUi)
                {
                    Debug.WriteLine($"LoadConfig UI apply error: {exUi.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadConfig error: {ex.Message}");
            }
        }
    }
}
