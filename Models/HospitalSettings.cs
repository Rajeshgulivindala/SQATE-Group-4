using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagementSystem.Models
{
 
    /// <summary>
    /// A data model for hospital settings.
    /// This class is used to structure the data retrieved from and saved to the database.
    /// </summary>
    public class HospitalSettings
    {
        public string HospitalName { get; set; }
        public string Address { get; set; }
        public string ContactPhone { get; set; }
        public string HospitalEmail { get; set; }
        public string Website { get; set; }
        public string LicenseNumber { get; set; }
        public string DefaultCurrency { get; set; }
        public string TimeZone { get; set; }
        public string LogoPath { get; set; }
    }
}


