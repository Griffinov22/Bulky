using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class ApplicationUserRoleRepository : Repository<ApplicationUserRole>, IApplicationUserRoleRepository
    {
        private ApplicationDbContext _db;
        public ApplicationUserRoleRepository(ApplicationDbContext db) : base(db) 
        {
            _db = db;
        }

        public void Update(ApplicationUserRole obj)
        {
            _db.ApplicationUserRoles.Update(obj);
        }

    }
}
