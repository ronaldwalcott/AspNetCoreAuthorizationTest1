# AspNetCoreAuthorizationTest1
Examining how to create a flexible authorization system in ASP.NET Core 2.0 where the creation of new privileges or access levels does not require code changes.

No detailed examples exist of how authorization and user management work for enterprise applications using ASP.NET Core. Even the MVC template presupposes a web application where users are allowed to register themselves. This is an investigation of how the addition of claim policies and requirement handlers to ASP.NET Core provides the flexibility needed to create a dynamic authorization system for enterprise applications.

Consider the use case for an enterprise application where we want to add new authorization roles and set privileges for those roles, without code changes requiring an application restart.

We will create a simplified proof of concept exploring how the new features can be used to satisfy this use case. This is not production ready code as paging, validation, logging, error checking, etc. will not be included.

For those who have read the documentation on the ASP.NET Core documents web page, the underlying assumption we will make is that a claim can be managed as a privilege and assigned to or removed from a role, thus granting any users assigned that role that same privilege.
We will start with the ASP.NET Core MVC web application using individual user accounts template and create a new application. This will set up entity framework and the basic ASP.NET Core identity framework.

The ASP.NET Core template uses email as the identifying user id which we see in the login page. This is not commonly done for enterprise applications, so the first thing we need to ensure is that the user table contains all the fields we need for our enterprise application.

We can examine this by viewing the code generated to create the database tables. This is contained in the create identity migration, named 00000000000000_CreateIdentitySchema.cs in the example below.

 
Executing the migration will create a table containing the fields below

```c#
migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    AccessFailedCount = table.Column<int>(nullable: false),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                    PasswordHash = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    SecurityStamp = table.Column<string>(nullable: true),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    UserName = table.Column<string>(maxLength: 256, nullable: true)
                },
```

Examining the Register Post action in the Account controller shows us that email is saved to the UserName property. This will not quite satisfy the needs for our enterprise application as we want to maintain the user’s name and employee number and maintain a username in a different format to that of an email address. To add these new fields we create a new class ApplicationUser.cs, in the Models folder, which inherits from IdentityUser and add our new fields as properties.

```c#
using Microsoft.AspNetCore.Identity;

namespace AuthorizationTest1.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string Surname { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string EmployeeNumber { get; set; }
    }
}
```

As neither the database name nor location matters for our test we leave the default connection string for the database, open a cmd window to run the migration and enter
```
dotnet ef database update
```

This will create the database using the 00000000000000_CreateIdentitySchema.cs migration
Open the AspNetUsers table by selecting
View – SQL Object Server Explorer  



and realizing that our new fields are not contained in the table, as we did not create a new migration. So we create a new migration by typing in the cmd window
```
dotnet ef migrations add InitialCreate
dotnet ef database update
```
Examining the table now shows our new fields 

 
Authorization roles and policies are declared on initial startup in the Startup.cs file
```c#
services.AddMvc();

    services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdministratorRole", policy => policy.RequireRole("Administrator","AdministratorManagement"));
    });
```
but as we want to be able to change the role requirements for the declared policies we will use the requirements model for declaring authorization, creating a declaration similar to
```c#
services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdministratorRole",
                          policy => policy.Requirements.Add(new AdministratorRequirement(“RequireAdministratorRole”)));
    });
```
But first we need to create a method to add roles to our roles table.

