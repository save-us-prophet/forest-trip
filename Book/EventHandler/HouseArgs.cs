using ShareInvest.Models;

using System;

namespace ShareInvest.EventHandler;

class HouseArgs : EventArgs
{
    internal ForestRetreat Item
    {
        get;
    }

    internal HouseArgs(ForestRetreat item) => Item = item;
}