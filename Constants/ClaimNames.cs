using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationTest1.Constants
{
    public static class ClaimNames
    {
        public static List<string> ClaimName = new List<string>() {
            PolicyNames.AdministratorPolicy,
            PolicyNames.SupervisorPolicy,
            PolicyNames.EmployeePolicy
        };
    }
}
