using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareInvest.Models;

public class Cabin
{
    [Key]
    public string? Id
    {
        get; set;
    }

    [Key]
    public string? Name
    {
        get; set;
    }

    [NotMapped]
    public string? Region
    {
        get; set;
    }

    [NotMapped]
    public string? Resort
    {
        get; set;
    }
}