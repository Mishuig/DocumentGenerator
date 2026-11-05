using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using DocumentGenerator.Domain;
using DocumentGenerator.Infrastructure;
using DocumentGenerator.Services;

namespace DocumentGenerator;

public partial class MainWindow : Window
{
    private readonly TemplateService _templates = new();
    private readonly DocxTemplateEngine _engine = new();
    private readonly Dictionary<string, TextBox> _inputs = new(StringComparer.OrdinalIgnoreCase);
    private TemplateRecord? _selected;
    private bool _loading;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadTemplatesAsync();
    }

    private async Task LoadTemplatesAsync(int? selectId = null)
    {
        try
        {
            _loading = true;
            var items = await _templates.GetAllAsync(SearchBox.Text);
            TemplatesList.ItemsSource = items;
            var selected = items.FirstOrDefault(x => x.Id == (selectId ?? _selected?.Id));
            TemplatesList.SelectedItem = selected ?? (items.Count > 0 ? items[0] : null);
            if (TemplatesList.SelectedItem is null) ShowNoSelection();
        }
        catch (Exception ex) { ShowError("Не вдалося завантажити шаблони", ex); }
        finally { _loading = false; }
    }

    private async void ImportTemplate_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Оберіть Word-шаблон", Filter = "Word documents (*.docx)|*.docx", CheckFileExists = true };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var record = await _templates.ImportAsync(dialog.FileName);
            await LoadTemplatesAsync(record.Id);
            MessageBox.Show($"Шаблон «{record.Name}» імпортовано.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError("Не вдалося імпортувати DOCX", ex); }
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    { if (!_loading && IsLoaded) await LoadTemplatesAsync(); }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadTemplatesAsync();

    private void TemplatesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selected = TemplatesList.SelectedItem as TemplateRecord;
        _inputs.Clear(); FieldsPanel.Children.Clear();
        if (_selected is null) { ShowNoSelection(); return; }
        try
        {
            SelectedTemplateTitle.Text = _selected.Name;
            var path = AppPaths.ResolveTemplate(_selected.RelativePath);
            var keys = _engine.FindPlaceholders(path);
            TemplateInfo.Text = $"Категорія: {_selected.Category} • Змінних знайдено: {keys.Count}. Заповніть поля нижче.";
            if (keys.Count == 0)
            {
                FieldsPanel.Children.Add(new TextBlock { Text = "У шаблоні не знайдено маркерів {{variable}}. Додайте їх у DOCX та повторно імпортуйте файл.", TextWrapping = TextWrapping.Wrap, Foreground = System.Windows.Media.Brushes.DarkOrange, Margin = new Thickness(0, 4, 0, 12) });
                return;
            }
            foreach (var key in keys)
            {
                FieldsPanel.Children.Add(new TextBlock { Text = Humanize(key), FontWeight = FontWeights.SemiBold, Foreground = System.Windows.Media.Brushes.DarkSlateGray, Margin = new Thickness(0, 9, 0, 0) });
                var input = new TextBox { Tag = key, MinWidth = 260, AcceptsReturn = key.Contains("address", StringComparison.OrdinalIgnoreCase) || key.Contains("comment", StringComparison.OrdinalIgnoreCase), TextWrapping = TextWrapping.Wrap };
                _inputs[key] = input; FieldsPanel.Children.Add(input);
            }
        }
        catch (Exception ex) { ShowError("Не вдалося прочитати шаблон", ex); }
    }

    private void ShowNoSelection()
    {
        _selected = null; _inputs.Clear(); FieldsPanel.Children.Clear();
        SelectedTemplateTitle.Text = "Оберіть шаблон";
        TemplateInfo.Text = "Імпортуйте DOCX або оберіть шаблон зі списку.";
        FieldsPanel.Children.Add(new TextBlock { Text = "Поля шаблону з'являться тут.", Foreground = System.Windows.Media.Brushes.Gray });
    }

    private static string Humanize(string key)
    {
        var spaced = System.Text.RegularExpressions.Regex.Replace(key, "([a-z])([A-Z])", "$1 $2");
        return $"{spaced} ({key})";
    }

    private void OpenTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        try { Process.Start(new ProcessStartInfo(AppPaths.ResolveTemplate(_selected.RelativePath)) { UseShellExecute = true }); }
        catch (Exception ex) { ShowError("Не вдалося відкрити файл", ex); }
    }

    private async void Rename_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        var dialog = new TemplateEditWindow(_selected.Name, _selected.Category) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try { await _templates.RenameAsync(_selected.Id, dialog.TemplateName, dialog.Category); await LoadTemplatesAsync(_selected.Id); }
        catch (Exception ex) { ShowError("Не вдалося зберегти налаштування шаблону", ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        if (MessageBox.Show($"Видалити шаблон «{_selected.Name}» з бібліотеки? Файл буде переміщено до backups.", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { await _templates.DeleteAsync(_selected.Id); await LoadTemplatesAsync(); }
        catch (Exception ex) { ShowError("Не вдалося видалити шаблон", ex); }
    }

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        try { await _templates.BackupAllAsync(); MessageBox.Show($"Резервну копію створено у папці:\n{AppPaths.Backups}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information); }
        catch (Exception ex) { ShowError("Не вдалося створити резервну копію", ex); }
    }

    private void ClearFields_Click(object sender, RoutedEventArgs e) { foreach (var input in _inputs.Values) input.Clear(); }

    private async void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) { MessageBox.Show("Спочатку оберіть шаблон.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (_inputs.Count == 0) { MessageBox.Show("У шаблоні немає змінних для заповнення.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var values = _inputs.ToDictionary(x => x.Key, x => x.Value.Text, StringComparer.OrdinalIgnoreCase);
        var missing = values.Where(x => string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Key).ToList();
        if (missing.Count > 0 && MessageBox.Show($"Не заповнені поля: {string.Join(", ", missing)}.\nПродовжити з порожніми значеннями?", "Перевірка даних", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var dialog = new SaveFileDialog { Title = "Зберегти сформований документ", Filter = "Word document (*.docx)|*.docx", FileName = $"{SafeFileName(_selected.Name)}_{DateTime.Now:yyyyMMdd_HHmm}.docx", DefaultExt = ".docx", InitialDirectory = AppPaths.Exports };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var templatePath = AppPaths.ResolveTemplate(_selected.RelativePath);
            if (string.Equals(Path.GetFullPath(templatePath), Path.GetFullPath(dialog.FileName), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Не можна зберігати результат поверх оригінального шаблону. Оберіть інше ім’я або папку exports.");
            await Task.Run(() => _engine.Generate(templatePath, dialog.FileName, values));
            MessageBox.Show("Документ успішно сформовано.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            if (MessageBox.Show("Відкрити сформований документ?", "Документ готовий", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
        }
        catch (Exception ex) { ShowError("Помилка генерації документа", ex); }
    }

    private static string SafeFileName(string name)
    { foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_'); return name; }

    private static void ShowError(string title, Exception ex)
    {
        try { File.AppendAllText(Path.Combine(AppPaths.Logs, "application.log"), $"{DateTime.UtcNow:O} | {title}\n{ex}\n\n"); } catch { }
        MessageBox.Show($"{title}.\n{ex.Message}\n\nДеталі записано в logs/application.log.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
