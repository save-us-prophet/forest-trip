using ShareInvest.Models;
using ShareInvest.ViewModels;

using System;
using System.IO;
using System.Media;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ShareInvest;

public partial class ReservationContainer : UserControl
{
    public event EventHandler<DateTime>? Send;

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

                _ = Task.Run(async () => await ExecuteAsync());
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

    CancellationTokenSource? CancellationTokenSource
    {
        get; set;
    }

    [SupportedOSPlatform("windows")]
    async Task ExecuteAsync(bool isSign = false)
    {
        foreach (Reservation reservation in reservation.Items)
        {
            if (!await IsPolicy(reservation.StartDate, reservation.Policy)) continue;

            using (var rs = new ReservationService(Properties.Resources.DOMAIN, args: isSign ? [Properties.Resources.HEADLESS] : []))
            {
                reservation.Resort!.BorderBrush = Brushes.DimGray;

                if (await rs.EnterInfomationAsync(reservation) is Reservation b)
                {
                    isSign =
#if DEBUG
                        false;
#else
                        true;
#endif
                    reservation.Resort!.BorderBrush = Brushes.Transparent;

                    if (b.Result)
                    {
                        (DataContext as ReservationViewModel)?.Remove(b);

                        using (MemoryStream ms = new(Properties.Resources.MARIO))
                        {
                            using (SoundPlayer sp = new(ms))
                            {
                                sp.PlaySync();
                            }
                        }
                        break;
                    }
                    continue;
                }
                reservation.Resort!.BorderBrush = Brushes.Transparent;

                await Task.Delay(0x400 * 0x40 * 0xA);
            }
            await Task.Delay(0x400);
        }
        var now = DateTime.Now;

        var nextTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 1).AddMinutes(1);

        Send?.Invoke(this, nextTime);

        await Task.Delay(nextTime - DateTime.Now);

        if (reservation.Items.Count > 0 && CancellationTokenSource?.Token.IsCancellationRequested is false)
        {
            _ = Task.Run(async () => await ExecuteAsync(isSign));
        }
        else
        {
            CancellationTokenSource?.Dispose();

            CancellationTokenSource = null;

            btn.IsChecked = null;
        }
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

    static async Task<bool> IsPolicy(DateTime startDate, string? policy)
    {
        if (string.IsNullOrEmpty(policy) is false)
        {
            var now = DateTime.Now;

            switch (policy[0..2])
            {
                case "매월" when now.Month == startDate.Month:
                    return true;

                case "매월" when now.AddMonths(1).Month == startDate.Month && int.TryParse(policy[2..4].Replace("일", string.Empty), out int day):

                    if (day <= now.Day)
                    {
                        if (day == now.Day && int.TryParse(policy[^5..^3].Replace("일", string.Empty), out int hour) && hour == now.Hour + 1)
                        {
                            await Task.Delay(new DateTime(now.Year, now.Month, now.Day, hour, 0, 1) - DateTime.Now);
                        }
                        return true;
                    }
                    break;

                case "매월":

                    break;

                default:

                    if (policy[^3] == '시')
                    {

                    }
                    break;
            }
        }
        return false;
    }
}