using Newtonsoft.Json;

using System.Runtime.Serialization;

namespace ShareInvest.Models;

internal struct Region
{
    [DataMember, JsonProperty("title")]
    internal string Title
    {
        get; set;
    }

    [DataMember, JsonProperty("text")]
    internal string Text
    {
        get; set;
    }
}