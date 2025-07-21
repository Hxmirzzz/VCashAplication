using VCashApp.Models.Entities;
using VCashApp.Services.DTOs;

namespace VCashApp.Services
{
    /// <summary>
    /// Interfaz para los servicios de lógica de negocio relacionados con la gestión de Rutas Diarias.
    /// Define las operaciones disponibles para las diferentes fases de una ruta,
    /// desde la planeación hasta el cierre, y es consumida por los controladores.
    /// </summary>
    public interface IRutaDiariaService
    {
        // PLANEADOR

        /// <summary>
        /// Crea un nuevo registro de Ruta Diaria en su estado inicial (GENERADO).
        /// Este método es utilizado para la creación manual de rutas.
        /// </summary>
        /// <param name="nuevaRuta">El objeto <see cref="TdvRutaDiaria"/> con los datos iniciales de la ruta.</param>
        /// <returns>La instancia de <see cref="TdvRutaDiaria"/> recién creada con su ID asignado.</returns>
        Task<TdvRutaDiaria> CrearRutaDiariaInicialAsync(TdvRutaDiaria nuevaRuta);

        /// <summary>
        /// Obtiene una lista de rutas diarias que se encuentran en estado 'GENERADO' para una fecha y sucursal específicas,
        /// adecuadas para que el planeador realice la asignación de recursos.
        /// </summary>
        /// <param name="fechaEjecucion">La fecha de ejecución de las rutas a buscar.</param>
        /// <param name="codSucursal">El código de la sucursal a la que pertenecen las rutas.</param>
        /// <returns>Una lista de objetos <see cref="TdvRutaDiaria"/>.</returns>
        Task<List<TdvRutaDiaria>> ObtenerRutasGeneradasParaPlaneacionAsync(DateOnly fechaEjecucion, int codSucursal);

        /// <summary>
        /// Actualiza los detalles de una ruta diaria por parte del planeador,
        /// incluyendo la asignación de vehículo y tripulación.
        /// </summary>
        /// <param name="rutaActualizada">El objeto <see cref="TdvRutaDiaria"/> con los datos actualizados.</param>
        /// <returns>
        /// <c>true</c> si la actualización fue exitosa y la ruta pasó a estado 'PLANEADO';
        /// <c>false</c> si la ruta no existe o hubo un conflicto de concurrencia.
        /// Lanza <see cref="InvalidOperationException"/> si la ruta no está en estado 'GENERADO'
        /// o si el vehículo ya está asignado a otra ruta para la misma fecha.
        /// </returns>
        Task<bool> ActualizarRutaDiariaPlaneadorAsync(TdvRutaDiaria rutaActualizada);

        /// <summary>
        /// Obtiene un registro de Ruta Diaria por su identificador único.
        /// </summary>
        /// <param name="id">El ID de la ruta diaria a buscar.</param>
        /// <returns>El objeto <see cref="TdvRutaDiaria"/> si se encuentra, de lo contrario, <c>null</c>.</returns>
        Task<TdvRutaDiaria> ObtenerRutaDiariaPorIdAsync(string id);

        /// <summary>
        /// Genera rutas diarias iniciales (estado GENERADO) a partir de las rutas maestras activas
        /// para una sucursal y fecha de ejecución dadas.
        /// </summary>
        /// <param name="codSucursal">El código de la sucursal para la cual se generarán las rutas.</param>
        /// <param name="fechaEjecucion">La fecha para la cual se están generando las rutas.</param>
        /// <param name="usuarioPlaneacionId">El ID del usuario que está realizando la generación de rutas.</param>
        /// <param name="usuarioPlaneacionNombre">El nombre del usuario que está realizando la generación de rutas.</param>
        /// <returns>Un objeto <see cref="GeneracionRutasDiariasResult"/> que contiene el número de rutas creadas y un mensaje.</returns>
        Task<GeneracionRutasDiariasResult> GenerarRutasDiariasInicialesPorSucursalAsync(int codSucursal, DateOnly fechaEjecucion, string usuarioPlaneacionId, string usuarioPlaneacionNombre);

        // CEF

        /// <summary>
        /// Registra la información de cargue de efectivo para una ruta diaria por parte del personal CEF.
        /// La ruta debe estar en estado 'PLANEADO' para poder registrar el cargue.
        /// </summary>
        /// <param name="rutaId">El ID de la ruta a la que se le registrará el cargue.</param>
        /// <param name="fechaCargue">La fecha en que se realizó el cargue.</param>
        /// <param name="horaCargue">La hora en que se realizó el cargue.</param>
        /// <param name="cantBolsaBilleteEntrega">Cantidad de bolsas de billete entregadas.</param>
        /// <param name="cantBolsaMonedaEntrega">Cantidad de bolsas de moneda entregadas.</param>
        /// <param name="cantPlanillaEntrega">Cantidad de planillas entregadas.</param>
        /// <param name="usuarioCefCargue">El ID del usuario CEF que registra el cargue.</param>
        /// <returns>
        /// <c>true</c> si el cargue fue registrado exitosamente y la ruta pasó a estado 'CARGUE_REGISTRADO';
        /// <c>false</c> si la ruta no existe o hubo un conflicto de concurrencia.
        /// Lanza <see cref="InvalidOperationException"/> si la ruta no está en estado 'PLANEADO'.
        /// </returns>
        Task<bool> RegistrarCargueCEFAsync(string rutaId, DateOnly fechaCargue, TimeOnly horaCargue, int cantBolsaBilleteEntrega, int cantBolsaMonedaEntrega,
            int cantPlanillaEntrega, string usuarioCefCargue);

