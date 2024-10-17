using Newtonsoft.Json;

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace ShareInvest.Models;

public class ForestRetreat
{
    [DataMember, JsonProperty("id"), Key]
    public string? Id
    {
        get; set;
    }

    [DataMember, JsonProperty("text"), Required]
    public string? Name
    {
        get; set;
    }

    [DataMember, JsonProperty("region"), Required]
    public string? Region
    {
        get; set;
    }
}