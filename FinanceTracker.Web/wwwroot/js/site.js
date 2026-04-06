// Finance Tracker - client-side utilities

/**
 * Sets up sortable columns and a search filter for a table.
 * @param {string} tableId - The id of the <table> element (with class sortable-table)
 * @param {string} searchInputId - The id of the text <input> used for filtering
 */
function setupSortFilter(tableId, searchInputId) {
    const table = document.getElementById(tableId);
    const searchInput = document.getElementById(searchInputId);
    if (!table) return;

    // Sort on header click
    const headers = table.querySelectorAll('th.sortable');
    headers.forEach(th => {
        th.style.cursor = 'pointer';
        th.addEventListener('click', () => {
            const col = parseInt(th.dataset.col, 10);
            const asc = th.dataset.sortDir !== 'asc';
            th.dataset.sortDir = asc ? 'asc' : 'desc';

            // Reset other headers
            headers.forEach(h => { if (h !== th) delete h.dataset.sortDir; });

            const tbody = table.querySelector('tbody');
            const rows = Array.from(tbody.querySelectorAll('tr'));
            rows.sort((a, b) => {
                const aText = a.cells[col]?.innerText.replace(/[₹,\s]/g, '') || '';
                const bText = b.cells[col]?.innerText.replace(/[₹,\s]/g, '') || '';
                const aNum = parseFloat(aText);
                const bNum = parseFloat(bText);
                if (!isNaN(aNum) && !isNaN(bNum)) return asc ? aNum - bNum : bNum - aNum;
                return asc ? aText.localeCompare(bText) : bText.localeCompare(aText);
            });
            rows.forEach(r => tbody.appendChild(r));
        });
    });

    // Filter on search input
    if (searchInput) {
        searchInput.addEventListener('input', () => {
            const term = searchInput.value.toLowerCase();
            const tbody = table.querySelector('tbody');
            tbody.querySelectorAll('tr').forEach(row => {
                const text = row.innerText.toLowerCase();
                row.style.display = text.includes(term) ? '' : 'none';
            });
        });
    }
}
