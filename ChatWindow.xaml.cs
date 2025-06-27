using System;
using System.Windows;

namespace Chatbot
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChatButton_Click(object sender, RoutedEventArgs e)
        {
            ChatWindow chatWindow = new ChatWindow();
            chatWindow.Show();
            this.Close(); // Close main menu after opening chat
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "CyberSecurity Chatbot helps you learn and stay aware of online safety tips!\n\nDeveloped for Part 3 POE",
                "About",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
