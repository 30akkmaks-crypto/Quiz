// quiz_forge.go - Викторина (Квиз) на Go (CLI)
package main

import (
	"bufio"
	"encoding/csv"
	"encoding/json"
	"flag"
	"fmt"
	"math/rand"
	"os"
	"strconv"
	"strings"
	"time"
)

type Question struct {
	Question   string   `json:"question"`
	Options    []string `json:"options"`
	Correct    int      `json:"correct"`
	Category   string   `json:"category"`
	Difficulty string   `json:"difficulty"`
}

type Score struct {
	Player   string `json:"player"`
	Score    int    `json:"score"`
	Correct  int    `json:"correct"`
	Total    int    `json:"total"`
	Date     string `json:"date"`
	Category string `json:"category"`
}

type Forge struct {
	Questions []Question `json:"questions"`
	Scores    []Score    `json:"scores"`
}

const qFile = "questions.json"
const sFile = "scores.json"

func loadForge() *Forge {
	var f Forge
	// Load questions
	file, err := os.ReadFile(qFile)
	if err != nil {
		f.Questions = defaultQuestions()
		f.Scores = []Score{}
		saveForge(&f)
		return &f
	}
	err = json.Unmarshal(file, &f)
	if err != nil {
		f.Questions = defaultQuestions()
		f.Scores = []Score{}
	}
	// Load scores separately
	scoresFile, err := os.ReadFile(sFile)
	if err == nil {
		var scores []Score
		json.Unmarshal(scoresFile, &scores)
		f.Scores = scores
	}
	return &f
}

func saveForge(f *Forge) {
	data, _ := json.MarshalIndent(f.Questions, "", "  ")
	os.WriteFile(qFile, data, 0644)
	scoresData, _ := json.MarshalIndent(f.Scores, "", "  ")
	os.WriteFile(sFile, scoresData, 0644)
}

func defaultQuestions() []Question {
	return []Question{
		{"Столица Франции?", []string{"Лондон", "Париж", "Берлин", "Мадрид"}, 1, "geography", "easy"},
		{"Сколько планет в Солнечной системе?", []string{"7", "8", "9", "10"}, 1, "science", "easy"},
		{"Кто написал 'Войну и мир'?", []string{"Достоевский", "Толстой", "Чехов", "Пушкин"}, 1, "literature", "medium"},
		{"Год первого полёта человека в космос?", []string{"1957", "1961", "1969", "1975"}, 1, "history", "hard"},
		{"Самый большой океан на Земле?", []string{"Атлантический", "Индийский", "Тихий", "Северный Ледовитый"}, 2, "geography", "medium"},
	}
}

func (f *Forge) getCategories() []string {
	catMap := make(map[string]bool)
	for _, q := range f.Questions {
		catMap[q.Category] = true
	}
	var cats []string
	for c := range catMap {
		cats = append(cats, c)
	}
	sortStrings(cats)
	return cats
}

func sortStrings(s []string) {
	for i := 0; i < len(s); i++ {
		for j := i + 1; j < len(s); j++ {
			if s[i] > s[j] {
				s[i], s[j] = s[j], s[i]
			}
		}
	}
}

func (f *Forge) getQuestions(category string, count int) []Question {
	filtered := f.Questions
	if category != "" {
		filtered = filterByCategory(filtered, category)
	}
	if count > 0 && count < len(filtered) {
		rand.Seed(time.Now().UnixNano())
		shuffled := make([]Question, len(filtered))
		copy(shuffled, filtered)
		rand.Shuffle(len(shuffled), func(i, j int) { shuffled[i], shuffled[j] = shuffled[j], shuffled[i] })
		return shuffled[:count]
	}
	return filtered
}

func filterByCategory(questions []Question, cat string) []Question {
	var res []Question
	for _, q := range questions {
		if q.Category == cat {
			res = append(res, q)
		}
	}
	return res
}

