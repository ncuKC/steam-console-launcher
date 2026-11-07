using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
namespace ConsoleLauncher;
public record Game(string Id, string Name, bool Installed);
public class Program : Application
{
    [STAThread] public static void Main() => new Program().Run(new LauncherWindow());
}
public class LauncherWindow : Window
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(25) };
    static readonly string Cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SteamConsoleLauncher", "library.json");
    readonly ListBox list = new();
    readonly TextBlock title = new(), status = new(), label = new();
    readonly Image backdrop = new() { Stretch = Stretch.UniformToFill, Opacity = .45 };
    readonly TextBox search = new();
    readonly ComboBox filter = new();
    readonly Button play;
    List<Game> games = new();
    bool home = true;
    static Brush Color(string value) => (Brush)new BrushConverter().ConvertFromString(value)!;
    public LauncherWindow()
    {
        Title = "Steam Console Launcher"; Width = 1440; Height = 900;
        MinWidth = 1000; MinHeight = 650; Background = Color("#0B1019"); Foreground = Brushes.White;
        FontFamily = new FontFamily("Segoe UI"); WindowState = WindowState.Maximized;
        var root = new Grid(); Content = root;
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var nav = new WrapPanel { Margin = new Thickness(36, 22, 36, 18) };
        root.Children.Add(nav);
        nav.Children.Add(new TextBlock { Text = "PLAY / STEAM", FontSize = 22, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 26, 0) });
        nav.Children.Add(MakeButton("Главная", () => { home = true; Render(); }));
        nav.Children.Add(MakeButton("Библиотека", () => { home = false; Render(); }));
        nav.Children.Add(MakeButton("Подключить библиотеку", Connect));
        nav.Children.Add(MakeButton("Обновить", async () => await Reload()));
        nav.Children.Add(MakeButton("Магазин", () => Open("https://store.steampowered.com/")));
        nav.Children.Add(MakeButton("Закрыть", Close));
        var body = new Grid { Margin = new Thickness(36, 0, 36, 0) }; Grid.SetRow(body, 1); root.Children.Add(body);
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
        body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Star) });
        var hero = new Grid { ClipToBounds = true, Background = Color("#151F30") }; body.Children.Add(hero); hero.Children.Add(backdrop);
        var shade = new Border { Background = new LinearGradientBrush(Colors.Transparent, System.Windows.Media.Color.FromRgb(11, 16, 25), new Point(1, 0), new Point(0, 0)) }; hero.Children.Add(shade);
        var heroText = new StackPanel { Margin = new Thickness(32), VerticalAlignment = VerticalAlignment.Bottom };
        hero.Children.Add(heroText);
        label.Text = "УСТАНОВЛЕННЫЕ ИГРЫ STEAM"; label.Foreground = Color("#71C7FF"); label.FontSize = 13; heroText.Children.Add(label);
        title.Text = "Ваша игровая коллекция"; title.FontSize = 38; title.FontWeight = FontWeights.SemiBold;
        title.TextWrapping = TextWrapping.Wrap; title.Margin = new Thickness(0, 10, 0, 18); heroText.Children.Add(title);
        var buttons = new WrapPanel(); heroText.Children.Add(buttons);
        play = MakeButton("Выберите игру", Launch, true); buttons.Children.Add(play);
        buttons.Children.Add(MakeButton("В магазине Steam", () => { if (list.SelectedItem is Game g) Open("https://store.steampowered.com/app/" + g.Id); }));
        var tools = new WrapPanel { Margin = new Thickness(0, 18, 0, 18) }; Grid.SetRow(tools, 1); body.Children.Add(tools);
        tools.Children.Add(new TextBlock { Text = "ВАША БИБЛИОТЕКА", FontSize = 18, Margin = new Thickness(0, 8, 24, 0) });
        search.Width = 260; search.FontSize = 17; search.Padding = new Thickness(12, 6, 12, 6); search.Background = Color("#182334"); search.Foreground = Brushes.White; search.BorderThickness = new Thickness(0); search.ToolTip = "Поиск по названию";
        search.TextChanged += (_, _) => Render(); tools.Children.Add(search);
        filter.Items.Add("Все"); filter.Items.Add("Установленные"); filter.Items.Add("Не установленные"); filter.SelectedIndex = 0; filter.FontSize = 16; filter.Width = 180; filter.Margin = new Thickness(12, 0, 0, 0);
        filter.SelectionChanged += (_, _) => Render(); tools.Children.Add(filter);
        list.Background = Brushes.Transparent; list.BorderThickness = new Thickness(0);
        list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        list.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        var panel = new FrameworkElementFactory(typeof(WrapPanel)); list.ItemsPanel = new ItemsPanelTemplate(panel);
        var card = new FrameworkElementFactory(typeof(StackPanel)); card.SetValue(FrameworkElement.WidthProperty, 190.0);
        var image = new FrameworkElementFactory(typeof(Image)); image.SetValue(FrameworkElement.HeightProperty, 240.0); image.SetValue(Image.StretchProperty, Stretch.UniformToFill); image.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("Id") { Converter = new CoverConverter() }); card.AppendChild(image);
        var name = new FrameworkElementFactory(typeof(TextBlock)); name.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Name")); name.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap); name.SetValue(TextBlock.ForegroundProperty, Brushes.White); name.SetValue(TextBlock.FontSizeProperty, 15.0); name.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 10, 8, 8)); card.AppendChild(name);
        list.ItemTemplate = new DataTemplate { VisualTree = card };
        var style = new Style(typeof(ListBoxItem));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(4))); style.Setters.Add(new Setter(Control.MarginProperty, new Thickness(0, 0, 14, 14))); style.Setters.Add(new Setter(Control.BackgroundProperty, Color("#182334")));
        var trigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true }; trigger.Setters.Add(new Setter(Control.BackgroundProperty, Color("#287EB0"))); style.Triggers.Add(trigger); list.ItemContainerStyle = style;
        Grid.SetRow(list, 2); body.Children.Add(list);
        list.SelectionChanged += (_, _) => Select(); list.MouseDoubleClick += (_, _) => Launch();
        list.KeyDown += (_, e) => { if (e.Key == Key.Enter) { Launch(); e.Handled = true; } };
        status.FontSize = 13; status.Foreground = Color("#9AADC2"); status.TextWrapping = TextWrapping.Wrap; status.Margin = new Thickness(36, 12, 36, 18); Grid.SetRow(status, 2); root.Children.Add(status);
        KeyDown += (_, e) => { if (e.Key == Key.F11) { WindowState = WindowState.Normal; WindowStyle = WindowStyle == WindowStyle.None ? WindowStyle.SingleBorderWindow : WindowStyle.None; WindowState = WindowState.Maximized; } };
        Loaded += async (_, _) => await Reload();
    }
    Button MakeButton(string text, Action action, bool primary = false)
    {
        var b = new Button { Content = text, Padding = new Thickness(18, 10, 18, 10), Margin = new Thickness(0, 0, 10, 0), FontSize = 15, Background = Color(primary ? "#70C8FF" : "#1D2B3E"), Foreground = primary ? Brushes.Black : Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
        var border = new FrameworkElementFactory(typeof(Border)); border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8)); border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) }); border.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter))); b.Template = new ControlTemplate(typeof(Button)) { VisualTree = border };
        b.Click += (_, _) => action(); return b;
    }
    void Render()
    {
        var selected = (list.SelectedItem as Game)?.Id;
        var shown = games.Where(g => (!home || g.Installed) && (filter.SelectedIndex != 1 || g.Installed) && (filter.SelectedIndex != 2 || !g.Installed) && g.Name.Contains(search.Text.Trim(), StringComparison.OrdinalIgnoreCase)).OrderBy(g => g.Name).ToList();
        list.ItemsSource = shown;
        list.SelectedItem = shown.FirstOrDefault(g => g.Id == selected) ?? shown.FirstOrDefault();
        Select();
    }
    void Select()
    {
        if (list.SelectedItem is not Game g) { title.Text = "Нет игр для выбранного фильтра"; play.IsEnabled = false; backdrop.Source = null; return; }
        play.IsEnabled = true; title.Text = g.Name; play.Content = g.Installed ? "Играть" : "Установить через Steam";
        label.Text = g.Installed ? "УСТАНОВЛЕНО · STEAM" : "В БИБЛИОТЕКЕ · STEAM";
        var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.UriSource = new Uri("https://cdn.akamai.steamstatic.com/steam/apps/" + g.Id + "/library_hero.jpg"); bitmap.EndInit(); backdrop.Source = bitmap;
    }
    void Launch() { if (list.SelectedItem is Game g) Open(g.Installed ? "steam://rungameid/" + g.Id : "steam://install/" + g.Id); }
    void Open(string uri) { try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); } catch { status.Text = "Не удалось открыть Steam или браузер."; } }
    async Task Reload()
    {
        try
        {
            var local = await Task.Run(Scan);
            List<Game> saved = new();
            if (File.Exists(Cache)) { try { saved = JsonSerializer.Deserialize<List<Game>>(await File.ReadAllTextAsync(Cache)) ?? new(); } catch (JsonException) { } }
            games = saved.Concat(local).GroupBy(g => g.Id).Select(group => new Game(group.Key, group.Last().Name, local.Any(g => g.Id == group.Key))).Where(g => g.Id != "228980").ToList();
            Render(); status.Text = $"В списке: {games.Count} · установлено: {games.Count(g => g.Installed)} · F11 — полный экран. " + (saved.Count == 0 ? "Для всей коллекции нажмите «Подключить библиотеку»." : "Импортированная коллекция из локального кеша; для синхронизации повторите подключение.");
        }
        catch { status.Text = "Не удалось прочитать локальную библиотеку. Проверьте доступ к папкам Steam."; }
    }
    void Connect()
    {
        var dialog = new Window { Owner = this, Title = "Импорт библиотеки Steam", Width = 520, Height = 410, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Color("#101A28"), Foreground = Brushes.White };
        var content = new StackPanel { Margin = new Thickness(24) }; dialog.Content = content;
        content.Children.Add(new TextBlock { Text = "Введите SteamID64 и личный Steam Web API key. Ключ отправляется только Steam, не сохраняется и не попадает в GitHub. Список игр должен быть доступен по настройкам приватности. Пароль Steam не нужен.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 16) });
        content.Children.Add(new TextBlock { Text = "SteamID64 (17 цифр)" }); var id = new TextBox { FontSize = 18, Margin = new Thickness(0, 4, 0, 12) }; content.Children.Add(id);
        content.Children.Add(new TextBlock { Text = "Steam Web API key" }); var key = new PasswordBox { FontSize = 18, Margin = new Thickness(0, 4, 0, 16) }; content.Children.Add(key);
        var message = new TextBlock { TextWrapping = TextWrapping.Wrap }; content.Children.Add(message);
        var import = MakeButton("Импортировать", () => { }); content.Children.Add(import);
        import.Click += async (_, _) =>
        {
            if (!Regex.IsMatch(id.Text.Trim(), "^[0-9]{17}$") || !Regex.IsMatch(key.Password.Trim(), "^[A-Fa-f0-9]{32}$")) { message.Text = "Проверьте SteamID64 и формат ключа."; return; }
            import.IsEnabled = false; message.Text = "Получение списка игр…";
            try
            {
                var url = "https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key=" + Uri.EscapeDataString(key.Password.Trim()) + "&steamid=" + id.Text.Trim() + "&include_appinfo=1&include_played_free_games=1";
                using var doc = JsonDocument.Parse(await Http.GetStringAsync(url)); var response = doc.RootElement.GetProperty("response");
                if (!response.TryGetProperty("game_count", out _)) { message.Text = "Steam не вернул список. Проверьте ключ и доступность «Данных об играх» в приватности Steam."; return; }
                var imported = new List<Game>();
                if (response.TryGetProperty("games", out var items)) foreach (var item in items.EnumerateArray()) imported.Add(new Game(item.GetProperty("appid").ToString(), item.TryGetProperty("name", out var n) ? n.GetString() ?? "Без названия" : "Без названия", false));
                Directory.CreateDirectory(Path.GetDirectoryName(Cache)!); await File.WriteAllTextAsync(Cache, JsonSerializer.Serialize(imported)); key.Clear(); home = false; await Reload(); dialog.Close();
            }
            catch { message.Text = "Импорт не удался. Проверьте сеть, SteamID и ключ. Существующая библиотека сохранена."; }
            finally { import.IsEnabled = true; }
        };
        dialog.ShowDialog();
    }
    static List<Game> Scan()
    {
        var steam = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.Combine(steam, "steamapps") };
        var file = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        if (File.Exists(file)) foreach (Match m in Regex.Matches(File.ReadAllText(file), "\"path\"\\s*\"([^\"]+)\"")) paths.Add(Path.Combine(m.Groups[1].Value.Replace(@"\\", @"\"), "steamapps"));
        var result = new List<Game>();
        foreach (var path in paths)
        {
            try
            {
                if (!Directory.Exists(path)) continue;
                foreach (var manifest in Directory.GetFiles(path, "appmanifest_*.acf"))
                {
                    try
                    {
                        var text = File.ReadAllText(manifest);
                        string Field(string name) => Regex.Match(text, "\"" + name + "\"\\s*\"([^\"]+)\"").Groups[1].Value;
                        if (uint.TryParse(Field("appid"), out var app) && int.TryParse(Field("StateFlags"), out var flags) && (flags & 4) != 0) result.Add(new Game(app.ToString(), Field("name"), true));
                    }
                    catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
            }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        return result;
    }
}
public class CoverConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => new BitmapImage(new Uri("https://cdn.akamai.steamstatic.com/steam/apps/" + value + "/library_600x900.jpg"));
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
}
