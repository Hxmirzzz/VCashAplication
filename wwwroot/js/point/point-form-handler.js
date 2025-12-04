document.addEventListener('DOMContentLoaded', () => {
    const sidebar = document.querySelector('.sidebar');
    const toggleButton = document.getElementById('sidebarToggle');
    const toggleIcon = toggleButton.querySelector('i');
    const themeButton = document.getElementById('theme-button');
    const themeIcon = themeButton.querySelector('i');

    // --- Sidebar Toggle Logic ---
    if (sidebar.classList.contains('expanded')) {
        toggleIcon.classList.remove('ri-arrow-right-s-line');
        toggleIcon.classList.add('ri-arrow-left-s-line');
    }

    toggleButton.addEventListener('click', () => {
        sidebar.classList.toggle('expanded');
        sidebar.classList.toggle('compact');

        if (sidebar.classList.contains('expanded')) {
            toggleIcon.classList.remove('ri-arrow-right-s-line');
            toggleIcon.classList.add('ri-arrow-left-s-line');
        } else {
            toggleIcon.classList.remove('ri-arrow-left-s-line');
            toggleIcon.classList.add('ri-arrow-right-s-line');
        }

        if (sidebar.classList.contains('compact')) {
            const openDropdowns = document.querySelectorAll('.sidebar__dropdown.show-submenu');
            openDropdowns.forEach(dropdown => {
                dropdown.classList.remove('show-submenu');
                dropdown.querySelector('.dropdown-arrow')?.classList.remove('active-dropdown');
            });
        }
    });

    // --- Dropdown Functionality Logic ---
    const dropdownBtns = document.querySelectorAll('.sidebar__link.dropdown-btn');

    dropdownBtns.forEach(btn => {
        btn.addEventListener('click', function (event) {
            event.preventDefault();

            if (sidebar.classList.contains('expanded')) {
                const parentDropdown = this.closest('.sidebar__dropdown');
                const submenu = parentDropdown.querySelector('.sidebar__submenu');
                const arrow = this.querySelector('.dropdown-arrow');

                const isCurrentlyOpen = parentDropdown.classList.contains('show-submenu');

                document.querySelectorAll('.sidebar__dropdown.show-submenu').forEach(otherDropdown => {
                    if (otherDropdown !== parentDropdown) {
                        otherDropdown.classList.remove('show-submenu');
                        otherDropdown.querySelector('.dropdown-arrow')?.classList.remove('active-dropdown');
                    }
                });

                parentDropdown.classList.toggle('show-submenu', !isCurrentlyOpen);
                arrow.classList.toggle('active-dropdown', !isCurrentlyOpen);

                if (isCurrentlyOpen) {
                    const activeSubLinks = parentDropdown.querySelectorAll('.sidebar__submenu .sidebar__link.active');
                    activeSubLinks.forEach(subLink => subLink.classList.remove('active'));
                }
            }
        });
    });

    // --- Theme Toggle Logic ---
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme) {
        document.body.classList.add(savedTheme);
        if (savedTheme === 'dark-theme') {
            themeIcon.classList.remove('ri-moon-line');
            themeIcon.classList.add('ri-sun-line');
        } else {
            themeIcon.classList.remove('ri-sun-line');
            themeIcon.classList.add('ri-moon-line');
        }
    }

    themeButton.addEventListener('click', () => {
        document.body.classList.toggle('dark-theme');

        if (document.body.classList.contains('dark-theme')) {
            themeIcon.classList.remove('ri-moon-line');
            themeIcon.classList.add('ri-sun-line');
            localStorage.setItem('theme', 'dark-theme');
        } else {
            themeIcon.classList.remove('ri-sun-line');
            themeIcon.classList.add('ri-moon-line');
            localStorage.setItem('theme', 'light-theme');
        }
    });

    // --- Sidebar Active State Logic ---
    const allSidebarLinks = document.querySelectorAll('.sidebar__link');

    allSidebarLinks.forEach(link => {
        link.addEventListener('click', function (event) {
            if (this.classList.contains('dropdown-btn') || this.id === 'theme-button') {
                return;
            }

            allSidebarLinks.forEach(item => item.classList.remove('active'));
            this.classList.add('active');

            const linkId = this.getAttribute('id') || this.querySelector('.nav-text')?.textContent.trim();
            if (linkId) {
                localStorage.setItem('activeSidebarLink', linkId);
                const parentDropdown = this.closest('.sidebar__dropdown');
                if (parentDropdown) {
                    localStorage.setItem('activeSidebarDropdown', parentDropdown.getAttribute('id') || parentDropdown.querySelector('.dropdown-btn .nav-text')?.textContent.trim());
                } else {
                    localStorage.removeItem('activeSidebarDropdown');
                }
            }

            const parentDropdown = this.closest('.sidebar__dropdown');
            if (parentDropdown) {
                parentDropdown.classList.add('show-submenu');
                parentDropdown.querySelector('.dropdown-btn')?.classList.add('active-dropdown');
            }
        });
    });

    // --- Restore Active State on Page Load ---
    const savedActiveLinkId = localStorage.getItem('activeSidebarLink');
    const savedActiveDropdownId = localStorage.getItem('activeSidebarDropdown');

    if (savedActiveLinkId) {
        let activeLinkToRestore;
        activeLinkToRestore = document.getElementById(savedActiveLinkId);

        if (!activeLinkToRestore) {
            activeLinkToRestore = Array.from(allSidebarLinks).find(link =>
                link.querySelector('.nav-text')?.textContent.trim() === savedActiveLinkId
            );
        }

        if (activeLinkToRestore) {
            allSidebarLinks.forEach(item => item.classList.remove('active'));
            activeLinkToRestore.classList.add('active');

            const parentDropdown = activeLinkToRestore.closest('.sidebar__dropdown');
            if (parentDropdown) {
                parentDropdown.classList.add('show-submenu');
                parentDropdown.querySelector('.dropdown-btn')?.classList.add('active-dropdown');
            }
        } else {
            localStorage.removeItem('activeSidebarLink');
            localStorage.removeItem('activeSidebarDropdown');
        }
    }

    const filterButton = document.getElementById('filterButton');
    const filterSidebar = document.getElementById('filterSidebar');
    const cancelFilterButton = document.getElementById('clearFilters');

    // Function to show the sidebar when the filter button is clicked
    if (filterButton) { // Ensure the element exists
        filterButton.addEventListener('click', (event) => {
            event.stopPropagation(); // Prevents the click from propagating to the document and closing the sidebar immediately
            if (filterSidebar) {
                filterSidebar.classList.add('show');
            }
        });
    }

    // Function to hide the sidebar when the cancel button is clicked
    if (cancelFilterButton) { // Ensure the element exists
        cancelFilterButton.addEventListener('click', (event) => {
            event.stopPropagation(); // Prevents click propagation
            if (filterSidebar) {
                filterSidebar.classList.remove('show');
            }
        });
    }

    // Function to hide the sidebar if a click occurs outside of it or the filter button
    document.addEventListener('click', function (event) {
        if (filterSidebar && filterButton) {
            if (!filterSidebar.contains(event.target) && !filterButton.contains(event.target)) {
                filterSidebar.classList.remove('show');
            }
        }
    });

    if (filterSidebar) {
        filterSidebar.addEventListener('click', function (event) {
            event.stopPropagation();
        });
    }
});

