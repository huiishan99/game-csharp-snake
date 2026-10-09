// Runs the shipped Form1 from SnakeGame.exe. No test state, score or artwork is injected.
// Deterministic gameplay invokes the existing key/timer handlers after a live timer check.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using SnakeGame;

internal static class WindowsSmoke
{
    static Form1 form;
    static string output;
    static readonly List<string> results = new List<string>();
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Field<T>(string name) { return (T)typeof(Form1).GetField(name, Private).GetValue(form); }
    static void Call(string name, params object[] args) { typeof(Form1).GetMethod(name, Private).Invoke(form, args); }
    static SnakeGameEngine Game { get { return Field<SnakeGameEngine>("game"); } }
    static void Check(bool value, string description) { if (!value) throw new Exception(description); results.Add("PASS: " + description); Console.WriteLine("PASS: " + description); }
    static void Pump(int ms) { var until = DateTime.UtcNow.AddMilliseconds(ms); do { Application.DoEvents(); Thread.Sleep(10); } while (DateTime.UtcNow < until); }
    static void Key(Keys key) { Call("OnKeyDown", new KeyEventArgs(key)); }
    static void Tick() { Call("timer1_Tick", null, EventArgs.Empty); }
    static void StopClock() { Field<System.Windows.Forms.Timer>("timer1").Stop(); }
    static void Capture(string name)
    {
        form.Activate(); form.BringToFront(); form.Refresh(); Pump(100);
        using (var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, form.ClientSize);
            bitmap.Save(Path.Combine(output, name + ".png"), ImageFormat.Png);
        }
    }
    static void Choose(string control, int index) { Field<ComboBox>(control).SelectedIndex = index; Pump(30); }
    static void Start()
    {
        Key(Keys.Enter);
        Check(Field<bool>("isCountdownActive"), "Enter starts countdown");
        Check(form.MinimumSize == form.MaximumSize, "Active run locks window size");
        Pump(2750); StopClock();
        Check(!Field<bool>("isCountdownActive") && Game.Status == GameStatus.Playing, "Real countdown timer reaches play");
    }
    static GridCell Next(GridCell p, Direction d)
    {
        int x = p.X, y = p.Y;
        if (d == Direction.Left) x--; if (d == Direction.Right) x++; if (d == Direction.Up) y--; if (d == Direction.Down) y++;
        if (Game.CurrentBoundaryMode == BoundaryMode.Wrap) { x = (x + Game.GridWidth) % Game.GridWidth; y = (y + Game.GridHeight) % Game.GridHeight; }
        return new GridCell(x, y);
    }
    static bool Opposite(Direction a, Direction b) { return (a == Direction.Up && b == Direction.Down) || (a == Direction.Down && b == Direction.Up) || (a == Direction.Left && b == Direction.Right) || (a == Direction.Right && b == Direction.Left); }
    static Direction? Route(GridCell target)
    {
        var queue = new Queue<GridCell>(); var first = new Dictionary<GridCell, Direction>();
        var blocked = new HashSet<GridCell>(Game.Obstacles);
        foreach (var cell in Game.Snake) blocked.Add(cell);
        var head = Game.Snake[0]; queue.Enqueue(head);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            foreach (Direction d in new[] { Direction.Right, Direction.Down, Direction.Left, Direction.Up })
            {
                if (p.Equals(head) && Opposite(Game.CurrentDirection, d)) continue;
                var n = Next(p, d);
                if (n.X < 0 || n.Y < 0 || n.X >= Game.GridWidth || n.Y >= Game.GridHeight || blocked.Contains(n) || first.ContainsKey(n)) continue;
                first[n] = p.Equals(head) ? d : first[p];
                if (n.Equals(target)) return first[n];
                queue.Enqueue(n);
            }
        }
        return null;
    }
    static void Move(Direction d, bool wasd)
    {
        Keys k = d == Direction.Right ? (wasd ? Keys.D : Keys.Right) : d == Direction.Left ? (wasd ? Keys.A : Keys.Left) : d == Direction.Up ? (wasd ? Keys.W : Keys.Up) : (wasd ? Keys.S : Keys.Down);
        Key(k); Tick();
    }
    static void EatTo(int score)
    {
        for (int i = 0; Game.Score < score && i < 3000; i++)
        {
            var d = Route(Game.Food); if (!d.HasValue) throw new Exception("No safe route to food");
            Move(d.Value, i % 2 == 0);
            if (Game.Status != GameStatus.Playing) throw new Exception("Unexpected collision during route");
        }
        Check(Game.Score >= score && Game.Snake.Count == Game.Score / 10 + 1, "Arrow/WASD handlers eat food, score and grow snake");
    }
    static string Layout() { var s = Game.Food.X + "," + Game.Food.Y; foreach (var c in Game.Obstacles) s += ";" + c.X + "," + c.Y; return s; }
    [STAThread]
    static int Main(string[] args)
    {
        output = Path.GetFullPath(args.Length > 0 ? args[0] : "artifacts/runtime"); Directory.CreateDirectory(output);
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            form = new Form1(); form.Show(); Pump(300);
            Check(form.Visible && Field<Panel>("pnlStartMenu").Visible, "Real WinForms app opens start menu");
            Field<CheckBox>("chkSound").Checked = false;
            Capture("01-menu-classic");
            for (int i = 0; i < 5; i++)
            {
                Choose("cmbMode", i); var preset = GamePresets.GetMode((GameModePreset)i);
                Check(Field<int>("selectedSpeed") == preset.Speed && Field<CheckBox>("chkWrapWalls").Checked == preset.WrapWalls && Field<CheckBox>("chkObstacles").Checked == preset.Obstacles && Field<CheckBox>("chkProgressiveSpeed").Checked == preset.ProgressiveSpeed, "Mode preset " + preset.Name);
            }
            for (int i = 0; i < 3; i++) { Choose("cmbBoardSize", i); var b = GamePresets.GetBoardSize((BoardSizePreset)i); Check(form.ClientSize.Width == b.GridWidth * 16 && form.ClientSize.Height == b.GridHeight * 16 + 44, "Board dimensions " + b.DisplayName); }
            Choose("cmbBoardSize", 1); Choose("cmbMode", 2); Choose("cmbTheme", 1);
            Field<TextBox>("txtChallengeSeed").Text = "SHAN-VERSE";
            Capture("02-menu-neon-maze");
            Start();
            var layout = Layout();
            var before = Game.Snake[0]; Field<System.Windows.Forms.Timer>("timer1").Start(); Pump(220); StopClock();
            Check(!Game.Snake[0].Equals(before), "Live WinForms timer moves snake");
            Check(Game.Obstacles.Count > 0 && Game.CurrentBoundaryMode == BoundaryMode.SolidWalls, "Maze starts with obstacles and solid walls");
            EatTo(120); Capture("03-gameplay-neon-maze");
            Key(Keys.Space); Check(Game.Status == GameStatus.Paused, "Space pauses");
            before = Game.Snake[0]; Tick(); Check(Game.Snake[0].Equals(before), "Paused gameplay does not advance"); Capture("04-paused-neon-maze");
            Key(Keys.Space); Check(Field<bool>("isCountdownActive"), "Space resume starts countdown"); Pump(1400); StopClock(); Check(Game.Status == GameStatus.Playing && !Field<bool>("isCountdownActive"), "Resume countdown completes");
            // Continue straight until a genuine wall/body/obstacle collision finishes this scoring run.
            for (int i = 0; i < 100 && !Game.IsFinished; i++) Tick();
            Check(Game.Status == GameStatus.GameOver && Field<Panel>("pnlStartMenu").Visible, "Collision returns to restart menu");
            Check(form.MinimumSize != form.MaximumSize, "Finished run unlocks window size");
            Check(Field<Label>("lblLeaderboard").Text.Contains("120"), "Scoring run appears in local mode leaderboard");
            Capture("05-result-neon-maze");
            Start(); Check(Layout() == layout, "Same challenge seed reproduces food and obstacle layout");
            for (int i = 0; i < 100 && !Game.IsFinished; i++) Tick();
            Choose("cmbTheme", 2); Capture("06-menu-handheld"); Choose("cmbTheme", 3); Capture("07-menu-soft");
            int speed = Field<int>("selectedSpeed"); Key(Keys.Right); Check(Field<int>("selectedSpeed") == speed + 1, "Menu Right increases speed"); Key(Keys.Left); Check(Field<int>("selectedSpeed") == speed, "Menu Left decreases speed");
            Call("SavePlayerSettings");
            form.Close(); form.Dispose();
            // Reload from disk rather than reusing the in-memory settings cache.
            var settings = typeof(Form1).Assembly.GetType("SnakeGame.Properties.Settings");
            var settingsObject = settings.GetProperty("Default", BindingFlags.Static | BindingFlags.Public).GetValue(null, null);
            ((System.Configuration.ApplicationSettingsBase)settingsObject).Reload();
            form = new Form1(); form.Show(); Pump(100);
            Check(Field<ComboBox>("cmbMode").SelectedIndex == 2 && Field<ComboBox>("cmbTheme").SelectedIndex == 3 && Field<ComboBox>("cmbBoardSize").SelectedIndex == 1 && Field<TextBox>("txtChallengeSeed").Text == "SHAN-VERSE" && !Field<CheckBox>("chkSound").Checked && Field<int>("highScore") >= 120 && Field<Label>("lblLeaderboard").Text.Contains("120"), "Settings, seed, best score and leaderboard reload from disk");
            results.Add("LIMIT: automated keyboard-handler/timer-handler integration, not human play or OS keyboard delivery; audio quality and high-DPI displays not tested.");
            results.Add("CAPTURE: CopyFromScreen of real visible Form1 loaded from the Release SnakeGame.exe; no generated artwork or injected game states.");
            return 0;
        }
        catch (Exception e) { results.Add("FAIL: " + e); Console.Error.WriteLine(e); if (form != null && !form.IsDisposed) Capture("failure"); return 1; }
        finally { File.WriteAllLines(Path.Combine(output, "smoke-results.txt"), results); if (form != null) form.Dispose(); }
    }
}
