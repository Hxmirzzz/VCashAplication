function confirmStatusChange(checkbox) {
    const form = checkbox.closest('.employee-status-form');
    if (!form) {
        console.error("No se encontró el formulario padre del switch.");
        checkbox.checked = !checkbox.checked;
        return;
    }

    const employeeId = parseInt(form.querySelector('input[name="EmployeeId"]').value);
    const nextStatusOn = parseInt(checkbox.dataset.nextStatusOn);
    const nextStatusOff = parseInt(checkbox.dataset.nextStatusOff);

    const newStatusTargetValue = checkbox.checked ? nextStatusOn : nextStatusOff;
    const newStatusTargetName = checkbox.checked ? "ACTIVO" : "INACTIVO";

    form.querySelector('input[name="NewStatus"]').value = newStatusTargetValue;

    Swal.fire({
        title: '¿Está seguro?',
        text: `¿Desea cambiar el estado del empleado ${employeeId} a ${newStatusTargetName}?`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Sí, cambiar',
        cancelButtonText: 'Cancelar'
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: 'Cambiando estado...',
                text: 'Por favor, espere.',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

            const requestBody = {
                EmployeeId: employeeId,
                NewStatus: newStatusTargetValue,
                ReasonForChange: `Cambio de estado desde la tabla a ${newStatusTargetName}`
            };

            const url = form.getAttribute('action');

            fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token // Use the token obtained here
                },
                body: JSON.stringify(requestBody)
            })
                .then(response => {
                    if (!response.ok) {
                        return response.json().catch(() => {
                            throw new Error(`Error del servidor: ${response.status} ${response.statusText}`);
                        });
                    }
                    return response.json();
                })
                .then(data => {
                    Swal.close();
                    if (data.success) {
                        Swal.fire(
                            '¡Cambiado!',
                            data.message,
                            'success'
                        ).then(() => {
                            // Recargar la página para reflejar los cambios en el badge y el switch
                            window.location.reload();
                        });
                    } else {
                        Swal.fire(
                            'Error',
                            data.message || 'No se pudo cambiar el estado.',
                            'error'
                        );
                        // Revertir el switch si la operación falló en el backend
                        checkbox.checked = !checkbox.checked;
                    }
                })
                .catch(error => {
                    Swal.close();
                    console.error('Error al cambiar estado:', error);
                    Swal.fire(
                        'Error',
                        `Ocurrió un error al intentar cambiar el estado: ${error.message}`,
                        'error'
                    );
                    // Revertir el switch si hubo un error de red o fetch
                    checkbox.checked = !checkbox.checked;
                });
        } else {
            checkbox.checked = !checkbox.checked;
        }
    });
}


