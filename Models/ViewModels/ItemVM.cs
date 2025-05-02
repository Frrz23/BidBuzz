using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Models.ViewModels;
using Models;
using Utility;

public class ItemVM
{
    // Existing properties
    public Item Item { get; set; }
    public int ItemId { get; set; }
    public AuctionStatus? AuctionStatus { get; set; }
    public string? UserName { get; set; }
    public List<Bid> BidList { get; set; } = new();
    public decimal HighestAmount { get; set; }

    [ValidateNever]
    public BidVM? BidModel { get; set; } = new();
    public int RemainingRelistAttempts { get; set; }

    // Add auto bid properties
    public bool HasActiveAutoBid { get; set; }
    public decimal? MaxAutoBidAmount { get; set; }
    public bool IsOwner { get; set; }
}