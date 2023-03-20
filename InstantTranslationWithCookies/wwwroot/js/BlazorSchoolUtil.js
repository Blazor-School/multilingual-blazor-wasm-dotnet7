class BlazorSchoolUtil
{
    updateCookies(key, value)
    {
        document.cookie = `${key}=${value}`;
    }

    getCookieValue(key) 
    {
        return document.cookie.match('(^|;)\\s*' + key + '\\s*=\\s*([^;]+)')?.pop() || '';
    }
}

window.BlazorSchoolUtil = new BlazorSchoolUtil();