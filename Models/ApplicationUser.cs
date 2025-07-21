using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string? NombreUsuario { get; set; }
    }
}