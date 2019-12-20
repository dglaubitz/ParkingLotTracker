using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Web.Configuration;

namespace ParkingLotTracker.Models
{
    public class VehicleInfo
    {
        public Owner VehicleOwner { get; set; }

        [StringLength(50, MinimumLength = 1)]
        [RegularExpression("^[a-zA-Z0-9]*$")]
        public string OwnerUsername { get; set; }

        public string PlaceholderRegistrationNumber { get; set; }

        [StringLength(10, MinimumLength = 1)]
        [RegularExpression("^[a-zA-Z0-9]*$")]
        public string RegistrationNumber { get; set; }

        [StringLength(50)]
        [RegularExpression("^[a-zA-Z0-9 ]*$")]
        public string Make { get; set; }

        [StringLength(50)]
        [RegularExpression("^[a-zA-Z0-9 ]*$")]
        public string Model { get; set; }

        [StringLength(25)]
        [RegularExpression("^[a-zA-Z0-9 ]*$")]
        public string Color { get; set; }

        [DataType(DataType.Date)]
        public DateTimeOffset DateEnteredSystem { get; set; }

        // sql query for updates - everything is updated even if only one item is changed
        public static void UpdateAll(VehicleInfo vehicleInfo)
        {
            var stringProperties = vehicleInfo.GetType().GetProperties()
              .Where(p => p.PropertyType == typeof(string));

            foreach (var stringProperty in stringProperties)
            {
                string currentValue = (string)stringProperty.GetValue(vehicleInfo, null);
                if (stringProperty != null && currentValue != null)
                {
                    stringProperty.SetValue(vehicleInfo, currentValue.Trim(), null);
                }
                else
                {
                    stringProperty.SetValue(vehicleInfo, "", null);
                }

            }

            string query = string.Format(
                    "UPDATE Vehicle " +
                    "SET RegistrationNumber=\'{0}\', OwnerUsername=\'{1}\', Make=\'{2}\', Model=\'{3}\', Color=\'{4}\', DateEnteredSystem=\'{5}\' " +
                    "WHERE RegistrationNumber=\'{6}'", vehicleInfo.RegistrationNumber.ToUpper(), vehicleInfo.VehicleOwner.Username.ToLower(), vehicleInfo.Make, 
                    vehicleInfo.Model, vehicleInfo.Color, vehicleInfo.DateEnteredSystem, vehicleInfo.PlaceholderRegistrationNumber.ToUpper());

            var sqlConnection = new SqlConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            sqlConnection.Open();

            var sqlCommand = new SqlCommand(query, sqlConnection);
            sqlCommand.ExecuteReader();
  
            sqlConnection.Close();
        }

        // accepts registration string returns the vehicle object
        public static VehicleInfo GetVehicleFromRegNumber(string registrationNumber)
        {
            var vehicle = new VehicleInfo();
            string query = string.Format(
            "SELECT * " +
            "FROM Vehicle " +
            "JOIN Owner ON Vehicle.OwnerUsername=Owner.Username " +
            "WHERE RegistrationNumber= \'{0}\' ", registrationNumber.Trim());

            var sqlConnection = new SqlConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            sqlConnection.Open();

            var sqlCommand = new SqlCommand(query, sqlConnection);
            var reader = sqlCommand.ExecuteReader();

            while (reader.Read())
            {
                vehicle.RegistrationNumber = reader.GetString(reader.GetOrdinal("RegistrationNumber")).Trim();
                vehicle.PlaceholderRegistrationNumber = reader.GetString(reader.GetOrdinal("RegistrationNumber")).Trim();
                vehicle.Make = reader.GetString(reader.GetOrdinal("Make")).Trim();
                vehicle.Model = reader.GetString(reader.GetOrdinal("Model")).Trim();
                vehicle.Color = reader.GetString(reader.GetOrdinal("Color")).Trim();
                vehicle.DateEnteredSystem = reader.GetDateTimeOffset(reader.GetOrdinal("DateEnteredSystem"));
                vehicle.OwnerUsername = reader.GetString(reader.GetOrdinal("OwnerUsername")).Trim();
                vehicle.VehicleOwner = Owner.GetOwnerFromUsername(vehicle.OwnerUsername);
            }
            reader.Close();
            sqlConnection.Close();

            return vehicle;
        }

