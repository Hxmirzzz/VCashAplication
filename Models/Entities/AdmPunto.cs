using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCashApp.Models.Entities
{
    [Table("AdmPuntos")]
    public class AdmPunto
    {
        [Key]
        [Column("CodigoPunto")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [StringLength(450)]
        public string PointCode { get; set; } = null!;

        [Column("CodPuntoVatco")]
        [StringLength(255)]
        public string? VatcoPointCode { get; set; }

        [Column("CodigoCliente")]
        [ForeignKey("ClientCode")]
        public int? ClientCode { get; set; }
        public virtual AdmCliente? Client { get; set; }

        [Column("CodPuntoCliente")]
        [StringLength(255)]
        public string? ClientPointCode { get; set; }

        [Column("CodClientePrincipal")]
        public int? MainClientCode { get; set; }

        [Column("NombrePunto")]
        [StringLength(255)] 
        public string? PointName { get; set; }

        [Column("NombreCorto")]
        [StringLength(255)]
        public string? ShortName { get; set; }

        [Column("PuntoFacturacion")]
        [StringLength(255)]
        public string? BillingPoint { get; set; }

        [Column("Direccion")]
        [StringLength(255)]
        public string? Address { get; set; }

        [Column("Telefono")]
        [StringLength(50)]
        public string? PhoneNumber { get; set; }

        [Column("Responsable")]
        [StringLength(255)]
        public string? Responsible { get; set; }

        [Column("CargoResponsable")]
        [StringLength(255)]
        public string? ResponsiblePosition { get; set; }

        [Column("CorreoResponsable")]
        [StringLength(255)]
        public string? ResponsibleEmail { get; set; }

        [Column("CodigoSucursal")]
        [ForeignKey("BranchCode")]
        public int? BranchCode { get; set; }
        public virtual AdmSucursal? Branch { get; set; }

        [Column("CodigoCiudad")]
        [ForeignKey("CityCode")]
        public int? CityCode { get; set; }
        public virtual AdmCiudad? City { get; set; }

        [Column("Latitud")]
        [StringLength(50)]
        public string? Latitude { get; set; }

        [Column("Longitud")]
        [StringLength(50)]
        public string? Longitude { get; set; }

        [Column("RadioPunto")]
        [StringLength(50)]
        public string? PointRadius { get; set; }

        [Column("BaseCambio")]
        public bool ChangeBase { get; set; }

        [Column("LlavesPunto")]
        public bool PointKeys { get; set; }

        [Column("SobresPunto")]
        public bool PointEnvelopes { get; set; }

        [Column("ChequesPunto")]
        public bool PointChecks { get; set; }

        [Column("FondoPunto")]
        public int? PointFund { get; set; }

        [Column("CodigoFondo")]
        [StringLength(450)]
        [ForeignKey("FundCode")]
        public string? FundCode { get; set; }
        public virtual AdmFondo? Fund { get; set; }

        [Column("TrasladoPunto")]
        public bool PointTransfer { get; set; }

        [Column("CoberturaPunto")]
        [StringLength(255)]
        public string? PointCoverage { get; set; }

        [Column("FechaIngreso", TypeName = "DATE")]
        public DateOnly? EntryDate { get; set; }

        [Column("FechaRetiro", TypeName = "DATE")]
        public DateOnly? WithdrawalDate { get; set; }

        [Column("TipoPunto")]
        public int? PointType { get; set; } // TipoPunto: 0=oficina/punto, 1=ATM

        [Column("CodigoRutaSuc")]
        [StringLength(450)]
        [ForeignKey("RouteBranchCode")]
        public string? RouteBranchCode { get; set; }
        public virtual AdmRuta? Route { get; set; }

        [Column("TipoNegocio")]
        public int? BusinessType { get; set; }

        [Column("DocumentosPunto")]
        public bool PointDocuments { get; set; }

        [Column("ExistenciasPunto")]
        public bool PointStock { get; set; }

        [Column("PrediccionPunto")]
        public bool PointPrediction { get; set; }

        [Column("CustodiaPunto")]
        public bool PointCustody { get; set; }

        [Column("OtrosValoresPunto")]
        public bool PointOtherValues { get; set; }

        [Column("Otros")]
        [StringLength(255)]
        public string? Others { get; set; }

        [Column("LiberacionEfectivoPunto")]
        public bool CashReleasePoint { get; set; }

        [Column("EscalaInterurbanos")]
        public int? InterurbanScale { get; set; }

        [Column("CodCas4u")]
        [StringLength(255)]
        public string? Cas4uCode { get; set; }

        [Column("NivelRiesgo")]
        [StringLength(255)]
        public string? RiskLevel { get; set; }

        [Column("CodigoRango")]
        [StringLength(255)]
        public string? RangeCode { get; set; }
        // [ForeignKey("CodigoRango")] // Comentado ya que no hay AdmRango en DbContext
        // public virtual AdmRango? RangoAtencion { get; set; }

        [Column("InfoRangoAtencion")]
        [StringLength(255)]
        public string? RangeAttentionInfo { get; set; }

        [Column("Bateria")]
        public bool Battery { get; set; }

        [Column("BateriaAtm")]
        public int? AtmBattery { get; set; }

        [Column("LocalizacionAtm")]
        public int? AtmLocation { get; set; }

        [Column("EmergenciaAtm")]
        public bool? AtmEmergency { get; set; }

        [Column("PrimeraProvision", TypeName = "DATE")]
        public DateOnly? FirstProvision { get; set; }

        [Column("MarcaAtm")]
        public int? AtmBrand { get; set; }

        [Column("ModalidadAtm")]
        public int? AtmModality { get; set; }

        [Column("CodigoSeteo")]
        [StringLength(255)]
        public string? SettingCode { get; set; }
        // [ForeignKey("CodigoSeteo")] // Comentado ya que no hay AdmSeteo en DbContext
        // public virtual AdmSeteo? Seteo { get; set; }

        [Column("DivisaAtm")]
        [StringLength(50)]
        public string? AtmCurrency { get; set; } 

        [Column("SolicitudWsAtm")]
        public int? AtmWsRequest { get; set; }

        [Column("TipoAtm")]
        [StringLength(50)]
        public string? AtmType { get; set; }

        [Column("PorcentajeAgotamiento")]
        [StringLength(50)]
        public string? ExhaustionPercentage { get; set; }

        [Column("CriticidadAtm")]
        public int? AtmCriticality { get; set; }

        [Column("Consignacion")]
        public int? Consignment { get; set; }

        [Column("CodigoComposicion")]
        [StringLength(255)]
        public string? CompositionCode { get; set; }
        // [ForeignKey("CodigoComposicion")] // Comentado ya que no hay AdmComposicion en DbContext
        // public virtual AdmComposicion? Composicion { get; set; }

        [Column("Estado")]
        public bool Status { get; set; }
    }
}