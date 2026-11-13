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
using GroupProject.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GroupProject.Controllers
{
    public class LocationController : Controller
    {

        //DMQ - making the dbcontext a read-only property
        private readonly UnMuddledDbContext _dbContext;
        //private readonly UserManager<User> _userManager;

        //DMQ - Dependency Injection for the database context
        public LocationController(UnMuddledDbContext dbContext)
        {
            _dbContext = dbContext;
            //_userManager = _userManager;
        }

        /// <summary>
        /// DAMQ - acts as the map a point
        /// JM: - loads items from database, places them in Viewmodel & renders page with items
        /// </summary>
        /// <returns> JM: list of SelectListItem objects for item dropdown </returns>
        [HttpGet] 
        public async Task<IActionResult> Index() 
        {
            var model = new LocationViewModel
            {
                Items = await GetItemsListAsync(),
            };
            return View(model);
        }


        /// <summary>
        /// Finds recycling location using browser geolocation or text input
        /// </summary>
        /// <param name="model">submitted form data, contains selected item and location</param>
        /// <returns>Index view populated with location results or errors </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LocationViewModel model)
        {
            //JM: Repopulate item dropdown
            model.Items = await GetItemsListAsync();
            
            //JM: Makes sure latitude and longitude are submitted together, so recycling location can be found
            if (model.Latitude.HasValue != model.Longitude.HasValue)
            {
                ModelState.AddModelError("", "Incomplete location data. Please try sharing your location again.");
            }
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            //JM: If no item has been selected display error 
            if (!model.SelectedItemId.HasValue)
            {
                ModelState.AddModelError(nameof(model.SelectedItemId), "Please select an item to recycle.");
                return View(model);
            }
            
            //JM: Checks that the selected item exists in the database
            var item = await _dbContext.Items
                .AsNoTracking()
                .FirstOrDefaultAsync(i =>  i.ItemID == model.SelectedItemId!.Value);
            
            if (item == null)
            {
                ModelState.AddModelError(nameof(model.SelectedItemId), "The selected item does not exist.");
                return View(model);
            }
            
            model.ItemName = item.ItemName;
            
            //JM: Base query for locations with selected item
            var locationsQuery = _dbContext.DisposalLocations
                .AsNoTracking()
                .Where(d => d.ItemDisposals.Any(id => id.ItemID == model.SelectedItemId!.Value));
            
            // JM: Checks user shared their location & latitude and longitude were populated and 
            if (model is { Latitude: not null, Longitude: not null })
            {
                //JM: Pass locationsQuery to helper method to find nearest recycling locations
                model.Locations = await GetLocationsByCoordinatesAsync(
                    locationsQuery, 
                    (double)model.Latitude.Value, 
                    (double)model.Longitude.Value);
            }
            // JM: Otherwise checks user searched by entering town or postcode
            else if (!string.IsNullOrWhiteSpace(model.LocationSearch))
            {
                //JM: Pass locationsQuery to helper method to find locations with town name or postcode
                model.Locations = await GetLocationsBySearchAsync(
                    locationsQuery, 
                    model.LocationSearch);
            }
            else //JM: If user didn't select a location, return an error message
            {
                ModelState.AddModelError("", "Please enter a location or share your location.");
            }
            
            //JM: reload items list dropdown
            model.Items = await GetItemsListAsync();
            return View(model);
        }
        
        // JM:
        /// <summary>
        /// Calculates distance in miles between two geographic coordinates using Haversine formula
        /// </summary>
        /// <param name="lat1">Latitude of the first point</param>
        /// <param name="lon1">Longitude of the first point</param>
        /// <param name="lat2">Latitude of the second point</param>
        /// <param name="lon2">Longitude of the second point</param>
        /// <returns>Distance between two coordinates in miles</returns>
        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 3958.8; // JM: Earth radius in miles
            
            double dLat = double.DegreesToRadians(lat2 - lat1);
            double dLon = double.DegreesToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(double.DegreesToRadians(lat1)) * Math.Cos(double.DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            a = Math.Clamp(a, 0.0, 1.0);
            
            return earthRadius *  (2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
        }
        
        // JM:
        /// <summary>
        /// Filters closet user locations using 50-mile bounding box, then sorts them by Haversine distance
        /// </summary>
        /// <param name="query">Query of locations accepting the selected item</param>
        /// <param name="userLat">User's latitude</param>
        /// <param name="userLng">User's longitude</param>
        /// <returns>Up to 20 nearest Dispoal Locations ordered within 50 miles</returns>
        private async Task<List<DisposalLocation>> GetLocationsByCoordinatesAsync(IQueryable<DisposalLocation> query, double userLat, double userLng)
        {
            double latBounding = 0.72;
            double lngBounding = latBounding / Math.Cos(double.DegreesToRadians(userLat));
            
            decimal minLat = (decimal)(userLat - latBounding);
            decimal maxLat = (decimal)(userLat + latBounding);
            decimal minLng = (decimal)(userLng - Math.Abs(lngBounding));
            decimal maxLng = (decimal)(userLng + Math.Abs(lngBounding));

            var nearbyLocations = await query
                .Where(d => d.Latitude >= minLat && d.Latitude <= maxLat &&
                            d.Longitude >= minLng && d.Longitude <= maxLng)
                .ToListAsync();

            return nearbyLocations
                .Select(d => new 
                { 
                    Location = d, 
                    Distance = CalculateDistance(userLat, userLng, (double)d.Latitude, (double)d.Longitude) 
                })
                .Where(x => x.Distance <= 50)
                .OrderBy(x => x.Distance)
                .Take(20)
                .Select(x => x.Location)
                .ToList();
        }

        
        // JM
        /// <summary>
        /// Loads all records from _dbContext.Items & converts them into dropdown options.
        /// </summary>
        /// <returns> A collection of dropdown item options. </returns>
        private async Task<IEnumerable<SelectListItem>> GetItemsListAsync()
        {
            return await _dbContext.Items
                .AsNoTracking()
                .Select(i => new SelectListItem { Value = i.ItemID.ToString(), Text = i.ItemName })
                .ToListAsync();
        }

        // JM:
        /// <summary>
        /// Searches location of nearest recycling locations by the beginning of the postcode or town
        /// </summary>
        /// <param name="query">Query of recycling locations accepting the selected item</param>
        /// <param name="searchLocation"> The town or postcode the user entered</param>
        /// <returns>Up to 20 Disposal locations ordered alphabetically</returns>
        private async Task<List<DisposalLocation>> GetLocationsBySearchAsync(IQueryable<DisposalLocation> query, string searchLocation)
        {
            //prevent wildcard injection
            string searchEscape = searchLocation.Trim()
                .Replace(@"\", @"\\")
                .Replace("%", @"\%")
                .Replace("_", @"\_");
                
            string search = $"%{searchEscape.ToUpperInvariant()}%";

            // Return 20 max nearby recycling locations
            return await query
                .Where(d => EF.Functions.Like(d.Postcode.ToUpper(), search, @"\") ||
                            EF.Functions.Like(d.Town.ToUpper(), search, @"\"))
                .OrderBy(d => d.Town) // JM: Might change to  nearest location in future updates
                .ThenBy(d => d.LocationName)
                .ThenBy(d => d.DisposalID)
                .Take(20)
                .ToListAsync();
        }
        
        
        
        
        // JM: Not within scope to implement currently
        /// <summary>
        ///  Determines if user is authenticated and has a saved postcode or city
        /// </summary>
        /// <returns> true if user has saved postcode or city, otherwise false </returns>
        // private async Task<bool> HasSavedAddressAsync()
        // {
        //     // JM: Checks if user is logged in, if they aren't return false
        //     if (User.Identity is not { IsAuthenticated: true })
        //     {
        //         return false;
        //     }
        //     
        //     // JM: Extract UserId
        //     var userId = _userManager.GetUserId(User);
        //
        //     // JM: Returns false if there's no ID found
        //     if (string.IsNullOrEmpty(userId))
        //     {
        //         return false;
        //     }
        //
        //     // JM: Finds user, checks if they have a saved address (postcode or city exist) 
        //     var hasSavedAddress = await _dbContext.Users
        //         .Where(u => u.Id == userId)
        //         .Select(u => !string.IsNullOrWhiteSpace(u.PostCode) || !string.IsNullOrWhiteSpace(u.City))
        //         .FirstOrDefaultAsync();
        //     
        //     // JM: Return result, true if they have postcode or city, otherwise returns false
        //     return hasSavedAddress;
        // }
        
        
        // JM: Commented out as using different logic.
        // <summary>
        // DMQ - Locates the recycling and drop off locations near the user for their selected item
        // </summary>
        // <param name="itemID"></param>
        // <param name="userid"></param>
        // <returns>searchString</returns>
        // public async Task<IActionResult> LocateDisposal(int? itemID, string userid)
        // {
        //     //if the post code or item id cannot be found, then retrun a not found form
        //     if(itemID == null || userid == null)
        //     {
        //         return NotFound();
        //     }
        //
        //     //locates the item via its id
        //     var item = await _dbContext.Items.FindAsync(itemID);
        //
        //     //retrieves the user from the Users table by their id
        //     var user = await _dbContext.Users.FindAsync(userid);
        //
        //
        //     //if the item cannot be found, then retrun a not found form
        //     if (item == null || user == null)
        //     {
        //         return NotFound();
        //     }
        //
        //     //retrieves the users postcode
        //     var postcode = user.PostCode;
        //
        //     //get the beggining of the users postcode
        //     string userOutcode = user.PostCode.Split(' ')[0].Trim().ToUpper();
        //
        //     //makes a lsit of the disposal sites near the user
        //     var nearbyLocations = await _dbContext.DisposalLocations
        //     .Where(d => d.Postcode.ToUpper().StartsWith(userOutcode))
        //     .ToListAsync();
        //
        //     //adds the search item name to the viewbag
        //     ViewBag.ItemName = item.ItemName;
        //
        //
        //     //returns the search query to the view
        //     return View("Index", nearbyLocations);
        // }//end of action result
        

    }//end of controller
}//end of namespace
