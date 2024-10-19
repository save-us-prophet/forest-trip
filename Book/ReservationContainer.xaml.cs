using ShareInvest.Models;
using ShareInvest.ViewModels;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

            reservation.ItemsSource = vm.Reservations;
        }
        btn.Checked += (sender, e) =>
        {
            if (CancellationTokenSource == null)
            {
                CancellationTokenSource = new CancellationTokenSource();

                _ = ExecuteAsync();
            }
        };
        btn.Unchecked += (sender, e) =>
        {
            if (CancellationTokenSource != null && CancellationTokenSource.Token.CanBeCanceled)
            {
                CancellationTokenSource.Cancel();
            }
        };
    }

    async Task ExecuteAsync()
    {
        var isSign = false;

        while (reservation.Items.Count > 0 && CancellationTokenSource?.Token.IsCancellationRequested is false)
        {
            foreach (Reservation reservation in reservation.Items)
            {
                reservation.Resort!.BorderBrush = Brushes.DimGray;

                using (var rs = new ReservationService(Properties.Resources.DOMAIN, args: isSign ? [Properties.Resources.HEADLESS] : []))
                {
                    if (await rs.EnterInfomationAsync(reservation) is Reservation b)
                    {
                        if (b.Result)
                        {
                            (DataContext as ReservationViewModel)?.Remove(b);
                        }
                        isSign = true;

                        reservation.Resort!.BorderBrush = Brushes.Transparent;
                        continue;
                    }
                    await Task.Delay(0x400 * 0x40 * 0xA);
                }
                await Task.Delay(0x400);
            }
            await Task.Delay(0x200);
        }
        CancellationTokenSource?.Dispose();

        CancellationTokenSource = null;

        btn.IsChecked = null;
    }

    void OnClick(object sender, MouseButtonEventArgs _)
    {
        if (this.reservation.SelectedIndex >= 0 && this.reservation.SelectedValue is Reservation reservation)
        {
            var result = MessageBox.Show($"예약 {reservation.CabinName}을 삭제하겠습니까?", reservation.Resort?.Name, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (MessageBoxResult.Yes == result)
            {
                (DataContext as ReservationViewModel)?.Remove(reservation);
            }
        }
    }

    CancellationTokenSource? CancellationTokenSource
    {
        get; set;
    }
}