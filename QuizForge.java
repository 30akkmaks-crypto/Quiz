// QuizForge.java - Викторина (Квиз) на Java (CLI + Swing GUI)
import javax.swing.*;
import java.awt.*;
import java.awt.event.*;
import java.io.*;
import java.nio.file.*;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.*;
import java.util.List;
import java.util.stream.Collectors;

public class QuizForge {
    private static final String Q_FILE = "questions.json";
    private static final String S_FILE = "scores.json";

    static class Question {
        String question; List<String> options; int correct; String category; String difficulty;
        Question(String q, List<String> opts, int c, String cat, String diff) {
            question = q; options = opts; correct = c; category = cat; difficulty = diff;
        }
    }

    static class Score {
        String player; int score; int correct; int total; String date; String category;
        Score(String p, int s, int c, int t, String cat) {
            player = p; score = s; correct = c; total = t; date = LocalDateTime.now().toString();
            category = cat;
        }
    }

    static class Forge {
        List<Question> questions = new ArrayList<>();
        List<Score> scores = new ArrayList<>();

        void load() {
            // Упрощённо: в реальном проекте использовать Jackson
            // Для этой версии оставляем заглушку и загружаем дефолтные
            if (questions.isEmpty()) {
                questions = defaultQuestions();
                saveQuestions();
            }
            // Загрузка счётов
            try {
                String json = new String(Files.readAllBytes(Paths.get(S_FILE)));
                // Упрощённо, оставляем пустым
            } catch (Exception e) {}
        }

        void saveQuestions() {
            try (PrintWriter pw = new PrintWriter(Q_FILE)) {
                pw.println("[");
                for (int i = 0; i < questions.size(); i++) {
                    Question q = questions.get(i);
                    pw.printf("  {\"question\":\"%s\",\"options\":[", q.question);
                    for (int j = 0; j < q.options.size(); j++) {
                        pw.printf("\"%s\"%s", q.options.get(j), (j < q.options.size()-1 ? "," : ""));
                    }
                    pw.printf("],\"correct\":%d,\"category\":\"%s\",\"difficulty\":\"%s\"}%s\n",
                            q.correct, q.category, q.difficulty, (i < questions.size()-1 ? "," : ""));
                }
                pw.println("]");
            } catch (IOException e) {}
        }

        void saveScores() {
            try (PrintWriter pw = new PrintWriter(S_FILE)) {
                pw.println("[");
                for (int i = 0; i < scores.size(); i++) {
                    Score s = scores.get(i);
                    pw.printf("  {\"player\":\"%s\",\"score\":%d,\"correct\":%d,\"total\":%d,\"date\":\"%s\",\"category\":\"%s\"}%s\n",
                            s.player, s.score, s.correct, s.total, s.date, s.category, (i < scores.size()-1 ? "," : ""));
                }
                pw.println("]");
            } catch (IOException e) {}
        }

        List<Question> defaultQuestions() {
            List<Question> list = new ArrayList<>();
            list.add(new Question("Столица Франции?",
                    Arrays.asList("Лондон", "Париж", "Берлин", "Мадрид"), 1, "geography", "easy"));
            list.add(new Question("Сколько планет в Солнечной системе?",
                    Arrays.asList("7", "8", "9", "10"), 1, "science", "easy"));
            list.add(new Question("Кто написал 'Войну и мир'?",
                    Arrays.asList("Достоевский", "Толстой", "Чехов", "Пушкин"), 1, "literature", "medium"));
            return list;
        }

        List<String> getCategories() {
            Set<String> set = new HashSet<>();
            for (Question q : questions) set.add(q.category);
            List<String> cats = new ArrayList<>(set);
            Collections.sort(cats);
            return cats;
        }

        List<Question> getQuestions(String category, int count) {
            List<Question> filtered = new ArrayList<>(questions);
            if (category != null) {
                filtered = filtered.stream().filter(q -> q.category.equals(category)).collect(Collectors.toList());
            }
            if (count > 0 && count < filtered.size()) {
                Collections.shuffle(filtered);
                return filtered.subList(0, count);
            }
            return filtered;
        }

