using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace DevERP.Desktop.Views;

public partial class PinLoginWindow : FluentWindow
{
    private readonly string _expectedPin;
    private string _currentPin = string.Empty;

    public bool IsUnlocked { get; private set; }

    private static readonly SolidColorBrush BrushEmptyBg = new(Color.FromRgb(0x16, 0x22, 0x38));
    private static readonly SolidColorBrush BrushEmptyBorder = new(Color.FromRgb(0x33, 0x48, 0x6E));
    private static readonly SolidColorBrush BrushFilled = new(Color.FromRgb(0x38, 0xBD, 0xF8));
    private static readonly SolidColorBrush BrushSuccess = new(Color.FromRgb(0x10, 0xB9, 0x81));
    private static readonly SolidColorBrush BrushError = new(Color.FromRgb(0xEF, 0x44, 0x44));

    public PinLoginWindow(string expectedPin, string developerName = "Eng. Ibrahim Tarek", string? professionalTitle = null)
    {
        InitializeComponent();
        _expectedPin = string.IsNullOrWhiteSpace(expectedPin) ? "1234" : expectedPin;

        if (!string.IsNullOrWhiteSpace(developerName))
        {
            TxtDeveloperName.Text = developerName.StartsWith("Eng.", StringComparison.OrdinalIgnoreCase) 
                ? developerName 
                : $"Eng. {developerName}";
        }

        if (_expectedPin != "1234")
        {
            TxtHint.Text = "Secured with DevERP Local Security PIN";
        }

        Loaded += (s, e) =>
        {
            Focus();
            UpdateDots();
        };
    }

    private void KeypadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement btn && btn.Tag is string digit)
        {
            AppendDigit(digit[0]);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPin();
    }

    private void BackspaceButton_Click(object sender, RoutedEventArgs e)
    {
        Backspace();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key >= Key.D0 && e.Key <= Key.D9)
        {
            char digit = (char)('0' + (e.Key - Key.D0));
            AppendDigit(digit);
            e.Handled = true;
        }
        else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
        {
            char digit = (char)('0' + (e.Key - Key.NumPad0));
            AppendDigit(digit);
            e.Handled = true;
        }
        else if (e.Key == Key.Back || e.Key == Key.Delete)
        {
            Backspace();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            if (_currentPin.Length > 0)
            {
                ClearPin();
            }
            else
            {
                DialogResult = false;
                Close();
            }
            e.Handled = true;
        }
    }

    private void AppendDigit(char digit)
    {
        if (_currentPin.Length >= 4)
            return;

        TxtErrorMessage.Text = string.Empty;
        _currentPin += digit;
        UpdateDots();

        if (_currentPin.Length == 4)
        {
            ValidatePin();
        }
    }

    private void Backspace()
    {
        if (_currentPin.Length > 0)
        {
            _currentPin = _currentPin[..^1];
            TxtErrorMessage.Text = string.Empty;
            UpdateDots();
        }
    }

    private void ClearPin()
    {
        _currentPin = string.Empty;
        TxtErrorMessage.Text = string.Empty;
        UpdateDots();
    }

    private void UpdateDots()
    {
        SetDotState(Dot1, _currentPin.Length >= 1);
        SetDotState(Dot2, _currentPin.Length >= 2);
        SetDotState(Dot3, _currentPin.Length >= 3);
        SetDotState(Dot4, _currentPin.Length >= 4);
    }

    private void SetDotState(Border dot, bool filled)
    {
        dot.Background = filled ? BrushFilled : BrushEmptyBg;
        dot.BorderBrush = filled ? BrushFilled : BrushEmptyBorder;
    }

    private void ValidatePin()
    {
        if (_currentPin == _expectedPin)
        {
            // Success
            Dot1.Background = Dot1.BorderBrush = BrushSuccess;
            Dot2.Background = Dot2.BorderBrush = BrushSuccess;
            Dot3.Background = Dot3.BorderBrush = BrushSuccess;
            Dot4.Background = Dot4.BorderBrush = BrushSuccess;

            IsUnlocked = true;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                DialogResult = true;
                Close();
            };
            timer.Start();
        }
        else
        {
            // Failed PIN
            Dot1.Background = Dot1.BorderBrush = BrushError;
            Dot2.Background = Dot2.BorderBrush = BrushError;
            Dot3.Background = Dot3.BorderBrush = BrushError;
            Dot4.Background = Dot4.BorderBrush = BrushError;

            TxtErrorMessage.Text = "Incorrect PIN. Please try again.";

            ShakeDots();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                _currentPin = string.Empty;
                UpdateDots();
            };
            timer.Start();
        }
    }

    private void ShakeDots()
    {
        var transform = new TranslateTransform();
        PinDotsPanel.RenderTransform = transform;

        var animation = new DoubleAnimationUsingKeyFrames();
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(-12, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(50))));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(12, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150))));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(-4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250))));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));
        animation.Duration = TimeSpan.FromMilliseconds(300);

        transform.BeginAnimation(TranslateTransform.XProperty, animation);
    }
}
