namespace SpaceInvader
{
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using Terminal.Gui;

    public class MainApp(LeaderBoardApi boardApi)
    {
        private readonly int width = 40, height = 20;
        private MainScreenState currentState = MainScreenState.Menu;

        // Menu state
        private int menuSelectedIndex = 0;
        private readonly string[] menuItems = { "Start", "Scores", "Settings", "Quit" };
        private Window? win;
        private Label? menuStatusLabel;
        private View? activeView;

        // Game state
        private GameView? gameView;
        private Label? status;
        private Thread? logicThread;
        private bool running;
        private readonly object locker = new();

        // For Scoreboard
        private static readonly ConcurrentBag<ScoreEntry> CachedScores = new();

        public void Init()
        {
            Application.Force16Colors = true;

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


        public void AskUserNameAndSaveScore()
        {
            var dialog = new Dialog
            {
                Title = "Enter Name",
                Width = 40,
                Height = 8
            };
            var label = new Label
            {
                Text = gameView.Won ? "Victory! Enter your name:" : "Game Over! Enter your name:",
                X = 2,
                Y = 1,
                Width = 30
            };
            var nameField = new TextField
            {
                X = 2,
                Y = 2,
                Width = 30,
                Text = ""
            };
            var okButton = new Button
            {
                Text = "OK",
                X = Pos.Center(),
                Y = 4,
                IsDefault = true
            };
            dialog.Add(label, nameField, okButton);
            okButton.Accepting += (_, _) =>
            {
                var userName = nameField.Text?.ToString()?.Trim() ?? "Player";
                boardApi.UpdateScore(userName, gameView.Score);
                dialog.RequestStop();
                // Ici, tu pourras ensuite naviguer vers le menu, nettoyer la vue, etc.
                // Mais pas AVANT la fermeture du dialog !
            };


            Application.Run(dialog);
        }

        // =========== MENU ============
        private void ShowMenu()
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

        private void MenuKeyHandler(object sender, Key key)
        {
            if (activeView is MenuListView menuView)
            {
                menuView.ProcessKey(key);
            }
            key.Handled = true;
        }

        // =========== GAME ============
        private void ShowGame()
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
            gameView = new GameView(width, height, boardApi)
            {
                X = 0,
                Y = 0,
            }
            ;
            win.Add(gameView);
            win.Add(status);

            // Your Init/game logic from your code
            InitGame();

            win.KeyDown += GameKeyHandler;
            activeView = gameView;
            gameView.SetFocus();
        }

        private void GameKeyHandler(object sender, Key e)
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
                if (e == Key.Esc)
                {
                    // Return to menu
                    running = false;
                    win.KeyDown -= GameKeyHandler;
                    AskUserNameAndSaveScore();
                    ShowMenu();
                }
            }
            e.Handled = true;
        }

        private void InitGame()
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
                    if (gameView.Lost || gameView.Won)
                    {
                        win.KeyDown -= GameKeyHandler;
                        Application.Invoke(AskUserNameAndSaveScore);
                        ShowMenu();
                    }
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

        // =========== SCORES ============
        private void ShowScores()
        {
            win.KeyDown -= MenuKeyHandler;
            currentState = MainScreenState.Scores;
            win!.RemoveAll();

            var scores = boardApi.GetScores();

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
        private void ScoresKeyHandler(object sender, Key key)
        {
            if (key == Key.Esc || key == Key.Enter)
            {
                win.KeyDown -= ScoresKeyHandler;
                ShowMenu();
            }
            key.Handled = true;
        }

        // =========== SETTINGS ============
        private void ShowSettings()
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
        private void SettingsKeyHandler(object sender, Key key)
        {
            if (key == Key.Esc || key == Key.Enter)
            {
                win.KeyDown -= SettingsKeyHandler;
                ShowMenu();
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
}