        int[] runQuiz(String category, int count, int timer, String player) {
            List<Question> qs = getQuestions(category, count);
            if (qs.isEmpty()) {
                System.out.println("❌ Нет вопросов в выбранной категории");
                return new int[]{0, 0};
            }
            Collections.shuffle(qs);
            System.out.printf("\n🎯 Начинаем викторину! Вопросов: %d\n\n", qs.size());
            int correct = 0;
            Scanner sc = new Scanner(System.in);
            for (int i = 0; i < qs.size(); i++) {
                Question q = qs.get(i);
                System.out.printf("Вопрос %d/%d [%s] (%s):\n", i+1, qs.size(), q.category, q.difficulty);
                System.out.println("  " + q.question);
                for (int j = 0; j < q.options.size(); j++) {
                    System.out.printf("  %d. %s\n", j+1, q.options.get(j));
                }
                if (timer > 0) System.out.printf("⏱️ У вас %d секунд!\n", timer);
                long start = System.currentTimeMillis();
                int answer = -1;
                while (answer == -1) {
                    System.out.print("Ваш ответ (1-4): ");
                    String input = sc.nextLine();
                    if (timer > 0 && (System.currentTimeMillis() - start) > timer * 1000) {
                        System.out.println("⏰ Время вышло!");
                        break;
                    }
                    try {
                        int ans = Integer.parseInt(input);
                        if (ans >= 1 && ans <= q.options.size()) {
                            answer = ans - 1;
                        } else {
                            System.out.println("Пожалуйста, введите число от 1 до " + q.options.size());
                        }
                    } catch (NumberFormatException e) {
                        System.out.println("Введите число");
                    }
                }
                if (answer == q.correct) {
                    System.out.println("✅ Правильно!");
                    correct++;
                } else if (answer != -1) {
                    System.out.printf("❌ Неправильно! Правильный ответ: %s\n", q.options.get(q.correct));
                } else {
                    System.out.printf("Правильный ответ: %s\n", q.options.get(q.correct));
                }
                System.out.println();
            }
            System.out.printf("🎯 Результат: %d/%d\n", correct, qs.size());
            return new int[]{correct, qs.size()};
        }

        void addScore(String player, int score, int correct, int total, String category) {
            scores.add(new Score(player, score, correct, total, category == null ? "all" : category));
            saveScores();
        }

        List<Score> getLeaderboard(int limit) {
            List<Score> sorted = new ArrayList<>(scores);
            sorted.sort((a, b) -> b.score - a.score);
            if (sorted.size() > limit) return sorted.subList(0, limit);
            return sorted;
        }

        void exportCSV(String filepath) throws IOException {
            try (PrintWriter pw = new PrintWriter(filepath)) {
                pw.println("Player,Score,Correct,Total,Date,Category");
                for (Score s : scores) {
                    pw.printf("%s,%d,%d,%d,%s,%s\n", s.player, s.score, s.correct, s.total, s.date, s.category);
                }
            }
        }
    }

