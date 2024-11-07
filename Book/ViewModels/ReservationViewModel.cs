using Microsoft.EntityFrameworkCore;

using ShareInvest.Data;
using ShareInvest.Models;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace ShareInvest.ViewModels;

public class ReservationViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<Reservation>? Reservations
    {
        get; private set;
    }

    public ReservationViewModel()
    {
        using (var context = new ForestTripContext())
        {
            context.Reservations.Load();

            Reservations = new ObservableCollection<Reservation>(from r in context.Reservations.Local
                                                                 where DateTime.Now.CompareTo(r.StartDate) < 0
                                                                 orderby r.StartDate ascending
                                                                 select new Reservation
                                                                 {
                                                                     CabinName = r.CabinName,
                                                                     ForestRetreat = r.ForestRetreat,
                                                                     NumberOfPeople = r.NumberOfPeople,
                                                                     Region = r.Region,
                                                                     Resort = new House
                                                                     {
                                                                         Name = r.ForestRetreat?[1..],
                                                                         Classification = $"{r.ForestRetreat?[0]}",
                                                                         BackgroudColor = (new BrushConverter().ConvertFromString(r.ForestRetreat?[0] switch
                                                                         {
                                                                             '공' => "#5468C7",
                                                                             '국' => "#008504",
                                                                             _ => "#AB49AF"
                                                                         }) as SolidColorBrush) ?? Brushes.Navy
                                                                     },
                                                                     StartDate = r.StartDate,
                                                                     EndDate = r.EndDate,
                                                                     Policy = r.Policy
                                                                 });
        }
    }

    public void Remove(Reservation reservation)
    {
        using (var context = new ForestTripContext())
        {
            context.Reservations.Remove(reservation);

            if (context.SaveChanges() > 0) Reservations?.Remove(reservation);
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}