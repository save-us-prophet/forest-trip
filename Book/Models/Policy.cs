using System.ComponentModel.DataAnnotations;

namespace ShareInvest.Models;

public class Policy
{
    [Key]
    public string? ResortId
    {
        get; set;
    }

    [Required]
    public string? ResortName
    {
        get; set;
    }

    public string? Reservation
    {
        get; set;
    }

    public bool Cabin
    {
        get; set;
    }

    public bool Campsite
    {
        get; set;
    }

    public bool Wait
    {
        get; set;
    }
}