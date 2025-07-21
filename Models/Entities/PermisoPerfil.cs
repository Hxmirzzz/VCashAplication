// En Models/Entities/PermisoPerfil.cs (Versión corregida y final)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity; // Necesario para IdentityRole
using VCashApp.Models.Entities; // Necesario para AdmVista

namespace VCashApp.Models.Entities
{
    public class PermisoPerfil
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string CodPerfilId { get; set; }
        [ForeignKey("CodPerfilId")]
        public virtual IdentityRole? Perfil { get; set; }

        public string CodVista { get; set; }
        [ForeignKey("CodVista")]
        public virtual AdmVista? Vista { get; set; }

        public bool PuedeVer { get; set; }
        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
    }
}