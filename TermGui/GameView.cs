using System.Diagnostics;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

internal class GameView : View
{
    public long Score { get; private set; }
    public int GameWidth { get; }
    public int GameHeight { get; }
    public int PlayerX { get; set; }
    public int PlayerY { get; set; }
    public List<(int x, int y)> Enemies { get; } = new();
    public List<(int x, int y)> Shots { get; } = new();
    public bool Won { get; set; }
    public bool Lost { get; set; }

    private int enemyDir = 1;

    private static readonly ColorScheme FlickerScheme = new ColorScheme
    {
        Normal = new Attribute(Color.White, Color.BrightRed),
        Focus = new Attribute(Color.BrightYellow, Color.Red),
        HotNormal = new Attribute(Color.BrightYellow, Color.BrightRed),
        HotFocus = new Attribute(Color.BrightYellow, Color.BrightRed)
    };

    private bool IsDanger(List<(int x, int y)> enemies)
    {
        int threshold = GameHeight - 4; // e.g. last 3 rows
        return enemies.Any(e => e.y >= threshold);
    }

    private readonly Label ScoreLabel;

    private long LastShot = Stopwatch.GetTimestamp();

    private readonly TimeSpan DefaultShotDelay = TimeSpan.FromMilliseconds(500);

    private void SetScore()
    {
        ScoreLabel.Text = $"Score: {Score}";
    }

    public void Fire()
    {
        var now = Stopwatch.GetTimestamp();
        if (Stopwatch.GetElapsedTime(LastShot, now) >= DefaultShotDelay)
        {
            Shots.Add((PlayerX, PlayerY - 1));
            LastShot = now;
        }
    }

    public GameView(int width, int height)
    {
        ScoreLabel = new Label();
        ScoreLabel.Width = width;
        ScoreLabel.Height = 2;


        GameWidth = width;
        GameHeight = height;
        X = 0;
        Y = 0;
        Width = width;
        Height = height;
        CanFocus = true;

        Add(ScoreLabel);
        SetScore();
    }

    public void Init()
    {
        Won = false;
        Lost = false;
        PlayerX = GameWidth / 2;
        PlayerY = GameHeight - 2;
        Enemies.Clear();
        for (int y = 2; y < 5; y++)
            for (int x = 6; x < GameWidth - 6; x += 4)
                Enemies.Add((x, y));
        Shots.Clear();
    }

    public void UpdateLogic()
    {
        // Move enemies
        bool needToDrop = false;
        for (int i = 0; i < Enemies.Count; i++)
        {
            var (x, y) = Enemies[i];
            int nx = x + enemyDir;
            if (nx < 1 || nx > GameWidth - 2)
            {
                needToDrop = true;
                enemyDir *= -1;
            }
        }
        for (int i = 0; i < Enemies.Count; i++)
        {
            var (x, y) = Enemies[i];
            if (needToDrop)
            {
                y += 1;
                if (y >= PlayerY)
                    Lost = true;
            }
            else
            {
                x += enemyDir;
            }
            Enemies[i] = (x, y);
        }

        // Move shots
        for (int i = 0; i < Shots.Count; i++)
        {
            var (x, y) = Shots[i];
            y -= 1;
            Shots[i] = (x, y);
        }
        Shots.RemoveAll(s => s.y < 1);

        // Detect collisions
        foreach (var shot in Shots.ToArray())
        {
            int hitIdx = Enemies.FindIndex(e => Math.Abs(e.x - shot.x) < 1 && e.y == shot.y);
            if (hitIdx >= 0)
            {
                Score += 20;
                Enemies.RemoveAt(hitIdx);
                Shots.Remove(shot);
                SetScore();
            }
        }

        // Win condition
        if (Enemies.Count == 0)
            Won = true;
    }

    public void RedrawGame() => SetNeedsDraw();

    protected override bool OnDrawingContent(DrawContext? context)
    {
        // Draw borders
        for (int x = 0; x < GameWidth; x++)
        {
            Move(x, 0);
            AddRune('-');
            Move(x, GameHeight - 1);
            AddRune('-');
        }
        for (int y = 1; y < GameHeight - 1; y++)
        {
            Move(0, y);
            AddRune('|');
            Move(GameWidth - 1, y);
            AddRune('|');
        }

        // Draw player
        Move(PlayerX, PlayerY);
        AddRune('A');

        // Draw enemies
        foreach (var (x, y) in Enemies)
        {
            Move(x, y);
            AddRune('W');
        }
        // Draw shots
        foreach (var (x, y) in Shots)
        {
            Move(x, y);
            AddRune('|');
        }

        return false;
    }

    private static void AskUserNameAndSaveScore(int score, bool won)
    {
        var dialog = new Dialog
        {
            Title = "Enter Name",
            Width = 40,
            Height = 8
        };
        var label = new Label
        {
            Text = won ? "Victory! Enter your name:" : "Game Over! Enter your name:",
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
            // Save to your scoreboard here (mock implementation below)
            scoreEntries.Insert(0, new ScoreEntry { UserName = userName, Score = score }); // Add on top
            Application.RequestStop(); // Close dialog
            ShowScores(); // Or ShowMenu(), as you wish
        };

        Application.Run(dialog);
    }
}