document.addEventListener('DOMContentLoaded', function () {
    // --- Función para el Modal de Confirmación Reusable ---
    // Parámetros:
    //   - message: Mensaje a mostrar en el modal (ej. "¿Desea eliminar este registro?").
    //   - onConfirmCallback: Función que se ejecuta si el usuario hace clic en "Confirmar" (o selecciona un formato).
    //   - title: Título del modal (opcional, por defecto "Confirmar Acción").
    //   - confirmButtonText: Texto del botón de confirmar (opcional, por defecto "Confirmar").
    //   - confirmButtonClass: Clase CSS para el botón de confirmar (opcional, por defecto "btn-primary").
    //   - needsExportButtons: Booleano que indica si se deben mostrar botones de formato de exportación.
    window.showConfirmationModal = function (message, onConfirmCallback, title = "Confirmar Acción", confirmButtonText = "Confirmar", confirmButtonClass = "btn-primary", needsExportButtons = false) {
        const modalElement = document.getElementById('confirmationModal');
        if (!modalElement) {
            console.error("El modal de confirmación '#confirmationModal' no se encontró en el DOM.");
            // Fallback al confirm() nativo si el modal no está presente
            if (confirm(message)) {
                onConfirmCallback();
            }
            return;
        }

        const modalTitle = modalElement.querySelector('#confirmationModalLabel');
        const modalMessage = modalElement.querySelector('#confirmationMessage');
        const modalFooter = modalElement.querySelector('.modal-footer'); // Selecciona el footer para manipular los botones

        if (modalTitle) modalTitle.textContent = title;
        if (modalMessage) modalMessage.textContent = message;

        // Limpiar el footer de botones anteriores para evitar duplicados o lógicas mezcladas
        modalFooter.innerHTML = '';

        // Si el modal necesita botones de formato de exportación
        if (needsExportButtons) {
            // Botón de cancelar
            const cancelButton = document.createElement('button');
            cancelButton.type = 'button';
            cancelButton.className = 'btn btn-secondary';
            cancelButton.dataset.bsDismiss = 'modal'; // Para Bootstrap 5
            cancelButton.textContent = 'Cancelar';
            modalFooter.appendChild(cancelButton);

            // Definir los formatos de exportación y sus propiedades
            const formats = [
                { format: 'PDF', text: 'PDF', iconClass: 'ri-file-pdf-2-fill', buttonClass: 'btn-danger' },
                { format: 'JSON', text: 'JSON', iconClass: 'ri-file-code-fill', buttonClass: 'btn-info' },
                { format: 'CSV', text: 'CSV', iconClass: 'ri-file-edit-fill', buttonClass: 'btn-secondary' },
                { format: 'EXCEL', text: 'EXCEL', iconClass: 'ri-file-excel-2-fill', buttonClass: 'btn-success' }
            ];

            // Crear y añadir los botones de formato de exportación
            formats.forEach(f => {
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.className = `btn ${f.buttonClass} ms-2 export-format-btn`; // ms-2 para margen a la izquierda
                btn.dataset.format = f.format;
                btn.innerHTML = `${f.text} <i class="${f.iconClass}"></i>`; // Incluir icono

                btn.addEventListener('click', function () {
                    const selectedFormat = this.dataset.format;
                    // Ocultar el modal
                    const bootstrapModal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
                    bootstrapModal.hide();
                    onConfirmCallback(selectedFormat); // Pasar el formato seleccionado al callback
                });
                modalFooter.appendChild(btn);
            });
        } else {
            // Si es un modal de confirmación estándar, añadir los botones Cancelar y Confirmar
            const cancelButton = document.createElement('button');
            cancelButton.type = 'button';
            cancelButton.className = 'btn btn-secondary';
            cancelButton.dataset.bsDismiss = 'modal';
            cancelButton.textContent = 'Cancelar';
            modalFooter.appendChild(cancelButton);

            const confirmButton = document.createElement('button');
            confirmButton.type = 'button';
            confirmButton.className = `btn ${confirmButtonClass}`; // Usar la clase personalizada
            confirmButton.id = 'confirmActionButton'; // Mantener el ID para eventos
            confirmButton.textContent = confirmButtonText;
            modalFooter.appendChild(confirmButton);

            // Adjuntar el evento de clic al botón de confirmar estándar
            // Remover cualquier evento de clic anterior si se ha manipulado el DOM
            const oldConfirmButton = modalFooter.querySelector('#confirmActionButton'); // Re-seleccionar el botón
            const newConfirmButton = oldConfirmButton.cloneNode(true);
            oldConfirmButton.parentNode.replaceChild(newConfirmButton, oldConfirmButton);

            newConfirmButton.addEventListener('click', function () {
                const bootstrapModal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
                bootstrapModal.hide();
                onConfirmCallback(); // Ejecutar la función de callback sin formato
            });
        }

        // Mostrar el modal (Bootstrap 5)
        const bootstrapModal = new bootstrap.Modal(modalElement);
        bootstrapModal.show();
    };
});