using System.Windows;

namespace DocumentGenerator;

public partial class TemplateEditWindow : Window
{
    public string TemplateName => NameBox.Text;
    public string Category => CategoryBox.Text;
    public TemplateEditWindow(string name, string category)
    {
        InitializeComponent(); NameBox.Text = name; CategoryBox.Text = category;
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text)) { MessageBox.Show("Вкажіть назву шаблону."); return; }
        DialogResult = true;
    }
}
