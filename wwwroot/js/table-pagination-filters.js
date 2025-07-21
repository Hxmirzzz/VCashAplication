document.addEventListener('DOMContentLoaded', function () {

    function setupTablePaginationAndFilters(options) {
        const {
            tableContainerId,
            controllerName,
            actionName = 'Index',
            searchInputId,
            filterInputsSelector,
            applyFiltersButtonId,
            clearFiltersButtonId,
            defaultPageSize = 15
        } = options;

        const tableContainer = document.getElementById(tableContainerId);
        if (!tableContainer) {
            console.error(`Error: Contenedor de tabla con ID '${tableContainerId}' no encontrado.`);
            return;
        }

        const currentControllerUrl = `/${controllerName}/${actionName}`;

        function loadTableData(page = 1) {
            let search = '';
            if (searchInputId) {
                const searchInput = document.getElementById(searchInputId);
                if (searchInput) {
                    search = searchInput.value;
                }
            }

            const ajaxData = {
                page: page,
                pageSize: defaultPageSize,
                search: search
            };

            if (filterInputsSelector) {
                document.querySelectorAll(filterInputsSelector).forEach(input => {
                    let inputValue = input.value;
                    let inputName = input.name;

                    // Si el valor es una cadena vacía, lo tratamos como null para los filtros opcionales.
                    // Esto es crucial para que MVC mapee correctamente a int? o DateOnly?.
                    if (inputValue === '') {
                        inputValue = null;
                    }

                    if (input.tagName === 'SELECT') {
                        // Lógica especial para el filtro de estado booleano (específico de Sucursales)
                        if (input.id === 'FilterEstado' && controllerName === 'Sucursales') {
                            let estadoBool = null;
                            if (inputValue === 'true') { estadoBool = true; } else if (inputValue === 'false') { estadoBool = false; }
                            ajaxData[inputName] = estadoBool; // Asigna el booleano o null
                        }
                        // Lógica para otros Selects (ej. FilterCodSuc en RutasDiarias, o FilterEstado en RutasDiarias que es int?)
                        else {
                            ajaxData[inputName] = inputValue; // Asigna la cadena o null
                        }
                    }
                    // Para inputs tipo date, text, number
                    else if (input.type === 'date' || input.type === 'text' || input.type === 'number') {
                        ajaxData[inputName] = inputValue; // Asigna la cadena o null
                    }
                    // Puedes añadir más tipos si tienes (checkbox, radio, etc.)
                });
            }

            // Realizar la llamada AJAX
            $.ajax({
                url: currentControllerUrl,
                type: 'GET',
                data: ajaxData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                success: function (result) {
                    tableContainer.innerHTML = result;
                    const newUrl = new URL(window.location.href);
                    newUrl.pathname = currentControllerUrl;
                    for (const key in ajaxData) {
                        // Eliminar parámetros nulos o vacíos de la URL
                        if (ajaxData[key] !== null && ajaxData[key] !== undefined && ajaxData[key] !== '') {
                            newUrl.searchParams.set(key, ajaxData[key]);
                        } else {
                            newUrl.searchParams.delete(key);
                        }
                    }
                    history.pushState({}, '', newUrl.toString());

                    // Llama a la función global para reiniciar el contador de sesión
                    if (window.initializeSessionTimer) {
                        window.initializeSessionTimer();
                    }
                },
                error: function (xhr, status, error) {
                    console.error("Error al cargar datos de la tabla:", error);
                    tableContainer.innerHTML = "<div class='alert alert-danger'>Error al cargar los datos. Intente de nuevo.</div>";
                }
            });
        }

        // --- Eventos de Paginación ---
        $(document).on('click', `#${tableContainerId} .pagination .page-link`, function (e) {
            e.preventDefault();
            const page = $(this).data('page');
            if (page) {
                loadTableData(page);
            }
        });

        // --- Eventos de Búsqueda y Filtro ---
        if (searchInputId) {
            const searchInput = document.getElementById(searchInputId);
            if (searchInput) {
                let typingTimer;
                const doneTypingInterval = 300;

                searchInput.addEventListener('input', function () {
                    clearTimeout(typingTimer);
                    typingTimer = setTimeout(() => loadTableData(1), doneTypingInterval);
                });
            }
        }

        if (filterInputsSelector) {
            document.querySelectorAll(filterInputsSelector).forEach(input => {
                input.addEventListener('change', () => loadTableData(1));
            });
        }

        if (applyFiltersButtonId) {
            const applyButton = document.getElementById(applyFiltersButtonId);
            if (applyButton) {
                applyButton.addEventListener('click', () => loadTableData(1));
            }
        }

        if (clearFiltersButtonId) {
            const clearButton = document.getElementById(clearFiltersButtonId);
            if (clearButton) {
                clearButton.addEventListener('click', () => {
                    if (searchInputId) {
                        const searchInput = document.getElementById(searchInputId);
                        if (searchInput) searchInput.value = '';
                    }

                    if (filterInputsSelector) {
                        document.querySelectorAll(filterInputsSelector).forEach(input => {
                            if (input.tagName === 'SELECT' || input.type === 'date' || input.type === 'text' || input.type === 'number') {
                                input.value = '';
                            }
                        });
                    }
                    loadTableData(1);
                });
            }
        }

        const urlParams = new URLSearchParams(window.location.search);
        const initialPage = urlParams.get('page') ? parseInt(urlParams.get('page')) : 1;
        loadTableData(initialPage);
    }
    window.setupTablePaginationAndFilters = setupTablePaginationAndFilters;
});