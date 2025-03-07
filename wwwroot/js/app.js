// app.js - GrokBot应用程序辅助脚本

// 导航处理 
window.appHelpers = {
    // 导航至指定路径
    navigateTo: function(path) {
        const base = document.querySelector('base').getAttribute('href') || '/';
        const fullPath = base.endsWith('/') 
            ? base + path.replace(/^\//, '') 
            : base + '/' + path.replace(/^\//, '');
        
        console.log('Navigating to:', fullPath);
        history.pushState(null, '', fullPath);
        return false;
    },
    
    // 清除本地存储
    clearStorage: function() {
        try {
            localStorage.clear();
            sessionStorage.clear();
            console.log('Browser storage cleared');
            return true;
        } catch (e) {
            console.error('Failed to clear storage:', e);
            return false;
        }
    },
    
    // 获取存储使用信息
    getStorageInfo: function() {
        try {
            const used = Object.keys(localStorage).reduce((acc, key) => {
                return acc + localStorage[key].length;
            }, 0);
            
            return {
                used: used,
                quota: 5 * 1024 * 1024, // 5MB 是典型浏览器本地存储限制
                percentage: Math.round((used / (5 * 1024 * 1024)) * 100)
            };
        } catch (e) {
            console.error('Failed to get storage info:', e);
            return {
                used: 0,
                quota: 5 * 1024 * 1024,
                percentage: 0,
                error: e.message
            };
        }
    },
    
    // 记录消息到控制台
    log: function(message, type = 'log') {
        switch(type) {
            case 'error':
                console.error(message);
                break;
            case 'warn':
                console.warn(message);
                break;
            case 'info':
                console.info(message);
                break;
            default:
                console.log(message);
        }
    },
    
    // 检测浏览器支持情况
    checkBrowserSupport: function() {
        const features = {
            localStorage: typeof localStorage !== 'undefined',
            sessionStorage: typeof sessionStorage !== 'undefined',
            fetch: typeof fetch !== 'undefined',
            historyAPI: typeof history.pushState !== 'undefined'
        };
        
        console.info('Browser support:', features);
        return features;
    }
};

// 初始化
(function() {
    // 触发一次浏览器支持检查
    window.appHelpers.checkBrowserSupport();
    
    // 记录存储使用情况
    const storageInfo = window.appHelpers.getStorageInfo();
    console.info('Storage usage:', 
        storageInfo.used, 'bytes /', 
        storageInfo.quota, 'bytes (',
        storageInfo.percentage, '%)'
    );
    
    // 如果存储使用接近限制，自动清理
    if (storageInfo.percentage > 80) {
        console.warn('Storage usage is high, clearing automatically');
        window.appHelpers.clearStorage();
    }
    
    console.info('GrokBot app helpers initialized');
})();
