<?php
// quiz_forge.php - Викторина (Квиз) на PHP (CLI + веб)
// CLI: php quiz_forge.php start --category=geography --questions=5

$qFile = 'questions.json';
$sFile = 'scores.json';

class Question {
    public $question;
    public $options;
    public $correct;
    public $category;
    public $difficulty;
    function __construct($q, $opts, $c, $cat, $diff) {
        $this->question = $q;
        $this->options = $opts;
        $this->correct = $c;
        $this->category = $cat;
        $this->difficulty = $diff;
    }
}

function loadQuestions() {
    global $qFile;
    if (file_exists($qFile)) {
        $json = file_get_contents($qFile);
        $data = json_decode($json, true);
        if ($data) {
            $questions = [];
            foreach ($data as $q) {
                $questions[] = new Question($q['question'], $q['options'], $q['correct'], $q['category'], $q['difficulty']);
            }
            return $questions;
        }
    }
    return defaultQuestions();
}

function defaultQuestions() {
    return [
        new Question("Столица Франции?", ["Лондон", "Париж", "Берлин", "Мадрид"], 1, "geography", "easy"),
        new Question("Сколько планет в Солнечной системе?", ["7", "8", "9", "10"], 1, "science", "easy"),
        new Question("Кто написал 'Войну и мир'?", ["Достоевский", "Толстой", "Чехов", "Пушкин"], 1, "literature", "medium"),
    ];
}

