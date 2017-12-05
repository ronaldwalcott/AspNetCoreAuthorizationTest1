using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationTest1.Models.ApplicationUserViewModels
{
    public class RoleClaimsViewModel
    {
        public string Id { get; set; }
        [Editable(false)]
        public string RoleName { get; set; }

        public List<ClaimsViewModel> RoleClaims { get; set; }
    }
}