(function () {
    // --- util: obtiene hidden asociado ---
    function findHidden(cb) {
        const form = cb.closest('form');
        if (!form) return null;
        const sel = cb.getAttribute('data-hidden') || 'input[name="isActive"]';
        return form.querySelector(sel);
    }

    // --- util: valor que se enviará según estado ---
    function getValue(cb, checked) {
        const onVal = cb.getAttribute('data-on');
        const offVal = cb.getAttribute('data-off');
        if (checked) return onVal != null ? onVal : 'true';
        return offVal != null ? offVal : 'false';
    }

    // --- util: mensaje a mostrar ---
    function getMessage(cb, checked) {
        const msgOn = cb.getAttribute('data-msg-on') || '¿Confirmas ACTIVAR este registro?';
        const msgOff = cb.getAttribute('data-msg-off') || '¿Confirmas DESACTIVAR este registro?';
        return checked ? msgOn : msgOff;
    }

    // --- util: envío (ajax opcional) ---
    async function submitForm(form, useAjax) {
        if (!useAjax) { form.submit(); return; }

        const action = form.getAttribute('action') || window.location.href;
        const method = (form.getAttribute('method') || 'POST').toUpperCase();
        const fd = new FormData(form);

        const anti = form.querySelector('input[name="__RequestVerificationToken"]');
        const headers = {};
        if (anti) headers['RequestVerificationToken'] = anti.value;

        try {
            const resp = await fetch(action, { method, body: fd, headers });
            let data = null;
            try { data = await resp.json(); } catch { }

            if (window.Swal) {
                if (data && (data.ok || data.success)) {
                    await Swal.fire({ icon: 'success', title: 'Listo', text: (data.message || 'Actualizado.'), timer: 1500, showConfirmButton: false });
                } else if (data && (data.ok === false || data.success === false)) {
                    await Swal.fire({ icon: 'error', title: 'Error', text: (data.message || 'No se pudo actualizar.') });
                } else {
                    await Swal.fire({ icon: 'success', title: 'Listo', text: 'Actualizado.', timer: 1200, showConfirmButton: false });
                }
            }
        } catch (e) {
            if (window.Swal) {
                await Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo completar la operación.' });
            }
            // location.reload();
        }
    }

    document.addEventListener('change', async function (e) {
        const cb = e.target.closest('.status-toggle');
        if (!cb) return;

        const form = cb.closest('.toggle-status-form');
        if (!form) return;

        const hidden = findHidden(cb);
        if (!hidden) { cb.checked = !cb.checked; return; }

        const willBeActive = cb.checked;
        const msg = getMessage(cb, willBeActive);

        hidden.value = getValue(cb, willBeActive);

        let confirmed = true;
        if (window.Swal) {
            const result = await Swal.fire({
                icon: 'question',
                title: 'Confirmar',
                text: msg,
                showCancelButton: true,
                confirmButtonText: 'Sí, confirmar',
                cancelButtonText: 'Cancelar',
                reverseButtons: true,
                focusCancel: true
            });
            confirmed = result.isConfirmed === true;
        } else {
            confirmed = window.confirm(msg);
        }

        if (confirmed) {
            const useAjax = true;
            await submitForm(form, useAjax);
        } else {
            cb.checked = !cb.checked;
            hidden.value = getValue(cb, cb.checked);
        }
    }, false);
})();