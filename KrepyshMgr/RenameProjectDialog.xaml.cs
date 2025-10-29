using System.Windows;

namespace KrepyshMgr
{
 public partial class RenameProjectDialog : Window
 {
 public string NewName { get; private set; }
 public RenameProjectDialog(string currentName)
 {
 InitializeComponent();
 NameTextBox.Text = currentName;
 NameTextBox.SelectAll();
 NameTextBox.Focus();
 }

 private void Ok_Click(object sender, RoutedEventArgs e)
 {
 NewName = NameTextBox.Text;
 DialogResult = true;
 }

 private void Cancel_Click(object sender, RoutedEventArgs e)
 {
 DialogResult = false;
 }
 }
}
