using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationTest1.Models.ApplicationUserViewModels
{
    public class ApplicationRolesViewModel
    {
        public string Id { get; set; }
        [Display(Name= "Role Name")]
        [Required]
        public string RoleName { get; set; }
    }
}
