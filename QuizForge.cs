// QuizForge.cs - Викторина (Квиз) на C# (CLI + WinForms)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Threading;

namespace QuizForge
{
    public class Question
    {
        public string QuestionText { get; set; }
        public List<string> Options { get; set; }
        public int Correct { get; set; }
        public string Category { get; set; }
        public string Difficulty { get; set; }
    }

    public class Score
    {
        public string Player { get; set; }
        public int Points { get; set; }
        public int Correct { get; set; }
        public int Total { get; set; }
        public string Date { get; set; }
        public string Category { get; set; }
    }

    public class Forge
    {
        public List<Question> Questions { get; set; } = new List<Question>();
        public List<Score> Scores { get; set; } = new List<Score>();
        private const string QFile = "questions.json";
        private const string SFile = "scores.json";

        public void Load()
        {
            if (File.Exists(QFile))
            {
                try
                {
                    string json = File.ReadAllText(QFile);
                    var qs = JsonSerializer.Deserialize<List<Question>>(json);
                    if (qs != null) Questions = qs;
                }
                catch { }
            }
            if (!Questions.Any()) { Questions = DefaultQuestions(); SaveQuestions(); }
            if (File.Exists(SFile))
            {
                try
                {
                    string json = File.ReadAllText(SFile);
                    var scores = JsonSerializer.Deserialize<List<Score>>(json);
                    if (scores != null) Scores = scores;
                }
                catch { }
            }
        }

        public void SaveQuestions()
        {
            File.WriteAllText(QFile, JsonSerializer.Serialize(Questions, new JsonSerializerOptions { WriteIndented = true }));
        }

        public void SaveScores()
        {
            File.WriteAllText(SFile, JsonSerializer.Serialize(Scores, new JsonSerializerOptions { WriteIndented = true }));
        }

        private List<Question> DefaultQuestions()
        {
            return new List<Question>
            {
                new Question { QuestionText = "Столица Франции?", Options = new List<string>{"Лондон","Париж","Берлин","Мадрид"}, Correct = 1, Category = "geography", Difficulty = "easy" },
                new Question { QuestionText = "Сколько планет в Солнечной системе?", Options = new List<string>{"7","8","9","10"}, Correct = 1, Category = "science", Difficulty = "easy" },
                new Question { QuestionText = "Кто написал 'Войну и мир'?", Options = new List<string>{"Достоевский","Толстой","Чехов","Пушкин"}, Correct = 1, Category = "literature", Difficulty = "medium" }
            };
        }

        public List<string> GetCategories()
        {
            return Questions.Select(q => q.Category).Distinct().OrderBy(c => c).ToList();
        }

        public List<Question> GetQuestions(string category, int count)
        {
            var filtered = Questions.AsEnumerable();
            if (!string.IsNullOrEmpty(category)) filtered = filtered.Where(q => q.Category == category);
            var list = filtered.ToList();
            if (count > 0 && count < list.Count)
            {
                var rnd = new Random();
                return list.OrderBy(x => rnd.Next()).Take(count).ToList();
            }
            return list;
        }

        public (int correct, int total) RunQuiz(string category, int count, int timer, string player)
        {
            var qs = GetQuestions(category, count);
            if (!qs.Any()) { Console.WriteLine("❌ Нет вопросов в выбранной категории"); return (0, 0); }
            var rnd = new Random();
            var shuffled = qs.OrderBy(x => rnd.Next()).ToList();
            Console.WriteLine($"\n🎯 Начинаем викторину! Вопросов: {shuffled.Count}\n");
            int correct = 0;
            for (int i = 0; i < shuffled.Count; i++)
            {
                var q = shuffled[i];
                Console.WriteLine($"Вопрос {i+1}/{shuffled.Count} [{q.Category}] ({q.Difficulty}):");
                Console.WriteLine($"  {q.QuestionText}");
                for (int j = 0; j < q.Options.Count; j++)
                    Console.WriteLine($"  {j+1}. {q.Options[j]}");
                if (timer > 0) Console.WriteLine($"⏱️ У вас {timer} секунд!");
                int answer = -1;
                DateTime start = DateTime.Now;
                while (answer == -1)
                {
                    Console.Write("Ваш ответ (1-4): ");
                    string input = Console.ReadLine();
                    if (timer > 0 && (DateTime.Now - start).TotalSeconds > timer)
                    {
                        Console.WriteLine("⏰ Время вышло!");
                        break;
                    }
                    if (int.TryParse(input, out int ans) && ans >= 1 && ans <= q.Options.Count)
                        answer = ans - 1;
                    else
                        Console.WriteLine("Пожалуйста, введите число от 1 до " + q.Options.Count);
                }
                if (answer == q.Correct) { Console.WriteLine("✅ Правильно!"); correct++; }
                else if (answer != -1) Console.WriteLine($"❌ Неправильно! Правильный ответ: {q.Options[q.Correct]}");
                else Console.WriteLine($"Правильный ответ: {q.Options[q.Correct]}");
                Console.WriteLine();
            }
            Console.WriteLine($"🎯 Результат: {correct}/{shuffled.Count}");
            return (correct, shuffled.Count);
        }

