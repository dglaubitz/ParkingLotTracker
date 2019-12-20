using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

using System.Data.SqlClient;
using System.Web.Configuration;

namespace ParkingLotTracker.Models
{
    public class Owner
    {
        [StringLength(50, MinimumLength = 1)]
        [RegularExpression("^[a-zA-Z0-9]*$")]
        public string Username { get; set; }

        public string PlaceholderUsername { get; set; }

        [StringLength(50)]
        [RegularExpression("^[a-zA-Z0-9 ]*$")]
        public string FirstName { get; set; }

        [StringLength(50)]
        [RegularExpression("^[a-zA-Z0-9 ]*$")]
        public string LastName { get; set; }

        [StringLength(25)]
        [RegularExpression("^[0-9]*$")]
        public string PhoneNumber { get; set; }

        [StringLength(10)]
        [RegularExpression("^[a-zA-Z0-9]*$")]
        public string ApartmentNumber { get; set; }

        [StringLength(10)]
        [RegularExpression("^[a-zA-Z0-9]*$")]
        public string UnitNumber { get; set; }

        // # of cars a user owns
        public int CarsOwned 
        {
            get
            {
                if(Username != null)
                {
                    Username = Username.Trim();
                }

                string query = string.Format(
                "SELECT Count(*) As Count " +
                "FROM Vehicle " +
                "WHERE OwnerUsername = \'{0}\' ", Username);

                int carsOwned = 0;
                var sqlConnection = new SqlConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
                sqlConnection.Open();

                var sqlCommand = new SqlCommand(query, sqlConnection);
                var reader = sqlCommand.ExecuteReader();

                while (reader.Read())
                {
                    carsOwned = reader.GetInt32(reader.GetOrdinal("Count"));
                }

                reader.Close();
                sqlConnection.Close();
                return carsOwned;
            }
            set {}
        }

        // sql query for updates - everything is updated even if only one item is changed
        public static void UpdateAll(Owner owner)
        {
            var stringProperties = owner.GetType().GetProperties()
              .Where(p => p.PropertyType == typeof(string));

            foreach (var stringProperty in stringProperties)
            {
                string currentValue = (string)stringProperty.GetValue(owner, null);
                if (stringProperty != null && currentValue != null)
                {
                    stringProperty.SetValue(owner, currentValue.Trim(), null);
                }
                else
                {
                    stringProperty.SetValue(owner, "", null);
                }

            }

            string query = string.Format(
                    "UPDATE Owner " +
                    "SET Username=\'{0}\',FirstName=\'{1}\', LastName=\'{2}\', PhoneNumber=\'{3}\', ApartmentNumber=\'{4}\', UnitNumber=\'{5}\' " +
                    "WHERE Username=\'{6}\'",
                    owner.Username.ToLower(), owner.FirstName, owner.LastName, owner.PhoneNumber, owner.ApartmentNumber, owner.UnitNumber, owner.PlaceholderUsername.ToLower());

            var sqlConnection = new SqlConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            sqlConnection.Open();

            var sqlCommand = new SqlCommand(query, sqlConnection);
            sqlCommand.ExecuteReader();

            sqlConnection.Close();
        }

        // accepts username string returns the owner object
        public static Owner GetOwnerFromUsername(string userName)
        {
            string query = string.Format(
            "SELECT * " +
            "FROM Owner " +
            "WHERE Username = \'{0}\' ", userName.Trim());

            var owner = new Owner();
            var sqlConnection = new SqlConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            sqlConnection.Open();

            var sqlCommand = new SqlCommand(query, sqlConnection);
            var reader = sqlCommand.ExecuteReader();

            while (reader.Read())
            { 
                owner.Username = reader.GetString(reader.GetOrdinal("Username")).Trim();
                owner.PlaceholderUsername = reader.GetString(reader.GetOrdinal("Username")).Trim();
                owner.FirstName = reader.GetString(reader.GetOrdinal("FirstName")).Trim();
                owner.LastName = reader.GetString(reader.GetOrdinal("LastName")).Trim();
                owner.PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")).Trim();
                owner.ApartmentNumber = reader.GetString(reader.GetOrdinal("ApartmentNumber")).Trim();
                owner.UnitNumber = reader.GetString(reader.GetOrdinal("UnitNumber")).Trim();           
            }

            reader.Close();
            sqlConnection.Close();
            return owner;
        }

