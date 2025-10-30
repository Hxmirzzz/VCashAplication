const generoSelect = document.getElementById('Genero');
const otroGeneroField = document.getElementById('otroGeneroField');

function toggleOtroGeneroField()
{
    if (generoSelect && otroGeneroField)
    {
        if (generoSelect.value === 'O')
        {
            otroGeneroField.style.display = 'block';
        }
        else
        {
            otroGeneroField.style.display = 'none';
        }
    }
}

toggleOtroGeneroField();
if (generoSelect)
{
    generoSelect.addEventListener('change', toggleOtroGeneroField);
}