        public void AddScore(string player, int score, int correct, int total, string category)
        {
            Scores.Add(new Score
            {
                Player = player,
                Points = score,
                Correct = correct,
                Total = total,
                Date = DateTime.Now.ToString("o"),
                Category = category ?? "all"
            });
            SaveScores();
        }

        public List<Score> GetLeaderboard(int limit)
        {
            return Scores.OrderByDescending(s => s.Points).Take(limit).ToList();
        }

        public void ExportCSV(string filepath)
        {
            using (var sw = new StreamWriter(filepath))
            {
                sw.WriteLine("Player,Score,Correct,Total,Date,Category");
                foreach (var s in Scores)
                    sw.WriteLine($"{s.Player},{s.Points},{s.Correct},{s.Total},{s.Date},{s.Category}");
            }
        }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--gui")
            {
                Application.EnableVisualStyles();
                Application.Run(new QuizForgeGUI());
                return;
            }
            var forge = new Forge();
            forge.Load();
            if (args.Length == 0) { InteractiveMode(forge); return; }
            try
            {
                string cmd = args[0];
                switch (cmd)
                {
                    case "start":
                        string category = null; int count = 10; int timer = 0; string player = "Player";
                        for (int i = 1; i < args.Length; i++)
                        {
                            if (args[i] == "--category") category = args[++i];
                            else if (args[i] == "--questions") count = int.Parse(args[++i]);
                            else if (args[i] == "--timer") timer = int.Parse(args[++i]);
                            else if (args[i] == "--player") player = args[++i];
                        }
                        var res = forge.RunQuiz(category, count, timer, player);
                        int score = res.correct * 10;
                        Console.WriteLine($"\n💯 Ваш счёт: {score} очков");
                        if (score > 0) { forge.AddScore(player, score, res.correct, res.total, category); Console.WriteLine("✅ Результат сохранён!"); }
                        break;
                    case "leaderboard":
                        int limit = 10;
                        for (int i = 1; i < args.Length; i++) if (args[i] == "--limit") limit = int.Parse(args[++i]);
                        var scores = forge.GetLeaderboard(limit);
                        if (!scores.Any()) { Console.WriteLine("Нет записей в таблице рекордов."); break; }
                        Console.WriteLine("🏆 ТАБЛИЦА РЕКОРДОВ");
                        Console.WriteLine($"{"#",-3} {"Игрок",-12} {"Счёт",-6} {"Правильные",-12} {"Дата",-20} {"Категория"}");
                        for (int i = 0; i < scores.Count; i++)
                        {
                            var s = scores[i];
                            Console.WriteLine($"{i+1,-3} {s.Player,-12} {s.Points,-6} {s.Correct}/{s.Total,-12} {s.Date.Substring(0,16),-20} {s.Category}");
                        }
                        break;
                    case "categories":
                        var cats = forge.GetCategories();
                        if (!cats.Any()) { Console.WriteLine("Нет категорий."); break; }
                        Console.WriteLine("📚 Доступные категории:");
                        foreach (var c in cats)
                        {
                            int cnt = forge.Questions.Count(q => q.Category == c);
                            Console.WriteLine($"  {c} ({cnt} вопросов)");
                        }
                        break;
                    case "export":
                        string output = null;
                        for (int i = 1; i < args.Length; i++) if (args[i] == "--output") output = args[++i];
                        if (output == null) { Console.WriteLine("Укажите --output"); return; }
                        forge.ExportCSV(output);
                        Console.WriteLine($"Экспортировано в {output}");
                        break;
                    default: InteractiveMode(forge); break;
                }
            }
            catch (Exception e) { Console.WriteLine($"Ошибка: {e.Message}"); }
        }

