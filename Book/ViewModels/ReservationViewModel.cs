using Microsoft.EntityFrameworkCore;

using ShareInvest.Data;
using ShareInvest.Models;

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ShareInvest.ViewModels;

public class ReservationViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<Reservation>? Reservations
    {
        set
        {
            reservations = value;

            OnPropertyChanged(nameof(Reservations));
        }
        get => reservations;
    }

    public ReservationViewModel()
    {
        using (var context = new ForestTripContext())
        {
            context.Reservations.Load();

            Reservations =
                new ObservableCollection<Reservation>(from r in context.Reservations.Local
                                                      where DateTime.Now.CompareTo(r.StartDate) < 0
                                                      orderby r.StartDate ascending
                                                      select r);
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    ObservableCollection<Reservation>? reservations;
}