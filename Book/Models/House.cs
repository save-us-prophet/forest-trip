using System.ComponentModel;
using System.Windows.Media;

namespace ShareInvest.Models;

public class House : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string? Classification
    {
        get; set;
    }

    public string? Name
    {
        get; set;
    }

    public Brush? BackgroudColor
    {
        get; set;
    }

    public Brush BorderBrush
    {
        set
        {
            brush = value;

            OnPropertyChanged(nameof(BorderBrush));
        }
        get => brush;
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    Brush brush = Brushes.Transparent;
}