Sandeep Shekhawat provides a Visual Studio 2015 solution (https://code.msdn.microsoft.com/ASPNET-Core-MVC-Authenticat-ef5942f5), which we will expand upon, that shows how users and roles are created.

So, first we need a method for maintaining roles. Let’s create a view model and scaffold a MVC Controller with read/write actions and call it ApplicationRoleController.

Create a new folder ApplicationUserViewModels and create a view model ApplicationRolesViewModel. The AspNetRoles table contains an Id, ConcurrencyStamp, Name and NormalizedName fields
 
We will only use Id and a RoleName field in the view model, so our view model class will look as follows.
```c#
namespace AuthorizationTest1.Models.ApplicationUserViewModels
{
    public class ApplicationRolesViewModel
    {
        public string Id { get; set; }
        [Display(Name= "Role Name")]
        public string RoleName { get; set; }
    }
}
```
We could have modified the AspNetRoles by adding new fields, similar to what was done for the application user, but the existing fields are sufficient for this example.
In our ApplicationRoleController we use dependency injection and inject the service IdentityRole, which was configured in Startup.cs
```c#
services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
```
using the constructor
```c#
 private readonly RoleManager<IdentityRole> roleManager;
        
        public ApplicationRoleController(RoleManager<IdentityRole> roleManager)
        {
            this.roleManager = roleManager;
        }
```
The default Index action will be created to show a list of all roles
```c#
public ActionResult Index()
        {
            List<ApplicationRolesViewModel> viewModel = new List<ApplicationRolesViewModel>();
            viewModel = roleManager.Roles.Select(r => new ApplicationRolesViewModel
            {
                Id = r.Id,
                RoleName = r.Name
            }).ToList();
            return View(viewModel);
        }
```
We create a new folder ApplicationRole under the Views folder and scaffold a List view based on the view model ApplicationRolesViewModel and name it Index.cshtml.
```c#
@model IEnumerable<AuthorizationTest1.Models.ApplicationUserViewModels.ApplicationRolesViewModel>

@{
    ViewData["Title"] = "Index";
}

<h2>Index</h2>

<p>
    <a asp-action="Create">Create New</a>
</p>
<table class="table">
    <thead>
        <tr>
                <th>
                    @Html.DisplayNameFor(model => model.RoleName)
                </th>
            <th></th>
        </tr>
    </thead>
    <tbody>
@foreach (var item in Model) {
        <tr>
            <td>
                @Html.DisplayFor(modelItem => item.RoleName)
            </td>
            <td>
                <a asp-action="Edit" asp-route-id="@item.Id">Edit</a> |
                <a asp-action="Details" asp-route-id="@item.Id">Details</a> |
                <a asp-action="Delete" asp-route-id="@item.Id">Delete</a>
            </td>
        </tr>
}
    </tbody>
</table>
```
We need to now complete methods for the Create, Edit, Details and Delete actions. Remember that deletion in real world situations should not be completed as will be shown here. This is only a technical exercise. A majority of enterprises will never delete users only disable them (required for audit), while handling of role removal may be as simple as deleting them.

Ok, complete the get create action method by passing an empty ApplicationRolesViewModel model to the view 
```c#
// GET: ApplicationRole/Create
        public ActionResult Create()
        {
            ApplicationRolesViewModel viewModel = new ApplicationRolesViewModel();
            return View(viewModel);
        }
```
Scaffold a Create view
```c#
@model AuthorizationTest1.Models.ApplicationUserViewModels.ApplicationRolesViewModel

@{
    ViewData["Title"] = "Create";
}

<h2>Create</h2>

<h4>ApplicationRolesViewModel</h4>
<hr />
<div class="row">
    <div class="col-md-4">
        <form asp-action="Create">
            <div asp-validation-summary="ModelOnly" class="text-danger"></div>
            <div class="form-group">
                <label asp-for="RoleName" class="control-label"></label>
                <input asp-for="RoleName" class="form-control" />
                <span asp-validation-for="RoleName" class="text-danger"></span>
            </div>
            <div class="form-group">
                <input type="submit" value="Create" class="btn btn-default" />
            </div>
        </form>
    </div>
</div>

<div>
    <a asp-action="Index">Back to List</a>
</div>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```
And create the post action
```c#
// POST: ApplicationRole/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ApplicationRolesViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                IdentityRole identityRole = new IdentityRole();
                identityRole.Name = viewModel.RoleName;
                IdentityResult roleResult = await roleManager.CreateAsync(identityRole);

                if (roleResult.Succeeded)
                {
                    return RedirectToAction("Index");
                }
            }

            
            return View();
            
        }
```
The post action uses roleManager.CreateAsync(identityRole) to create the role. Remember to capture any errors and add them to the model state before returning the model to the view.

Create the Edit actions
```c#
        // GET: ApplicationRole/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            ApplicationRolesViewModel viewModel = new ApplicationRolesViewModel();
            if (!String.IsNullOrEmpty(id))
            {
                IdentityRole identityRole = await roleManager.FindByIdAsync(id);
                if (identityRole != null)
                {
                    viewModel.Id = identityRole.Id;
                    viewModel.RoleName = identityRole.Name;
                    return View(viewModel);
                }
            }
            return RedirectToAction("Index");
        }

        // POST: ApplicationRole/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, ApplicationRolesViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                if (!String.IsNullOrEmpty(id))
                {
                    IdentityRole identityRole = await roleManager.FindByIdAsync(id);
                    if (identityRole != null)
                    {
                        identityRole.Name = viewModel.RoleName;
                        IdentityResult roleResult = await roleManager.UpdateAsync(identityRole);

                        if (roleResult.Succeeded)
                        {
                            return RedirectToAction("Index");
                        }
                    }
                }

            }
            return View();
        }
```
And scaffold an edit view.
```c#
@model AuthorizationTest1.Models.ApplicationUserViewModels.ApplicationRolesViewModel

@{
    ViewData["Title"] = "Edit";
}

<h2>Edit</h2>

<h4>ApplicationRolesViewModel</h4>
<hr />
<div class="row">
    <div class="col-md-4">
        <form asp-action="Edit">
            <div asp-validation-summary="ModelOnly" class="text-danger"></div>
            <input type="hidden" asp-for="Id" />
            <div class="form-group">
                <label asp-for="RoleName" class="control-label"></label>
                <input asp-for="RoleName" class="form-control" />
                <span asp-validation-for="RoleName" class="text-danger"></span>
            </div>
            <div class="form-group">
                <input type="submit" value="Save" class="btn btn-default" />
            </div>
        </form>
    </div>
</div>

<div>
    <a asp-action="Index">Back to List</a>
</div>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```
Now, the delete actions (get and post) and a delete view to complete role maintenance. Remember that delete should not be performed in the get action only in the post. The delete view acts as a confirmation for the deletion process.
```c#
        // GET: ApplicationRole/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            string roleName = string.Empty;
            if (!String.IsNullOrEmpty(id))
            {
                IdentityRole identityRole = await roleManager.FindByIdAsync(id);
                if (identityRole != null)
                {
                    roleName = identityRole.Name;
                }
            }
            return View("Delete", roleName);
        }

        // POST: ApplicationRole/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            try
            {
                if (!String.IsNullOrEmpty(id))
                {
                    IdentityRole identityRole = await roleManager.FindByIdAsync(id);
                    if (identityRole != null)
                    {
                        IdentityResult roleResult = await roleManager.DeleteAsync(identityRole);

                        if (roleResult.Succeeded)
                        {
                            return RedirectToAction("Index");
                        }
                    }
                }
                //TODO Display error message
                return RedirectToAction("Index");
            }
            catch
            {
                //TODO Display error message

                return RedirectToAction("Index");
            }
        }
```
A details view was not created since it is not necessary. Therefore our next step is maintaining users’ information and assigning roles.

We first scaffold a MVC controller with read/write actions (right click on the controllers folder, select Add – Controller) and name it Usercontroller.
```c#
public class UserController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;

        public UserController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
        }
```
Use dependency injection to inject UserManager and RoleManager to provide a method of accessing the user and role data.

A user can have one or more roles, so we will use checkboxes for the selection of roles. The user view will need to display all roles which can be assigned to the user. Considering that this is being created for an enterprise application we can assume that this function will be performed on a computer or tablet device. If access is required on a mobile device with a smaller screen then this design may be slightly unwieldy and will require some form of paging. The ordering of the roles on the screen should also be considered but we will ignore that in this example.

For our “create new user action” a view model, UserViewModel, is created containing the user fields which we will be maintaining; Id, Email, UserName, EmployeeNumber, FirstName, MiddleName, Surname, LockoutEnabled (have no idea how this field is used), Password, ConfirmPassword and EmployeeNumber. A roles view model (UserRolesViewModel) is also created and a relationship established between the models indicating the roles that the user has. We use the List collection to create this relationship.
```c#
public List<UserRolesViewModel> UserRoles {get; set;}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationTest1.Models.ApplicationUserViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        [Display(Name = "Middle Name")]
        public string MiddleName { get; set; }
        [Required]
        public string Surname { get; set; }
        public string EmployeeNumber { get; set; }
        public Boolean LockoutEnabled { get; set; }
        [Required]
        [RegularExpression("^.*(?=.{8,})(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[@#$%^&+=;]).*$", ErrorMessage = "Passwords should contain uppercase, lowercase, numbers and special characters") ]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required]
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
        [EmailAddress]
        public string Email { get; set; }

        public List<UserRolesViewModel> UserRoles { get; set; }
    }
}
```

```c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationTest1.Models.ApplicationUserViewModels
{
    public class UserRolesViewModel
    {
        public string Id { get; set; }
        public string RoleName { get; set; }
        public Boolean HasRole { get; set; }
    }
}
```
A view model (UserListViewModel) to display the user information in the default index view is also created, with the RoleNames field containing the amalgamation of the assigned roles.
```c#
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationTest1.Models.ApplicationUserViewModels
{
    public class UserListViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string RoleNames { get; set; }
        public Boolean LockoutEnabled { get; set; }
    }
}
```
We also create a separate view model (UserEditViewModel) to be used for the Edit action
```c#
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationTest1.Models.ApplicationUserViewModels
{
    public class UserEditViewModel
    {
        public string Id { get; set; }
        [Editable(false)]
        public string UserName { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        [Display(Name = "Middle Name")]
        public string MiddleName { get; set; }
        [Required]
        public string Surname { get; set; }
        public string EmployeeNumber { get; set; }
        public Boolean LockoutEnabled { get; set; }
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
        
        public List<UserRolesViewModel> UserRoles { get; set; }
    }
}
```
Index, Create, Edit and Delete actions now need to be created to maintain the user and add and remove role assignments.

The Index method uses userManager to get a list of all users
```c#
List<ApplicationUser> users = userManager.Users.ToList();
```
and
```c#
userRolesList = await userManager.GetRolesAsync(user);
```
to get the list of roles assigned to the user.

It then loops through the roles assigned to the user to populate the RolesName field.
```c#
        public async Task<ActionResult> Index()
        {
            List<UserListViewModel> viewModel = new List<UserListViewModel>();

            List<ApplicationUser> users = userManager.Users.ToList();

            foreach (var user in users)
            {
                IList<string> userRolesList = new List<string>();
                userRolesList = await userManager.GetRolesAsync(user);
                string roles = null;

                foreach (var role in userRolesList)
                {
                    if (roles is null)
                    {
                        roles = role;
                    }
                    else
                    {
                        roles = roles + ", " + role;
                    }

                }

                viewModel.Add
                    (new UserListViewModel()
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Name = user.Surname + ", " + user.FirstName + " " + user.MiddleName,
                        LockoutEnabled = user.LockoutEnabled,
                        RoleNames = roles
                    }
                    );
            }

            return View(viewModel);
        }
```
There is nothing interesting about the associated view as it just scaffolded from the view model.

The Create get method populates the UserViewModel model with all roles and initially sets the HasRole field to false.
```c#
        public ActionResult Create()
        {
            UserViewModel viewModel = new UserViewModel();
            viewModel.UserRoles = roleManager.Roles.Select(r => new UserRolesViewModel
            {
                Id = r.Id,
                RoleName = r.Name,
                HasRole = false

            }).ToList();

            return View(viewModel);
        }
```
The Create post method uses
```c#
await userManager.CreateAsync(user, viewModel.Password)
```
to create the user with a new password and loops through the role list using
```c#
await userManager.AddToRoleAsync(user, role.RoleName)
```
to assign a role to the user.
```c#
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = new ApplicationUser()
                { UserName = viewModel.UserName,
                  FirstName = viewModel.FirstName,
                  MiddleName = viewModel.MiddleName,
                  Surname = viewModel.Surname,
                  EmployeeNumber = viewModel.EmployeeNumber,
                  LockoutEnabled = viewModel.LockoutEnabled,
                  Email =viewModel.Email
                };

                
                IdentityResult userResult = await userManager.CreateAsync(user, viewModel.Password);

                if (userResult.Succeeded)
                {
                    foreach (UserRolesViewModel role in viewModel.UserRoles)
                    {
                        if (role.HasRole)
                        {
                            IdentityRole identityRole = await roleManager.FindByIdAsync(role.Id);
                            if (identityRole != null)
                            {
                                IdentityResult roleResult = await userManager.AddToRoleAsync(user, role.RoleName);
                                //consider what to do if adding one role does not succeed
                            }

                        }
                    }
                    return RedirectToAction("Index");
                }
                else
                {
                    AddErrors(userResult);
                }
            }

            return View(viewModel);

        }
```
The associated view scaffolded from the UserViewModel model needs to be modified to display the roles list as checkboxes. We can loop through the roles list in the view model to display the roles
```c#
                    @{ int i = 0; }
                    @foreach (var item in Model.UserRoles)
                    {
                        <div class="form-group col-md-6">
                            <div class="checkbox">
                                <label>
                                    <input asp-for="UserRoles[i].HasRole" /> @Html.DisplayFor(model => item.RoleName)
                                </label>
                            </div>
                        </div>
                        //create two hidden fields for the id and rolename values
                        <div class="hidden">
                            <input type ="hidden" name ="UserRoles[@i].Id" value="@item.Id" />
                            <input type="hidden" name="UserRoles[@i].RoleName" value="@item.RoleName" />
                        </div>
                        i++;
                    }
```
Our Edit get method is slightly more complicated as we need to determine which roles have been assigned to the user when creating our view model. We use the UserManager GetRolesAsync method to get all of the roles assigned to the user and loop through all roles in the view model roles list assigning HasRole where a match exists. Not an optimal method but as it works and this function will not be executed very often …  
```c#
        public async Task<ActionResult> Edit(string id)
        {
            UserEditViewModel viewModel = new UserEditViewModel();
            if (!String.IsNullOrEmpty(id))
            {
                ApplicationUser user = await userManager.FindByIdAsync(id);
                if (user != null)
                {
                    viewModel.Id = user.Id;
                    viewModel.UserName = user.UserName;
                    viewModel.Surname = user.Surname;
                    viewModel.FirstName = user.FirstName;
                    viewModel.MiddleName = user.MiddleName;
                    viewModel.Email = user.Email;
                    viewModel.EmployeeNumber = user.EmployeeNumber;
                    viewModel.LockoutEnabled = user.LockoutEnabled;

                    //add all rolenames default to false
                    viewModel.UserRoles = roleManager.Roles.Select(r => new UserRolesViewModel
                    {
                        Id = r.Id,
                        RoleName = r.Name,
                        HasRole = false

                    }).ToList();

                    IList<string> userRolesList = new List<string>();
                    userRolesList = await userManager.GetRolesAsync(user);
                    foreach (var role in userRolesList)
                    {
                        foreach (var userrole in viewModel.UserRoles)
                        {
                            if (role == userrole.RoleName)
                            {
                                userrole.HasRole = true;
                            }
                        }
                    }
                }
            }

            return View(viewModel);
        }
```
Our Edit post method is a tad bit more complicated than the get method as we have to check where the user currently has the role and now access has been removed and where the user did not have the role and now access has been granted
```c#
                        foreach (var role in viewModel.UserRoles)
                            {
                                if (await userManager.IsInRoleAsync(user, role.RoleName) & !role.HasRole)
                                {
                                    IdentityResult roleResult = await userManager.RemoveFromRoleAsync(user, role.RoleName);
                                    //if (!roleResult.Succeeded) { }
                                }
                                else if (role.HasRole & !(await userManager.IsInRoleAsync(user, role.RoleName)))
                                {
                                    IdentityResult roleResult = await userManager.AddToRoleAsync(user, role.RoleName);
                                    //if (!roleResult.Succeeded) { }
                                }
                            }
```
removing or adding the role where applicable.
```c#
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, UserEditViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    ApplicationUser user = await userManager.FindByIdAsync(id);
                    if (user != null)
                    {
                        user.Surname = viewModel.Surname;
                        user.FirstName = viewModel.FirstName;
                        user.MiddleName = viewModel.MiddleName;
                        user.Email = viewModel.Email;
                        user.EmployeeNumber = viewModel.EmployeeNumber;
                        user.LockoutEnabled = viewModel.LockoutEnabled;

                        IdentityResult userResult = await userManager.UpdateAsync(user);
                        if (userResult.Succeeded)
                        {
                            //password reset should really be a separate action
                            if (!String.IsNullOrEmpty(viewModel.Password))
                            {
                                IdentityResult passwordResult = await userManager.ResetPasswordAsync(user, await userManager.GeneratePasswordResetTokenAsync(user), viewModel.Password);
                                //if password reset fails return to view
                            }
                            //changes to roles are not tracked therefore every role has to be checked for updates
                            foreach (var role in viewModel.UserRoles)
                            {
                                if (await userManager.IsInRoleAsync(user, role.RoleName) & !role.HasRole)
                                {
                                    IdentityResult roleResult = await userManager.RemoveFromRoleAsync(user, role.RoleName);
                                    //if (!roleResult.Succeeded) { }
                                }
                                else if (role.HasRole & !(await userManager.IsInRoleAsync(user, role.RoleName)))
                                {
                                    IdentityResult roleResult = await userManager.AddToRoleAsync(user, role.RoleName);
                                    //if (!roleResult.Succeeded) { }
                                }
                            }
                        }
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
```
As with the create view after scaffolding the edit view we need to add the checkboxes displaying the role list. Even though our edit view model contains password fields this is a bad design as password resets should be a completely separate action. So we will ignore those password fields for our authorization use case test.

A delete user action is now created even though it should only be used when users are created incorrectly. There is nothing unique about the delete action it is created similar to the user delete. It must be repeated that when users are to be removed they should probably be disabled not deleted. This only serves as a technical exercise.

Disabling could be an action of removing the password from a user account, therefore re-enabling would be adding a password. Actions should therefore be created using the following methods along with a method for the user to reset their password.

UserManager<TUser>.AddPasswordAsync
UserManager<TUser>.RemovePasswordAsync
UserManager<TUser>.ResetAccessFailedCountAsync

But this is not our current focus.

So, we can now maintain roles and users, and assign roles to users.

Our use case requires us to have the flexibility of changing permissions for options at runtime. This requires the use of the authorization Policy syntax which allows us to authorize within code and does not limit us to the declaration within the ConfigureServices method. We want to create declarations such as:
```c#
options.AddPolicy(“Administrator”, policy=>policy.Requirements.Add(new AdministratorRequirement()));
```
Remember that this method uses dependency injection and authorization handlers must be declared as services or it will not work.

The AdministratorRequirement handler will evaluate the access permissions. We already have methods to establish relationships between users and roles but we also need to effectively establish a dynamic relationship between policies and roles. This is what claims allow us to do. We will effectively treat claims as privileges assigned to roles. Access can be configured as granular as it needs to be. We can create read employee claims or write employee claims or whatever form of privilege we can think of for the particular application domain.

As an example, completely abstract, we will configure access for three levels administrator, supervisor and employee. So in our ConfigureServices method we will create
```c#
options.AddPolicy(“Administrator”, policy=>policy.Requirements.Add(new AdministratorRequirement()));
options.AddPolicy(“Supervisor”, policy=>policy.Requirements.Add(new SupervisorRequirement()));
options.AddPolicy(“Employee”, policy=>policy.Requirements.Add(new EmployeeRequirement()));
```
We should avoid using string literals. Create a new folder call it Constants and create a class, PolicyNames, to declare our policy names.
```c#
namespace AuthorizationTest1.Constants
{
    public static class PolicyNames
    {
        public const string AdministratorPolicy = "Administrator";
        public const string SupervisorPolicy = "Supervisor";
        public const string EmployeePolicy = "Employee";
    }
} 
```
We declare our policies in the ConfigureServices method of Startup.cs
```c#
services.AddAuthorization(options =>
            {
                options.AddPolicy(PolicyNames.AdministratorPolicy, policy => policy.Requirements.Add(new AuthorizationNameRequirement(PolicyNames.AdministratorPolicy)));
                options.AddPolicy(PolicyNames.SupervisorPolicy, policy => policy.Requirements.Add(new AuthorizationNameRequirement(PolicyNames.SupervisorPolicy)));
                options.AddPolicy(PolicyNames.EmployeePolicy, policy => policy.Requirements.Add(new AuthorizationNameRequirement(PolicyNames.EmployeePolicy)));
            });
```
One authorization requirement will be used passing the policy name as a parameter to identify the claim being checked. 

These three policies can be used to decorate our actions setting the required access levels
```c#
[Authorize(Policy = PolicyNames.AdministratorPolicy)] 
```
Policies are always pre-determined, so we need to think about the different and unique access levels needed, e.g. access to view, edit and delete so that we can create them as policies.

The best way to understand this approach is to consider a user who is assigned one or more roles and each of those roles can be assigned one or more claims (in our instance claim is synonymous with policy), effectively classifying the user under one or more policies. The handler for a particular policy then checks if the user has the corresponding claim. Therefore we also need to create views to assign and remove claims from roles, understanding that there is a one to one relationship between policies and claims. We also need to create a list representing all the possible claims. To reduce possible programming errors where a claim may not be associated with a policy, we will create the list based on the policy names.

Create a static list under the Constants folder called ClaimNames made up of the policy names
```c#
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
```
We add two new view models ClaimsViewModel and RoleClaimsViewModel to facilitate adding claims to roles.
```c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationTest1.Models.ApplicationUserViewModels
{
    public class ClaimsViewModel
    {
        public string Id { get; set; }
        public string ClaimName { get; set; }
        public Boolean HasClaim { get; set; }
    }
}
```

```c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationTest1.Models.ApplicationUserViewModels
{
    public class RoleClaimsViewModel
    {
        public string Id { get; set; }
        public string RoleName { get; set; }

        public List<ClaimsViewModel> RoleClaims { get; set; }
    }
}
```
We should probably modify the role create and edit views allowing claims to be added but we may not use this authorization technique in all applications so let’s just create it as a separate view and add a link to it in the Role Name list view.

In ApplicationRoleController we create a new get and post method called ManageClaim.

For our get method we use the GetClaimsAsync method of the Role Manager to populate the RoleClaimsViewModel view model looping through our list of claims contained in our ClaimName list. We are only concerned with the existence of the claim so only the claim type is used for comparison.
```c#
        public async Task<ActionResult> ManageClaim(string id)
        {
            RoleClaimsViewModel viewModel = new RoleClaimsViewModel();
            if (!String.IsNullOrEmpty(id))
            {
                IdentityRole identityRole = await roleManager.FindByIdAsync(id);
                if (identityRole != null)
                {
                    //Get claims associated with the role
                    IList<Claim> roleClaimList = await roleManager.GetClaimsAsync(identityRole);
                    List<string> roleClaimTypeList = new List<string>();
                    foreach (var roleClaim in roleClaimList)
                    {
                        roleClaimTypeList.Add(roleClaim.Type);
                    }

                    viewModel.Id = identityRole.Id;
                    viewModel.RoleName = identityRole.Name;
                    viewModel.RoleClaims = new List<ClaimsViewModel>();

                    foreach (var claimName in ClaimNames.ClaimName)
                    {
                        viewModel.RoleClaims.Add(new ClaimsViewModel() {ClaimName=claimName, HasClaim=roleClaimTypeList.Contains(claimName)});
                    } 
                   return View("ManageClaim",viewModel);     
                }
            }
            return RedirectToAction("Index");
        }
```
The post method is similar to the user’s post method where the user’s roles were being managed. The claim types are extracted from the claims for comparison as we are maintaining a static list of claim types or names and not claims.
```c#
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ManageClaim(string id, RoleClaimsViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                if (!String.IsNullOrEmpty(id))
                {
                    IdentityRole identityRole = await roleManager.FindByIdAsync(id);
                    if (identityRole != null)
                    {
                        //Get claims associated with the role
                        IList<Claim> roleClaimList = await roleManager.GetClaimsAsync(identityRole);
                        //Extract the claim type
                        List<string> roleClaimTypeList = new List<string>();
                        foreach (var roleClaim in roleClaimList)
                        {
                            roleClaimTypeList.Add(roleClaim.Type);
                        }

                        foreach (var roleClaim in viewModel.RoleClaims)
                        {
                            //create a new claim with the claim name
                            Claim claim = new Claim(roleClaim.ClaimName, "");
                            //get the associated claim from the role's claim list
                            Claim associatedClaim = roleClaimList.Where (x => x.Type==roleClaim.ClaimName).FirstOrDefault();

                            if (roleClaim.HasClaim && !roleClaimTypeList.Contains(roleClaim.ClaimName))
                            {
                                IdentityResult claimResult = await roleManager.AddClaimAsync(identityRole,claim);
                                if (!claimResult.Succeeded)
                                {
                                    //TODO log details and display some sort of error
                                }
                            }
                            else if (!roleClaim.HasClaim && roleClaimTypeList.Contains(roleClaim.ClaimName))
                            {
                                IdentityResult claimResult = await roleManager.RemoveClaimAsync(identityRole, associatedClaim);
                                if (!claimResult.Succeeded)
                                {
                                    //TODO log details and display some sort of error
                                }
                            }
                        }
                    }
                    return RedirectToAction("Index");
                }

            }
            return View();
        }
```
We add the link to our role management Index.cshtml
```c#
                <a asp-action="ManageClaim" asp-route-id="@item.Id">Manage Claims</a> |
```
and create the new view ManageClaim.cshtml
```c#
@model AuthorizationTest1.Models.ApplicationUserViewModels.RoleClaimsViewModel
@{
    ViewData["Title"] = "ManageClaim";
}
<h2>ManageClaim</h2>
<h4>RoleClaimsViewModel</h4>
<hr />
<div class="row">
    <div class="col-md-8">
        <form asp-action="ManageClaim">
            <div class="row">
                <div class="col-md-4">
                    <div asp-validation-summary="ModelOnly" class="text-danger"></div>
                    <input type="hidden" asp-for="Id" />
                    <div class="form-group">
                        <label asp-for="RoleName" class="control-label"></label>
                        <input asp-for="RoleName" class="form-control" readonly />
                        <span asp-validation-for="RoleName" class="text-danger"></span>
                    </div>
                </div>
            </div>
            <div class="row">
                @{ int i = 0; }
                @foreach (var item in Model.RoleClaims)
                {
                    <div class="form-group col-sm-2">
                        <div class="checkbox">
                            <label>
                                <input asp-for="RoleClaims[i].HasClaim" /> @Html.DisplayFor(model => item.ClaimName)
                            </label>
                        </div>
                    </div>
                    //create two hidden fields for the id and rolename values
                    <input type="hidden" asp-for="RoleClaims[i].Id" />
                    <input type="hidden" asp-for="RoleClaims[i].ClaimName" />

                    i++;
                }

            </div>
            <div class="row">
                <div class="col-md-4">
                    <div class="form-group">
                        <input type="submit" value="Save" class="btn btn-default" />
                    </div>
                </div>
            </div>
        </form>
    </div>
</div>
<div>
    <a asp-action="Index">Back to List</a>
</div>
@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```
We need to complete our authorization by defining the authorization requirement and handler.
Create a new folder naming it Authorization and create a class named AuthorizationNameRequirement
```c#
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
```
Create the authorization handler class naming it AuthorizationNameHandler which checks to see if the current user has a claim with the claim type passed as the parameter.
```c#
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationTest1.Authorization
{
    public class AuthorizationNameHandler : AuthorizationHandler<AuthorizationNameRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizationNameRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == requirement.AuthorizationName))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
        
    }
}
```
Oh yeah, forgot this as usual. We are using dependency injection therefore add
```c#
            services.AddSingleton<IAuthorizationHandler, AuthorizationNameHandler>();
```
to Startup.cs


The default login screen needs to be modified for the use of usernames other than those formatted as an email address. For testing purposes we can modify the email property in LoginViewModel.cs (in the Models – AccountViewModels folder) by removing the EmailAddress annotation and use the default login screen to test the authorization.

Modify the HomeController by decorating the About action with an Authorize policy. The example below shows the EmployeePolicy being used.
```c#
[Authorize(Policy=PolicyNames.EmployeePolicy)]
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }  
```
Create a new user without assigning a role. Login and select About. You should be prompted with a do not have access message.
 
Create a role and assign the Employee claim. Assign the role to the employee, log out, and log back in. You should now have access to About.
This only serves as a proof of concept the code is far from production ready.
