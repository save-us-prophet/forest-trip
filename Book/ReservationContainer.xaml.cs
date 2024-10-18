using ShareInvest.Models;
using ShareInvest.ViewModels;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShareInvest;

public partial class ReservationContainer : UserControl
{
    public ReservationContainer()
    {
        InitializeComponent();

        if (new ReservationViewModel() is ReservationViewModel vm)
        {
            DataContext = vm;

            reservation.ItemsSource = vm.Reservations?.Select(e =>
            {
                e.Resort = new House
                {
                    Name = e.ForestRetreat?[1..],
                    Classification = $"{e.ForestRetreat?[0]}",
                    BackgroudColor = (new BrushConverter().ConvertFromString(e.ForestRetreat?[0] switch
                    {
                        '공' => "#5468C7",
                        '국' => "#008504",
                        _ => "#AB49AF"
                    }) as SolidColorBrush) ?? Brushes.Navy
                };
                return e;
            });
        }
    }

    void OnClick(object sender, RoutedEventArgs e)
    {

    }
}