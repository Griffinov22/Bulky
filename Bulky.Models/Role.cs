using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models
{
    public class Role : IdentityRole
    {
        public Role(string roleName) : base(roleName) { }
        public Role() { }
    }
}
