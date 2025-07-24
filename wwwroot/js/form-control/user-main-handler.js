document.addEventListener("DOMContentLoaded", function () {
    const roleSelect = document.getElementById('roleSelect');
    const viewPermissionsContainer = document.getElementById('viewPermissionsContainer');

    if (roleSelect && viewPermissionsContainer) {
        async function loadViewPermissions(selectedRoleName) {
            if (!selectedRoleName) {
                viewPermissionsContainer.innerHTML = '<p class="text-muted">Seleccione un rol para ver sus permisos de vistas.</p>';
                return;
            }

            viewPermissionsContainer.innerHTML = '<p class="text-info">Cargando permisos...</p>';

            try {
                const url = `/User/GetViewsForRole?roleName=${encodeURIComponent(selectedRoleName)}`;
                const response = await fetch(url);

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                const views = await response.json();

                let html = '';
                if (views.length === 0) {
                    html = `<p class="text-muted">No se encontraron vistas o permisos para el rol "${selectedRoleName}".</p>`;
                } else {
                    html = `<table class="table table-sm table-bordered">
                                <thead>
                                    <tr>
                                        <th>Vista</th>
                                        <th>Ver</th>
                                        <th>Crear</th>
                                        <th>Editar</th>
                                    </tr>
                                </thead>
                                <tbody>`;
                    views.forEach(view => {
                        html += `<tr>
                                    <td>${view.nombreVista}</td>
                                    <td><input type="checkbox" ${view.puedeVer ? 'checked' : ''} disabled></td>
                                    <td><input type="checkbox" ${view.puedeCrear ? 'checked' : ''} disabled></td>
                                    <td><input type="checkbox" ${view.puedeEditar ? 'checked' : ''} disabled></td>
                                </tr>`;
                    });
                    html += `</tbody></table>`;
                }
                viewPermissionsContainer.innerHTML = html;

            } catch (error) {
                console.error("Error al cargar permisos de vistas:", error);
                viewPermissionsContainer.innerHTML = '<p class="text-danger">Error al cargar los permisos. Intente de nuevo.</p>';
            }
        }

        roleSelect.addEventListener('change', function () {
            loadViewPermissions(this.value);
        });

        if (roleSelect.value) {
            loadViewPermissions(roleSelect.value);
        }
    }

    // --- Lógica para el manejo del formulario de creación/edición de usuario (AJAX + SweetAlert2) ---
    const userForm = document.getElementById('createUserForm') || document.getElementById('editUserForm');
    const submitButton = userForm ? userForm.querySelector('button[type="submit"]') : null;
    const statusMessage = document.getElementById('statusMessage');
    const confirmationMessage = document.getElementById('confirmationMessage');

    if (userForm) {
        userForm.addEventListener('submit', async function (event) {
            event.preventDefault();

            if ($.validator && !$(userForm).valid()) {
                return;
            }

            // Limpiar mensajes anteriores
            if (statusMessage) statusMessage.style.display = 'none';
            if (confirmationMessage) confirmationMessage.style.display = 'none';

            if (submitButton) {
                submitButton.disabled = true; // Deshabilitar botón para evitar envíos duplicados
                submitButton.textContent = 'Procesando...';
            }

            const formData = new FormData(userForm);
            const actionUrl = userForm.action; // Obtener la URL de acción del formulario

            try {
                const response = await fetch(actionUrl, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                });

                const result = await response.json();

                if (result.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Éxito',
                        text: result.message,
                        confirmButtonText: 'Ok'
                    }).then((res) => {
                        if (res.isConfirmed) {
                            if (userForm.id === 'createUserForm') {
                                window.location.reload();
                            } else if (userForm.id === 'editUserForm') {
                                window.location.href = '/User/Index';
                            }
                        }
                    });
                } else {
                    let errorMessage = result.message || 'Ocurrió un error desconocido.';
                    if (result.errors) {
                        errorMessage += '<ul class="text-start">';
                        for (const key in result.errors) {
                            if (result.errors.hasOwnProperty(key)) {
                                result.errors[key].forEach(error => {
                                    errorMessage += `<li>${error}</li>`;
                                });
                            }
                        }
                        errorMessage += '</ul>';
                    }

                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        html: errorMessage,
                        confirmButtonText: 'Cerrar'
                    });
                }

            } catch (error) {
                console.error('Error en el envío del formulario:', error);
                Swal.fire({
                    icon: 'error',
                    title: 'Error de Conexión',
                    text: 'No se pudo conectar con el servidor. Intente de nuevo más tarde.',
                    confirmButtonText: 'Cerrar'
                });
            } finally {
                if (submitButton) {
                    submitButton.disabled = false;
                    submitButton.textContent = 'Registrar';
                }
            }
        });
    }
});