document.addEventListener("DOMContentLoaded", function () {
    const createForm = document.getElementById('createEmployeeForm');
    const editForm = document.getElementById('editEmployeeForm');

    function handleFormSubmit(form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();

            if ($.validator && !$(form).valid()) {
                console.log("Validación del lado del cliente falló. No se envía el formulario.");
                return;
            }

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
                body: formData,
                headers: {
                    'RequestVerificationToken': token
                }
            })
                .then(response => {
                    if (!response.ok) {
                        return response.json().catch(() => {
                            throw new Error(`Error del servidor: ${response.status} ${response.statusText}`);
                        });
                    }
                    return response.json();
                })
                .then(result => {
                    Swal.close();

                    if (result.success) {
                        Swal.fire({
                            title: '¡Éxito!',
                            text: result.message,
                            icon: 'success'
                        }).then(() => {
                            if (form.id === 'createEmployeeForm') {
                                window.location.reload();
                            } else if (form.id === 'editEmployeeForm') {
                                window.location.href = '/Employee/Index';
                            }
                        });
                    } else {
                        let errorText = result.message || "Por favor, corrija los errores en el formulario.";
                        if (result.errors) {
                            document.querySelectorAll('.text-danger').forEach(span => span.textContent = '');
                            Object.keys(result.errors).forEach(key => {
                                const errorSpan = document.querySelector(`[data-valmsg-for="${key}"]`);
                                if (errorSpan) {
                                    errorSpan.textContent = result.errors[key].join(', ');
                                }
                            });
                            errorText = "Se encontraron errores. Por favor, revise los campos marcados.";
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
                    console.error('Error en fetch:', error);
                    Swal.fire({
                        title: 'Error Inesperado',
                        text: 'Ocurrió un error al enviar el formulario. Revisa la consola para más detalles. ' + (error.message || ''),
                        icon: 'error'
                    });
                });
        });
    }

    if (createForm) {
        handleFormSubmit(createForm);
    }
    if (editForm) {
        handleFormSubmit(editForm);
    }

    // Lógica para el campo "Otro Género"
    const generoSelect = document.getElementById('Genero');
    const otroGeneroField = document.getElementById('otroGeneroField');

    function toggleOtroGeneroField() {
        if (generoSelect && otroGeneroField) {
            if (generoSelect.value === 'O') {
                otroGeneroField.style.display = 'block';
            } else {
                otroGeneroField.style.display = 'none';
            }
        }
    }

    toggleOtroGeneroField();
    if (generoSelect) {
        generoSelect.addEventListener('change', toggleOtroGeneroField);
    }

    // Lógica para los componentes de carga de archivos (Drag & Drop con Preview)
    function updatePreview(file, inputElement, previewContainer, dropZoneElement) {
        if (file) {
            const reader = new FileReader();
            reader.onload = (e) => {
                if (previewContainer) {
                    previewContainer.innerHTML = `<img src="${e.target.result}" alt="Preview" class="img-thumbnail" />
                                                  <button type="button" class="drop-zone-remove">X</button>`;
                    const newRemoveButton = previewContainer.querySelector('.drop-zone-remove');
                    if (newRemoveButton) {
                        newRemoveButton.addEventListener('click', (event) => {
                            event.stopPropagation();
                            clearFileInput(inputElement, previewContainer, dropZoneElement);
                        });
                    }
                    dropZoneElement.classList.add('has-file');
                }
            };
            reader.readAsDataURL(file);
        } else {
            clearFileInput(inputElement, previewContainer, dropZoneElement);
        }
    }

    function clearFileInput(input, preview, dzEl) {
        input.value = '';
        if (preview) {
            preview.innerHTML = '';
        }
        if (dzEl) {
            dzEl.classList.remove('has-file');
        }
    }

    document.querySelectorAll('.drop-zone').forEach(dropZoneElement => {
        const inputElement = dropZoneElement.querySelector('.drop-zone-input');
        const previewContainer = dropZoneElement.querySelector('.drop-zone-preview');
        const selectFileSpan = dropZoneElement.querySelector('.drop-zone-select');

        const initialRemoveButton = previewContainer ? previewContainer.querySelector('.drop-zone-remove') : null;
        if (initialRemoveButton) {
            dropZoneElement.classList.add('has-file');
            initialRemoveButton.addEventListener('click', (event) => {
                event.stopPropagation();
                clearFileInput(inputElement, previewContainer, dropZoneElement);
            });
        }

        if (selectFileSpan) {
            selectFileSpan.addEventListener('click', () => {
                inputElement.click();
            });
        }

        inputElement.addEventListener('change', function () {
            if (this.files.length > 0) {
                const file = this.files[0];
                if (file.size > 3 * 1024 * 1024) { // 3MB
                    Swal.fire('Error', 'El tamaño del archivo excede el límite de 3MB.', 'error');
                    clearFileInput(inputElement, previewContainer, dropZoneElement);
                    return;
                }
                if (!file.type.match('image/jpeg') && !file.type.match('image/png')) {
                    Swal.fire('Error', 'Solo se permiten archivos JPG, JPEG o PNG.', 'error');
                    clearFileInput(inputElement, previewContainer, dropZoneElement);
                    return;
                }
                updatePreview(file, inputElement, previewContainer, dropZoneElement);
            } else {
                clearFileInput(inputElement, previewContainer, dropZoneElement);
            }
        });

        dropZoneElement.addEventListener('dragover', (e) => {
            e.preventDefault();
            dropZoneElement.classList.add('dragover');
        });

        dropZoneElement.addEventListener('dragleave', () => {
            dropZoneElement.classList.remove('dragover');
        });

        dropZoneElement.addEventListener('drop', (e) => {
            e.preventDefault();
            dropZoneElement.classList.remove('dragover');

            if (e.dataTransfer.files.length > 0) {
                inputElement.files = e.dataTransfer.files;
                inputElement.dispatchEvent(new Event('change'));
            }
        });
    });
});