        static void InteractiveMode(Forge forge)
        {
            while (true)
            {
                Console.WriteLine("\n📝 QuizForge - Викторина (интерактивный)");
                Console.WriteLine("1. Начать викторину");
                Console.WriteLine("2. Таблица рекордов");
                Console.WriteLine("3. Список категорий");
                Console.WriteLine("4. Экспорт результатов");
                Console.WriteLine("0. Выход");
                Console.Write("Выберите действие: ");
                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "0": return;
                    case "1":
                        var cats = forge.GetCategories();
                        Console.WriteLine("Доступные категории: " + (cats.Any() ? string.Join(", ", cats) : "Нет категорий"));
                        Console.Write("Категория (Enter для всех): ");
                        string cat = Console.ReadLine();
                        if (!string.IsNullOrEmpty(cat) && !cats.Contains(cat)) { Console.WriteLine("Категория не найдена, будут вопросы из всех категорий"); cat = null; }
                        Console.Write("Количество вопросов (по умолчанию 10): ");
                        string cntStr = Console.ReadLine();
                        int count = string.IsNullOrEmpty(cntStr) ? 10 : int.Parse(cntStr);
                        Console.Write("Таймер (сек, Enter без таймера): ");
                        string timerStr = Console.ReadLine();
                        int timer = string.IsNullOrEmpty(timerStr) ? 0 : int.Parse(timerStr);
                        Console.Write("Ваше имя (по умолчанию Player): ");
                        string player = Console.ReadLine();
                        if (string.IsNullOrEmpty(player)) player = "Player";
                        var result = forge.RunQuiz(cat, count, timer, player);
                        int score = result.correct * 10;
                        Console.WriteLine($"\n💯 Ваш счёт: {score} очков");
                        if (score > 0) { forge.AddScore(player, score, result.correct, result.total, cat); Console.WriteLine("✅ Результат сохранён!"); }
                        break;
                    case "2":
                        var scores = forge.GetLeaderboard(10);
                        if (!scores.Any()) { Console.WriteLine("Нет записей в таблице рекордов."); break; }
                        Console.WriteLine("🏆 ТАБЛИЦА РЕКОРДОВ");
                        Console.WriteLine($"{"#",-3} {"Игрок",-12} {"Счёт",-6} {"Правильные",-12} {"Дата",-20} {"Категория"}");
                        for (int i = 0; i < scores.Count; i++)
                        {
                            var s = scores[i];
                            Console.WriteLine($"{i+1,-3} {s.Player,-12} {s.Points,-6} {s.Correct}/{s.Total,-12} {s.Date.Substring(0,16),-20} {s.Category}");
                        }
                        break;
                    case "3":
                        var categoryList = forge.GetCategories();
                        if (!categoryList.Any()) { Console.WriteLine("Нет категорий."); break; }
                        Console.WriteLine("📚 Доступные категории:");
                        foreach (var c in categoryList)
                        {
                            int cnt = forge.Questions.Count(q => q.Category == c);
                            Console.WriteLine($"  {c} ({cnt} вопросов)");
                        }
                        break;
                    case "4":
                        Console.Write("Имя файла (CSV): ");
                        string file = Console.ReadLine();
                        if (string.IsNullOrEmpty(file)) file = "scores.csv";
                        forge.ExportCSV(file);
                        Console.WriteLine($"Экспортировано в {file}");
                        break;
                    default: Console.WriteLine("Неверный выбор");
                }
            }
        }
    }

    // ========== GUI ==========
    public class QuizForgeGUI : Form
    {
        private Forge forge = new Forge();
        private ComboBox catBox;
        private TextBox countBox, timerBox, nameBox;
        private RichTextBox resultBox;
        private List<Question> currentQuestions;
        private int currentIdx, correctCount;

        public QuizForgeGUI()
        {
            forge.Load();
            Text = "📝 QuizForge - Викторина";
            Size = new System.Drawing.Size(700, 550);
            StartPosition = FormStartPosition.CenterScreen;
            ShowMenu();
        }

