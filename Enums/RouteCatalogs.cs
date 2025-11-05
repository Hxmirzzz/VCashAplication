namespace VCashApp.Enums
{
    /// <summary>
    /// Proporciona definiciones de catálogos para códigos relacionados con rutas y su lógica de validación.
    /// Contiene clases anidadas para tipos de ruta, tipos de servicio y tipos de vehículo utilizados en el sistema VCash.
    /// </summary>
    public static class RouteCatalogs
    {
        /// <summary>
        /// Define códigos de tipos de ruta y sus etiquetas correspondientes.
        /// Las rutas pueden ser tradicionales, basadas en ATM, mixtas o de liberación de efectivo.
        /// </summary>
        public static class RouteTypeCode
        {
            /// <summary>
            /// Código de ruta tradicional.
            /// </summary>
            public const string Traditional = "T";

            /// <summary>
            /// Código de ruta ATM.
            /// </summary>
            public const string ATM = "A";

            /// <summary>
            /// Código de ruta mixta (combinación de tradicional y ATM).
            /// </summary>
            public const string Mixed = "M";

            /// <summary>
            /// Código de ruta de liberación de efectivo.
            /// </summary>
            public const string CashLiberation = "L";

            /// <summary>
            /// Valida si el código proporcionado es un tipo de ruta válido.
            /// </summary>
            /// <param name="code">El código de tipo de ruta a validar.</param>
            /// <returns>True si el código es válido (T, A, M o L); de lo contrario, false.</returns>
            public static bool IsValid(string? code) =>
                code is "T" or "A" or "M" or "L";

            /// <summary>
            /// Convierte un código de tipo de ruta a su etiqueta legible en español.
            /// </summary>
            /// <param name="code">El código de tipo de ruta.</param>
            /// <returns>La etiqueta correspondiente o "Desconocido" si el código es inválido.</returns>
            public static string ToLabel(string? code) => code switch
            {
                "T" => "TRADICIONAL",
                "A" => "ATM",
                "M" => "MIXTA",
                "L" => "LIBERACIÓN EFECTIVO",
                _ => "Desconocido"
            };
        }

        /// <summary>
        /// Define códigos de tipos de servicio que representan diferentes períodos de tiempo para servicios de ruta.
        /// Los servicios pueden programarse para mañana, tarde o franjas horarias adicionales.
        /// </summary>
        public static class ServiceTypeCode
        {
            /// <summary>
            /// Código de servicio matutino (AM).
            /// </summary>
            public const string Morning = "AM";

            /// <summary>
            /// Código de servicio vespertino (PM).
            /// </summary>
            public const string Afternoon = "PM";

            /// <summary>
            /// Código de servicio adicional (fuera del horario regular).
            /// </summary>
            public const string Additional = "AD";

            /// <summary>
            /// Valida si el código proporcionado es un tipo de servicio válido.
            /// </summary>
            /// <param name="code">El código de tipo de servicio a validar.</param>
            /// <returns>True si el código es válido (AM, PM o AD); de lo contrario, false.</returns>
            public static bool IsValid(string? code) =>
                code is "AM" or "PM" or "AD";

            /// <summary>
            /// Convierte un código de tipo de servicio a su etiqueta legible en español.
            /// </summary>
            /// <param name="code">El código de tipo de servicio.</param>
            /// <returns>La etiqueta correspondiente o "Desconocido" si el código es inválido.</returns>
            public static string ToLabel(string? code) => code switch
            {
                "AM" => "MAÑANA",
                "PM" => "TARDE",
                "AD" => "ADICIONAL",
                _ => "Desconocido"
            };
        }

        /// <summary>
        /// Define códigos de tipos de vehículo utilizados para el transporte en rutas.
        /// Incluye vehículos blindados, motocicletas, camionetas y camiones.
        /// </summary>
        public static class VehicleTypeCode
        {
            /// <summary>
            /// Código de vehículo blindado.
            /// </summary>
            public const string Armored = "B";

            /// <summary>
            /// Código de motocicleta.
            /// </summary>
            public const string Motorcycle = "M";

            /// <summary>
            /// Código de camioneta.
            /// </summary>
            public const string Van = "C";

            /// <summary>
            /// Código de camión.
            /// </summary>
            public const string Truck = "T";

            /// <summary>
            /// Valida si el código proporcionado es un tipo de vehículo válido.
            /// </summary>
            /// <param name="code">El código de tipo de vehículo a validar.</param>
            /// <returns>True si el código es válido (B, M, C o T); de lo contrario, false.</returns>
            public static bool IsValid(string? code) =>
                code is "B" or "M" or "C" or "T";

            /// <summary>
            /// Convierte un código de tipo de vehículo a su etiqueta legible en español.
            /// </summary>
            /// <param name="code">El código de tipo de vehículo.</param>
            /// <returns>La etiqueta correspondiente o "Desconocido" si el código es inválido.</returns>
            public static string ToLabel(string? code) => code switch
            {
                "B" => "BLINDADO",
                "M" => "MOTO",
                "C" => "CAMIONETA",
                "T" => "CAMIÓN",
                _ => "Desconocido"
            };
        }
    }
}