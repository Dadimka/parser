using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using ToCCourseWork.Service;
using ToCCourseWork.Entity;
using System.Diagnostics;


namespace ToCCourseWork
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string currentFilePath;
        private string _lastSavedText;
        private bool _isCommand = false;
        private UndoStack _undoStack = new UndoStack();
        private RedoStack _redoStack = new RedoStack();

        public event PropertyChangedEventHandler? PropertyChanged;

        // Регулярное выражение для поиска ФИО (фамилия и инициалы)
        private string pattern = @"([А-ЯЁ][а-яё]{1,}(-[А-ЯЁ][а-яё]{1,})?(ов|ова|ин|ина|ий|ая|ой))\s*[А-ЯЁ]\.\s*[А-ЯЁ]\.|[А-ЯЁ]\.\s*[А-ЯЁ]\.\s*([А-ЯЁ][а-яё]{1,}(-[А-ЯЁ][а-яё]{1,})?(ов|ова|ин|ина|ий|ая|ой))";
        public UndoStack UndoStack
        {
            get => _undoStack;
            private set
            {
                _undoStack = value;
                OnPropertyChanged(nameof(UndoStack));
            }
        }

        public RedoStack RedoStack
        {
            get => _redoStack;
            private set
            {
                _redoStack = value;
                OnPropertyChanged(nameof(RedoStack));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            currentFilePath = string.Empty;
            _undoStack = new UndoStack();
            _redoStack = new RedoStack();
            DataContext = this;
            // Устанавливаем обработчик события изменения текста
            TextEditor.PreviewKeyDown += TextEditor_PreviewKeyDown;
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                RunSyntaxCheck(sender, e); // Или любой другой метод
                e.Handled = true; // Предотвращаем дальнейшую обработку
            }
        }
        // Обработчик нажатия клавиш
        private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, была ли нажата клавиша Enter или Tab
            if (e.Key == Key.Enter || e.Key == Key.Back || e.Key == Key.Tab || e.Key == Key.Space)
            {
                // Добавляем текущее состояние текста в стек отмены
                _undoStack.Push(TextEditor.Text);
                _redoStack.Clear(); // Очищаем стек повторов при изменении текста
            }
        }



        private void RunSyntaxCheck(object sender, RoutedEventArgs e)
        {
            /*------------------------Лаба----------------------------*/
            ErrorOutput.Clear();
            TokenOutput.Clear();
            Lexer lexer = new();

            string inputText = TextEditor.Text;
            var outputTokens = lexer.GetTokens(inputText);

            List<Error> errors = lexer.Errors;

            if (outputTokens.Count >= 0)
            {
                foreach (var token in outputTokens)
                {
                    TokenOutput.AppendText(token.ToString() + "\n");
                }
            }


            Parser parser = new Parser(outputTokens, errors);

            List<Error> parsedErrors = parser.Parse();


            if (parsedErrors.Count >= 0)
            {
                foreach (var error in parsedErrors)
                {
                    ErrorOutput.AppendText($"Ошибка: {error.ErrorText} в строке {error.Line}, столбце {error.Column}\n");
                }
            }

        }
        private void HighlightErrors(string errors)
        {
            if (string.IsNullOrWhiteSpace(errors))
            {
                TextEditor.Background = Brushes.White;
                return;
            }

            TextEditor.Background = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0)); // Легкий красный оттенок
        }

        /*По курсачу*/
        private void NewFile(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentFilePath) || !string.IsNullOrEmpty(TextEditor.Text))
            {
                MessageBoxResult result = MessageBox.Show(
                    "Хотите сохранить файл перед созданием нового файла?",
                    "Подтверждение",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveFile(sender, e);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            TextEditor.Clear();
            currentFilePath = string.Empty;

            // Очищаем стеки Undo и Redo
            _undoStack.Clear();
            _redoStack.Clear();
        }

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentFilePath) || !string.IsNullOrEmpty(TextEditor.Text))
            {
                MessageBoxResult result = MessageBox.Show(
                    "Хотите сохранить файл перед открытием нового файла?",
                    "Подтверждение",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveFile(sender, e);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                TextEditor.Text = File.ReadAllText(openFileDialog.FileName);
                currentFilePath = openFileDialog.FileName;

                // Очищаем стеки Undo и Redo
                _undoStack.Clear();
                _redoStack.Clear();
            }
        }

        private void SaveFile(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveFileAs(sender, e);
            }
            else
            {
                File.WriteAllText(currentFilePath, TextEditor.Text);

                // Очищаем стеки Undo и Redo после сохранения
                _undoStack.Clear();
                _redoStack.Clear();
            }
        }

        private void SaveFileAs(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt",
                DefaultExt = ".txt"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, TextEditor.Text);
                currentFilePath = saveFileDialog.FileName;
            }
        }

        private void ExitApp(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentFilePath) || !string.IsNullOrEmpty(TextEditor.Text))
            {

                // Спрашиваем у пользователя, хочет ли он сохранить изменения
                MessageBoxResult result = MessageBox.Show(
                    "У вас есть несохраненные изменения. Хотите сохранить файл перед выходом?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Если пользователь выбрал "Да", то сохраняем файл
                    SaveFile(sender, e);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    // Если пользователь выбрал "Отмена", то отменяем выход
                    return;
                }
            }

            // Закрываем приложение
            Application.Current.Shutdown();
        }

        private void CutText(object sender, RoutedEventArgs e)
        {
            _undoStack.Push(TextEditor.Text);
            _redoStack.Clear();
            TextEditor.Cut();
        }

        private void CopyText(object sender, RoutedEventArgs e)
        {
            TextEditor.Copy();
        }

        private void PasteText(object sender, RoutedEventArgs e)
        {
            _undoStack.Push(TextEditor.Text);
            _redoStack.Clear();
            TextEditor.Paste();
        }

        private void DeleteText(object sender, RoutedEventArgs e)
        {
            _undoStack.Push(TextEditor.Text);
            _redoStack.Clear();
            TextEditor.SelectedText = string.Empty;
        }

        private void SelectAllText(object sender, RoutedEventArgs e)
        {
            TextEditor.SelectAll();
        }

        private void UndoText(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count > 0)
            {
                int caretPos = TextEditor.CaretIndex; // Запоминаем позицию курсора
                _redoStack.Push(TextEditor.Text);
                _isCommand = true;
                TextEditor.Text = _undoStack.Pop();
                TextEditor.CaretIndex = Math.Min(caretPos, TextEditor.Text.Length); // Восстанавливаем позицию курсора
                _isCommand = false;
            }
        }

        private void RedoText(object sender, RoutedEventArgs e)
        {
            if (_redoStack.Count > 0)
            {
                int caretPos = TextEditor.CaretIndex; // Запоминаем позицию курсора
                _undoStack.Push(TextEditor.Text);
                _isCommand = true;
                TextEditor.Text = _redoStack.Pop();
                TextEditor.CaretIndex = Math.Min(caretPos, TextEditor.Text.Length); // Восстанавливаем позицию курсора
                _isCommand = false;
            }
        }

        private void ShowHelp(object sender, RoutedEventArgs e)
        {

            string helpFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help.html");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(helpFilePath) { UseShellExecute = true });
        }

        private void AboutApp(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Текстовый редактор на WPF\nРазработан для лабораторной работы.", "О программе");
        }

        private void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void ToggleMaximize(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void HighlightedText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Передаем фокус на TextEditor
            TextEditor.Focus();

            // Вызываем метод TextEditor_TextChanged
            TextEditor_TextChanged(sender, null);
        }

        private void TextEditor_TextChanged(object sender, TextChangedEventArgs e)
        {

            UpdateLineNumbers();
            LineNumbers.Height = TextEditor.ActualHeight;
            LineNumbersScroll.Height = TextEditorScroll.ActualHeight;
            TextEditor.Visibility = Visibility.Visible;
            //HighlightedText.Visibility = Visibility.Collapsed;
            if (!_isCommand && _lastSavedText != TextEditor.Text)
            {
                _undoStack.Push(_lastSavedText);
                _redoStack.Clear();
                _lastSavedText = TextEditor.Text;
            }
        }

        private void TextEditorScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Синхронизация вертикальной прокрутки
            LineNumbersScroll.ScrollToVerticalOffset(e.VerticalOffset);

            // Синхронизация горизонтальной прокрутки (если нужно)
            // LineNumbersScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        private void TextEditorScroll_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Синхронизация высоты при изменении размера
            LineNumbersScroll.Height = TextEditorScroll.ActualHeight;
            LineNumbers.Height = TextEditor.ActualHeight;
        }

        private void UpdateLineNumbers()
        {
            // Получаем текст из RichTextBox
            string text = TextEditor.Text;

            // Разделяем текст по строкам
            string[] lines = text.Split('\n');

            // Создаем строку для номеров строк
            StringBuilder lineNumbers = new StringBuilder();

            // Заполняем строку номерами строк
            for (int i = 1; i <= lines.Length; i++)
            {
                lineNumbers.AppendLine(i.ToString());  // Добавляем номер строки с новой строки
            }

            // Устанавливаем номера строк в TextBlock
            LineNumbers.Text = lineNumbers.ToString();
        }

        private void ClearAll(object sender, RoutedEventArgs e)
        {
            _undoStack.Push(TextEditor.Text);
            _redoStack.Clear();

            TextEditor.Clear();
            ErrorOutput.Clear();
        }


        private void GetRegularExpression(object sender, RoutedEventArgs e)
        {
            TextEditor.Text = pattern;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            RunSyntaxCheck(sender, e);
        }

        private void OpenTask(object sender, RoutedEventArgs e)
        {
            ShowHtmlPage("Resource/task.html");
        }

        private void OpenGrammar(object sender, RoutedEventArgs e)
        {
            ShowHtmlPage("Resource/grammar.html");
        }

        private void OpenGrammarClass(object sender, RoutedEventArgs e)
        {
            ShowHtmlPage("Resource/grammar_class.html");
        }

        private void OpenAnalyze(object sender, RoutedEventArgs e)
        {
            ShowHtmlPage("Resource/analyze.html");
        }

        private void OpenErrors(object sender, RoutedEventArgs e)
        {
            ShowHtmlPage("Resource/errors.html");
        }

        private void OpenTest(object sender, RoutedEventArgs e)
        {
            ShowHtmlPage("Resource/test.html");
        }

        private void OpenLit(object sender, RoutedEventArgs e)
        {
            ShowHtmlPage("Resource/lit.html");
        }

        private void OpenCode(object sender, RoutedEventArgs e)
        {
            ShowHtmlPage("Resource/code.html");
        }

        private void OpenHelp(object sender, RoutedEventArgs e)
        {
            ShowHtmlPage("Resource/help.html");
        }

        private void OpenAbout(object sender, RoutedEventArgs e)
        {
            ShowHtmlPage("Resource/about.html");
        }


        private void ShowHtmlPage(object parameter)
        {
            string htmlResourcePath = (string)parameter;
            try
            {
                string fileName = Path.GetFileName(htmlResourcePath);
                string tempPath = Path.Combine(Path.GetTempPath(), fileName);

                using (Stream resourceStream = Application.GetResourceStream(new Uri($"pack://application:,,,/{htmlResourcePath}")).Stream)
                using (Stream fileStream = File.Create(tempPath))
                {
                    resourceStream.CopyTo(fileStream);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии страницы: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }


    // Классы для хранения стека операций Undo и Redo
    public class UndoStack : INotifyPropertyChanged
    {
        private readonly Stack<string> _stack = new Stack<string>();

        public event PropertyChangedEventHandler PropertyChanged;

        public void Push(string text)
        {
            _stack.Push(text);
            OnPropertyChanged(nameof(Count));
        }

        public string Pop()
        {
            var result = _stack.Count > 0 ? _stack.Pop() : null;
            OnPropertyChanged(nameof(Count));
            return result;
        }

        public void Clear()
        {
            _stack.Clear();
            OnPropertyChanged(nameof(Count));
        }

        public int Count => _stack.Count;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RedoStack : INotifyPropertyChanged
    {
        private readonly Stack<string> _stack = new Stack<string>();

        public event PropertyChangedEventHandler PropertyChanged;

        public void Push(string text)
        {
            _stack.Push(text);
            OnPropertyChanged(nameof(Count));
        }

        public string Pop()
        {
            var result = _stack.Count > 0 ? _stack.Pop() : null;
            OnPropertyChanged(nameof(Count));
            return result;
        }

        public void Clear()
        {
            _stack.Clear();
            OnPropertyChanged(nameof(Count));
        }

        public int Count => _stack.Count;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}