        // lists all owners based on search string (blank means list all)
        public static List<Owner> GetFilteredOwnersData(string searchString)
        {
            if (searchString != null)
            {
                searchString = searchString.Trim();
            }
            string query = string.Format(
                "SELECT * " +
                "FROM Owner " +
                "WHERE Username like \'%{0}%\' " +
                "ORDER BY LastName, FirstName, Username", searchString);

            List<Owner> owners = new List<Owner>();

            var sqlConnection = new SqlConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            sqlConnection.Open();

            var sqlCommand = new SqlCommand(query, sqlConnection);
            var reader = sqlCommand.ExecuteReader();

            while (reader.Read())
            {
                var owner = new Owner
                {
                    Username = reader.GetString(reader.GetOrdinal("Username")).Trim(),
                    PlaceholderUsername = reader.GetString(reader.GetOrdinal("Username")).Trim(),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")).Trim(),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")).Trim(),
                    PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")).Trim(),
                    ApartmentNumber = reader.GetString(reader.GetOrdinal("ApartmentNumber")).Trim(),
                    UnitNumber = reader.GetString(reader.GetOrdinal("UnitNumber")).Trim()
                };
                owners.Add(owner);
            }

            reader.Close();
            sqlConnection.Close();
            return owners;
        }

        // checks for duplicate username (primary key)
        public static bool UsernameExists(string username)
        {
            string query = string.Format(
            "SELECT Username " +
            "FROM Owner " +
            "WHERE Username = \'{0}\' ", username.Trim());

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

        // delete owner (deletes vehicles along with it)
        public static bool Delete(string username)
        {
            if (!UsernameExists(username))
            {
                return false;
            }

            string query = string.Format(
                        "DELETE FROM Owner " +
                        "WHERE Username=\'{0}\'", username.Trim());

            var sqlConnection = new SqlConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            sqlConnection.Open();

            var sqlCommand = new SqlCommand(query, sqlConnection);
            sqlCommand.ExecuteReader();
            sqlConnection.Close();

            return true;
        }

        // adds owner (no vehicle added)
        public static bool Add(Owner owner)
        {
            if (UsernameExists(owner.Username))
            {
                return false;
            }

            var stringProperties = owner.GetType().GetProperties()
              .Where(p => p.PropertyType == typeof(string));

            foreach (var stringProperty in stringProperties)
            {
                string currentValue = (string)stringProperty.GetValue(owner, null);
                if (stringProperty != null && currentValue != null)
                {
                    stringProperty.SetValue(owner, currentValue.Trim(), null);
                }
                else
                {
                    stringProperty.SetValue(owner, "", null);
                }

            }

            string query = string.Format(
                        "INSERT INTO Owner (Username, FirstName, LastName, PhoneNumber, ApartmentNumber, UnitNumber) " +
                        "VALUES (\'{0}\',\'{1}\',\'{2}\',\'{3}\',\'{4}\',\'{5}\')", 
                        owner.Username.ToLower(), owner.FirstName, owner.LastName, owner.PhoneNumber, owner.ApartmentNumber, owner.UnitNumber);

            var sqlConnection = new SqlConnection(WebConfigurationManager.AppSettings["ConnectionString"]);
            sqlConnection.Open();

            var sqlCommand = new SqlCommand(query, sqlConnection);
            sqlCommand.ExecuteReader();
            sqlConnection.Close();

            return true;
        }
    }
}