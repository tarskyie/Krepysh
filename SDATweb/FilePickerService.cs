using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

namespace SDATweb
{
    public class FilePickerService
    {
        public async Task<StorageFile> SelectFile(List<string> filterFormats, IntPtr hWnd)
        {
            var picker = new FileOpenPicker();

            InitializeWithWindow.Initialize(picker, hWnd);

            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            foreach (string format in filterFormats)
            {
                picker.FileTypeFilter.Add(format);
            }

            StorageFile file = await picker.PickSingleFileAsync();
            return file;
        }
    }
}
