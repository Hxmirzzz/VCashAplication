using System; // Para DateOnly
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VCashApp.Models;
using VCashApp.Models.Entities;

namespace VCashApp.Models.Entities
{
    public class SegRegistroEmpleado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CodCedula { get; set; }
        [ForeignKey("CodCedula")]
        public virtual AdmEmpleado? Empleado { get; set; }

        public int CodCargo { get; set; }
        [ForeignKey("CodCargo")]
        public virtual AdmCargo? Cargo { get; set; }

        public string CodUnidad { get; set; }
        [ForeignKey("CodUnidad")]
        public virtual AdmUnidad? Unidad { get; set; }

        public int CodSucursal { get; set; }
        [ForeignKey("CodSucursal")]
        public virtual AdmSucursal? Sucursal { get; set; }


        // --- Datos Propios del Registro (Horas/Fechas) ---
        public DateOnly FechaEntrada { get; set; } // NOT NULL
        public TimeOnly HoraEntrada { get; set; } // NOT NULL
        public DateOnly? FechaSalida { get; set; } // NULLABLE
        public TimeOnly? HoraSalida { get; set; } // NULLABLE

        public bool IndicadorEntrada { get; set; } // BIT NOT NULL
        public bool IndicadorSalida { get; set; } // BIT NOT NULL

        // --- Auditoría ---
        public string RegistroUsuarioId { get; set; }
        [ForeignKey("RegistroUsuarioId")]
        public virtual ApplicationUser? UsuarioRegistro { get; set; }

        public string? PrimerNombreEmpleado { get; set; }
        public string? SegundoNombreEmpleado { get; set; }
        public string? PrimerApellidoEmpleado { get; set; }
        public string? SegundoApellidoEmpleado { get; set; }
        public string? NombreCargoEmpleado { get; set; }
        public string? NombreUnidadEmpleado { get; set; }
        public string? NombreSucursalEmpleado { get; set; }
    }
}