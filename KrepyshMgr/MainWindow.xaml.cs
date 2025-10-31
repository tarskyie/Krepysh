using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace KrepyshMgr
{
    public partial class MainWindow : Window
    {
        private string DataFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Krepysh\\projects.json");
        public ObservableCollection<ProjectItem> Projects { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            ProjectsListBox.ItemsSource = Projects;
            ProjectsListBox.SelectionChanged += ProjectsListBox_SelectionChanged;
            LoadProjects();
            UpdateStatus();
        }

        private void ProjectsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveButton.IsEnabled = ProjectsListBox.SelectedItem != null;
            RenameButton.IsEnabled = ProjectsListBox.SelectedItem != null;
            UpdateStatus();
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            var idx = Projects.Count + 1;
            var p = new ProjectItem { Name = $"Project {idx}", Path = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Krepysh\\Project{idx}" };
            Projects.Add(p);
            ProjectsListBox.SelectedItem = p;
            SaveProjects();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsListBox.SelectedItem is ProjectItem p)
            {
                Projects.Remove(p);
                SaveProjects();

                try
                {
                    DirectoryInfo di = new DirectoryInfo(p.Path);

                    di.Delete(true);
                }
                catch (Exception ex)
                {
                    StatusTextBlock.Text = ex.Message;
                }
            }
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsListBox.SelectedItem is ProjectItem p)
            {
                var dlg = new RenameProjectDialog(p.Name) { Owner = this };
                if (dlg.ShowDialog() == true)
                {
                    var newName = dlg.NewName?.Trim();
                    if (!string.IsNullOrEmpty(newName) && !Projects.Any(x => x != p && x.Name == newName))
                    {
                        p.Name = newName;
                        ProjectsListBox.Items.Refresh();
                        SaveProjects();
                    }
                    else
                    {
                        MessageBox.Show(this, "Invalid or duplicate project name.", "Rename", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void UpdateStatus()
        {
            StatusTextBlock.Text = $"Projects: {Projects.Count}" + (ProjectsListBox.SelectedItem is ProjectItem s ? $" Selected: {s.Name}" : string.Empty);
        }

        private void LoadProjects()
        {
            try
            {
                if (File.Exists(DataFile))
                {
                    var json = File.ReadAllText(DataFile);
                    var list = JsonSerializer.Deserialize<ProjectItem[]>(json);
                    if (list != null)
                    {
                        foreach (var item in list)
                            Projects.Add(item);
                    }
                }
            }
            catch { }
        }

        private void SaveProjects()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Krepysh")))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Krepysh"));
                }
                var json = JsonSerializer.Serialize(Projects.ToArray());
                File.WriteAllText(DataFile, json);
            }
            catch (Exception e)
            {
                StatusTextBlock.Text = e.Message;
            }
        }

        private void OpenProject(object sender, RoutedEventArgs e)
        {
            if (ProjectsListBox.SelectedIndex != -1)
            {
                int idx = ProjectsListBox.SelectedIndex;
                string arguments = $"-path \"{Projects[idx].Path}\" -name \"{Projects[idx].Name}\" -url \"{ApiUrlTextBox.Text}\" -key \"{ApiKeyTextBox.Text}\"";
                StatusTextBlock.Text = arguments;

                try
                {
                    using (Process redactorProcess = new Process())
                    {
                        redactorProcess.StartInfo.FileName = Path.Combine(AppContext.BaseDirectory, "SDATweb\\SDATweb.exe");
                        redactorProcess.StartInfo.Arguments = arguments;
                        redactorProcess.StartInfo.WorkingDirectory = Path.Combine(AppContext.BaseDirectory, "SDATweb");
                        redactorProcess.Start();
                    }
                }
                catch (Exception ex)
                {
                    StatusTextBlock.Text = ex.Message;
                }
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }

    public class ProjectItem : INotifyPropertyChanged
    {
        private string? name;
        private string? path;

        public string? Name { get => name; set { if (name != value) { name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } } }
        public string? Path { get => path; set { if (path != value) { path = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Path))); } } }
        public override string? ToString() => Name;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}