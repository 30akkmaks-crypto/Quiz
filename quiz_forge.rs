// quiz_forge.rs - Викторина (Квиз) на Rust (CLI)
use serde::{Serialize, Deserialize};
use std::collections::HashSet;
use std::fs;
use std::io::{self, Write, BufRead};
use std::path::Path;
use std::str::FromStr;
use std::time::{Duration, Instant};
use rand::seq::SliceRandom;
use rand::thread_rng;

#[derive(Serialize, Deserialize, Clone)]
struct Question {
    question: String,
    options: Vec<String>,
    correct: usize,
    category: String,
    difficulty: String,
}

#[derive(Serialize, Deserialize, Clone)]
struct Score {
    player: String,
    score: u32,
    correct: u32,
    total: u32,
    date: String,
    category: String,
}

#[derive(Serialize, Deserialize)]
struct Forge {
    questions: Vec<Question>,
    scores: Vec<Score>,
}

const Q_FILE: &str = "questions.json";
const S_FILE: &str = "scores.json";

impl Forge {
    fn load() -> Self {
        let mut f = Forge { questions: vec![], scores: vec![] };
        if Path::new(Q_FILE).exists() {
            if let Ok(data) = fs::read_to_string(Q_FILE) {
                if let Ok(qs) = serde_json::from_str(&data) {
                    f.questions = qs;
                }
            }
        }
        if f.questions.is_empty() {
            f.questions = default_questions();
            f.save_questions();
        }
        if Path::new(S_FILE).exists() {
            if let Ok(data) = fs::read_to_string(S_FILE) {
                if let Ok(scores) = serde_json::from_str(&data) {
                    f.scores = scores;
                }
            }
        }
        f
    }

    fn save_questions(&self) {
        let data = serde_json::to_string_pretty(&self.questions).unwrap();
        fs::write(Q_FILE, data).unwrap();
    }

    fn save_scores(&self) {
        let data = serde_json::to_string_pretty(&self.scores).unwrap();
        fs::write(S_FILE, data).unwrap();
    }

    fn get_categories(&self) -> Vec<String> {
        let mut set = HashSet::new();
        for q in &self.questions {
            set.insert(q.category.clone());
        }
        let mut cats: Vec<_> = set.into_iter().collect();
        cats.sort();
        cats
    }

    fn get_questions(&self, category: Option<&str>, count: usize) -> Vec<Question> {
        let mut filtered = self.questions.clone();
        if let Some(cat) = category {
            filtered.retain(|q| q.category == cat);
        }
        if count > 0 && count < filtered.len() {
            let mut rng = thread_rng();
            filtered.shuffle(&mut rng);
            filtered.truncate(count);
        }
        filtered
    }

    fn run_quiz(&mut self, category: Option<&str>, count: usize, timer: Option<u64>, player: &str) -> (u32, u32) {
        let mut questions = self.get_questions(category, count);
        if questions.is_empty() {
            println!("❌ Нет вопросов в выбранной категории");
            return (0, 0);
        }
        let mut rng = thread_rng();
        questions.shuffle(&mut rng);
        println!("\n🎯 Начинаем викторину! Вопросов: {}\n", questions.len());
        let mut correct = 0;
        let stdin = io::stdin();
        let mut stdout = io::stdout();

        for (i, q) in questions.iter().enumerate() {
            println!("Вопрос {}/{} [{}] ({}):", i+1, questions.len(), q.category, q.difficulty);
            println!("  {}", q.question);
            for (j, opt) in q.options.iter().enumerate() {
                println!("  {}. {}", j+1, opt);
            }
            if let Some(t) = timer {
                println!("⏱️ У вас {} секунд!", t);
            }
            let start = Instant::now();
            let mut answer: Option<usize> = None;
            let mut answered = false;
            while !answered {
                print!("Ваш ответ (1-{}): ", q.options.len());
                stdout.flush().unwrap();
                let mut input = String::new();
                stdin.read_line(&mut input).unwrap();
                if let Some(t) = timer {
                    if start.elapsed() > Duration::from_secs(t) {
                        println!("⏰ Время вышло!");
                        break;
                    }
                }
                if let Ok(num) = input.trim().parse::<usize>() {
                    if num >= 1 && num <= q.options.len() {
                        answer = Some(num - 1);
                        answered = true;
                    } else {
                        println!("Пожалуйста, введите число от 1 до {}", q.options.len());
                    }
                } else {
                    println!("Пожалуйста, введите число");
                }
            }
            if let Some(ans) = answer {
                if ans == q.correct {
                    println!("✅ Правильно!");
                    correct += 1;
                } else {
                    println!("❌ Неправильно! Правильный ответ: {}", q.options[q.correct]);
                }
            } else {
                println!("Правильный ответ: {}", q.options[q.correct]);
            }
            println!();
        }
        println!("🎯 Результат: {}/{}", correct, questions.len());
        (correct as u32, questions.len() as u32)
    }

