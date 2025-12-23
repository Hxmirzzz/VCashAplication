using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.Dtos.Point
{
    /// <summary>
    /// Modelo DTO para la creación o actualización de un punto.
    /// </summary>
    public sealed class PointUpsertDto
    {
        /// <summary>Código del punto</summary>
        [Required]
        public string CodPunto { get; set; } = string.Empty;
        /// <summary>Código VATCO del punto</summary>
        [Required]
        public string? VatcoPointCode { get; set; } = string.Empty;
        /// <summary>Código del punto según cliente</summary>
        [Required]
        public string CodPCliente { get; set; } = string.Empty;
        /// <summary>Código del cliente</summary>
        [Required]
        public int CodCliente { get; set; }
        /// <summary>Tipo de punto</summary>
        [Required]
        public int TipoPunto { get; set; }
        /// <summary>Código del cliente principal</summary>
        public int? CodClientePpal { get; set; }
        /// <summary>Nombre del punto.</summary>
        [Required]
        public string? NombrePunto { get; set; }
        /// <summary>Nombre corto del punto.</summary>
        public string? NombreCorto { get; set; }
        /// <summary>Punto de facturación.</summary>
        public string? NombrePuntoFact { get; set; }
        /// <summary>Dirección del punto.</summary>
        public string? DirPunto { get; set; }
        /// <summary>Teléfono del punto.</summary>
        public string? TelPunto { get; set; }
        /// <summary>Responsable del punto.</summary>
        public string? RespPunto { get; set; }
        /// <summary>Cargo del responsable del punto.</summary>
        public string? CargoRespPunto { get; set; }
        /// <summary>Correo del responsable del punto.</summary>
        public string? CorreoRespPunto { get; set; }
        /// <summary>Código de la sucursal asociada al punto.</summary>
        [Required]
        public int CodSuc { get; set; }
        /// <summary>Código de la ciudad asociada al punto.</summary>
        [Required]
        public int CodCiudad { get; set; }
        /// <summary>Latitud.</summary>
        public string? LatPunto { get; set; }
        /// <summary>Longitud.</summary>
        public string? LngPunto { get; set; }
        /// <summary>Radio de cobertura del punto.</summary>
        public int? RadioPunto { get; set; }
        /// <summary>Base de cambio del punto.</summary>
        public int BaseCambio { get; set; }
        /// <summary>Llaves del punto.</summary>
        public int LlavesPunto { get; set; }
        /// <summary>Sobres del punto.</summary>
        public int SobresPunto { get; set; }
        /// <summary>Cheques del punto.</summary>
        public int ChequesPunto { get; set; }
        /// <summary>Documentos del punto.</summary>
        public int DocumentosPunto { get; set; }
        /// <summary>Existencias del punto.</summary>
        public int ExistenciasPunto { get; set; }
        /// <summary>Prediccion del punto.</summary>
        public int PrediccionPunto { get; set; }
        /// <summary>Custodia del punto.</summary>
        public int CustodiaPunto { get; set; }
        /// <summary>Otros valores del punto.</summary>
        public int OtrosValoresPunto { get; set; }
        /// <summary>Descripcion.</summary>
        public string? Otros { get; set; }
        /// <summary>Liberacion del punto.</summary>
        public int LiberacionEfectivoPunto { get; set; }
        /// <summary>Fondo del punto.</summary>
        public int FondoPunto { get; set; }
        /// <summary>Código del fondo.</summary>
        public string? CodFondo { get; set; }
        /// <summary>Ruta del punto.</summary>
        [Required]
        public string? CodRutaSuc { get; set; }
        /// <summary>Rango del punto.</summary>
        [Required]
        public int? CodRango { get; set; }
        /// <summary>Tipo de negocio del punto.</summary>
        [Required]
        public int? BusinessType { get; set; }

        /// <summary>Fecha de ingreso del punto.</summary>
        [DataType(DataType.Date)]
        public DateOnly FecIngreso { get; init; }
        /// <summary>Fecha de retiro del punto.</summary>
        [DataType(DataType.Date)]
        public DateOnly? FecRetiro { get; init; }
        /// <summary>Codigo Cas4U.</summary>
        public string? CodCas4u { get; set; }
        /// <summary>Indica si el punto tiene riesgo.</summary>
        public string NivelRiesgo { get; set; } = "M";
        /// <summary>Cobertura del punto.</summary>
        public string CoberturaPunto { get; set; } = "U";
        /// <summary>Escala.</summary>
        public int EscalaInterurbanos { get; set; } = 0;
        /// <summary>Indica si el punto maneja consignación.</summary>
        public int Consignacion { get; set; }
        /// <summary>Estado del punto (activo/inactivo).</summary>
        public bool EstadoPunto { get; set; }
        /// <summary>Ruta del archivo de la carta asociada al punto.</summary>
        public string? CartaFilePath { get; set; }
    }
}