function saveQuestions($questions) {
    global $qFile;
    file_put_contents($qFile, json_encode($questions, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
}

function loadScores() {
    global $sFile;
    if (file_exists($sFile)) {
        $json = file_get_contents($sFile);
        $data = json_decode($json, true);
        return $data ?: [];
    }
    return [];
}

function saveScores($scores) {
    global $sFile;
    file_put_contents($sFile, json_encode($scores, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
}

function getCategories($questions) {
    $cats = [];
    foreach ($questions as $q) {
        $cats[$q->category] = true;
    }
    $cats = array_keys($cats);
    sort($cats);
    return $cats;
}

function getQuestions($questions, $category, $count) {
    $filtered = $questions;
    if ($category) {
        $filtered = array_filter($filtered, function($q) use ($category) { return $q->category == $category; });
    }
    if ($count && $count < count($filtered)) {
        shuffle($filtered);
        return array_slice($filtered, 0, $count);
    }
    return $filtered;
}

function runQuiz($questions, $category, $count, $timer, $player) {
    $qs = getQuestions($questions, $category, $count);
    if (empty($qs)) {
        echo "❌ Нет вопросов в выбранной категории\n";
        return [0, 0];
    }
    shuffle($qs);
    echo "\n🎯 Начинаем викторину! Вопросов: " . count($qs) . "\n\n";
    $correct = 0;
    foreach ($qs as $i => $q) {
        echo "Вопрос " . ($i+1) . "/" . count($qs) . " [{$q->category}] ({$q->difficulty}):\n";
        echo "  {$q->question}\n";
        foreach ($q->options as $j => $opt) {
            echo "  " . ($j+1) . ". $opt\n";
        }
        if ($timer) echo "⏱️ У вас $timer секунд!\n";
        $start = time();
        $answer = -1;
        while ($answer == -1) {
            echo "Ваш ответ (1-" . count($q->options) . "): ";
            $input = trim(fgets(STDIN));
            if ($timer && (time() - $start) > $timer) {
                echo "⏰ Время вышло!\n";
                break;
            }
            if (is_numeric($input) && $input >= 1 && $input <= count($q->options)) {
                $answer = $input - 1;
            } else {
                echo "Пожалуйста, введите число от 1 до " . count($q->options) . "\n";
            }
        }
        if ($answer == $q->correct) {
            echo "✅ Правильно!\n";
            $correct++;
        } elseif ($answer != -1) {
            echo "❌ Неправильно! Правильный ответ: {$q->options[$q->correct]}\n";
        } else {
            echo "Правильный ответ: {$q->options[$q->correct]}\n";
        }
        echo "\n";
    }
    echo "🎯 Результат: $correct/" . count($qs) . "\n";
    return [$correct, count($qs)];
}

function addScore(&$scores, $player, $score, $correct, $total, $category) {
    $scores[] = [
        'player' => $player,
        'score' => $score,
        'correct' => $correct,
        'total' => $total,
        'date' => date('c'),
        'category' => $category ?: 'all'
    ];
    saveScores($scores);
}

function getLeaderboard($scores, $limit = 10) {
    usort($scores, function($a, $b) { return $b['score'] - $a['score']; });
    return array_slice($scores, 0, $limit);
}

function exportCSV($scores, $filepath) {
    $f = fopen($filepath, 'w');
    fputcsv($f, ['Player', 'Score', 'Correct', 'Total', 'Date', 'Category']);
    foreach ($scores as $s) {
        fputcsv($f, [$s['player'], $s['score'], $s['correct'], $s['total'], $s['date'], $s['category']]);
    }
    fclose($f);
}

// ========== CLI ==========
if (php_sapi_name() === 'cli') {
    $options = getopt("", ["cmd:", "category:", "questions:", "timer:", "player:", "limit:", "output:"]);
    $cmd = $options['cmd'] ?? null;
    $questions = loadQuestions();
    $scores = loadScores();

    switch ($cmd) {
        case 'start':
            $category = $options['category'] ?? null;
            $count = isset($options['questions']) ? (int)$options['questions'] : 10;
            $timer = isset($options['timer']) ? (int)$options['timer'] : 0;
            $player = $options['player'] ?? 'Player';
            list($correct, $total) = runQuiz($questions, $category, $count, $timer, $player);
            $score = $correct * 10;
            echo "\n💯 Ваш счёт: $score очков\n";
            if ($score > 0) {
                addScore($scores, $player, $score, $correct, $total, $category);
                echo "✅ Результат сохранён!\n";
            }
            break;
        case 'leaderboard':
            $limit = isset($options['limit']) ? (int)$options['limit'] : 10;
            $leaderboard = getLeaderboard($scores, $limit);
            if (empty($leaderboard)) {
                echo "Нет записей в таблице рекордов.\n";
            } else {
                echo "🏆 ТАБЛИЦА РЕКОРДОВ\n";
                printf("%-3s %-12s %-6s %-12s %-20s %s\n", "#", "Игрок", "Счёт", "Правильные", "Дата", "Категория");
                foreach ($leaderboard as $i => $s) {
                    printf("%-3d %-12s %-6d %d/%d %-20s %s\n", $i+1, $s['player'], $s['score'], $s['correct'], $s['total'], substr($s['date'], 0, 16), $s['category']);
                }
            }
            break;
        case 'categories':
            $cats = getCategories($questions);
            if (empty($cats)) {
                echo "Нет категорий.\n";
            } else {
                echo "📚 Доступные категории:\n";
                foreach ($cats as $c) {
                    $count = count(array_filter($questions, function($q) use ($c) { return $q->category == $c; }));
                    echo "  $c ($count вопросов)\n";
                }
            }
            break;
        case 'export':
            $output = $options['output'] ?? null;
            if (!$output) { echo "Укажите --output\n"; break; }
            exportCSV($scores, $output);
            echo "Экспортировано в $output\n";
            break;
        default:
            interactiveMode($questions, $scores);
            break;
    }
    exit;
}

// ========== ИНТЕРАКТИВНЫЙ РЕЖИМ ==========
function interactiveMode($questions, &$scores) {
    while (true) {
        echo "\n📝 QuizForge - Викторина (интерактивный)\n";
        echo "1. Начать викторину\n";
        echo "2. Таблица рекордов\n";
        echo "3. Список категорий\n";
        echo "4. Экспорт результатов\n";
        echo "0. Выход\n";
        echo "Выберите действие: ";
        $choice = trim(fgets(STDIN));
        switch ($choice) {
            case '0': return;
            case '1':
                $cats = getCategories($questions);
                echo "Доступные категории: " . (empty($cats) ? "Нет категорий" : implode(", ", $cats)) . "\n";
                echo "Категория (Enter для всех): ";
                $cat = trim(fgets(STDIN));
                if ($cat && !in_array($cat, $cats)) {
                    echo "Категория не найдена, будут вопросы из всех категорий\n";
                    $cat = null;
                }
                echo "Количество вопросов (по умолчанию 10):
