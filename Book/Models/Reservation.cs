using System.ComponentModel.DataAnnotations;

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
}