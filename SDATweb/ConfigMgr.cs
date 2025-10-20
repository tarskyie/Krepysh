using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Services.Maps;
using Windows.Storage;

namespace SDATweb
{
    public class ConfigModel
    {
        public string AppName { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string ApiUrl { get; set; } = "";
        public string DeployFolder { get; set; } = "";
        public string AssetsFolder { get; set; } = "";
        public int AppPort { get; set; } = 5500;
        public bool UseLocalServer { get; set; } = false;
        public bool IndexToggle { get; set; } = false;
        public string Css { get; set; } = "";
        public string IconPath { get; set; } = "";
        public List<PageInfo> Pages { get; set; } = new List<PageInfo>();
        public List<AssetInfo> Assets { get; set; } = new List<AssetInfo>();
    }

    public class PageInfo
    {
        public string Name { get; set; } = "";
        public string Content { get; set; } = "";
    }

    public class AssetInfo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }
}