    fn add_score(&mut self, player: &str, score: u32, correct: u32, total: u32, category: &str) {
        let s = Score {
            player: player.to_string(),
            score,
            correct,
            total,
            date: chrono::Local::now().to_rfc3339(),
            category: category.to_string(),
        };
        self.scores.push(s);
        self.save_scores();
    }

    fn get_leaderboard(&self, limit: usize) -> Vec<Score> {
        let mut sorted = self.scores.clone();
        sorted.sort_by(|a, b| b.score.cmp(&a.score));
        sorted.truncate(limit);
        sorted
    }

    fn export_csv(&self, filepath: &str) -> Result<(), Box<dyn std::error::Error>> {
        let mut writer = csv::Writer::from_path(filepath)?;
        writer.write_record(&["Player", "Score", "Correct", "Total", "Date", "Category"])?;
        for s in &self.scores {
            writer.serialize((&s.player, s.score, s.correct, s.total, &s.date, &s.category))?;
        }
        writer.flush()?;
        Ok(())
    }
}

fn default_questions() -> Vec<Question> {
    vec![
        Question {
            question: "Столица Франции?".to_string(),
            options: vec!["Лондон".to_string(), "Париж".to_string(), "Берлин".to_string(), "Мадрид".to_string()],
            correct: 1,
            category: "geography".to_string(),
            difficulty: "easy".to_string(),
        },
        Question {
            question: "Сколько планет в Солнечной системе?".to_string(),
            options: vec!["7".to_string(), "8".to_string(), "9".to_string(), "10".to_string()],
            correct: 1,
            category: "science".to_string(),
            difficulty: "easy".to_string(),
        },
        Question {
            question: "Кто написал 'Войну и мир'?".to_string(),
            options: vec!["Достоевский".to_string(), "Толстой".to_string(), "Чехов".to_string(), "Пушкин".to_string()],
            correct: 1,
            category: "literature".to_string(),
            difficulty: "medium".to_string(),
        },
        Question {
            question: "Год первого полёта человека в космос?".to_string(),
            options: vec!["1957".to_string(), "1961".to_string(), "1969".to_string(), "1975".to_string()],
            correct: 1,
            category: "history".to_string(),
            difficulty: "hard".to_string(),
        },
        Question {
            question: "Самый большой океан на Земле?".to_string(),
            options: vec!["Атлантический".to_string(), "Индийский".to_string(), "Тихий".to_string(), "Северный Ледовитый".to_string()],
            correct: 2,
            category: "geography".to_string(),
            difficulty: "medium".to_string(),
        },
    ]
}

fn read_line(prompt: &str) -> String {
    print!("{}", prompt);
    io::stdout().flush().unwrap();
    let mut input = String::new();
    io::stdin().read_line(&mut input).unwrap();
    input.trim().to_string()
}

fn main() {
    let args: Vec<String> = std::env::args().collect();
    if args.len() < 2 {
        interactive_mode();
        return;
    }
    let mut forge = Forge::load();
    match args[1].as_str() {
        "start" => {
            let mut category = None;
            let mut count = 10;
            let mut timer = None;
            let mut player = "Player".to_string();
            let mut i = 2;
            while i < args.len() {
                match args[i].as_str() {
                    "--category" => { category = Some(args[i+1].clone()); i += 2; }
                    "--questions" => { count = args[i+1].parse().unwrap_or(10); i += 2; }
                    "--timer" => { timer = Some(args[i+1].parse().unwrap_or(0)); i += 2; }
                    "--player" => { player = args[i+1].clone(); i += 2; }
                    _ => { i += 1; }
                }
            }
            let (correct, total) = forge.run_quiz(category.as_deref(), count, timer, &player);
            let score = correct * 10;
            println!("\n💯 Ваш счёт: {} очков", score);
            if score > 0 {
                let cat = category.unwrap_or_else(|| "all".to_string());
                forge.add_score(&player, score, correct, total, &cat);
                println!("✅ Результат сохранён!");
            }
        }
        "leaderboard" => {
            let mut limit = 10;
            let mut i = 2;
            while i < args.len() {
                if args[i] == "--limit" {
                    limit = args[i+1].parse().unwrap_or(10);
                    i += 2;
                } else { i += 1; }
            }
            let scores = forge.get_leaderboard(limit);
            if scores.is_empty() {
                println!("Нет записей в таблице рекордов.");
            } else {
                println!("🏆 ТАБЛИЦА РЕКОРДОВ");
                println!("{:<3} {:<12} {:<6} {:<12} {:<20} {}", "#", "Игрок", "Счёт", "Правильные", "Дата", "Категория");
                for (i, s) in scores.iter().enumerate() {
                    println!("{:<3} {:<12} {:<6} {}/{} {:<20} {}", i+1, s.player, s.score, s.correct, s.total, &s.date[..16], s.category);
                }
            }
        }
        "categories" => {
            let cats = forge.get_categories();
            if cats.is_empty() {
                println!("Нет категорий.");
            } else {
                println!("📚 Доступные категории:");
                for c in cats {
                    let count = forge.questions.iter().filter(|q| q.category == c).count();
                    println!("  {} ({} вопросов)", c, count);
                }
            }
        }
        "export" => {
            let mut output = String::new();
            let mut i = 2;
            while i < args.len() {
                if args[i] == "--output" {
                    output = args[i+1].clone();
                    i += 2;
                } else { i += 1; }
            }
            if output.is_empty() {
                println!("Укажите --output");
                return;
            }
            if let Err(e) = forge.export_csv(&output) {
                println!("Ошибка экспорта: {}", e);
            } else {
                println!("Экспортировано в {}", output);
            }
        }
        _ => interactive_mode(),
    }
}

