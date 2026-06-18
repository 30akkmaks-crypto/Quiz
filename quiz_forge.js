#!/usr/bin/env node
/**
 * quiz_forge.js - Викторина (Квиз) на JavaScript (Node.js CLI + веб)
 */
const fs = require('fs');
const path = require('path');
const { program } = require('commander');
const readline = require('readline');

const QUESTIONS_FILE = path.join(__dirname, 'questions.json');
const SCORES_FILE = path.join(__dirname, 'scores.json');

class Question {
    constructor(question, options, correct, category, difficulty) {
        this.question = question;
        this.options = options;
        this.correct = correct;
        this.category = category || 'general';
        this.difficulty = difficulty || 'medium';
    }
}

class Score {
    constructor(player, score, correct, total, category) {
        this.player = player;
        this.score = score;
        this.correct = correct;
        this.total = total;
        this.date = new Date().toISOString();
        this.category = category || 'all';
    }
}

class QuizForge {
    constructor() {
        this.questions = [];
        this.scores = [];
        this.loadQuestions();
        this.loadScores();
    }

    loadQuestions() {
        if (fs.existsSync(QUESTIONS_FILE)) {
            try {
                const data = JSON.parse(fs.readFileSync(QUESTIONS_FILE, 'utf8'));
                this.questions = data.map(q => new Question(q.question, q.options, q.correct, q.category, q.difficulty));
                return;
            } catch {}
        }
        this.questions = this.defaultQuestions();
        this.saveQuestions();
    }

    saveQuestions() {
        fs.writeFileSync(QUESTIONS_FILE, JSON.stringify(this.questions, null, 2));
    }

    loadScores() {
        if (fs.existsSync(SCORES_FILE)) {
            try {
                this.scores = JSON.parse(fs.readFileSync(SCORES_FILE, 'utf8'));
            } catch {}
        }
    }

    saveScores() {
        fs.writeFileSync(SCORES_FILE, JSON.stringify(this.scores, null, 2));
    }

    defaultQuestions() {
        return [
            new Question("Столица Франции?", ["Лондон", "Париж", "Берлин", "Мадрид"], 1, "geography", "easy"),
            new Question("Сколько планет в Солнечной системе?", ["7", "8", "9", "10"], 1, "science", "easy"),
            new Question("Кто написал 'Войну и мир'?", ["Достоевский", "Толстой", "Чехов", "Пушкин"], 1, "literature", "medium"),
            new Question("Год первого полёта человека в космос?", ["1957", "1961", "1969", "1975"], 1, "history", "hard"),
            new Question("Самый большой океан на Земле?", ["Атлантический", "Индийский", "Тихий", "Северный Ледовитый"], 2, "geography", "medium"),
        ];
    }

    getCategories() {
        return [...new Set(this.questions.map(q => q.category))].sort();
    }

    getQuestions(category, count) {
        let filtered = this.questions;
        if (category) filtered = filtered.filter(q => q.category === category);
        if (count && count < filtered.length) {
            const shuffled = [...filtered].sort(() => Math.random() - 0.5);
            return shuffled.slice(0, count);
        }
        return filtered;
    }

    async runQuiz(category, count, timer, player) {
        const questions = this.getQuestions(category, count);
        if (!questions.length) {
            console.log('❌ Нет вопросов в выбранной категории');
            return { correct: 0, total: 0 };
        }
        const shuffled = questions.sort(() => Math.random() - 0.5);
        console.log(`\n🎯 Начинаем викторину! Вопросов: ${shuffled.length}\n`);
        let correct = 0;
        const rl = readline.createInterface({
            input: process.stdin,
            output: process.stdout
        });
        const prompt = (q) => new Promise(resolve => rl.question(q, resolve));

        for (let i = 0; i < shuffled.length; i++) {
            const q = shuffled[i];
            console.log(`Вопрос ${i+1}/${shuffled.length} [${q.category}] (${q.difficulty}):`);
            console.log(`  ${q.question}`);
            q.options.forEach((opt, j) => console.log(`  ${j+1}. ${opt}`));
            if (timer) console.log(`⏱️ У вас ${timer} секунд!`);
            let answer = -1;
            let answered = false;
            const timeout = setTimeout(() => {
                if (!answered) {
                    console.log(`\n⏰ Время вышло!`);
                    answered = true;
                }
            }, timer ? timer * 1000 : 0);
            while (!answered) {
                const input = await prompt('Ваш ответ (1-4): ');
                if (answered) break;
                const ans = parseInt(input) - 1;
                if (ans >= 0 && ans < q.options.length) {
                    answer = ans;
                    answered = true;
                    clearTimeout(timeout);
                } else {
                    console.log('Пожалуйста, введите число от 1 до 4');
                }
            }
            if (answer === q.correct) {
                console.log('✅ Правильно!');
                correct++;
            } else if (answer !== -1) {
                console.log(`❌ Неправильно! Правильный ответ: ${q.options[q.correct]}`);
            }
            console.log();
        }
        rl.close();
        console.log(`🎯 Результат: ${correct}/${shuffled.length}`);
        return { correct, total: shuffled.length };
    }

    addScore(player, score, correct, total, category) {
        const s = new Score(player, score, correct, total, category);
        this.scores.push(s);
        this.saveScores();
    }

