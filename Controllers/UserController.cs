using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AuthorizationTest1.Models;
using AuthorizationTest1.Models.ApplicationUserViewModels;

namespace AuthorizationTest1.Controllers
{
    public class UserController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;

        public UserController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
        }
        // GET: User
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

            //viewModel = userManager.Users.Select(user => new UserListViewModel
            //{
            //    Id = user.Id,
            //    UserName = user.UserName,
            //    Name = user.Surname + ", " + user.FirstName + user.MiddleName,
            //    LockoutEnabled = user.LockoutEnabled,
            //    RoleNames = string.Join(", ", userManager.GetRolesAsync(user))
            //}).ToList();

            return View(viewModel);
        }

        // GET: User/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: User/Create
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

        // POST: User/Create
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

        // GET: User/Edit/5
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

                    //populate HasRole boolean field if the user has the role
                    //get all user's roles
                    IList<string> userRolesList = new List<string>();
                    userRolesList = await userManager.GetRolesAsync(user);
                    //not an efficient method would have to change to a different data structure other than list
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

        // POST: User/Edit/5
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

        // GET: User/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            string userName = string.Empty;
            if (!String.IsNullOrEmpty(id))
            {
                ApplicationUser user = await userManager.FindByIdAsync(id);
                if (user != null)
                {
                    userName = user.Surname + ", " + user.FirstName + " " + user.MiddleName;
                }
            }
            return View("Delete", userName);
        }

        // POST: User/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            try
            {
                if (!String.IsNullOrEmpty(id))
                {
                    ApplicationUser user = await userManager.FindByIdAsync(id);
                    if (user != null)
                    {
                        IdentityResult userResult = await userManager.DeleteAsync(user);

                        if (userResult.Succeeded)
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
                return View();
            }
        }


        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }

}