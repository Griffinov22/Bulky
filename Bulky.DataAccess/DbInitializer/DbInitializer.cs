using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ApplicationDbContext _db;
        public DbInitializer(UserManager<ApplicationUser> userManager, RoleManager<Role> roleManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public async Task Initialize() 
        {
            // migrations if they are not applied
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex) { }

            // create roles if they are not created
            if (!await _roleManager.RoleExistsAsync(SD.Role_Customer))
            {
                await _roleManager.CreateAsync((Role) new IdentityRole(SD.Role_Customer));
                await _roleManager.CreateAsync((Role) new IdentityRole(SD.Role_Company));
                await _roleManager.CreateAsync((Role) new IdentityRole(SD.Role_Employee));
                await _roleManager.CreateAsync((Role) new IdentityRole(SD.Role_Admin));

                // if roles are not created, then will create admin user as well
                await _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "admin@gmail.com",
                    Email = "admin@gmail.com",
                    Name = "Admin User",
                    PhoneNumber = "111-111-1111",
                    StreetAddress = "test 123 Drive",
                    State = "IL",
                    PostalCode = "89012",
                    City = "Chicago",

                }, "REDACTED");

                ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@gmail.com")!;
                await _userManager.AddToRoleAsync(user, SD.Role_Admin);
            }

            return;
            

        }
    }
}
