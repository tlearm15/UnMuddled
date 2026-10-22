using GroupProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.Metrics;
using System.Runtime.Intrinsics.X86;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace GroupProject.Controllers
{
    public class DisposalController : Controller
    {

        //DMQ - making the dbcontext a read-only property
        private UnMuddledDbContext _dbContext;


        //DMQ - Dependency Injection for the database context
        public DisposalController(UnMuddledDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// DAMQ - acts as the map apoint
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }


        /// <summary>
        /// DMQ - Locates the recycling and drop off locations near the user for their selected item
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="userid"></param>
        /// <returns>searchString</returns>
        public async Task<IActionResult> LocateDisposal(int? itemID, string userid)
        {
            //if the post code or item id cannot be found, then retrun a not found form
            if(itemID == null || userid == null)
            {
                return NotFound();
            }

            //locates the item via its id
            var item = await _dbContext.Items.FindAsync(itemID);

            //retrieves the user from the Users table by their id
            var user = await _dbContext.Users.FindAsync(userid);


            //if the item cannot be found, then retrun a not found form
            if (item == null || user == null)
            {
                return NotFound();
            }

            //retrieves the users postcode
            var postcode = user.PostCode;

            //combines the post code, and the item name into a "near me query"
            string searchString = $"{item.ItemName} drop off or recycling near me {postcode}";

            //adds the search item name to the viewbag
            ViewBag.ItemName = item.ItemName;

            //returns the search query to the view
            return View("Index",searchString);
        }//end of action result

     
    }//end of controller
}//end of namespace