        void ShowMenu()
        {
            Controls.Clear();
            var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(20) };
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            var title = new Label { Text = "📝 QuizForge", Font = new System.Drawing.Font("Arial", 24, System.Drawing.FontStyle.Bold), TextAlign = System.Drawing.ContentAlignment.MiddleCenter };
            main.Controls.Add(title, 0, 0);
            var startBtn = new Button { Text = "🎯 Начать викторину", Font = new System.Drawing.Font("Arial", 14), Dock = DockStyle.Fill };
            startBtn.Click += (s, e) => ShowSettings();
            main.Controls.Add(startBtn, 0, 1);
            var lbBtn = new Button { Text = "🏆 Таблица рекордов", Font = new System.Drawing.Font("Arial", 14), Dock = DockStyle.Fill };
            lbBtn.Click += (s, e) => ShowLeaderboard();
            main.Controls.Add(lbBtn, 0, 2);
            var catBtn = new Button { Text = "📚 Список категорий", Font = new System.Drawing.Font("Arial", 14), Dock = DockStyle.Fill };
            catBtn.Click += (s, e) => ShowCategories();
            main.Controls.Add(catBtn, 0, 3);
            var expBtn = new Button { Text = "💾 Экспорт CSV", Font = new System.Drawing.Font("Arial", 14), Dock = DockStyle.Fill };
            expBtn.Click += (s, e) => ExportCSV();
            main.Controls.Add(expBtn, 0, 4);
            Controls.Add(main);
        }

