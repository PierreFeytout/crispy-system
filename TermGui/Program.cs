using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

public class ScoreEntry
{
    public string UserName { get; set; }
    public int Score { get; set; }
}

// Enum for screen states
internal enum MainScreenState
{
    Menu,
    Game,
    Scores,
    Settings
}

internal class Program
{
    private static readonly int width = 40, height = 20;
    private static MainScreenState currentState = MainScreenState.Menu;

    // Menu state
    private static int menuSelectedIndex = 0;
    private static readonly string[] menuItems = { "Start", "Scores", "Settings", "Quit" };
    private static Window? win;
    private static Label? menuStatusLabel;
    private static View? activeView;

    // Game state
    private static GameView? gameView;
    private static Label? status;
    private static Thread? logicThread;
    private static bool running;
    private static readonly object locker = new();

    // For Scoreboard
    private static readonly ConcurrentBag<ScoreEntry> CachedScores = new();

    public static void Main()
    {
        var colorSchemes = new ColorScheme().GetHighlightColorScheme();

        var top = new Toplevel
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = new ColorScheme(colorSchemes.Normal),
        };
        Application.Init();

        win = new Window
        {
            X = 0,
            Y = 0,
            Width = width + 2,
            Height = height + 6,
            Title = "Space Invader",
            ColorScheme = new ColorScheme(colorSchemes.Normal),
        };
        top.Add(win);

        ShowMenu();

        Application.Run(top);
        Application.Shutdown();
    }

    // =========== MENU ============
    private static void ShowMenu()
    {
        win.KeyDown -= ScoresKeyHandler;
        win.KeyDown -= GameKeyHandler;
        win.KeyDown -= SettingsKeyHandler;
        currentState = MainScreenState.Menu;
        win!.RemoveAll();
        menuStatusLabel = new Label
        {
            X = 0,
            Y = height + 1,
            Width = width + 2,
            Height = 1,
            Text = "Use ↑↓ and Enter. ESC to quit."
        };
        var menuView = new MenuListView(menuItems)
        {
            X = 0,
            Y = 3,
            Width = width + 2,
            Height = 8,
        };
        menuView.SelectedIndex = menuSelectedIndex;

        menuView.OnActivated += idx =>
        {
            menuSelectedIndex = idx;
            switch (idx)
            {
                case 0:
                    ShowGame();
                    break;
                case 1:
                    ShowScores();
                    break;
                case 2:
                    ShowSettings();
                    break;
                case 3:
                    Application.RequestStop();
                    return;
            }
        };
        activeView = menuView;
        win.Add(menuView);
        win.Add(menuStatusLabel);

        win.KeyDown += MenuKeyHandler;
        menuView.SetFocus();
    }

    private static void MenuKeyHandler(object sender, Key key)
    {
        if (activeView is MenuListView menuView)
        {
            menuView.ProcessKey(key);
        }
        key.Handled = true;
    }

    // =========== GAME ============
    private static void ShowGame()
    {
        win.KeyDown -= MenuKeyHandler;
        currentState = MainScreenState.Game;
        win!.RemoveAll();
        status = new Label
        {
            X = 0,
            Y = height + 1,
            Width = width + 2,
            Height = 1
        };
        gameView = new GameView(width, height)
        {
            X = 0,
            Y = 0,
        };
        win.Add(gameView);
        win.Add(status);

        // Your Init/game logic from your code
        InitGame();

        win.KeyDown += GameKeyHandler;
        activeView = gameView;
        gameView.SetFocus();
    }

    private static void GameKeyHandler(object sender, Key e)
    {
        if (gameView == null)
            return;

        lock (locker)
        {
            if (e == Key.R)
            {
                InitGame();
            }
            if (e == Key.CursorLeft && gameView.PlayerX > 1)
                gameView.PlayerX--;
            if (e == Key.CursorRight && gameView.PlayerX < width - 2)
                gameView.PlayerX++;
            if (e == Key.A)
                gameView.Fire();
            if (e == Key.Esc && (gameView.Won || gameView.Lost))
            {
                // Return to menu
                running = false;
                win.KeyDown -= GameKeyHandler;
                ShowMenu();
                return;
            }
        }
        e.Handled = true;
    }

    private static void InitGame()
    {
        if (logicThread != null && logicThread.IsAlive)
        {
            running = false;
            logicThread.Join();
        }
        gameView!.Init(); // Call your own method to initialize gameView state

        running = true;
        logicThread = new Thread(() =>
        {
            var moveInterval = TimeSpan.FromMilliseconds(200);
            var lastMove = Stopwatch.GetTimestamp();
            var drawInterval = TimeSpan.FromMilliseconds(33);
            var lastDraw = Stopwatch.GetTimestamp();

            while (running)
            {
                var now = Stopwatch.GetTimestamp();

                if (Stopwatch.GetElapsedTime(lastMove, now) >= moveInterval && !gameView.Lost && !gameView.Won)
                {
                    lock (locker)
                    {
                        gameView.UpdateLogic();
                    }
                    lastMove = now;
                }
                if (Stopwatch.GetElapsedTime(lastDraw, now) >= drawInterval)
                {
                    Application.Invoke(() =>
                    {
                        lock (locker)
                        {
                            gameView.RedrawGame();

                            if (gameView.Won)
                                status!.Text = "You win! Press ESC to return to menu or R to restart.";
                            else if (gameView.Lost)
                                status!.Text = "You lose! Press ESC to return to menu or R to restart.";
                            else
                                status!.Text = "←/→ to move, A to shoot and R to restart";
                        }
                    });
                    lastDraw = now;
                }
                Thread.Sleep(5);
            }
        });
        logicThread.Start();
    }

    private static async Task<IEnumerable<ScoreEntry>> GetScores()
    {

        try
        {
            using HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://localhost:7164");
            var result = await client.GetAsync("scores");

            var scores = await result.Content.ReadFromJsonAsync<IEnumerable<ScoreEntry>>();

            return scores ?? [];
        }
        catch
        {
            return [];
        }
    }

    // =========== SCORES ============
    private static void ShowScores()
    {
        win.KeyDown -= MenuKeyHandler;
        currentState = MainScreenState.Scores;
        win!.RemoveAll();

        var task = Task.Factory.StartNew(GetScores);
        task.Wait();

        var scores = task.Result.Result;


        var view = new ScoresView(scores.ToList())
        {
            X = 0,
            Y = 2,
            Width = width + 2,
            Height = height
        };
        win.Add(view);
        var label = new Label
        {
            X = 0,
            Y = height + 2,
            Width = width + 2,
            Height = 1,
            Text = "Press ESC or Enter to return to menu."
        };
        win.Add(label);
        activeView = view;

        win.KeyDown += ScoresKeyHandler;
        view.SetFocus();
    }
    private static void ScoresKeyHandler(object sender, Key key)
    {
        if (key == Key.Esc || key == Key.Enter)
        {
            win.KeyDown -= ScoresKeyHandler;
            ShowMenu();
            return;
        }
        key.Handled = true;
    }

    // =========== SETTINGS ============
    private static void ShowSettings()
    {
        win.KeyDown -= MenuKeyHandler;
        currentState = MainScreenState.Settings;
        win!.RemoveAll();
        var label = new Label
        {
            X = 2,
            Y = 4,
            Width = width,
            Height = 2,
            Text = "Settings not implemented yet.\nPress ESC or Enter to return to menu."
        };
        win.Add(label);
        activeView = label;

        win.KeyDown += SettingsKeyHandler;
    }
    private static void SettingsKeyHandler(object sender, Key key)
    {
        if (key == Key.Esc || key == Key.Enter)
        {
            win.KeyDown -= SettingsKeyHandler;
            ShowMenu();
            return;
        }
        key.Handled = true;
    }
}

