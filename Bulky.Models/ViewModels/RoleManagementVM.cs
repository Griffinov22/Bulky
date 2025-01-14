using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bulky.Models.ViewModels
{
    public class RoleManagementVM
    {
        public ApplicationUser User {  get; set; }
        [DisplayName("Company List")]
        public IEnumerable<SelectListItem> CompanyList { get; set; }
        public int? CompanyId { get; set; }
        [DisplayName("Roles List")]
        public IEnumerable<SelectListItem> RoleList { get; set; }
        [Required]
        public required string RoleId { get; set; }

    }
}