    // ========== CLI ==========
    public static void main(String[] args) {
        if (args.length > 0 && args[0].equals("--gui")) {
            SwingUtilities.invokeLater(() -> new QuizForgeGUI().setVisible(true));
            return;
        }
        Forge forge = new Forge();
        forge.load();
        if (args.length == 0) {
            interactiveMode(forge);
            return;
        }
        try {
            String cmd = args[0];
            switch (cmd) {
                case "start": {
                    String category = null; int count = 10; int timer = 0; String player = "Player";
                    for (int i = 1; i < args.length; i++) {
                        if (args[i].equals("--category")) category = args[++i];
                        else if (args[i].equals("--questions")) count = Integer.parseInt(args[++i]);
                        else if (args[i].equals("--timer")) timer = Integer.parseInt(args[++i]);
                        else if (args[i].equals("--player")) player = args[++i];
                    }
                    int[] res = forge.runQuiz(category, count, timer, player);
                    int score = res[0] * 10;
                    System.out.printf("\n💯 Ваш счёт: %d очков\n", score);
                    if (score > 0) {
                        forge.addScore(player, score, res[0], res[1], category);
                        System.out.println("✅ Результат сохранён!");
                    }
                    break;
                }
                case "leaderboard": {
                    int limit = 10;
                    for (int i = 1; i < args.length; i++) {
                        if (args[i].equals("--limit")) limit = Integer.parseInt(args[++i]);
                    }
                    List<Score> scores = forge.getLeaderboard(limit);
                    if (scores.isEmpty()) {
                        System.out.println("Нет записей в таблице рекордов.");
                    } else {
                        System.out.println("🏆 ТАБЛИЦА РЕКОРДОВ");
                        System.out.printf("%-3s %-12s %-6s %-12s %-20s %s\n", "#", "Игрок", "Счёт", "Правильные", "Дата", "Категория");
                        for (int i = 0; i < scores.size(); i++) {
                            Score s = scores.get(i);
                            System.out.printf("%-3d %-12s %-6d %d/%d %-20s %s\n", i+1, s.player, s.score, s.correct, s.total, s.date.substring(0,16), s.category);
                        }
                    }
                    break;
                }
                case "categories": {
                    List<String> cats = forge.getCategories();
                    if (cats.isEmpty()) {
                        System.out.println("Нет категорий.");
                    } else {
                        System.out.println("📚 Доступные категории:");
                        for (String c : cats) {
                            int count = (int) forge.questions.stream().filter(q -> q.category.equals(c)).count();
                            System.out.printf("  %s (%d вопросов)\n", c, count);
                        }
                    }
                    break;
                }
                case "export": {
                    String output = null;
                    for (int i = 1; i < args.length; i++) {
                        if (args[i].equals("--output")) output = args[++i];
                    }
                    if (output == null) { System.out.println("Укажите --output"); return; }
                    forge.exportCSV(output);
                    System.out.println("Экспортировано в " + output);
                    break;
                }
                default: interactiveMode(forge);
            }
        } catch (Exception e) {
            System.err.println("Ошибка: " + e.getMessage());
        }
    }

    static void interactiveMode(Forge forge) {
        Scanner sc = new Scanner(System.in);
        while (true) {
            System.out.println("\n📝 QuizForge - Викторина (интерактивный)");
            System.out.println("1. Начать викторину");
            System.out.println("2. Таблица рекордов");
            System.out.println("3. Список категорий");
            System.out.println("4. Экспорт результатов");
            System.out.println("0. Выход");
            System.out.print("Выберите действие: ");
            String choice = sc.nextLine();
            switch (choice) {
                case "0": return;
                case "1": {
                    List<String> cats = forge.getCategories();
                    System.out.println("Доступные категории: " + String.join(", ", cats));
                    System.out.print("Категория (Enter для всех): ");
                    String cat = sc.nextLine();
                    if (!cat.isEmpty() && !cats.contains(cat)) {
                        System.out.println("Категория не найдена, будут вопросы из всех категорий");
                        cat = null;
                    }
                    System.out.print("Количество вопросов (по умолчанию 10): ");
                    String cntStr = sc.nextLine();
                    int count = cntStr.isEmpty() ? 10 : Integer.parseInt(cntStr);
                    System.out.print("Таймер (сек, Enter без таймера): ");
                    String timerStr = sc.nextLine();
                    int timer = timerStr.isEmpty() ? 0 : Integer.parseInt(timerStr);
                    System.out.print("Ваше имя (по умолчанию Player): ");
                    String player = sc.nextLine();
                    if (player.isEmpty()) player = "Player";
                    int[] res = forge.runQuiz(cat, count, timer, player);
                    int score = res[0] * 10;
                    System.out.printf("\n💯 Ваш счёт: %d очков\n", score);
                    if (score > 0) {
                        forge.addScore(player, score, res[0], res[1], cat);
                        System.out.println("✅ Результат сохранён!");
                    }
                    break;
                }
                case "2": {
                    List<Score> scores = forge.getLeaderboard(10);
                    if (scores.isEmpty()) {
                        System.out.println("Нет записей в таблице рекордов.");
                    } else {
                        System.out.println("🏆 ТАБЛИЦА РЕКОРДОВ");
                        System.out.printf("%-3s %-12s %-6s %-12s %-20s %s\n", "#", "Игрок", "Счёт", "Правильные", "Дата", "Категория");
                        for (int i = 0; i < scores.size(); i++) {
                            Score s = scores.get(i);
                            System.out.printf("%-3d %-12s %-6d %d/%d %-20s %s\n", i+1, s.player, s.score, s.correct, s.total, s.date.substring(0,16), s.category);
                        }
                    }
                    break;
                }
                case "3": {
                    List<String> cats = forge.getCategories();
                    if (cats.isEmpty()) {
                        System.out.println("Нет категорий.");
                    } else {
                        System.out.println("📚 Доступные категории:");
                        for (String c : cats) {
                            int count = (int) forge.questions.stream().filter(q -> q.category.equals(c)).count();
                            System.out.printf("  %s (%d вопросов)\n", c, count);
                        }
                    }
                    break;
                }
                case "4": {
                    System.out.print("Имя файла (CSV): ");
                    String file = sc.nextLine();
                    if (file.isEmpty()) file = "scores.csv";
                    try {
                        forge.exportCSV(file);
                        System.out.println("Экспортировано в " + file);
                    } catch (IOException e) {
                        System.out.println("Ошибка: " + e.getMessage());
                    }
                    break;
                }
                default: System.out.println("Неверный выбор");
            }
        }
    }

