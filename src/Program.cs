using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace ConsoleLauncher;

public record Game(string Id, string Name);

public class Program : Application
{
    [STAThread]
    public static void Main() => new Program().Run(new LauncherWindow());
}

public class LauncherWindow : Window
{
    readonly ListBox library = new();
    readonly TextBlock title = new();
    readonly TextBlock status = new();
    readonly TextBox search = new();
    List<Game> games = new();
    static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(75, 177, 245));

    public LauncherWindow()
    {
        Title = "Steam Console Launcher — прототип";
        Width = 1280; Height = 800; MinWidth = 900; MinHeight = 600;
        WindowState = WindowState.Maximized;
        Background = new SolidColorBrush(Color.FromRgb(16, 22, 32));
        Foreground = Brushes.White;
        FontFamily = new FontFamily("Segoe UI");
        var root = new DockPanel { Margin = new Thickness(40) };
        Content = root;
        var top = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 24) };
        DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);
        top.Children.Add(new TextBlock { Text = "Библиотека Steam", FontSize = 30, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        top.Children.Add(Button("Обновить", async () => await Reload()));
        top.Children.Add(Button("Магазин Steam", () => Open("https://store.steampowered.com/")));
        top.Children.Add(Button("Полный экран · F11", ToggleFullscreen));
        status.FontSize = 14; status.Foreground = Brushes.LightGray;
        status.Margin = new Thickness(0, 18, 0, 0);
        DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);
        var hero = new StackPanel { Margin = new Thickness(0, 0, 0, 24) };
        DockPanel.SetDock(hero, Dock.Top); root.Children.Add(hero);
        hero.Children.Add(new TextBlock { Text = "ВАША ИГРОВАЯ КОЛЛЕКЦИЯ", Foreground = Accent, FontSize = 14 });
        title.Text = "Выберите игру"; title.FontSize = 42;
        title.Margin = new Thickness(0, 12, 0, 12); title.TextWrapping = TextWrapping.Wrap;
        hero.Children.Add(title);
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(Button("Играть", Launch));
        actions.Children.Add(Button("Страница игры в Steam", () => { if (library.SelectedItem is Game g) Open("https://store.steampowered.com/app/" + g.Id + "/"); }));
        hero.Children.Add(actions);
        search.Margin = new Thickness(0, 18, 0, 0); search.FontSize = 20;
        search.Padding = new Thickness(12); search.ToolTip = "Поиск установленных игр";
        search.TextChanged += (_, _) => Filter(); hero.Children.Add(search);
        library.Background = Brushes.Transparent; library.BorderThickness = new Thickness(0);
        library.Foreground = Brushes.White; library.FontSize = 24;
        library.DisplayMemberPath = "Name";
        var itemStyle = new Style(typeof(ListBoxItem));
        itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(18)));
        itemStyle.Setters.Add(new Setter(Control.MarginProperty, new Thickness(0, 4, 8, 4)));
        itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        library.ItemContainerStyle = itemStyle;
        library.SelectionChanged += (_, _) => title.Text = (library.SelectedItem as Game)?.Name ?? "Выберите игру";
        library.MouseDoubleClick += (_, _) => Launch();
        library.KeyDown += (_, e) => { if (e.Key == Key.Enter) { Launch(); e.Handled = true; } };
        root.Children.Add(library);
        KeyDown += (_, e) => { if (e.Key == Key.F11) ToggleFullscreen(); if (e.Key == Key.Escape && WindowStyle == WindowStyle.None) ToggleFullscreen(); };
        Loaded += async (_, _) => await Reload();
    }

    Button Button(string text, Action action)
    {
        var b = new Button { Content = text, FontSize = 16, Padding = new Thickness(18, 10, 18, 10), Margin = new Thickness(12, 0, 0, 0), Background = Accent, Foreground = Brushes.Black };
        b.Click += (_, _) => action(); return b;
    }

    void ToggleFullscreen()
    {
        if (WindowStyle == WindowStyle.None) { WindowStyle = WindowStyle.SingleBorderWindow; WindowState = WindowState.Normal; }
        else { WindowState = WindowState.Normal; WindowStyle = WindowStyle.None; WindowState = WindowState.Maximized; }
    }

    async Task Reload()
    {
        status.Text = "Поиск установленных игр Steam…";
        try
        {
            games = await Task.Run(Scan);
            Filter();
            status.Text = games.Count == 0 ? "Игры не найдены. Установите Steam и игры, затем нажмите «Обновить». Это не импорт всей библиотеки аккаунта." : $"Найдено установленных приложений Steam: {games.Count}. Enter — запуск · F11 — полный экран. Пароль Steam не требуется.";
        }
        catch (Exception ex) { status.Text = "Не удалось прочитать библиотеку: " + ex.Message; }
    }

    void Filter()
    {
        library.ItemsSource = games.Where(g => g.Name.Contains(search.Text.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (library.Items.Count > 0) library.SelectedIndex = 0;
    }

    void Launch() { if (library.SelectedItem is Game g) Open("steam://rungameid/" + g.Id); }
    void Open(string uri)
    {
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
        catch (Exception ex) { status.Text = "Не удалось открыть Steam или браузер: " + ex.Message; }
    }

    static List<Game> Scan()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var steam = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
        if (string.IsNullOrWhiteSpace(steam)) steam = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        paths.Add(Path.Combine(steam, "steamapps"));
        var libraries = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        if (File.Exists(libraries))
        {
            foreach (Match m in Regex.Matches(File.ReadAllText(libraries), "\"path\"\\s*\"([^\"]+)\""))
                paths.Add(Path.Combine(m.Groups[1].Value.Replace(@"\\", @"\"), "steamapps"));
        }
        var result = new Dictionary<string, Game>();
        foreach (var path in paths)
        {
            if (!Directory.Exists(path)) continue;
            string[] manifests;
            try { manifests = Directory.GetFiles(path, "appmanifest_*.acf"); } catch (IOException) { continue; } catch (UnauthorizedAccessException) { continue; }
            foreach (var manifest in manifests)
            {
                try
                {
                    var text = File.ReadAllText(manifest);
                    var id = Regex.Match(text, "\"appid\"\\s*\"(\\d+)\"").Groups[1].Value;
                    var name = Regex.Match(text, "\"name\"\\s*\"([^\"]+)\"").Groups[1].Value;
                    var flags = Regex.Match(text, "\"StateFlags\"\\s*\"(\\d+)\"").Groups[1].Value;
                    if (id.Length > 0 && name.Length > 0 && int.TryParse(flags, out var state) && (state & 4) != 0)
                        result[id] = new Game(id, name);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        return result.Values.OrderBy(g => g.Name).ToList();
    }
}
