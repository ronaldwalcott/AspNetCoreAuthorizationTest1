using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationTest1.Authorization
{
    public class AuthorizationNameRequirement : IAuthorizationRequirement
    {

        public string AuthorizationName { get; private set; }

        public AuthorizationNameRequirement(string authorizationName)
        {
            AuthorizationName = authorizationName;
        }
    }
}
    