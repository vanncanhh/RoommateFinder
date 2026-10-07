// Tiện ích JS nhỏ dùng qua IJSRuntime (Blazor không truy cập trực tiếp được DOM/trình duyệt).
window.rf = {
    storageGet: (key) => { try { return localStorage.getItem(key); } catch { return null; } },
    storageSet: (key, value) => { try { localStorage.setItem(key, value); } catch { } },
    storageRemove: (key) => { try { localStorage.removeItem(key); } catch { } },
    scrollToBottom: (el) => { if (el) el.scrollTop = el.scrollHeight; },
    scrollTop: (el) => el ? el.scrollTop : 0,
    keepScroll: (el, oldHeight) => { if (el) el.scrollTop = el.scrollHeight - oldHeight; },
    scrollHeight: (el) => el ? el.scrollHeight : 0,
    setTitle: (title) => { document.title = title; },
    focus: (el) => { if (el) el.focus(); },
};

// Ô nhập có thuộc tính data-enter-send: Enter (không Shift, không đang gõ bộ gõ tiếng Việt/IME) không chèn xuống dòng.
// Blazor vẫn nhận sự kiện keydown để gửi tin; Blazor không tự chặn mặc định được theo từng phím.
document.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && !e.shiftKey && !e.isComposing && e.target && e.target.hasAttribute && e.target.hasAttribute('data-enter-send')) {
        e.preventDefault();
    }
});

// Lỗi Blazor không bắt được: hiện thanh báo lỗi ở cuối trang.
document.addEventListener('click', (e) => {
    if (e.target && e.target.classList && e.target.classList.contains('dismiss')) {
        document.getElementById('blazor-error-ui').classList.add('d-none');
    }
});
