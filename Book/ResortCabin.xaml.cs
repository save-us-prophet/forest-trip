using ShareInvest.Models;

using System.Collections.Generic;
using System.Windows;

namespace ShareInvest;

partial class ResortCabin : Window
{
    internal ResortCabin(IEnumerable<Cabin> cabins)
    {
        InitializeComponent();

        cabin.ItemsSource = cabins;

        cabin.SelectionChanged += (sender, e) =>
        {
            if (cabin.SelectedIndex >= 0)
            {
                SelectedCabin = cabin.SelectedValue as Cabin;

                DialogResult = SelectedCabin != null;

                Close();
            }
        };
    }

    internal Cabin? SelectedCabin
    {
        get; private set;
    }
}