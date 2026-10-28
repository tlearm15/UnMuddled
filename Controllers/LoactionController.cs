using GroupProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.Metrics;
using System.Runtime.Intrinsics.X86;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace GroupProject.Controllers
{
    public class LoactionController : Controller
    {

        //DMQ - making the dbcontext a read-only property
        private UnMuddledDbContext _dbContext;


        //DMQ - Dependency Injection for the database context
        public LoactionController(UnMuddledDbContext dbContext)
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

            //get the beggining of the users postcode
            string userOutcode = user.PostCode.Split(' ')[0].Trim().ToUpper();

            //makes a lsit of the disposal sites near the user
            var nearbyLocations = await _dbContext.DisposalLocations
            .Where(d => d.Postcode.ToUpper().StartsWith(userOutcode))
            .ToListAsync();

            //adds the search item name to the viewbag
            ViewBag.ItemName = item.ItemName;


            //returns the search query to the view
            return View("Index", nearbyLocations);
        }//end of action result


        /// <summary>
        /// DMQ - allows the user to view the location details
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IActionResult LocationDetails(int? id)
        {

            //checks if the id cannot be found
            if(id == null)
            {
                return NotFound();
            }

            //finds the location by id
            var disposalLocation = _dbContext.DisposalLocations.FirstOrDefault(dl => dl.DisposalID == id);

            //checks if the location was found
            if(disposalLocation == null)
            {
                return NotFound();
            }

            //retruns location details to view
            return View(disposalLocation);
        }


        /// <summary>
        /// DMQ - creates a new location within the database
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles ="Staff")]
        public async Task<IActionResult> CreateLocation()
        {
            //creates a new disposla location
           DisposalLocation disposalLocation = new DisposalLocation();

            //returns the new location object to view
            return View(disposalLocation);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLocationConfirmed(
            [Bind("DisposalID,LocationName,AddressLine1,AddressLine2,Town,Postcode,Latitude,Longitude")] DisposalLocation disposalLocation)
        {
            //if the model state is valid - then save the bound variables to the database
            if (ModelState.IsValid)
            {
                _dbContext.DisposalLocations.Add(disposalLocation);

               await _dbContext.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            //if the model state was NOT valid - return to view
            return View(disposalLocation);
        }



        /// <summary>
        /// DMQ - Deletes the location from the database
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles ="Staff")]
        public async Task<IActionResult> DeleteLocation(int? id)
        {
            //if the id is null - then return user to not found form
            if(id ==null)
            {
                return NotFound();
            }

            //finds the loctation through the database
            var location = await _dbContext.DisposalLocations.FindAsync(id);

            //if the location is null - then return user to not found form
            if (location == null)
            {
                return NotFound();
            }

            //sends the location to the view
            return View(location);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocationConfirmed(int? id)
        {

            //if the id is null - then return user to not found form
            if (id == null)
            {
                return NotFound();
            }

            var location = await _dbContext.DisposalLocations.FindAsync(id);

            //if the location is null - then return user to not found form
            if (location == null)
            {
                return NotFound();
            }

            //removes the location from the database & saves the change
            _dbContext.DisposalLocations.Remove(location);
            await _dbContext.SaveChangesAsync();

            //redirects the user to the view
            return RedirectToAction("Index");
        }


    }//end of controller
}//end of namespace
