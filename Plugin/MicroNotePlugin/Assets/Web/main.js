// 初始化 Mermaid
mermaid.initialize({ startOnLoad: false, theme: 'default' });

// 监听来自 C# 的消息
if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', event => {
        const message = event.data;
        if (message.type === 'updateContent') {
            updateContent(message.content);
        } else if (message.type === 'scrollTo') {
            scrollToPercentage(message.percentage);
        } else if (message.type === 'setTheme') {
            setTheme(message.theme);
        }
    });
}

function updateContent(html) {
    const container = document.getElementById('preview');
    container.innerHTML = html;

    // 渲染代码高亮
    document.querySelectorAll('pre code').forEach((block) => {
        hljs.highlightElement(block);
    });

    // 渲染 Mermaid
    mermaid.run({
        nodes: document.querySelectorAll('.mermaid')
    });

    // 渲染数学公式
    renderMathInElement(container, {
        delimiters: [
            { left: '$$', right: '$$', display: true },
            { left: '$', right: '$', display: false }
        ]
    });

    // 处理图片点击
    document.querySelectorAll('img').forEach(img => {
        img.onclick = () => {
            // 可以发送消息回 C# 查看大图
        };
    });
}

function scrollToPercentage(percentage) {
    const height = document.documentElement.scrollHeight - window.innerHeight;
    window.scrollTo(0, height * percentage);
}

function setTheme(theme) {
    if (theme === 'Dark') {
        document.documentElement.style.setProperty('--color-fg-default', '#c9d1d9');
        document.documentElement.style.setProperty('--color-canvas-default', '#0d1117');
        // 更新 Mermaid 主题等
    } else {
        document.documentElement.style.setProperty('--color-fg-default', '#24292f');
        document.documentElement.style.setProperty('--color-canvas-default', '#ffffff');
    }
}

// 监听滚动发送回 C# (可选)
window.onscroll = () => {
    const percentage = window.scrollY / (document.documentElement.scrollHeight - window.innerHeight);
    // window.chrome.webview.postMessage({ type: 'scroll', percentage: percentage });
};
