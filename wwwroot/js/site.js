document.addEventListener('DOMContentLoaded', () => {
    const sidebar = document.querySelector('.sidebar');
    const toggleButton = document.getElementById('sidebarToggle');
    const toggleIcon = toggleButton.querySelector('i');
    const themeButton = document.getElementById('theme-button');
    const themeIcon = themeButton.querySelector('i');
    const viewTitle = document.getElementById('viewTitle');

    // Helper function to update the view title
    const updateViewTitle = (titleText) => {
        if (viewTitle) {
            viewTitle.textContent = titleText;
        }
    };

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

            const linkText = this.querySelector('.nav-text')?.textContent.trim();
            if (linkText) {
                updateViewTitle(linkText);
            }

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

    // --- Restore Active State and View Title on Page Load ---
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
            const linkText = activeLinkToRestore.querySelector('.nav-text')?.textContent.trim();
            if (linkText) {
                updateViewTitle(linkText);
            }
        } else {
            localStorage.removeItem('activeSidebarLink');
            localStorage.removeItem('activeSidebarDropdown');
            updateViewTitle("Dashboard");
        }
    } else {
        updateViewTitle("Dashboard");
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
            // If the click was NOT inside the sidebar AND NOT on the filter button
            if (!filterSidebar.contains(event.target) && !filterButton.contains(event.target)) {
                filterSidebar.classList.remove('show');
            }
        }
    });

    // Optional: Prevent clicks inside the sidebar (that are not action buttons) from closing it
    if (filterSidebar) {
        filterSidebar.addEventListener('click', function(event) {
            event.stopPropagation();
        });
    }
});