    getLeaderboard(limit = 10) {
        return this.scores.sort((a, b) => b.score - a.score).slice(0, limit);
    }

    exportCSV(filepath) {
        const lines = ['Player,Score,Correct,Total,Date,Category'];
        this.scores.forEach(s => {
            lines.push(`${s.player},${s.score},${s.correct},${s.total},${s.date},${s.category}`);
        });
        fs.writeFileSync(filepath, lines.join('\n'));
    }
}

program
    .command('start')
    .option('--category <category>', 'Категория')
    .option('--questions <count>', 'Количество вопросов', parseInt, 10)
    .option('--timer <seconds>', 'Таймер на вопрос', parseInt)
    .option('--player <name>', 'Имя игрока', 'Player')
    .action(async (options) => {
        const forge = new QuizForge();
        const { correct, total } = await forge.runQuiz(options.category, options.questions, options.timer, options.player);
        const score = correct * 10;
        console.log(`\n💯 Ваш счёт: ${score} очков`);
        if (score > 0) {
            forge.addScore(options.player, score, correct, total, options.category || 'all');
            console.log('✅ Результат сохранён!');
        }
    });

program
    .command('leaderboard')
    .option('--limit <limit>', 'Количество записей', parseInt, 10)
    .action((options) => {
        const forge = new QuizForge();
        const scores = forge.getLeaderboard(options.limit);
        if (!scores.length) {
            console.log('Нет записей в таблице рекордов.');
            return;
        }
        console.log('🏆 ТАБЛИЦА РЕКОРДОВ');
        console.log('#   Игрок       Счёт   Правильные    Дата                 Категория');
        scores.forEach((s, i) => {
            console.log(`${i+1}.  ${s.player.padEnd(10)} ${s.score.toString().padEnd(5)} ${s.correct}/${s.total}   ${s.date.slice(0,16)}   ${s.category}`);
        });
    });

program
    .command('categories')
    .action(() => {
        const forge = new QuizForge();
        const cats = forge.getCategories();
        if (cats.length) {
            console.log('📚 Доступные категории:');
            cats.forEach(c => {
                const count = forge.questions.filter(q => q.category === c).length;
                console.log(`  ${c} (${count} вопросов)`);
            });
        } else {
            console.log('Нет категорий.');
        }
    });

program
    .command('export')
    .requiredOption('-o, --output <file>', 'Имя CSV файла')
    .action((options) => {
        const forge = new QuizForge();
        forge.exportCSV(options.output);
        console.log(`Экспортировано в ${options.output}`);
    });

if (process.argv.length <= 2) {
    // Interactive mode
    const forge = new QuizForge();
    const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
    const prompt = (q) => new Promise(resolve => rl.question(q, resolve));

    (async () => {
        while (true) {
            console.log('\n📝 QuizForge - Викторина (интерактивный)');
            console.log('1. Начать викторину');
            console.log('2. Таблица рекордов');
            console.log('3. Список категорий');
            console.log('4. Экспорт результатов');
            console.log('0. Выход');
            const choice = await prompt('Выберите действие: ');
            switch (choice.trim()) {
                case '0': rl.close(); return;
                case '1': {
                    const cats = forge.getCategories();
                    console.log('Доступные категории:', cats.join(', ') || 'Нет категорий');
                    const cat = await prompt('Категория (Enter для всех): ');
                    const catFinal = cat && cats.includes(cat) ? cat : null;
                    const count = parseInt(await prompt('Количество вопросов (по умолчанию 10): ') || '10');
                    const timer = parseInt(await prompt('Таймер (сек, Enter без таймера): ') || '0') || null;
                    const player = await prompt('Ваше имя (по умолчанию Player): ') || 'Player';
                    const { correct, total } = await forge.runQuiz(catFinal, count, timer, player);
                    const score = correct * 10;
                    console.log(`\n💯 Ваш счёт: ${score} очков`);
                    if (score > 0) {
                        forge.addScore(player, score, correct, total, catFinal || 'all');
                        console.log('✅ Результат сохранён!');
                    }
                    break;
                }
                case '2': {
                    const scores = forge.getLeaderboard(10);
                    if (!scores.length) {
                        console.log('Нет записей в таблице рекордов.');
                    } else {
                        console.log('🏆 ТАБЛИЦА РЕКОРДОВ');
                        console.log('#   Игрок       Счёт   Правильные    Дата                 Категория');
                        scores.forEach((s, i) => {
                            console.log(`${i+1}.  ${s.player.padEnd(10)} ${s.score.toString().padEnd(5)} ${s.correct}/${s.total}   ${s.date.slice(0,16)}   ${s.category}`);
                        });
                    }
                    break;
                }
                case '3': {
                    const cats = forge.getCategories();
                    if (cats.length) {
                        console.log('📚 Доступные категории:');
                        cats.forEach(c => {
                            const count = forge.questions.filter(q => q.category === c).length;
                            console.log(`  ${c} (${count} вопросов)`);
                        });
                    } else {
                        console.log('Нет категорий.');
                    }
                    break;
                }
                case '4': {
                    const file = await prompt('Имя файла (CSV): ') || 'scores.csv';
                    forge.exportCSV(file);
                    console.log(`Экспортировано в ${file}`);
                    break;
                }
                default: console.log('Неверный выбор');
            }
        }
    })();
} else {
    program.parse(process.argv);
}
