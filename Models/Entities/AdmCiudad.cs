using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    public class AdmCiudad
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CodCiudad { get; set; }

        public string? NombreCiudad { get; set; }

        public string? CodDepartamento { get; set; }
        [ForeignKey("CodDepartamento")]
        public virtual AdmDepartamento? Departamento { get; set; }

        public string? CodPais { get; set; }
        [ForeignKey("CodPais")]
        public virtual AdmPais? Pais { get; set; }

        public int? CodSucursal { get; set; }
        [ForeignKey("CodSucursal")]
        public virtual AdmSucursal? Sucursal { get; set; }

        public bool Estado { get; set; }
    }
}