        void ShowSettings()
        {
            Controls.Clear();
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(20) };
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label { Text = "Имя игрока:", AutoSize = true }, 0, 0);
            nameBox = new TextBox { Text = "Player" };
            panel.Controls.Add(nameBox, 1, 0);
            panel.Controls.Add(new Label { Text = "Категория:", AutoSize = true }, 0, 1);
            catBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            catBox.Items.Add("");
            foreach (var c in forge.GetCategories()) catBox.Items.Add(c);
            catBox.SelectedIndex = 0;
            panel.Controls.Add(catBox, 1, 1);
            panel.Controls.Add(new Label { Text = "Количество вопросов:", AutoSize = true }, 0, 2);
            countBox = new TextBox { Text = "10" };
            panel.Controls.Add(countBox, 1, 2);
            panel.Controls.Add(new Label { Text = "Таймер (сек, 0=нет):", AutoSize = true }, 0, 3);
            timerBox = new TextBox { Text = "0" };
            panel.Controls.Add(timerBox, 1, 3);
            var startBtn = new Button { Text = "▶️ Начать", Dock = DockStyle.Fill };
            startBtn.Click += (s, e) => StartQuiz();
            panel.Controls.Add(startBtn, 0, 4);
            panel.SetColumnSpan(startBtn, 2);
            var backBtn = new Button { Text = "🏠 В меню", Dock = DockStyle.Fill };
            backBtn.Click += (s, e) => ShowMenu();
            panel.Controls.Add(backBtn, 0, 5);
            panel.SetColumnSpan(backBtn, 2);
            Controls.Add(panel);
        }

        void StartQuiz()
        {
            string cat = catBox.SelectedItem.ToString();
            if (cat == "") cat = null;
            int count = int.Parse(countBox.Text);
            int timer = int.Parse(timerBox.Text);
            currentQuestions = forge.GetQuestions(cat, count);
            if (!currentQuestions.Any()) { MessageBox.Show("Нет вопросов в выбранной категории"); return; }
            var rnd = new Random();
            currentQuestions = currentQuestions.OrderBy(x => rnd.Next()).ToList();
            currentIdx = 0;
            correctCount = 0;
            ShowQuestion(timer);
        }

        void ShowQuestion(int timer)
        {
            if (currentIdx >= currentQuestions.Count) { ShowResults(); return; }
            Controls.Clear();
            var q = currentQuestions[currentIdx];
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 + q.Options.Count, Padding = new Padding(20) };
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            for (int i = 0; i < q.Options.Count; i++) panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            int row = 0;
            panel.Controls.Add(new Label { Text = $"Вопрос {currentIdx+1}/{currentQuestions.Count}", Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold) }, 0, row++);
            panel.Controls.Add(new Label { Text = $"[{q.Category}] {q.Difficulty}", Font = new System.Drawing.Font("Arial", 10) }, 0, row++);
            panel.Controls.Add(new Label { Text = q.QuestionText, Font = new System.Drawing.Font("Arial", 14), AutoSize = true }, 0, row++);
            var radios = new RadioButton[q.Options.Count];
            var group = new ButtonGroup();
            for (int i = 0; i < q.Options.Count; i++)
            {
                radios[i] = new RadioButton { Text = q.Options[i], AutoSize = true };
                panel.Controls.Add(radios[i], 0, row++);
            }
            if (timer > 0) panel.Controls.Add(new Label { Text = $"⏱️ {timer} секунд!", AutoSize = true }, 0, row++);
            var answerBtn = new Button { Text = "✅ Ответить", Dock = DockStyle.Fill };
            answerBtn.Click += (s, e) =>
            {
                int answer = -1;
                for (int i = 0; i < radios.Length; i++) if (radios[i].Checked) { answer = i; break; }
                if (answer == -1) { MessageBox.Show("Выберите вариант ответа"); return; }
                if (answer == q.Correct) { correctCount++; MessageBox.Show("✅ Правильно!"); }
                else MessageBox.Show($"❌ Неправильно! Правильный ответ: {q.Options[q.Correct]}");
                currentIdx++;
                ShowQuestion(timer);
            };
            panel.Controls.Add(answerBtn, 0, row++);
            Controls.Add(panel);
        }

        void ShowResults()
        {
            Controls.Clear();
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(20) };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            panel.Controls.Add(new Label { Text = "🎯 Результат!", Font = new System.Drawing.Font("Arial", 20, System.Drawing.FontStyle.Bold), TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 0, 0);
            panel.Controls.Add(new Label { Text = $"Правильных ответов: {correctCount}/{currentQuestions.Count}", Font = new System.Drawing.Font("Arial", 14), TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 0, 1);
            int score = correctCount * 10;
            panel.Controls.Add(new Label { Text = $"Счёт: {score} очков", Font = new System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Bold), TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 0, 2);
            if (score > 0)
            {
                string cat = catBox.SelectedItem.ToString();
                if (cat == "") cat = "all";
                forge.AddScore(nameBox.Text, score, correctCount, currentQuestions.Count, cat);
                panel.Controls.Add(new Label { Text = "✅ Результат сохранён!", Font = new System.Drawing.Font("Arial", 12), ForeColor = System.Drawing.Color.Green, TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 0, 3);
            }
            else
            {
                var backBtn = new Button { Text = "🏠 В меню", Dock = DockStyle.Fill };
                backBtn.Click += (s, e) => ShowMenu();
                panel.Controls.Add(backBtn, 0, 3);
            }
            Controls.Add(panel);
        }

        void ShowLeaderboard()
        {
            Controls.Clear();
            var scores = forge.GetLeaderboard(10);
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(10) };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 90));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 10));
            var area = new RichTextBox { ReadOnly = true };
            if (!scores.Any()) { area.Text = "Нет записей в таблице рекордов."; }
            else
            {
                area.AppendText("🏆 ТАБЛИЦА РЕКОРДОВ\n");
                area.AppendText($"{"#",-3} {"Игрок",-12} {"Счёт",-6} {"Правильные",-12} {"Дата",-20} {"Категория"}\n");
                for (int i = 0; i < scores.Count; i++)
                {
                    var s = scores[i];
                    area.AppendText($"{i+1,-3} {s.Player,-12} {s.Points,-6} {s.Correct}/{s.Total,-12} {s.Date.Substring(0,16),-20} {s.Category}\n");
                }
            }
            panel.Controls.Add(area, 0, 0);
            var backBtn = new Button { Text = "🏠 В меню", Dock = DockStyle.Fill };
            backBtn.Click += (s, e) => ShowMenu();
            panel.Controls.Add(backBtn, 0, 1);
            Controls.Add(panel);
        }

        void ShowCategories()
        {
            Controls.Clear();
            var cats = forge.GetCategories();
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(10) };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 90));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 10));
            var area = new RichTextBox { ReadOnly = true };
            if (!cats.Any()) { area.Text = "Нет категорий."; }
            else
            {
                area.AppendText("📚 Доступные категории:\n");
                foreach (var c in cats)
                {
                    int cnt = forge.Questions.Count(q => q.Category == c);
                    area.AppendText($"  {c} ({cnt} вопросов)\n");
                }
            }
            panel.Controls.Add(area, 0, 0);
            var backBtn = new Button { Text = "🏠 В меню", Dock = DockStyle.Fill };
            backBtn.Click += (s, e) => ShowMenu();
            panel.Controls.Add(backBtn, 0, 1);
            Controls.Add(panel);
        }

        void ExportCSV()
        {
            var sfd = new SaveFileDialog { Filter = "CSV files|*.csv", DefaultExt = "csv" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                forge.ExportCSV(sfd.FileName);
                MessageBox.Show("Экспортировано");
            }
        }
    }
}
