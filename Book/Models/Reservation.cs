namespace ShareInvest.Models;

class Reservation
{
    internal int NumberOfPeople
    {
        get; set;
    }

    internal string? Region
    {
        get; set;
    }

    internal string? ForestRetreat
    {
        get; set;
    }

    internal string? CabinName
    {
        get; set;
    }

    internal DateTime StartDate
    {
        get; set;
    }

    internal DateTime EndDate
    {
        get; set;
    }
}