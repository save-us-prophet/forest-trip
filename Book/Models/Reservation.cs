using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareInvest.Models;

public class Reservation
{
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
        get; set;
    }
}