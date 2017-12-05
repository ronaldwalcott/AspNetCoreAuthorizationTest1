using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AuthorizationTest1.Models;
using AuthorizationTest1.Models.ApplicationUserViewModels;

namespace AuthorizationTest1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }

        public DbSet<AuthorizationTest1.Models.ApplicationUserViewModels.ApplicationRolesViewModel> ApplicationRolesViewModel { get; set; }

        public DbSet<AuthorizationTest1.Models.ApplicationUserViewModels.UserListViewModel> UserListViewModel { get; set; }

        public DbSet<AuthorizationTest1.Models.ApplicationUserViewModels.UserViewModel> UserViewModel { get; set; }

        public DbSet<AuthorizationTest1.Models.ApplicationUserViewModels.UserEditViewModel> UserEditViewModel { get; set; }

        public DbSet<AuthorizationTest1.Models.ApplicationUserViewModels.RoleClaimsViewModel> RoleClaimsViewModel { get; set; }
    }
}
