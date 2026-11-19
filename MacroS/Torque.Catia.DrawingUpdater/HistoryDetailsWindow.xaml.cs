using System.Windows;

namespace Torque.Catia.DrawingUpdater;

public partial class HistoryDetailsWindow : Window
{
    public HistoryDetailsWindow(MainWindow.HistoryItem item)
    {
        InitializeComponent();
        DataContext = item;
        if (item.Status == "Sucesso")
        {
            StatusBadge.Background = System.Windows.Media.Brushes.Honeydew;
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 101, 52));
        }
        else if (item.Status == "Atenção")
        {
            StatusBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 243, 199));
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(146, 64, 14));
        }
        else
        {
            StatusBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 226, 226));
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(153, 27, 27));
        }
    }
}
