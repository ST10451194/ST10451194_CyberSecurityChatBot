// Chatbot.cs - Full NLP-enabled WPF Cybersecurity Chatbot with Tasks, Quiz, Sentiment, and Detailed Responses
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Text.RegularExpressions;

namespace Chatbot
{
    public class TaskItem
    {
        public string Title;
        public string Description;
        public DateTime? Reminder;
        public bool IsCompleted;

        public override string ToString()
        {
            string status = IsCompleted ? "\u2713 Completed" : "Pending";
            string reminder = Reminder.HasValue ? Reminder.Value.ToString("g") : "No reminder";
            return $"{Title} - {Description} | Reminder: {reminder} | Status: {status}";
        }
    }

    public class QuizQuestion
    {
        public string QuestionText { get; set; }
        public string[] Options { get; set; }
        public string CorrectAnswer { get; set; }
        public string Explanation { get; set; }
    }

    public class ActivityLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }

        public override string ToString() => $"{Timestamp:g} - {Description}";
    }

    public class ChatBot
    {
        private Random random = new Random();
        private string lastTopic;
        private string userInterest = null;

        private List<TaskItem> tasks = new List<TaskItem>();
        private enum TaskConversationState { None, WaitingDescription, WaitingReminder }
        private TaskConversationState taskState = TaskConversationState.None;
        private TaskItem currentTask;

        private List<QuizQuestion> quizQuestions;
        private int currentQuizIndex = -1;
        private int quizScore = 0;
        private bool isQuizActive = false;

        private Dictionary<string, string[]> responseBank;
        private Dictionary<string, string> sentimentResponses;
        private Dictionary<string, List<string>> keywordGroups;

        private List<ActivityLogEntry> activityLog = new List<ActivityLogEntry>();
        public List<ActivityLogEntry> GetActivityLog(int skip = 0, int take = 10) => activityLog.Skip(skip).Take(take).ToList();
        private void Log(string description) => activityLog.Insert(0, new ActivityLogEntry { Timestamp = DateTime.Now, Description = description });

        public ChatBot()
        {
            InitializeResponses();
            InitializeKeywordGroups();
        }

        private void InitializeKeywordGroups()
        {
            keywordGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["hello"] = new List<string> { "hello", "hi", "hey", "greetings" },
                ["password"] = new List<string> { "password", "passcode", "credentials" },
                ["phishing"] = new List<string> { "phishing", "scam", "fake email", "suspicious message" },
                ["malware"] = new List<string> { "malware", "virus", "trojan", "spyware" },
                ["ransomware"] = new List<string> { "ransomware", "encrypted files", "locked data" },
                ["identity theft"] = new List<string> { "identity theft", "impersonation", "stolen identity" },
                ["public wifi"] = new List<string> { "public wifi", "open wifi", "unsecured network" },
                ["vpn"] = new List<string> { "vpn", "virtual private network" },
                ["2fa"] = new List<string> { "2fa", "two factor", "multi-factor" },
                ["safe browsing"] = new List<string> { "safe browsing", "secure browsing", "https" },
                ["update"] = new List<string> { "update", "patch", "upgrade" },
                ["scam"] = new List<string> { "scam", "fraud", "con" }
            };

            sentimentResponses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["worried"] = "It’s okay to feel worried. The first step in cybersecurity is awareness, and you're already doing great just by starting this conversation. Let's explore your concerns together!",
                ["scared"] = "Fear is natural, especially when it comes to online threats. You're not alone—I'm here to walk you through every step of staying safe.",
                ["confused"] = "Confused about cybersecurity? You're not the only one. Let's simplify it together—just ask and I’ll make it clear.",
                ["nervous"] = "Feeling nervous is understandable. The internet can be risky, but with the right knowledge, we can beat those risks.",
                ["anxious"] = "Take a deep breath. You're doing great. Let's focus on one topic at a time and strengthen your digital defenses.",
                ["frustrated"] = "Frustration is valid—technology can be tricky. Let's break it down step-by-step and find solutions.",
                ["angry"] = "Getting angry is okay. Security issues are serious. Let's redirect that energy into protecting your information.",
                ["sad"] = "I'm here for you. Want to lighten the mood with a cybersecurity joke or tip? You’ve got this.",
                ["overwhelmed"] = "One step at a time is all it takes. Don’t worry—we’ll tackle cybersecurity topics together in a manageable way.",
                ["happy"] = "That’s fantastic to hear! Let’s keep up the good habits and learn some cool things about staying safe online.",
                ["curious"] = "Curiosity is powerful. Ask me anything about cybersecurity—I love your enthusiasm!"
            };
        }

        private List<string> DetectMultipleTopics(string input)
        {
            var cleanedInput = new string(input.ToLower().Where(c => !char.IsPunctuation(c)).ToArray());
            return keywordGroups.Where(g => g.Value.Any(k => cleanedInput.Contains(k)))
                                 .Select(g => g.Key).ToList();
        }

        private void InitializeResponses()
        {
            responseBank = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["hello"] = new[]
                {
                    "Hello! How can I assist you with cybersecurity today?",
                    "Hi there! Ready to learn some cybersecurity tips?",
                    "Hey! I'm here to help you stay safe online. What would you like to know?"
                },
                ["password"] = new[]
                {
                    "Always use complex passwords with a mix of letters, numbers, and special symbols. Avoid using the same password across multiple sites, and change your passwords regularly.",
                    "Use a trusted password manager to create and store strong, unique passwords. It’s safer and easier than trying to remember them all yourself.",
                    "Never share your passwords, even with people you trust. Instead, use secure password recovery options and enable two-factor authentication."
                },
                ["phishing"] = new[]
                {
                    "Phishing is a tactic cybercriminals use to trick you into providing personal info. Look out for urgent messages, suspicious links, and emails from unknown senders.",
                    "Avoid clicking links in emails that seem out of place, even if they appear to come from trusted companies. Always verify the sender and check for grammar or formatting errors.",
                    "A good rule: When in doubt, throw it out. If an email seems fishy, don’t engage with it—delete it or report it as phishing."
                },
                ["malware"] = new[]
                {
                    "Malware includes viruses, ransomware, spyware, and more. It’s often hidden in email attachments, software cracks, or untrusted websites.",
                    "Install reputable antivirus software and keep it updated. It helps detect and remove malware before it harms your system.",
                    "Don’t download software or open files from unknown sources. Malware can hijack your device or steal sensitive data."
                },
                ["ransomware"] = new[]
                {
                    "Ransomware locks or encrypts your files until you pay a ransom. The best defense is a strong backup system and secure habits.",
                    "Never pay the ransom—it doesn’t guarantee you’ll get your files back. Instead, restore from backups and report the incident.",
                    "Keep your system updated and be cautious with email attachments. Prevention is key with ransomware."
                },
                ["identity theft"] = new[]
                {
                    "Identity theft happens when someone uses your personal information without permission. Guard your ID numbers, address, and financial details.",
                    "Shred important documents before discarding them. Monitor your bank statements and credit reports for suspicious activity.",
                    "Limit how much personal information you share online. Even birthday and hometown info can be used for identity theft."
                },
                ["public wifi"] = new[]
                {
                    "Public Wi-Fi is often unsecured. Avoid entering sensitive information or logging into important accounts on it.",
                    "Use a VPN (Virtual Private Network) when on public Wi-Fi. It encrypts your data and helps protect your privacy.",
                    "If possible, use your mobile data or personal hotspot instead of public networks."
                },
                ["vpn"] = new[]
                {
                    "A VPN secures your internet connection by routing it through encrypted servers, masking your IP address and protecting your data.",
                    "Using a VPN on public Wi-Fi protects you from hackers who try to intercept your data.",
                    "Choose a trustworthy VPN provider—free ones might compromise your data or sell your information."
                },
                ["2fa"] = new[]
                {
                    "Two-Factor Authentication (2FA) adds a second verification step, making it much harder for attackers to access your account.",
                    "Always enable 2FA on your accounts. Even if your password is stolen, 2FA can prevent unauthorized access.",
                    "Use authenticator apps rather than SMS for better protection against SIM-swapping attacks."
                },
                ["safe browsing"] = new[]
                {
                    "Stick to secure websites with HTTPS in the URL. Avoid downloading files or clicking links from shady sites.",
                    "Keep your browser and extensions up to date. Many updates patch security holes exploited by hackers.",
                    "Install browser add-ons like ad-blockers and script blockers to defend against malicious ads and trackers."
                },
                ["update"] = new[]
                {
                    "Updates aren’t just for new features—they often fix critical security issues. Always install them promptly.",
                    "Automate your updates whenever possible. Outdated software is a major vulnerability.",
                    "Don’t ignore update notifications—they’re your shield against the latest threats."
                },
                ["scam"] = new[]
                {
                    "Scams can take many forms—fake job offers, phishing emails, or fraudulent websites. Be skeptical and research everything.",
                    "If something online seems too good to be true, it probably is. Don’t give out your personal information easily.",
                    "Always verify links and company names. Scammers often use slight misspellings or altered URLs."
                },
            };
        }

        private void InitializeQuiz()
        {
            quizQuestions = new List<QuizQuestion>
            {
                new QuizQuestion { QuestionText = "Phishing uses fake messages to trick you.", Options = new[] { "True", "False" }, CorrectAnswer = "True", Explanation = "Be cautious with unknown emails." },
                new QuizQuestion { QuestionText = "Public Wi-Fi is always safe.", Options = new[] { "True", "False" }, CorrectAnswer = "False", Explanation = "Public Wi-Fi can be unsafe." }
            };
        }

        private string GetNextQuizQuestion()
        {
            currentQuizIndex++;
            if (currentQuizIndex < quizQuestions.Count)
            {
                var q = quizQuestions[currentQuizIndex];
                return $"Question {currentQuizIndex + 1}:\n{q.QuestionText}\nOptions: {string.Join(", ", q.Options)}";
            }
            else
            {
                isQuizActive = false;
                string result = $"Quiz completed. Score: {quizScore}/{quizQuestions.Count}";
                Log(result);
                return result;
            }
        }

        public string GetResponse(string input, ref string userName)
        {
            if (input == "start quiz")
            {
                InitializeQuiz();
                currentQuizIndex = -1;
                quizScore = 0;
                isQuizActive = true;
                Log("Quiz started.");
                return "Quiz Started!\n" + GetNextQuizQuestion();
            }

            if (isQuizActive && currentQuizIndex >= 0)
            {
                var q = quizQuestions[currentQuizIndex];
                string result = input.Equals(q.CorrectAnswer, StringComparison.OrdinalIgnoreCase)
                    ? $"\u2705 Correct! {q.Explanation}" : $"\u274C Incorrect. {q.Explanation}";
                if (input.Equals(q.CorrectAnswer, StringComparison.OrdinalIgnoreCase)) quizScore++;
                Log($"Answered quiz Q{currentQuizIndex + 1}: {input}");
                return result + "\n\n" + GetNextQuizQuestion();
            }

            input = input.ToLowerInvariant().Trim();

            if (taskState == TaskConversationState.WaitingDescription)
            {
                currentTask.Description = input;
                taskState = TaskConversationState.WaitingReminder;
                return "Task description set. Add reminder? (yes/no)";
            }

            if (taskState == TaskConversationState.WaitingReminder)
            {
                if (input == "yes") return "Please say e.g. 'in 3 days'.";
                if (input == "no")
                {
                    tasks.Add(currentTask);
                    Log($"Task added: {currentTask.Title}");
                    taskState = TaskConversationState.None;
                    return $"Task '{currentTask.Title}' added.";
                }
                if (TryParseReminder(input, out DateTime reminder))
                {
                    currentTask.Reminder = reminder;
                    tasks.Add(currentTask);
                    Log($"Task added with reminder: {currentTask.Title} - {reminder:g}");
                    taskState = TaskConversationState.None;
                    return $"Task with reminder added for {reminder:g}.";
                }
                return "Say 'yes', 'no', or provide reminder.";
            }

            if (input.StartsWith("add task -"))
            {
                var title = input.Substring(10).Trim();
                if (string.IsNullOrWhiteSpace(title)) return "Please provide task title.";
                currentTask = new TaskItem { Title = title };
                taskState = TaskConversationState.WaitingDescription;
                return $"What's the description for '{title}'?";
            }

            if (input == "show tasks")
                return tasks.Count == 0 ? "No tasks yet." : string.Join("\n", tasks.Select((t, i) => $"{i + 1}. {t}"));

            if (input == "show log")
            {
                var logEntries = GetActivityLog(0, 5);
                return logEntries.Count == 0 ? "No activity log entries yet." : string.Join("\n", logEntries);
            }

            foreach (var kv in sentimentResponses)
                if (input.Contains(kv.Key)) return kv.Value;

            if (input.Contains("name is"))
            {
                var parts = input.Split(new[] { "name is" }, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    userName = parts[1].Trim();
                    return $"Nice to meet you, {userName}!";
                }
            }

            var matchedTopics = DetectMultipleTopics(input);
            if (matchedTopics.Count > 0)
            {
                string combinedResponse = "";
                foreach (var topic in matchedTopics)
                {
                    if (responseBank.TryGetValue(topic, out var responses))
                    {
                        lastTopic = topic;
                        Log($"Discussed topic: {topic}");
                        combinedResponse += $"{topic.ToUpper()}: {responses[random.Next(responses.Length)]}\n\n";
                    }
                }
                return combinedResponse.Trim();
            }

            return $"Sorry {userName}, I didn’t understand that.";
        }

        private bool TryParseReminder(string input, out DateTime reminder)
        {
            reminder = DateTime.Now;
            var parts = input.Split(' ');
            if (parts.Length >= 3 && parts[0] == "in" && int.TryParse(parts[1], out int num))
            {
                if (parts[2].StartsWith("day")) { reminder = DateTime.Now.AddDays(num); return true; }
                if (parts[2].StartsWith("hour")) { reminder = DateTime.Now.AddHours(num); return true; }
            }
            return false;
        }
    }

    public partial class ChatWindow : Window
    {
        private ChatBot bot = new ChatBot();
        private string userName = "friend";

        public ChatWindow()
        {
            InitializeComponent();
            chatbotOutput.AppendText("\uD83E\uDD16 Hello! I’m your Cybersecurity Assistant.\n");
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = userInputBox.Text.Trim();
            if (!string.IsNullOrEmpty(userInput))
            {
                chatbotOutput.AppendText($"\n\uD83E\uDDD1\u200D\uD83D\uDCBB You: {userInput}");
                string response = bot.GetResponse(userInput, ref userName);
                chatbotOutput.AppendText($"\n🤖 CyberBot: {response}\n");
                userInputBox.Clear();
                chatbotOutput.ScrollToEnd();
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();  // create main menu window
            mainWindow.Show();                         // show main menu window
            this.Close();                             // close current chat window
        }

        private void chatbotOutput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) { }
    }
}

