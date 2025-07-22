document.addEventListener("DOMContentLoaded", function () {
    // Este manejador funcionará tanto para el formulario de crear como para el de editar
    const form = document.getElementById('createEmployeeForm') || document.getElementById('editEmployeeForm');

    if (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault(); // Evitar el envío tradicional

            // Mostrar SweetAlert de "cargando"
            Swal.fire({
                title: 'Guardando...',
                text: 'Por favor, espere.',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            const formData = new FormData(form);
            const url = form.getAttribute('action');
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

            fetch(url, {
                method: 'POST',
                body: formData, // FormData maneja automáticamente los archivos y el formato multipart/form-data
                headers: {
                    // NO establecemos 'Content-Type'. El navegador lo hace por nosotros cuando usamos FormData.
                    'RequestVerificationToken': token
                }
            })
                .then(response => {
                    if (!response.ok) {
                        // Si la respuesta del servidor es un error (ej. 500)
                        throw new Error(`Error del servidor: ${response.status} ${response.statusText}`);
                    }
                    return response.json();
                })
                .then(result => {
                    Swal.close(); // Cerrar el "cargando"

                    if (result.success) {
                        Swal.fire({
                            title: '¡Éxito!',
                            text: result.message,
                            icon: 'success'
                        }).then(() => {
                            // Redirigir a la lista después de que el usuario cierre la alerta
                            window.location.href = '/Employee';
                        });
                    } else {
                        // Si el resultado no fue exitoso (ej. error de validación)
                        let errorText = result.message;
                        if (result.errors) {
                            // Limpiar errores anteriores
                            document.querySelectorAll('.text-danger').forEach(span => span.textContent = '');
                            // Mostrar errores de cada campo
                            Object.keys(result.errors).forEach(key => {
                                const errorSpan = document.querySelector(`[data-valmsg-for="${key}"]`);
                                if (errorSpan) {
                                    errorSpan.textContent = result.errors[key].join(', ');
                                }
                            });
                            errorText = "Por favor, corrija los errores en el formulario.";
                        }
                        Swal.fire({
                            title: 'Error',
                            text: errorText,
                            icon: 'error'
                        });
                    }
                })
                .catch(error => {
                    Swal.close();
                    console.error('Error:', error);
                    Swal.fire({
                        title: 'Error Inesperado',
                        text: 'Ocurrió un error al enviar el formulario. Revisa la consola para más detalles.',
                        icon: 'error'
                    });
                });
        });
    }
});