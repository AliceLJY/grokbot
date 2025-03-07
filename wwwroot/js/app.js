// app.js - GrokBot应用程序辅助脚本

// 导航处理 
window.appHelpers = {
    // 导航至指定路径
    navigateTo: function(path) {
        const base = document.querySelector('base').getAttribute('href') || '/';
        
        // 确保路径格式正确
        if (path.startsWith('/')) {
            path = path.substring(1);
        }
        
        const fullPath = base.endsWith('/') 
            ? base + path
            : base + '/' + path;
        
        console.log('导航到路径:', fullPath);
        
        // 优先使用pushState以支持SPA路由
        if (window.history && window.history.pushState) {
            window.history.pushState(null, '', fullPath);
            // 触发popstate事件以使Blazor检测到路由变化
            window.dispatchEvent(new Event('popstate'));
            return true;
        } else {
            // 后备方案
            window.location.href = fullPath;
            return false;
        }
    },
    
    // 清除本地存储 - 改进以保留聊天数据
    clearStorage: function() {
        try {
            // 保留聊天数据
            const chatDataKey = 'grokbot_chats';
            const chatData = localStorage.getItem(chatDataKey);
            
            // 选择性清除除聊天数据外的所有内容
            const keysToRemove = Object.keys(localStorage).filter(key => key !== chatDataKey);
            keysToRemove.forEach(key => localStorage.removeItem(key));
            
            console.log('已选择性清除浏览器存储，保留聊天数据');
            return true;
        } catch (e) {
            console.error('清除存储失败:', e);
            return false;
        }
    },
    
    // 获取存储使用信息
    getStorageInfo: function() {
        try {
            const used = Object.keys(localStorage).reduce((acc, key) => {
                return acc + (localStorage[key] ? localStorage[key].length : 0);
            }, 0);
            
            return {
                used: used,
                quota: 5 * 1024 * 1024, // 5MB 是典型浏览器本地存储限制
                percentage: Math.round((used / (5 * 1024 * 1024)) * 100)
            };
        } catch (e) {
            console.error('获取存储信息失败:', e);
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
        
        console.info('浏览器功能支持情况:', features);
        return features;
    }
};

// 改进的路由处理辅助函数
window.routeHandler = {
    // 内存中维护的重定向计数，而不是使用localStorage
    _redirectCount: 0,
    
    // 导航到指定路径
    navigateTo: function(path) {
        try {
            console.log('路由处理器: 导航到', path);
            
            // 获取基础路径
            var base = document.querySelector('base').getAttribute('href') || '/';
            
            // 确保路径格式正确
            if (path.startsWith('/')) {
                path = path.substring(1);
            }
            
            // 构建完整URL
            var fullPath = base.endsWith('/') ? base + path : base + '/' + path;
            console.log('完整导航路径:', fullPath);
            
            // 使用 pushState 避免页面刷新
            if (window.history && window.history.pushState) {
                window.history.pushState(null, '', fullPath);
                // 触发popstate事件让Blazor知道路由已更改
                window.dispatchEvent(new Event('popstate'));
                return true;
            } else {
                // 后备方案
                window.location.href = fullPath;
                return true;
            }
        } catch (e) {
            console.error('导航错误:', e);
            return false;
        }
    },
    
    // 安全导航，防止循环重定向
    safeNavigate: function(path, maxRedirects = 3) {
        console.log('安全导航: 目标路径=', path, '当前计数=', this._redirectCount);
        
        // 检测重定向循环
        if (this._redirectCount >= maxRedirects) {
            console.error('检测到过多重定向，返回首页');
            this._redirectCount = 0;
            
            // 使用replace直接返回首页
            var base = document.querySelector('base').getAttribute('href') || '/';
            window.location.replace(window.location.origin + base);
            return false;
        }
        
        this._redirectCount++;
        
        // 尝试导航
        var result = this.navigateTo(path);
        
        // 5秒后重置计数器
        setTimeout(() => {
            console.log('重置重定向计数器');
            this._redirectCount = 0;
        }, 5000);
        
        return result;
    },
    
    // 处理导航错误
    handleNavigationError: function() {
        var path = window.location.pathname;
        console.log('检查路径是否存在导航错误:', path);
        
        // 检查404路径
        if (path.endsWith('/404') || path.endsWith('/404.html')) {
            console.log('检测到404页面，重定向到主页');
            this._redirectCount = 0;
            var base = document.querySelector('base').getAttribute('href') || '/';
            window.location.replace(window.location.origin + base);
            return true;
        }
        
        return false;
    }
};

// 初始化
(function() {
    // 检查路由是否出错
    window.routeHandler.handleNavigationError();
    
    // 触发一次浏览器支持检查
    window.appHelpers.checkBrowserSupport();
    
    // 记录存储使用情况
    const storageInfo = window.appHelpers.getStorageInfo();
    console.info('存储使用情况:', 
        storageInfo.used, '字节 /', 
        storageInfo.quota, '字节 (',
        storageInfo.percentage, '%)'
    );
    
    // 如果存储使用接近限制，选择性清理
    if (storageInfo.percentage > 80) {
        console.warn('存储使用率过高，正在自动清理非关键数据');
        window.appHelpers.clearStorage();
    }
    
    console.info('GrokBot 应用助手已初始化');
})();