    // ========== GUI ==========
    static class QuizForgeGUI extends JFrame {
        private Forge forge = new Forge();
        private JComboBox<String> catBox;
        private JTextField countField, timerField, nameField;
        private JTextArea resultArea;
        private List<Question> currentQuestions;
        private int currentIdx;
        private int correctCount;

        public QuizForgeGUI() {
            forge.load();
            setTitle("📝 QuizForge - Викторина");
            setSize(700, 550);
            setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
            setLayout(new BorderLayout(5,5));
            showMenu();
        }

        void showMenu() {
            getContentPane().removeAll();
            JPanel main = new JPanel(new GridBagLayout());
            GridBagConstraints gbc = new GridBagConstraints();
            gbc.insets = new Insets(10,10,10,10);
            JLabel title = new JLabel("📝 QuizForge", SwingConstants.CENTER);
            title.setFont(new Font("Arial", Font.BOLD, 24));
            gbc.gridx = 0; gbc.gridy = 0; gbc.gridwidth = 1;
            main.add(title, gbc);
            JButton startBtn = new JButton("🎯 Начать викторину");
            startBtn.setFont(new Font("Arial", Font.PLAIN, 16));
            startBtn.addActionListener(e -> showSettings());
            gbc.gridy = 1; main.add(startBtn, gbc);
            JButton lbBtn = new JButton("🏆 Таблица рекордов");
            lbBtn.addActionListener(e -> showLeaderboard());
            gbc.gridy = 2; main.add(lbBtn, gbc);
            JButton catBtn = new JButton("📚 Список категорий");
            catBtn.addActionListener(e -> showCategories());
            gbc.gridy = 3; main.add(catBtn, gbc);
            JButton expBtn = new JButton("💾 Экспорт CSV");
            expBtn.addActionListener(e -> exportCSV());
            gbc.gridy = 4; main.add(expBtn, gbc);
            add(main, BorderLayout.CENTER);
            revalidate();
            repaint();
        }

        void showSettings() {
            getContentPane().removeAll();
            JPanel panel = new JPanel(new GridBagLayout());
            GridBagConstraints gbc = new GridBagConstraints();
            gbc.insets = new Insets(5,5,5,5);
            gbc.fill = GridBagConstraints.HORIZONTAL;
            gbc.gridx = 0; gbc.gridy = 0;
            panel.add(new JLabel("Имя игрока:"), gbc);
            gbc.gridx = 1; nameField = new JTextField("Player", 15); panel.add(nameField, gbc);
            gbc.gridx = 0; gbc.gridy = 1;
            panel.add(new JLabel("Категория:"), gbc);
            gbc.gridx = 1; catBox = new JComboBox<>(); catBox.addItem("");
            for (String c : forge.getCategories()) catBox.addItem(c);
            panel.add(catBox, gbc);
            gbc.gridx = 0; gbc.gridy = 2;
            panel.add(new JLabel("Количество вопросов:"), gbc);
            gbc.gridx = 1; countField = new JTextField("10", 5); panel.add(countField, gbc);
            gbc.gridx = 0; gbc.gridy = 3;
            panel.add(new JLabel("Таймер (сек, 0=нет):"), gbc);
            gbc.gridx = 1; timerField = new JTextField("0", 5); panel.add(timerField, gbc);
            gbc.gridy = 4; gbc.gridx = 0; gbc.gridwidth = 2;
            JButton startBtn = new JButton("▶️ Начать");
            startBtn.addActionListener(e -> startQuiz());
            panel.add(startBtn, gbc);
            gbc.gridy = 5;
            JButton backBtn = new JButton("🏠 В меню");
            backBtn.addActionListener(e -> showMenu());
            panel.add(backBtn, gbc);
            add(panel, BorderLayout.CENTER);
            revalidate();
            repaint();
        }

