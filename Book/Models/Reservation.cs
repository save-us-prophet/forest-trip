using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareInvest.Models;

public class Reservation : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public int NumberOfPeople
    {
        get; set;
    }

    [Required]
    public string? Region
    {
        get; set;
    }

    [Key]
    public string? ForestRetreat
    {
        get; set;
    }

    [Key]
    public string? CabinName
    {
        get; set;
    }

    [Key]
    public DateTime StartDate
    {
        get; set;
    }

    public DateTime EndDate
    {
        get; set;
    }

    [NotMapped]
    public string StrStartDate
    {
        get => StartDate.ToString("d");
    }

    [NotMapped]
    public string StrEndDate
    {
        get => EndDate.ToString("d");
    }

    [NotMapped]
    public House? Resort
    {
        set
        {
            resort = value;

            OnPropertyChanged(nameof(Resort));
        }
        get => resort;
    }

    [NotMapped]
    public bool Result
    {
        get; set;
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    House? resort;
}