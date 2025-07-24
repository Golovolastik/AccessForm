window.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('accessForm');
    if (!form) return;

    // Определяем тип заявки для этой страницы через data-request-type
    const requestTypeId = Number(form.dataset.requestType || 1);
    console.log('requestType при отправке формы:', requestTypeId);

    let lastFormData = null;
    form.addEventListener('submit', async function(e) {
        // Проверяем валидность формы перед preventDefault
        if (!this.checkValidity()) {
            // Показываем ошибки, если есть
            e.preventDefault();
            alert("Пожалуйста, заполните все обязательные поля");
            return;
        }
        e.preventDefault();
        // Отключаем кнопку отправки формы
        const submitBtn = this.querySelector('.submit-btn');
        submitBtn.disabled = true;
        submitBtn.textContent = 'Подождите...';

        // Если модальное окно уже открыто — не отправлять форму повторно
        if (document.querySelector('.pdf-modal')) {
            return;
        }

        // Создаем объект для JSON
        const formData = {};
        const formElements = this.elements;
        for (let element of formElements) {
            if (element.name) {
                if (element.type === 'checkbox') {
                    formData[element.name] = element.checked;
                } else if (element.type === 'date') {
                    formData[element.name] = element.value;
                } else {
                    formData[element.name] = element.value;
                }
            }
        }
        lastFormData = formData;
        formData['requestTypeId'] = Number(requestTypeId);

        try {
            const response = await fetch('/api/Request', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/pdf'
                },
                body: JSON.stringify(formData)
            });

            if (response.ok) {
                // Создаем модальное окно для PDF
                const modal = document.createElement('div');
                modal.className = 'pdf-modal';
                modal.style.position = 'fixed';
                modal.style.top = '0';
                modal.style.left = '0';
                modal.style.width = '100%';
                modal.style.height = '100%';
                modal.style.backgroundColor = 'rgba(0,0,0,0.5)';
                modal.style.display = 'flex';
                modal.style.justifyContent = 'center';
                modal.style.alignItems = 'center';
                modal.style.zIndex = '1000';

                const modalContent = document.createElement('div');
                modalContent.style.backgroundColor = 'white';
                modalContent.style.padding = '20px';
                modalContent.style.borderRadius = '5px';
                modalContent.style.width = '80%';
                modalContent.style.height = '80%';
                modalContent.style.display = 'flex';
                modalContent.style.flexDirection = 'column';

                const header = document.createElement('div');
                header.style.display = 'flex';
                header.style.justifyContent = 'space-between';
                header.style.marginBottom = '10px';

                const title = document.createElement('h2');
                title.textContent = 'Ваш документ готов';
                header.appendChild(title);

                const closeButton = document.createElement('button');
                closeButton.textContent = '✕';
                closeButton.style.border = 'none';
                closeButton.style.background = 'none';
                closeButton.style.fontSize = '20px';
                closeButton.style.cursor = 'pointer';
                closeButton.onclick = () => {
                    document.body.removeChild(modal);
                };
                header.appendChild(closeButton);

                const pdfContainer = document.createElement('iframe');
                pdfContainer.style.width = '100%';
                pdfContainer.style.height = '100%';
                pdfContainer.style.border = 'none';

                // Создаем Blob из ответа и создаем URL для него
                const pdfBlob = await response.blob();
                const pdfUrl = URL.createObjectURL(pdfBlob);
                pdfContainer.src = pdfUrl;
                localStorage.setItem('lastPdfUrl', pdfUrl);

                const printButton = document.createElement('button');
                printButton.textContent = 'Распечатать';
                printButton.style.marginTop = '10px';
                printButton.style.padding = '10px 20px';
                printButton.style.backgroundColor = '#4CAF50';
                printButton.style.color = 'white';
                printButton.style.border = 'none';
                printButton.style.borderRadius = '5px';
                printButton.style.cursor = 'pointer';
                printButton.onclick = () => {
                    pdfContainer.contentWindow.print();
                };

                const downloadBtn = document.createElement('button');
                downloadBtn.textContent = 'Загрузить скан';
                downloadBtn.style.display = 'inline-block';
                downloadBtn.style.margin = '20px 0 10px 0';
                downloadBtn.style.padding = '12px 24px';
                downloadBtn.style.backgroundColor = '#3498db';
                downloadBtn.style.color = 'white';
                downloadBtn.style.border = 'none';
                downloadBtn.style.borderRadius = '5px';
                downloadBtn.style.textDecoration = 'none';
                downloadBtn.style.fontSize = '16px';
                downloadBtn.style.cursor = 'pointer';

                // Создаём скрытый input для загрузки файла
                const fileInput = document.createElement('input');
                fileInput.type = 'file';
                fileInput.accept = 'application/pdf,image/*';
                fileInput.style.display = 'none';
                fileInput.onchange = async (e) => {
                    const file = e.target.files[0];
                    if (file && lastFormData) {
                        const uploadFormData = new FormData();
                        uploadFormData.append('file', file);
                        if (lastFormData['name']) uploadFormData.append('fullName', lastFormData['name']);
                        if (lastFormData['position']) uploadFormData.append('position', lastFormData['position']);
                        const formRequestTypeId = Number(form.dataset.requestType || 1);
                        console.log('requestTypeId при загрузке скана:', formRequestTypeId);
                        uploadFormData.append('requestTypeId', formRequestTypeId);

                        downloadBtn.disabled = true;
                        downloadBtn.textContent = 'Загрузка...';
                        try {
                            const resp = await fetch('/api/Request/UploadWithForm', {
                                method: 'POST',
                                body: uploadFormData
                            });
                            if (resp.ok) {
                                downloadBtn.textContent = 'Успешно!';
                                downloadBtn.style.background = '#4CAF50';
                            } else {
                                downloadBtn.textContent = 'Ошибка';
                                downloadBtn.style.background = 'red';
                            }
                        } catch (err) {
                            downloadBtn.textContent = 'Ошибка';
                            downloadBtn.style.background = 'red';
                        }
                        setTimeout(() => {
                            downloadBtn.disabled = false;
                            downloadBtn.textContent = 'Загрузить скан';
                            downloadBtn.style.background = '#3498db';
                        }, 2000);
                    }
                };
                downloadBtn.onclick = () => {
                    fileInput.click();
                };

                modalContent.appendChild(header);
                modalContent.appendChild(pdfContainer);
                modalContent.appendChild(printButton);
                modalContent.appendChild(downloadBtn);
                modalContent.appendChild(fileInput);
                modal.appendChild(modalContent);
                document.body.appendChild(modal);

                form.reset();
            } else {
                const contentType = response.headers.get('content-type');
                if (contentType && contentType.includes('application/json')) {
                    const errorData = await response.json();
                    alert(`Ошибка при отправке формы: ${errorData.detail || errorData.message || 'Неизвестная ошибка'}`);
                } else {
                    alert('Произошла ошибка при отправке формы');
                }
            }
        } catch (error) {
            alert('Произошла ошибка при отправке формы. Проверьте консоль для деталей.');
            console.error('Ошибка:', error);
        }
    });
}); 