using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AuthorizationTest1.Models.ApplicationUserViewModels;
using AuthorizationTest1.Constants;
using System.Security.Claims;

namespace AuthorizationTest1.Controllers
{
    public class ApplicationRoleController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;
        
        public ApplicationRoleController(RoleManager<IdentityRole> roleManager)
        {
            this.roleManager = roleManager;
        }
        
        // GET: ApplicationRole
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

        // GET: ApplicationRole/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ApplicationRole/Create
        public ActionResult Create()
        {
            ApplicationRolesViewModel viewModel = new ApplicationRolesViewModel();
            return View(viewModel);
        }

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
//display message indicating problem editing
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


    }
}