        /// <summary>
        /// Registra la información de descargue de efectivo para una ruta diaria por parte del personal CEF.
        /// La ruta debe estar en estado 'SALIDA_REGISTRADA' y tener una salida registrada previamente.
        /// </summary>
        /// <param name="rutaId">El ID de la ruta a la que se le registrará el descargue.</param>
        /// <param name="fechaDescargue">La fecha en que se realizó el descargue.</param>
        /// <param name="horaDescargue">La hora en que se realizó el descargue.</param>
        /// <param name="cantBolsaBilleteRecibe">Cantidad de bolsas de billete recibidas.</param>
        /// <param name="cantBolsaMonedaRecibe">Cantidad de bolsas de moneda recibidas.</param>
        /// <param name="cantPlanillaRecibe">Cantidad de planillas recibidas.</param>
        /// <param name="usuarioCefDescargue">El ID del usuario CEF que registra el descargue.</param>
        /// <returns>
        /// <c>true</c> si el descargue fue registrado exitosamente y la ruta pasó a estado 'DESCARGUE_REGISTRADO';
        /// <c>false</c> si la ruta no existe o hubo un conflicto de concurrencia.
        /// Lanza <see cref="InvalidOperationException"/> si la ruta no está en estado 'SALIDA_REGISTRADA' o si la salida del vehículo no está registrada.
        /// </returns>
        Task<bool> RegistrarDescargueCEFAsync(string rutaId, DateOnly fechaDescargue, TimeOnly horaDescargue, int cantBolsaBilleteRecibe, int cantBolsaMonedaRecibe,
            int cantPlanillaRecibe, string usuarioCefDescargue);

        // SUPERVISOR

        /// <summary>
        /// Registra el kilometraje inicial y la hora de salida del vehículo para una ruta diaria por parte del supervisor.
        /// La ruta debe estar en estado 'CARGUE_REGISTRADO'.
        /// </summary>
        /// <param name="rutaId">El ID de la ruta a la que se le registrará la salida.</param>
        /// <param name="kmInicial">El kilometraje del vehículo al iniciar la ruta.</param>
        /// <param name="fechaSalidaRuta">La fecha de salida del vehículo de la sucursal.</param>
        /// <param name="horaSalidaRuta">La hora de salida del vehículo de la sucursal.</param>
        /// <param name="usuarioSupervisorApertura">El ID del supervisor que registra la salida.</param>
        /// <returns>
        /// <c>true</c> si la salida fue registrada exitosamente y la ruta pasó a estado 'SALIDA_REGISTRADA';
        /// <c>false</c> si la ruta no existe o hubo un conflicto de concurrencia.
        /// Lanza <see cref="InvalidOperationException"/> si la ruta no está en estado 'CARGUE_REGISTRADO'
        /// o si los datos de kilometraje/fecha/hora son inválidos.
        /// </returns>
        Task<bool> RegistrarSalidaVehiculoAsync(string rutaId, decimal kmInicial, DateOnly? fechaSalidaRuta, TimeOnly? horaSalidaRuta, string usuarioSupervisorApertura);

        /// <summary>
        /// Registra el kilometraje final y la hora de entrada del vehículo para una ruta diaria por parte del supervisor,
        /// marcando la ruta como 'CERRADA'.
        /// </summary>
        /// <param name="rutaId">El ID de la ruta a la que se le registrará la entrada.</param>
        /// <param name="kmFinal">El kilometraje del vehículo al finalizar la ruta.</param>
        /// <param name="fechaEntradaRuta">La fecha de entrada del vehículo a la sucursal.</param>
        /// <param name="horaEntradaRuta">La hora de entrada del vehículo a la sucursal.</param>
        /// <param name="usuarioSupervisorCierre">El ID del supervisor que registra la entrada y cierra la ruta.</param>
        /// <returns>
        /// <c>true</c> si la entrada fue registrada exitosamente y la ruta pasó a estado 'CERRADO';
        /// <c>false</c> si la ruta no existe o hubo un conflicto de concurrencia.
        /// Lanza <see cref="InvalidOperationException"/> si la ruta no está en estado 'DESCARGUE_REGISTRADO',
        /// si los datos de kilometraje/fecha/hora son inválidos, o si la fecha/hora de entrada es anterior a la de salida.
        /// </returns>
        Task<bool> RegistrarEntradaVehiculoAsync(string rutaId, decimal kmFinal, DateOnly? fechaEntradaRuta, TimeOnly? horaEntradaRuta, string usuarioSupervisorCierre);
    }
}