func (f *Forge) runQuiz(category string, count int, timer int, player string) (int, int) {
	questions := f.getQuestions(category, count)
	if len(questions) == 0 {
		fmt.Println("❌ Нет вопросов в выбранной категории")
		return 0, 0
	}
	rand.Seed(time.Now().UnixNano())
	shuffled := make([]Question, len(questions))
	copy(shuffled, questions)
	rand.Shuffle(len(shuffled), func(i, j int) { shuffled[i], shuffled[j] = shuffled[j], shuffled[i] })
	fmt.Printf("\n🎯 Начинаем викторину! Вопросов: %d\n\n", len(shuffled))
	correct := 0
	scanner := bufio.NewScanner(os.Stdin)
	for i, q := range shuffled {
		fmt.Printf("Вопрос %d/%d [%s] (%s):\n", i+1, len(shuffled), q.Category, q.Difficulty)
		fmt.Printf("  %s\n", q.Question)
		for j, opt := range q.Options {
			fmt.Printf("  %d. %s\n", j+1, opt)
		}
		if timer > 0 {
			fmt.Printf("⏱️ У вас %d секунд!\n", timer)
		}
		var answer int
		var answered bool
		if timer > 0 {
			// Простой таймер с горутиной
			go func() {
				time.Sleep(time.Duration(timer) * time.Second)
				if !answered {
					fmt.Println("\n⏰ Время вышло!")
					answered = true
				}
			}()
		}
		for {
			if answered {
				break
			}
			fmt.Print("Ваш ответ (1-4): ")
			scanner.Scan()
			ansStr := scanner.Text()
			ans, err := strconv.Atoi(ansStr)
			if err != nil || ans < 1 || ans > 4 {
				fmt.Println("Пожалуйста, введите число от 1 до 4")
				continue
			}
			answer = ans - 1
			answered = true
			break
		}
		if !answered {
			fmt.Printf("Правильный ответ: %s\n", q.Options[q.Correct])
		} else if answer == q.Correct {
			fmt.Println("✅ Правильно!")
			correct++
		} else {
			fmt.Printf("❌ Неправильно! Правильный ответ: %s\n", q.Options[q.Correct])
		}
		fmt.Println()
	}
	fmt.Printf("🎯 Результат: %d/%d\n", correct, len(shuffled))
	return correct, len(shuffled)
}

func (f *Forge) addScore(player string, score, correct, total int, category string) {
	s := Score{
		Player:   player,
		Score:    score,
		Correct:  correct,
		Total:    total,
		Date:     time.Now().Format(time.RFC3339),
		Category: category,
	}
	f.Scores = append(f.Scores, s)
	saveForge(f)
}

func (f *Forge) getLeaderboard(limit int) []Score {
	sorted := make([]Score, len(f.Scores))
	copy(sorted, f.Scores)
	for i := 0; i < len(sorted); i++ {
		for j := i + 1; j < len(sorted); j++ {
			if sorted[i].Score < sorted[j].Score {
				sorted[i], sorted[j] = sorted[j], sorted[i]
			}
		}
	}
	if limit > 0 && limit < len(sorted) {
		return sorted[:limit]
	}
	return sorted
}

func (f *Forge) exportCSV(filepath string) {
	file, err := os.Create(filepath)
	if err != nil {
		fmt.Println("Ошибка создания файла:", err)
		return
	}
	defer file.Close()
	writer := csv.NewWriter(file)
	defer writer.Flush()
	writer.Write([]string{"Player", "Score", "Correct", "Total", "Date", "Category"})
	for _, s := range f.Scores {
		writer.Write([]string{s.Player, strconv.Itoa(s.Score), strconv.Itoa(s.Correct), strconv.Itoa(s.Total), s.Date, s.Category})
	}
}

func main() {
	var (
		cmd       string
		category  string
		count     int
		timer     int
		player    string
		limit     int
		output    string
	)
	flag.StringVar(&cmd, "cmd", "", "Команда: start, leaderboard, categories, export")
	flag.StringVar(&category, "category", "", "Категория")
	flag.IntVar(&count, "questions", 10, "Количество вопросов")
	flag.IntVar(&timer, "timer", 0, "Таймер (сек)")
	flag.StringVar(&player, "player", "Player", "Имя игрока")
	flag.IntVar(&limit, "limit", 10, "Количество записей")
	flag.StringVar(&output, "output", "", "Имя CSV файла")
	flag.Parse()

	forge := loadForge()

	switch cmd {
	case "start":
		correct, total := forge.runQuiz(category, count, timer, player)
		score := correct * 10
		fmt.Printf("\n💯 Ваш счёт: %d очков\n", score)
		if score > 0 {
			cat := category
			if cat == "" {
				cat = "all"
			}
			forge.addScore(player, score, correct, total, cat)
			fmt.Println("✅ Результат сохранён!")
		}
	case "leaderboard":
		scores := forge.getLeaderboard(limit)
		if len(scores) == 0 {
			fmt.Println("Нет записей в таблице рекордов.")
		} else {
			fmt.Println("🏆 ТАБЛИЦА РЕКОРДОВ")
			fmt.Printf("%-3s %-12s %-6s %-12s %-20s %s\n", "#", "Игрок", "Счёт", "Правильные", "Дата", "Категория")
			for i, s := range scores {
				fmt.Printf("%-3d %-12s %-6d %d/%d %-20s %s\n", i+1, s.Player, s.Score, s.Correct, s.Total, s.Date[:16], s.Category)
			}
		}
	case "categories":
		cats := forge.getCategories()
		if len(cats) > 0 {
			fmt.Println("📚 Доступные категории:")
			for _, c := range cats {
				countQ := 0
				for _, q := range forge.Questions {
					if q.Category == c {
						countQ++
					}
				}
				fmt.Printf("  %s (%d вопросов)\n", c, countQ)
			}
		} else {
			fmt.Println("Нет категорий.")
		}
	case "export":
		if output == "" {
			fmt.Println("Укажите --output")
			return
		}
		forge.exportCSV(output)
		fmt.Printf("Экспортировано в %s\n", output)
	default:
		interactiveMode(forge)
	}
}