fn interactive_mode() {
    let mut forge = Forge::load();
    let stdin = io::stdin();
    let mut stdout = io::stdout();
    loop {
        println!("\n📝 QuizForge - Викторина (интерактивный)");
        println!("1. Начать викторину");
        println!("2. Таблица рекордов");
        println!("3. Список категорий");
        println!("4. Экспорт результатов");
        println!("0. Выход");
        print!("Выберите действие: ");
        stdout.flush().unwrap();
        let mut choice = String::new();
        stdin.read_line(&mut choice).unwrap();
        match choice.trim() {
            "0" => break,
            "1" => {
                let cats = forge.get_categories();
                println!("Доступные категории: {}", if cats.is_empty() { "Нет категорий".to_string() } else { cats.join(", ") });
                let cat = read_line("Категория (Enter для всех): ");
                let cat = if cat.is_empty() || !cats.contains(&cat) { None } else { Some(cat) };
                let count = read_line("Количество вопросов (по умолчанию 10): ").parse::<usize>().unwrap_or(10);
                let timer_str = read_line("Таймер (сек, Enter без таймера): ");
                let timer = if timer_str.is_empty() { None } else { Some(timer_str.parse::<u64>().unwrap_or(0)) };
                let player = read_line("Ваше имя (по умолчанию Player): ");
                let player = if player.is_empty() { "Player".to_string() } else { player };
                let (correct, total) = forge.run_quiz(cat.as_deref(), count, timer, &player);
                let score = correct * 10;
                println!("\n💯 Ваш счёт: {} очков", score);
                if score > 0 {
                    let cat_final = cat.unwrap_or_else(|| "all".to_string());
                    forge.add_score(&player, score, correct, total, &cat_final);
                    println!("✅ Результат сохранён!");
                }
            }
            "2" => {
                let scores = forge.get_leaderboard(10);
                if scores.is_empty() {
                    println!("Нет записей в таблице рекордов.");
                } else {
                    println!("🏆 ТАБЛИЦА РЕКОРДОВ");
                    println!("{:<3} {:<12} {:<6} {:<12} {:<20} {}", "#", "Игрок", "Счёт", "Правильные", "Дата", "Категория");
                    for (i, s) in scores.iter().enumerate() {
                        println!("{:<3} {:<12} {:<6} {}/{} {:<20} {}", i+1, s.player, s.score, s.correct, s.total, &s.date[..16], s.category);
                    }
                }
            }
            "3" => {
                let cats = forge.getCategories();
                if cats.is_empty() {
                    println!("Нет категорий.");
                } else {
                    println!("📚 Доступные категории:");
                    for c in cats {
                        let count = forge.questions.iter().filter(|q| q.category == c).count();
                        println!("  {} ({} вопросов)", c, count);
                    }
                }
            }
            "4" => {
                let file = read_line("Имя файла (CSV): ");
                let file = if file.is_empty() { "scores.csv".to_string() } else { file };
                if let Err(e) = forge.export_csv(&file) {
                    println!("Ошибка экспорта: {}", e);
                } else {
                    println!("Экспортировано в {}", file);
                }
            }
            _ => println!("Неверный выбор"),
        }
    }
}
