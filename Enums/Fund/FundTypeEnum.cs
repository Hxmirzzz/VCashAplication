using System.ComponentModel.DataAnnotations;

namespace VCashApp.Enums.Fund
{
    /// <summary>
    /// Tipo de fondo.
    /// </summary>
    public enum FundTypeEnum
    {
        /// <summary>
        /// Indicador oficina.
        /// </summary>
        [Display(Name = "Oficina")]
        OFICINA = 0,

        /// <summary>
        /// Indicador ATM/Cajero.
        /// </summary>
        [Display(Name = "ATM/Cajero")]
        ATM = 1
    }
}