        void startQuiz() {
            String category = catBox.getSelectedItem().toString();
            if (category.isEmpty()) category = null;
            int count = Integer.parseInt(countField.getText());
            int timer = Integer.parseInt(timerField.getText());
            currentQuestions = forge.getQuestions(category, count);
            if (currentQuestions.isEmpty()) {
                JOptionPane.showMessageDialog(this, "Нет вопросов в выбранной категории");
                return;
            }
            Collections.shuffle(currentQuestions);
            currentIdx = 0;
            correctCount = 0;
            showQuestion(timer);
        }

        void showQuestion(int timer) {
            if (currentIdx >= currentQuestions.size()) {
                showResults();
                return;
            }
            getContentPane().removeAll();
            Question q = currentQuestions.get(currentIdx);
            JPanel panel = new JPanel(new GridBagLayout());
            GridBagConstraints gbc = new GridBagConstraints();
            gbc.insets = new Insets(5,5,5,5);
            gbc.fill = GridBagConstraints.HORIZONTAL;
            gbc.gridx = 0; gbc.gridy = 0; gbc.gridwidth = 2;
            JLabel title = new JLabel(String.format("Вопрос %d/%d", currentIdx+1, currentQuestions.size()));
            title.setFont(new Font("Arial", Font.BOLD, 14));
            panel.add(title, gbc);
            gbc.gridy = 1;
            panel.add(new JLabel("[" + q.category + "] " + q.difficulty), gbc);
            gbc.gridy = 2;
            JLabel qLabel = new JLabel(q.question);
            qLabel.setFont(new Font("Arial", Font.PLAIN, 16));
            panel.add(qLabel, gbc);
            gbc.gridy = 3;
            ButtonGroup group = new ButtonGroup();
            JRadioButton[] radios = new JRadioButton[q.options.size()];
            for (int i = 0; i < q.options.size(); i++) {
                radios[i] = new JRadioButton(q.options.get(i));
                group.add(radios[i]);
                gbc.gridy = 3 + i;
                panel.add(radios[i], gbc);
            }
            if (timer > 0) {
                gbc.gridy = 3 + q.options.size();
                panel.add(new JLabel("⏱️ " + timer + " секунд!"), gbc);
            }
            gbc.gridy = 4 + q.options.size();
            JButton answerBtn = new JButton("✅ Ответить");
            answerBtn.addActionListener(e -> {
                int answer = -1;
                for (int i = 0; i < radios.length; i++) {
                    if (radios[i].isSelected()) { answer = i; break; }
                }
                if (answer == -1) {
                    JOptionPane.showMessageDialog(this, "Выберите вариант ответа");
                    return;
                }
                if (answer == q.correct) {
                    correctCount++;
                    JOptionPane.showMessageDialog(this, "✅ Правильно!");
                } else {
                    JOptionPane.showMessageDialog(this, "❌ Неправильно! Правильный ответ: " + q.options.get(q.correct));
                }
                currentIdx++;
                showQuestion(timer);
            });
            panel.add(answerBtn, gbc);
            add(panel, BorderLayout.CENTER);
            revalidate();
            repaint();
        }

