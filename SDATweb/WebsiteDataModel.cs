using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace SDATweb
{
    public class WebsiteDataModel
    {
        public List<string> PagesContent { get; } = new List<string>();
        public List<string> PagesName { get; } = new List<string>();
        public List<Windows.Storage.StorageFile> Assets { get; } = new List<Windows.Storage.StorageFile>();

        public void AddNewPage(string content, string name)
        {
            PagesContent.Add(content);
            PagesName.Add(name);
        }

        // Remove a page at the specified index (removes both name and content)
        public void RemovePage(int index)
        {
            if (index >= 0 && index < PagesContent.Count && index < PagesName.Count)
            {
                PagesContent.RemoveAt(index);
                PagesName.RemoveAt(index);
            }
        }

        public void UpdatePageContent(int index, string content)
        {
            if (index >= 0 && index < PagesContent.Count)
            {
                PagesContent[index] = content;
            }
        }

        public void UpdatePageName(int index, string name)
        {
            if (index >= 0 && index < PagesName.Count)
            {
                PagesName[index] = name;
            }
        }

        public void AddAsset(Windows.Storage.StorageFile asset)
        {
            Assets.Add(asset);
        }

        public void ClearAll()
        {
            PagesContent.Clear();
            PagesName.Clear();
            Assets.Clear();
        }

        public void ClearAssets()
        {
            Assets.Clear();
        }
    }

    public class PageItem
    {
        public string Name { get; set; } = "Sample Page";
        public string Content { get; set; } = "<!DOCTYPE html>\n<html>\n<head>\n    <title>Sample Page</title>\n</head>\n<body>\n    <h1>Welcome to My HTML Page</h1>\n    <p>This is a sample paragraph with proper line breaks.</p>\n</body>\n</html>";
    }
}
