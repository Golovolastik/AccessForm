// Функция для отправки формы
async function submitForm(formId, endpoint) {
    console.log(`Отправка формы ${formId} на эндпоинт ${endpoint}`);
    const form = document.getElementById(formId);
    const formData = new FormData(form);
    
    console.log('Данные формы:');
    for (let pair of formData.entries()) {
        console.log(pair[0] + ': ' + pair[1]);
    }

    try {
        const response = await fetch(endpoint, {
            method: 'POST',
            body: formData
        });

        console.log('Ответ сервера:', response);
        const result = await response.json();
        console.log('Данные ответа:', result);

        if (response.ok) {
            alert(result.message);
            form.reset();
        } else {
            alert('Произошла ошибка при отправке формы');
        }
    } catch (error) {
        console.error('Ошибка при отправке формы:', error);
        alert('Произошла ошибка при отправке формы');
    }
}

// Обработчики отправки форм
document.getElementById('accessRequestForm').addEventListener('submit', function(e) {
    e.preventDefault();
    submitForm('accessRequestForm', '/api/access-request');
});

document.getElementById('transferNoticeForm').addEventListener('submit', function(e) {
    e.preventDefault();
    submitForm('transferNoticeForm', '/api/transfer-notice');
});

document.getElementById('terminateAccessForm').addEventListener('submit', function(e) {
    e.preventDefault();
    submitForm('terminateAccessForm', '/api/terminate-access');
}); 