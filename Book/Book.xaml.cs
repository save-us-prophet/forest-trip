﻿using Microsoft.EntityFrameworkCore;

using ShareInvest.Data;
using ShareInvest.EventHandler;
using ShareInvest.Models;
using ShareInvest.ViewModels;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ShareInvest;

public partial class Book : Window
{
    public Book()
    {
        menu = new System.Windows.Forms.ContextMenuStrip
        {
            Cursor = System.Windows.Forms.Cursors.Hand
        };
        menu.Items.AddRange([
            new System.Windows.Forms.ToolStripMenuItem
            {
                Name = nameof(Properties.Resources.EXIT),
                Text = Properties.Resources.EXIT
            }
        ]);
        menu.ItemClicked += (sender, e) =>
        {
            Visibility = Visibility.Hidden;

            Close();
        };
        notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            ContextMenuStrip = menu,
            BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info,
            Icon = Properties.Resources.ICON
        };
        notifyIcon.MouseDoubleClick += (sender, _) =>
        {
            if (!IsVisible)
            {
                Show();

                WindowState = WindowState.Normal;

                notifyIcon.Visible = false;
            }
        };
        InitializeComponent();

        webView = new CoreWebView(webView2);

        np.Text = Convert.ToString(NumberOfPeople);

        webView.Send += (sender, e) =>
        {
            switch (e)
            {
                case HouseArgs h when !string.IsNullOrEmpty(h.Item.Region) && houses.TryGetValue(h.Item.Region, out List<ForestRetreat>? list):

                    if (!string.IsNullOrEmpty(h.Item.Id) && !list.Any(e => h.Item.Id.Equals(e.Id)))
                    {
                        list.Add(h.Item);
                    }
                    return;

                case LocationArgs l when !loc.Items.Cast<ComboBoxItem>().Any(item => l.Item.LocName.Equals(item.Content.ToString())):
                    loc.Items.Add(new ComboBoxItem
                    {
                        Content = l.Item.LocName,
                        TabIndex = l.Item.Count,
                        ContentTemplate = (DataTemplate)FindResource("LocItem")
                    });

                    if (houses.ContainsKey(l.Item.LocName) is false)
                    {
                        ForestRetreat[] resorts = [];

                        using (var context = new ForestTripContext())
                        {
                            resorts = [.. from fr in context.ForestRetreat.AsNoTracking()
                                          where l.Item.LocName.Equals(fr.Region)
                                          select fr];
                        }
                        houses[l.Item.LocName] = new List<ForestRetreat>(resorts);
                    }
                    return;

                case IntervalArgs i when Math.Abs(i.Interval.TotalSeconds) > 5:
#if DEBUG

#else
                    using (MemoryStream ms = new(Properties.Resources.BEEP))
                    {
                        using (SoundPlayer sp = new(ms))
                        {
                            sp.PlaySync();
                        }
                    }
#endif
                    return;
            }
        };
        reservation.Send += (sender, e) =>
        {
            notifyIcon.Text = $"{e:G}";
        };
        _ = webView.OnInitializedAsync(Properties.Resources.DOMAIN);
    }

    void OnRegionHouseClick(object sender, RoutedEventArgs e)
    {
        if (loc.SelectedIndex >= 0)
        {
            var key = (loc.SelectedValue as ComboBoxItem)?.Content.ToString();

            if (!string.IsNullOrEmpty(key) && houses.TryGetValue(key, out List<ForestRetreat>? items) && items.Count > 0)
            {
                var page = new RegionHouse(items)
                {
                    Owner = this
                };

                if (page != null && page.ShowDialog() is bool result && result)
                {
                    DataContext = new BookViewModel
                    {
                        House = new HouseViewModel
                        {
                            SelectedHouse = page.SelectedHouse
                        },
                        DateRange = (DataContext as BookViewModel)?.DateRange
                    };
                }
            }
        }
    }

    void OnClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DatePicker dp && dp.Template.FindName("sc", dp) is Popup p)
        {
            p.IsOpen = p.IsOpen is false;
        }
    }

    [SupportedOSPlatform("windows")]
    void OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is BookViewModel vm && vm.DateRange != null && vm.House != null)
        {
            var region = (loc.SelectedValue as ComboBoxItem)?.Content.ToString();
            var forestRetreat = string.Concat(vm.House.SelectedHouse?.Classification, vm.House.SelectedHouse?.Name);

            DateTime startDate = vm.DateRange.StartDate, endDate = vm.DateRange.EndDate;

            if (!string.IsNullOrEmpty(region) && houses.TryGetValue(region, out List<ForestRetreat>? list) && list.Any(e => !string.IsNullOrEmpty(e.Name) && e.Name.Equals(forestRetreat)))
            {
                var policy = string.Empty;

                Cabin[] cabins = [];

                using (var context = new ForestTripContext())
                {
                    if (context.ForestRetreat.AsNoTracking().FirstOrDefault(e => forestRetreat.Equals(e.Name)) is ForestRetreat fr && !string.IsNullOrEmpty(fr.Id))
                    {
                        cabins = [.. from cabin in context.Cabin.AsNoTracking()
                                     where  fr.Id.Equals(cabin.Id)
                                     select cabin];

                        policy = context.Policy.AsNoTracking().FirstOrDefault(e => fr.Id.Equals(e.ResortId))?.Reservation;
                    }
                }

                if (cabins.Length > 0)
                {
                    var page = new ResortCabin(cabins)
                    {
                        Owner = this
                    };

                    if (page != null && page.ShowDialog() is bool result && result)
                    {
                        using (var context = new ForestTripContext())
                        {
                            var reservation = new Reservation
                            {
                                Policy = policy,
                                NumberOfPeople = NumberOfPeople,
                                StartDate = startDate,
                                EndDate = endDate,
                                Region = region,
                                CabinName = page.SelectedCabin?.Name,
                                ForestRetreat = forestRetreat
                            };

                            if (context.Reservations.Find(startDate, forestRetreat, page.SelectedCabin?.Name) is Reservation rs)
                            {
                                if ((this.reservation.DataContext as ReservationViewModel)?.Reservations?.Remove(reservation) is bool)
                                {
                                    rs.EndDate = endDate;
                                    rs.Region = region;
                                    rs.NumberOfPeople = NumberOfPeople;
                                    rs.Policy = policy;
                                }
                            }
                            else
                            {
                                context.Reservations.Add(reservation);
                            }

                            if (context.SaveChanges() > 0)
                            {
                                using (MemoryStream ms = new(Properties.Resources.BINGO))
                                {
                                    using (SoundPlayer sp = new(ms))
                                    {
                                        sp.PlaySync();
                                    }
                                }
                                reservation.Resort = new House
                                {
                                    Name = forestRetreat?[1..],
                                    Classification = $"{forestRetreat?[0]}",
                                    BackgroudColor = (new BrushConverter().ConvertFromString(forestRetreat?[0] switch
                                    {
                                        '공' => "#5468C7",
                                        '국' => "#008504",
                                        _ => "#AB49AF"
                                    }) as SolidColorBrush) ?? Brushes.Navy
                                };
                                (this.reservation.DataContext as ReservationViewModel)?.Reservations?.Add(reservation);
                            }
                        }
                    }
                }
            }
        }
    }

    void OnLoaded(object sender, RoutedEventArgs _)
    {
        if (sender is DatePicker picker && FindVisualChild<Popup>(picker) is Popup popup)
        {
            popup.Opened += (sender, _) =>
            {
                if (sender is Popup p && p.Child is Border { Child: Calendar calendar })
                {
                    calendar.DisplayDateStart = DateTime.Today;
                }
            };
        }
    }

    void SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is Calendar calendar)
        {
            DateTime startDate, endDate;

            switch (e.AddedItems.Count)
            {
                case > 1:
                    var items = e.AddedItems.Cast<DateTime>();

                    var sortedItems = items.OrderBy(e => e);

                    startDate = sortedItems.First();
                    endDate = sortedItems.Last();
                    break;

                case 1:
                    var reservationDate = e.AddedItems.Cast<DateTime>().First();

                    startDate = reservationDate;
                    endDate = reservationDate.AddDays(1);
                    break;

                default:
                    return;
            }

            if (calendar.Parent is Border b && b.Parent is Popup p)
            {
                p.IsOpen = false;
            }
            DataContext = new BookViewModel
            {
                DateRange = new DateRangeViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate
                },
                House = (DataContext as BookViewModel)?.House
            };
        }
    }

    void OnIncreaseClick(object _, RoutedEventArgs e)
    {
        if (NumberOfPeople > 0x20)
        {
            return;
        }
        np.Text = Convert.ToString(++NumberOfPeople);
    }

    void OnDecreaseClick(object _, RoutedEventArgs e)
    {
        if (NumberOfPeople < 2)
        {
            return;
        }
        np.Text = Convert.ToString(--NumberOfPeople);
    }

    void OnStateChanged(object sender, EventArgs _)
    {
        if (WindowState.Minimized == WindowState)
        {
            notifyIcon.Visible = true;

            Hide();
        }
    }

    void OnClosing(object _, CancelEventArgs e) => GC.Collect();

    T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);

            if (child is T obj)
            {
                return obj;
            }

            if (FindVisualChild<T>(child) is T childOfChild)
            {
                return childOfChild;
            }
        }
        return null;
    }

    int NumberOfPeople
    {
        get; set;
    }
        = 4;

    readonly CoreWebView webView;

    readonly Dictionary<string, List<ForestRetreat>> houses = [];

    readonly System.Windows.Forms.NotifyIcon notifyIcon;
    readonly System.Windows.Forms.ContextMenuStrip menu;
}