func interactiveMode(f *Forge) {
	scanner := bufio.NewScanner(os.Stdin)
	for {
		fmt.Println("\n📝 QuizForge - Викторина (интерактивный)")
		fmt.Println("1. Начать викторину")
		fmt.Println("2. Таблица рекордов")
		fmt.Println("3. Список категорий")
		fmt.Println("4. Экспорт результатов")
		fmt.Println("0. Выход")
		fmt.Print("Выберите действие: ")
		scanner.Scan()
		choice := scanner.Text()
		switch choice {
		case "0":
			return
		case "1":
			cats := f.getCategories()
			fmt.Println("Доступные категории:", strings.Join(cats, ", "))
			fmt.Print("Категория (Enter для всех): ")
			scanner.Scan()
			cat := scanner.Text()
			if cat != "" && !contains(cats, cat) {
				fmt.Println("Категория не найдена, будут вопросы из всех категорий")
				cat = ""
			}
			fmt.Print("Количество вопросов (по умолчанию 10): ")
			scanner.Scan()
			countStr := scanner.Text()
			count := 10
			if countStr != "" {
				count, _ = strconv.Atoi(countStr)
			}
			fmt.Print("Таймер (сек, Enter без таймера): ")
			scanner.Scan()
			timerStr := scanner.Text()
			timer := 0
			if timerStr != "" {
				timer, _ = strconv.Atoi(timerStr)
			}
			fmt.Print("Ваше имя (по умолчанию Player): ")
			scanner.Scan()
			player := scanner.Text()
			if player == "" {
				player = "Player"
			}
			correct, total := f.runQuiz(cat, count, timer, player)
			score := correct * 10
			fmt.Printf("\n💯 Ваш счёт: %d очков\n", score)
			if score > 0 {
				catFinal := cat
				if catFinal == "" {
					catFinal = "all"
				}
				f.addScore(player, score, correct, total, catFinal)
				fmt.Println("✅ Результат сохранён!")
			}
		case "2":
			scores := f.getLeaderboard(10)
			if len(scores) == 0 {
				fmt.Println("Нет записей в таблице рекордов.")
			} else {
				fmt.Println("🏆 ТАБЛИЦА РЕКОРДОВ")
				fmt.Printf("%-3s %-12s %-6s %-12s %-20s %s\n", "#", "Игрок", "Счёт", "Правильные", "Дата", "Категория")
				for i, s := range scores {
					fmt.Printf("%-3d %-12s %-6d %d/%d %-20s %s\n", i+1, s.Player, s.Score, s.Correct, s.Total, s.Date[:16], s.Category)
				}
			}
		case "3":
			cats := f.getCategories()
			if len(cats) > 0 {
				fmt.Println("📚 Доступные категории:")
				for _, c := range cats {
					countQ := 0
					for _, q := range f.Questions {
						if q.Category == c {
							countQ++
						}
					}
					fmt.Printf("  %s (%d вопросов)\n", c, countQ)
				}
			} else {
				fmt.Println("Нет категорий.")
			}
		case "4":
			fmt.Print("Имя файла (CSV): ")
			scanner.Scan()
			file := scanner.Text()
			if file == "" {
				file = "scores.csv"
			}
			f.exportCSV(file)
			fmt.Printf("Экспортировано в %s\n", file)
		default:
			fmt.Println("Неверный выбор")
		}
	}
}

func contains(slice []string, item string) bool {
	for _, s := range slice {
		if s == item {
			return true
		}
	}
	return false
}
