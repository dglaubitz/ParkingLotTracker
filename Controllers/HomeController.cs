using ParkingLotTracker.Models;
using System.Web.Mvc;
using System.Configuration;

namespace ParkingLotTracker.Controllers
{
    public class HomeController : Controller
    {
        // list all vehicles with search bar. Main info page. Vehicle must have an owner
        public ActionResult Index(string registrationSearchString)
        {
            return View(VehicleInfo.GetFilteredVehiclesData(registrationSearchString));
        }

        // list all owners with search bar. Owners don't need to actually own any vehicles. Can own multiple vehicles.
        public ActionResult OwnerList(string usernameSearchString)
        {
            return View(Owner.GetFilteredOwnersData(usernameSearchString));
        }

        // vehicle edit menu
        public ActionResult EditVehicle(string registrationNumber)
        {            
            return View(VehicleInfo.GetVehicleFromRegNumber(registrationNumber));
        }

        // owner edit menu
        public ActionResult EditOwner(string username)
        {
            return View(Owner.GetOwnerFromUsername(username));
        }

        // save vehicle edit
        public ActionResult SaveVehicle(VehicleInfo vehicleInfo)
        {
            ViewBag.registrationeError = null;
            string newRegistrationNumber = InitString(vehicleInfo.RegistrationNumber, "ToUpper");
            string oldRegistrationNumber = InitString(vehicleInfo.PlaceholderRegistrationNumber, "ToUpper");
            string newUsername = InitString(vehicleInfo.VehicleOwner.Username, "ToLower");
            string oldUsername = InitString(vehicleInfo.VehicleOwner.PlaceholderUsername, "ToLower");

            try
            {                
                if (RegistrationIsBlank(vehicleInfo, newRegistrationNumber, oldRegistrationNumber) ||
                    RegistrationIsTaken(vehicleInfo, newRegistrationNumber, oldRegistrationNumber) ||
                    UsernameIsBlank(vehicleInfo.VehicleOwner, newUsername, oldUsername))                  
                {
                    return View("EditVehicle", vehicleInfo);
                }

                VehicleInfo.UpdateAll(vehicleInfo);
                return View("Index", VehicleInfo.GetFilteredVehiclesData(""));
            }
            catch
            {
                VehicleInvalidOther(vehicleInfo, oldRegistrationNumber, oldUsername);
                return View("EditVehicle", vehicleInfo);
            }
        }

        // save owner edit
        public ActionResult SaveOwner(Owner owner)
        {
       
                ViewBag.usernameError = null;
                string newUsername = InitString(owner.Username, "ToLower");
                string oldUsername = InitString(owner.PlaceholderUsername, "ToLower");

            try
            {
                if(UsernameIsBlank(owner, newUsername, oldUsername) ||
                   UsernameIsTaken(owner, newUsername, oldUsername))
                {
                    return View("EditOwner", owner);
                }
                Owner.UpdateAll(owner);
                return View("Index", VehicleInfo.GetFilteredVehiclesData(""));
            }
            catch
            {
                return View("EditOwner", owner);
            }
        }

