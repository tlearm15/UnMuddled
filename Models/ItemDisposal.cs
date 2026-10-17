using System.ComponentModel.DataAnnotations;

namespace GroupProject.Models;

public class ItemDisposal
{
    [Display(Name = "Disposal ID")]
    public int DisposalID { get; set; }
    public DisposalLocation? DisposalLocation { get; set; }

    [Display(Name = "Item ID")]
    public int ItemID { get; set; }
    public Item? Item { get; set; }

    //TL - Removed DisposalMethod property but kept as comment as leftover from DB Schema 
    //[Required(ErrorMessage = "Please specify the disposal method")]
    //[Display(Name = "Disposal Method")]
    //public DisposalMethod DisposalMethod { get; set; }
}