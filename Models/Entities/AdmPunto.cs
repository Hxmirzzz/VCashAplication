using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    public class AdmPunto
    {
        [Key]
        public string CodigoPunto { get; set; } = null!;
        public string? CodPuntoVatco { get; set; }
        public int? CodigoCliente { get; set; }
        [ForeignKey("CodigoCliente")]
        //public virtual AdmCliente? Cliente { get; set; }
        public string? CodPuntoCliente { get; set; }
        public int? CodClientePrincipal { get; set; }
        public string? NombrePunto { get; set; }
        public string? NombreCorto { get; set; }
        public string? PuntoFacturacion { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Responsable { get; set; }
        public string? CargoResponsable { get; set; }
        public string? CorreoResponsable { get; set; }
        public int? CodigoSucursal { get; set; }
        [ForeignKey("CodigoSucursal")]
        public virtual AdmSucursal? Sucursal { get; set; }
        public int? CodigoCiudad { get; set; }
        [ForeignKey("CodigoCiudad")]
        public virtual AdmCiudad? Ciudad { get; set; }
        public string? Latitud { get; set; }
        public string? Longitud { get; set; }
        public int? RadioPunto { get; set; }
        public int? BaseCambio { get; set; }
        public int? LlavesPunto { get; set; }
        public int? SobresPunto { get; set; }
        public int? ChequesPunto { get; set; }
        public int? FondoPunto { get; set; }
        public string? CodigoFondo { get; set; }
        [ForeignKey("CodigoFondo")]
        public virtual AdmFondo? Fondo { get; set; }
        public int? TrasladoPunto { get; set; }
        public string? CoberturaPunto { get; set; }
        public DateOnly? FechaIngreso { get; set; }
        public DateOnly? FechaRetiro { get; set; }
        public int? TipoPunto { get; set; }
        public string? CodigoRutaSuc { get; set; }
        [ForeignKey("CodigoRutaSuc")]
        public virtual AdmRuta? Ruta { get; set; }
        public int? TipoNegocio { get; set; }
        public int? DocumentosPunto { get; set; }
        public int? ExistenciasPunto { get; set; }
        public int? PrediccionPunto { get; set; }
        public int? CustodiaPunto { get; set; }
        public int? OtrosValoresPunto { get; set; }
        public string? Otros { get; set; }
        public int? LiberacionEfectivoPunto { get; set; }
        public int? EscalaInterurbanos { get; set; }
        public string? CodCas4u { get; set; }
        public string? NivelRiesgo { get; set; }
        public string? CodigoRango { get; set; }
        [ForeignKey("CodigoRango")]
        //public virtual AdmRango? RangoAtencion { get; set; }
        public string? InfoRangoAtencion { get; set; }
        public int? Bateria { get; set; }
        public int? BateriaAtm { get; set; }
        public int? LocalizacionAtm { get; set; }
        public int? EmergenciaAtm { get; set; }
        public DateOnly? PrimeraProvision { get; set; }
        public int? MarcaAtm { get; set; }
        public int? ModalidadAtm { get; set; }
        public string? CodigoSeteo { get; set; }
        [ForeignKey("CodigoSeteo")]
        //public virtual AdmSeteo? Seteo { get; set; }
        public string? DivisaAtm { get; set; }
        public int? SolicitudWsAtm { get; set; }
        public string? TipoAtm { get; set; }
        public string? PorcentajeAgotamiento { get; set; }
        public int? CriticidadAtm { get; set; }
        public int? Consignacion { get; set; }
        public string? CodigoComposicion { get; set; }
        [ForeignKey("CodigoComposicion")]
        // public virtual AdmComposicion? Composicion { get; set; }
        public bool Estado { get; set; }
        public string? UsuarioRegistroId { get; set; }
        public virtual ApplicationUser? UsuarioRegistro { get; set; }
    }
}