        // change owner when editing vehicle via username
        public ActionResult ChangeOwner(string registrationNumber, string username)
        {
            var vehicleInfo = VehicleInfo.GetVehicleFromRegNumber(registrationNumber);
            var owner = Owner.GetOwnerFromUsername(username);
            if (owner.Username != null && owner.Username != "")
            {
                vehicleInfo.VehicleOwner = owner;
                return Json(owner, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(owner, JsonRequestBehavior.AllowGet);
            }
        }

        // owner add menu
        public ActionResult AddOwner(Owner owner)
        {
            return View("AddOwner", owner);
        }

        // perform db action on button click in add owner menu
        public ActionResult SubmitOwnerInsert(Owner owner)
        {  
                ViewBag.usernameError = null;
  
                string newUsername = InitString(owner.Username, "ToLower");
                string oldUsername = InitString(owner.PlaceholderUsername, "ToLower");
            try
            {
                if (UsernameIsBlank(owner, newUsername, oldUsername) ||
                    UsernameIsTaken(owner, newUsername, oldUsername))
                {
                    return View("AddOwner", owner);
                }
                Owner.Add(owner);
                return View("OwnerList", Owner.GetFilteredOwnersData(""));
            }
            catch
            {
                OwnerInvalidOther(owner, oldUsername);
                return View("AddOwner", VehicleInfo.GetFilteredVehiclesData(""));
            }
        }

        // vehicle add menu with "add owner" partial view - can add new owner with new vehicle
        public ActionResult AddVehicleInfo(VehicleInfo vehicleInfo)
        {
            ViewBag.choosePartial = "add";
            return View("AddVehicleInfo", vehicleInfo);
        }

        // vehicle add menu with "change owner" partial view - can add vehicle with an existing owner
        public ActionResult AddVehicleInfoChooseOwner(VehicleInfo vehicleInfo)
        {
            ViewBag.choosePartial = "choose";
            return View("AddVehicleInfo", vehicleInfo);
        }

        // perform db action on button click in "add vehicle" menu
        public ActionResult SubmitVehicleInsert(VehicleInfo vehicleInfo)
        {
            if (vehicleInfo.VehicleOwner.PlaceholderUsername == ConfigurationManager.AppSettings["ownerAddMode"])
            {
                ViewBag.choosePartial = "add";
            }
            else
            {
                ViewBag.choosePartial = "choose";
            }
            string newRegistrationNumber = InitString(vehicleInfo.RegistrationNumber, "ToUpper");
            string oldRegistrationNumber = InitString(vehicleInfo.PlaceholderRegistrationNumber, "ToUpper");

            string newUsername = InitString(vehicleInfo.VehicleOwner.Username, "ToLower");
            string oldUsername = InitString(vehicleInfo.VehicleOwner.PlaceholderUsername, "ToLower");

            try
            { 
                if (RegistrationIsBlank(vehicleInfo, newRegistrationNumber, oldRegistrationNumber) ||
                   RegistrationIsTaken(vehicleInfo, newRegistrationNumber, oldRegistrationNumber) ||
                   UsernameIsBlank(vehicleInfo.VehicleOwner, newUsername, oldUsername))
                {
                    return View("AddVehicleInfo", vehicleInfo);
                }
                if (UsernameIsTaken(vehicleInfo.VehicleOwner, newUsername, oldUsername) && oldUsername == ConfigurationManager.AppSettings["ownerAddMode"])
                {
                    vehicleInfo.OwnerUsername = "";
                    vehicleInfo.VehicleOwner.Username = "";
                    return View("AddVehicleInfo", vehicleInfo);
                }
                if (oldUsername != ConfigurationManager.AppSettings["ownerAddMode"] && !Owner.UsernameExists(newUsername))
                {
                    ModelState.Clear();
                    ViewBag.usernameError = "Username doesn't exist";
                    vehicleInfo.VehicleOwner.Username = oldUsername;
                    return View("AddVehicleInfo", vehicleInfo);
                }
                if (!UsernameIsTaken(vehicleInfo.VehicleOwner, newUsername, oldUsername))
                {
                    Owner.Add(vehicleInfo.VehicleOwner);
                }
                else
                {
                    vehicleInfo.OwnerUsername = newUsername;
                    vehicleInfo.VehicleOwner.Username = newUsername;
                }
                    VehicleInfo.Add(vehicleInfo);
                    return View("Index", VehicleInfo.GetFilteredVehiclesData(""));
            }
            catch
            {
                ModelState.Clear();
                ViewBag.registrationError = "Error creating record";
                vehicleInfo.RegistrationNumber = vehicleInfo.PlaceholderRegistrationNumber;

                return View("AddVehicleInfo", vehicleInfo);
            }
        }

        // owner add menu - stand alone; no partial view attached to "add vehicle" view
        public ActionResult AddOwnerInfo(Owner owner)
        {
            return View(owner);
        }

        // remove owner
        public ActionResult DeleteOwner(string username)
        {
            Owner.Delete(username);
            return View("OwnerList", Owner.GetFilteredOwnersData(""));
        }

        // remove vehicle
        public ActionResult DeleteVehicle(string registrationNumber)
        {
            VehicleInfo.Delete(registrationNumber);
            return View("Index", VehicleInfo.GetFilteredVehiclesData(""));
        }

        // ensure strings are in a consistent format upon assignment
        public string InitString(string tString, string mods)
        {
            if (tString != null)
            {
                if (mods == "ToUpper")
                    return (tString.ToUpper());
                else if (mods == "ToLower")
                    return (tString.ToLower());
                return (tString.Trim());
            }
            return tString;
        }

        // vehicle registration can't be blank when adding/editing
        public bool RegistrationIsBlank(VehicleInfo vehicleInfo, string newField, string oldField)
        {
            if (newField == null || newField == "")
            {
                ModelState.Clear();
                ViewBag.registrationError = "Plate # can't be blank";
                vehicleInfo.RegistrationNumber = oldField;
                return true;
            }
            return false;
        }

        // username can't be blank when adding/editing
        public bool UsernameIsBlank(Owner owner, string newField, string oldField)
        {
            if (newField == null || newField == "")
            {
                ModelState.Clear();
                ViewBag.usernameError = "Username can't be blank";
                if (oldField != ConfigurationManager.AppSettings["ownerAddMode"])
                {
                    owner.Username = oldField;
                }
                return true;
            }
            return false;
        }

        // return true if user-entered registration # already exists (can't add a vehicle if registration already exists)
        public bool RegistrationIsTaken(VehicleInfo vehicleInfo, string newField, string oldField)
        {
            if (newField != oldField && VehicleInfo.VehicleRegistrationExists(newField))
            {
                ModelState.Clear();
                ViewBag.registrationError = "Plate # already exists";
                vehicleInfo.RegistrationNumber = oldField;
                return true;
            }
            return false;
        }

        // returns true if someone has username already (can't add an owner if username already exists_
        public bool UsernameIsTaken(Owner owner, string newField, string oldField)
        {
            if (newField != oldField && Owner.UsernameExists(newField))
            {
                ModelState.Clear();
                ViewBag.usernameError = "Username already exists";
                owner.Username = oldField;
                return true;
            }
            return false;
        }

        // general vehicle form error for things not already caught
        public void VehicleInvalidOther(VehicleInfo vehicleInfo, string oldRegistration, string oldUsername)
        {
            ModelState.Clear();
            ViewBag.registrationError = "Invalid Entry";
            vehicleInfo.RegistrationNumber = oldRegistration;
            vehicleInfo.VehicleOwner.Username = oldUsername;
        }

        // general owner form error for things not already caught
        public void OwnerInvalidOther(Owner owner, string oldUsername)
        {
            ModelState.Clear();
            ViewBag.usernameError = "Invalid Entry";
            owner.Username = oldUsername;
        }
    }
}