// ========== MenuListView (custom control for menu) ==========
internal class MenuListView : View
{
    private readonly string[] items;
    public int SelectedIndex { get; set; }
    public Action<int>? OnActivated;

    public MenuListView(string[] items)
    {
        this.items = items;
        CanFocus = true;
        Width = Dim.Fill();
        Height = items.Length + 2;
    }

    public void ProcessKey(Key key)
    {
        if (key == Key.CursorUp)
        {
            SelectedIndex = (SelectedIndex + items.Length - 1) % items.Length;
            SetNeedsDraw();
        }
        else if (key == Key.CursorDown)
        {
            SelectedIndex = (SelectedIndex + 1) % items.Length;
            SetNeedsDraw();
        }
        else if (key == Key.Enter)
        {
            OnActivated?.Invoke(SelectedIndex);
        }
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        for (int i = 0; i < items.Length; i++)
        {
            Move(2, i + 1);
            if (i == SelectedIndex)
            {
                Driver.SetAttribute(new Attribute(Color.BrightYellow, Color.Black));
                foreach (char c in "> " + items[i])
                    AddRune(c);
            }
            else
            {
                Driver.SetAttribute(new Attribute(Color.Gray, Color.Black));
                foreach (char c in "  " + items[i])
                    AddRune(c);
            }
        }
        return false;
    }
}

// ========== ScoresView (display dummy scores) ==========
internal class ScoresView : View
{
    private readonly List<ScoreEntry> scores;
    public ScoresView(List<ScoreEntry> scores)
    {
        this.scores = scores;
        CanFocus = true;
        Width = Dim.Fill();
        Height = scores.Count + 3;
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        string title = "HIGH SCORES";
        Move(2, 0);
        Driver.SetAttribute(new Attribute(Color.BrightCyan, Color.Black));
        foreach (char c in title)
            AddRune(c);

        for (int i = 0; i < scores.Count; i++)
        {
            Move(4, i + 2);
            Driver.SetAttribute(ColorScheme.Normal);
            var line = $"{i + 1,2}. {scores[i].UserName,-10} {scores[i].Score}";
            foreach (var ch in line)
                AddRune(ch);
        }
        return false;
    }
}