        void showResults() {
            getContentPane().removeAll();
            JPanel panel = new JPanel(new GridBagLayout());
            GridBagConstraints gbc = new GridBagConstraints();
            gbc.insets = new Insets(10,10,10,10);
            gbc.gridx = 0; gbc.gridy = 0;
            JLabel title = new JLabel("🎯 Результат!", SwingConstants.CENTER);
            title.setFont(new Font("Arial", Font.BOLD, 20));
            panel.add(title, gbc);
            gbc.gridy = 1;
            JLabel res = new JLabel(String.format("Правильных ответов: %d/%d", correctCount, currentQuestions.size()));
            res.setFont(new Font("Arial", Font.PLAIN, 16));
            panel.add(res, gbc);
            int score = correctCount * 10;
            gbc.gridy = 2;
            JLabel scoreLabel = new JLabel("Счёт: " + score + " очков");
            scoreLabel.setFont(new Font("Arial", Font.BOLD, 16));
            panel.add(scoreLabel, gbc);
            if (score > 0) {
                String cat = catBox.getSelectedItem().toString();
                if (cat.isEmpty()) cat = "all";
                forge.addScore(nameField.getText(), score, correctCount, currentQuestions.size(), cat);
                gbc.gridy = 3;
                panel.add(new JLabel("✅ Результат сохранён!", SwingConstants.CENTER), gbc);
            }
            gbc.gridy = 4;
            JButton menuBtn = new JButton("🏠 В меню");
            menuBtn.addActionListener(e -> showMenu());
            panel.add(menuBtn, gbc);
            add(panel, BorderLayout.CENTER);
            revalidate();
            repaint();
        }

        void showLeaderboard() {
            getContentPane().removeAll();
            List<Score> scores = forge.getLeaderboard(10);
            JPanel panel = new JPanel(new BorderLayout());
            JTextArea area = new JTextArea();
            area.setEditable(false);
            if (scores.isEmpty()) {
                area.setText("Нет записей в таблице рекордов.");
            } else {
                area.append("🏆 ТАБЛИЦА РЕКОРДОВ\n");
                area.append(String.format("%-3s %-12s %-6s %-12s %-20s %s\n", "#", "Игрок", "Счёт", "Правильные", "Дата", "Категория"));
                for (int i = 0; i < scores.size(); i++) {
                    Score s = scores.get(i);
                    area.append(String.format("%-3d %-12s %-6d %d/%d %-20s %s\n", i+1, s.player, s.score, s.correct, s.total, s.date.substring(0,16), s.category));
                }
            }
            panel.add(new JScrollPane(area), BorderLayout.CENTER);
            JButton backBtn = new JButton("🏠 В меню");
            backBtn.addActionListener(e -> showMenu());
            panel.add(backBtn, BorderLayout.SOUTH);
            add(panel, BorderLayout.CENTER);
            revalidate();
            repaint();
        }

        void showCategories() {
            getContentPane().removeAll();
            List<String> cats = forge.getCategories();
            JPanel panel = new JPanel(new BorderLayout());
            JTextArea area = new JTextArea();
            area.setEditable(false);
            if (cats.isEmpty()) {
                area.setText("Нет категорий.");
            } else {
                area.append("📚 Доступные категории:\n");
                for (String c : cats) {
                    int count = (int) forge.questions.stream().filter(q -> q.category.equals(c)).count();
                    area.append(String.format("  %s (%d вопросов)\n", c, count));
                }
            }
            panel.add(new JScrollPane(area), BorderLayout.CENTER);
            JButton backBtn = new JButton("🏠 В меню");
            backBtn.addActionListener(e -> showMenu());
            panel.add(backBtn, BorderLayout.SOUTH);
            add(panel, BorderLayout.CENTER);
            revalidate();
            repaint();
        }

        void exportCSV() {
            JFileChooser fc = new JFileChooser();
            if (fc.showSaveDialog(this) == JFileChooser.APPROVE_OPTION) {
                try {
                    forge.exportCSV(fc.getSelectedFile().getAbsolutePath());
                    JOptionPane.showMessageDialog(this, "Экспортировано");
                } catch (IOException e) {
                    JOptionPane.showMessageDialog(this, "Ошибка: " + e.getMessage());
                }
            }
        }
    }
}