        // lists all vehicles info based on search string (blank means list all)
        public static List<VehicleInfo> GetFilteredVehiclesData(string searchString)
        {
            if (searchString != null)
            {
                searchString = searchString.Trim();
            }
            string query = string.Format(
                "SELECT * " +
                "FROM Vehicle " +
                "JOIN Owner ON Vehicle.OwnerUsername=Owner.Username " +
                "WHERE RegistrationNumber like \'%{0}%\' " + 
                "ORDER BY Color, Make, Model, LastName, FirstName, Username", searchString);

            List<VehicleInfo> vehicles = new List<VehicleInfo>();

            var sqlConnection = new SqlConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            sqlConnection.Open();

            var sqlCommand = new SqlCommand(query, sqlConnection);
            var reader = sqlCommand.ExecuteReader();

            while (reader.Read())
            {
                var vehicle = new VehicleInfo
                {
                    RegistrationNumber = reader.GetString(reader.GetOrdinal("RegistrationNumber")).Trim(),
                    PlaceholderRegistrationNumber = reader.GetString(reader.GetOrdinal("RegistrationNumber")).Trim(),
                    Make = reader.GetString(reader.GetOrdinal("Make")).Trim(),
                    Model = reader.GetString(reader.GetOrdinal("Model")).Trim(),
                    Color = reader.GetString(reader.GetOrdinal("Color")).Trim(),
                    DateEnteredSystem = reader.GetDateTimeOffset(reader.GetOrdinal("DateEnteredSystem")),
                    OwnerUsername = reader.GetString(reader.GetOrdinal("OwnerUsername")).Trim()
                };
                vehicle.VehicleOwner = Owner.GetOwnerFromUsername(vehicle.OwnerUsername);
                vehicles.Add(vehicle);
            }
            reader.Close();
            sqlConnection.Close();

            return vehicles;
        }

        // checks for duplicate vehicle registrations (primary key)
        public static bool VehicleRegistrationExists(string registrationNumber)
        {
            string query = string.Format(
            "SELECT RegistrationNumber " +
            "FROM Vehicle " +
            "WHERE RegistrationNumber = \'{0}\' ", registrationNumber.Trim());

            var sqlConnection = new SqlConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            sqlConnection.Open();

            var sqlCommand = new SqlCommand(query, sqlConnection);
            var reader = sqlCommand.ExecuteReader();

            if (reader.HasRows)
            {
                reader.Close();
                sqlConnection.Close();

                return true;
            }
            reader.Close();
            sqlConnection.Close();

            return false;
        }

        // delete vehicle (does not delete owner under any circumstance)
        public static bool Delete(string registrationNumber)
        {
            if (!VehicleRegistrationExists(registrationNumber))
            {
                return false;
            }

            string query = string.Format(
                        "DELETE FROM Vehicle " +
                        "WHERE RegistrationNumber=\'{0}\'", registrationNumber.Trim());

            var sqlConnection = new SqlConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            sqlConnection.Open();

            var sqlCommand = new SqlCommand(query, sqlConnection);
            sqlCommand.ExecuteReader();
            sqlConnection.Close();

            return true;
        }

          // adds vehicle (must have a username for owner)
        public static bool Add(VehicleInfo vehicle)
        {
            if (VehicleRegistrationExists(vehicle.RegistrationNumber))
            {
                return false;
            }

           var stringProperties = vehicle.GetType().GetProperties()
                          .Where(p => p.PropertyType == typeof(string));

            foreach (var stringProperty in stringProperties)
            {
                string currentValue = (string)stringProperty.GetValue(vehicle, null);
                if (stringProperty != null && currentValue != null)
                {
                    stringProperty.SetValue(vehicle, currentValue.Trim(), null);
                }
                else
                {
                    stringProperty.SetValue(vehicle, "", null);
                }

            }

            string query = string.Format(
                        "INSERT INTO Vehicle (RegistrationNumber, OwnerUsername, Make, Model, Color, DateEnteredSystem) " +
                        "VALUES (\'{0}\',\'{1}\',\'{2}\',\'{3}\',\'{4}\',\'{5}\')",
                        vehicle.RegistrationNumber.ToUpper(), vehicle.VehicleOwner.Username.ToLower(), vehicle.Make, vehicle.Model, vehicle.Color, DateTimeOffset.Now);

            var sqlConnection = new SqlConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            sqlConnection.Open();

            var sqlCommand = new SqlCommand(query, sqlConnection);
            sqlCommand.ExecuteReader();
            sqlConnection.Close();

            return true;
        }
    }
}