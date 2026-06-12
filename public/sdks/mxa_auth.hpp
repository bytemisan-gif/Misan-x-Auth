#pragma once
#include <string>
#include <sstream>
#include <windows.h>
#include <wininet.h>

#pragma comment(lib, "wininet.lib")

class MxaAuth {
private:
    std::string secret;
    std::string appName;
    std::string appVersion;
    std::string baseUrl = "misanxauth.qzz.io"; // Host name

    std::string username;
    std::string subscription;
    std::string expiry;

    std::string getHwid() {
        HW_PROFILE_INFO hwProfileInfo;
        if (GetCurrentHwProfile(&hwProfileInfo)) {
            return hwProfileInfo.szHwProfileGuid;
        }
        return "UNKNOWN-C++-HWID";
    }

    // Helper to find a JSON field's value in a flat JSON string
    std::string findJsonField(const std::string& json, const std::string& field) {
        size_t pos = json.find("\"" + field + "\"");
        if (pos == std::string::npos) return "";
        pos = json.find(":", pos);
        if (pos == std::string::npos) return "";
        pos = json.find_first_not_of(" \t\r\n", pos + 1);
        if (pos == std::string::npos) return "";
        
        if (json[pos] == '"') {
            size_t end = json.find("\"", pos + 1);
            if (end == std::string::npos) return "";
            return json.substr(pos + 1, end - pos - 1);
        } else {
            size_t end = json.find_first_of(",}", pos);
            if (end == std::string::npos) return "";
            std::string val = json.substr(pos, end - pos);
            val.erase(0, val.find_first_not_of(" \t\r\n"));
            val.erase(val.find_last_not_of(" \t\r\n") + 1);
            return val;
        }
    }

    std::string httpPost(const std::string& path, const std::string& jsonPayload) {
        std::string response = "";
        HINTERNET hSession = InternetOpenA("MXA-Auth-SDK", INTERNET_OPEN_TYPE_PRECONFIG, NULL, NULL, 0);
        if (hSession) {
            HINTERNET hConnect = InternetConnectA(hSession, baseUrl.c_str(), INTERNET_DEFAULT_HTTPS_PORT, NULL, NULL, INTERNET_SERVICE_HTTP, 0, 1);
            if (hConnect) {
                const char* acceptTypes[] = { "application/json", NULL };
                HINTERNET hRequest = HttpOpenRequestA(hConnect, "POST", path.c_str(), NULL, NULL, acceptTypes, INTERNET_FLAG_SECURE | INTERNET_FLAG_RELOAD, 1);
                if (hRequest) {
                    std::string headers = "Content-Type: application/json\r\n";
                    BOOL sent = HttpSendRequestA(hRequest, headers.c_str(), (DWORD)headers.length(), (LPVOID)jsonPayload.c_str(), (DWORD)jsonPayload.length());
                    if (sent) {
                        char buffer[1024];
                        DWORD bytesRead = 0;
                        while (InternetReadFile(hRequest, buffer, sizeof(buffer) - 1, &bytesRead) && bytesRead > 0) {
                            buffer[bytesRead] = '\0';
                            response += buffer;
                        }
                    }
                    InternetCloseHandle(hRequest);
                }
                InternetCloseHandle(hConnect);
            }
            InternetCloseHandle(hSession);
        }
        return response;
    }

public:
    MxaAuth(const std::string& secret, const std::string& appName, const std::string& appVersion) 
        : secret(secret), appName(appName), appVersion(appVersion) {}

    bool checkVersion() {
        std::stringstream ss;
        ss << "{\"secret\":\"" << secret << "\",\"appName\":\"" << appName << "\",\"appVersion\":\"" << appVersion << "\"}";
        std::string res = httpPost("/versioncheck", ss.str());
        return findJsonField(res, "message") == "VERSION_OK" && findJsonField(res, "success") == "true";
    }

    struct Response {
        bool success;
        std::string message;
        std::string username;
        std::string subscription;
        std::string expiry;
    };

    Response login(const std::string& user, const std::string& pass) {
        std::stringstream ss;
        ss << "{\"username\":\"" << user << "\",\"password\":\"" << pass 
           << "\",\"secret\":\"" << secret << "\",\"appName\":\"" << appName 
           << "\",\"appVersion\":\"" << appVersion << "\",\"hwid\":\"" << getHwid() << "\"}";
        
        std::string res = httpPost("/login", ss.str());
        Response r;
        r.success = findJsonField(res, "success") == "true";
        r.message = findJsonField(res, "message");
        if (r.success) {
            this->username = findJsonField(res, "username");
            this->subscription = findJsonField(res, "subscription");
            this->expiry = findJsonField(res, "expiry");
            r.username = this->username;
            r.subscription = this->subscription;
            r.expiry = this->expiry;
        }
        return r;
    }

    Response registerUser(const std::string& user, const std::string& pass, const std::string& key) {
        std::stringstream ss;
        ss << "{\"username\":\"" << user << "\",\"password\":\"" << pass 
           << "\",\"licenseKey\":\"" << key << "\",\"secret\":\"" << secret 
           << "\",\"appName\":\"" << appName << "\",\"appVersion\":\"" << appVersion 
           << "\",\"hwid\":\"" << getHwid() << "\"}";
        
        std::string res = httpPost("/register", ss.str());
        Response r;
        r.success = findJsonField(res, "success") == "true";
        r.message = findJsonField(res, "message");
        return r;
    }

    std::string getVariable(const std::string& varName) {
        std::stringstream ss;
        ss << "{\"secret\":\"" << secret << "\",\"appName\":\"" << appName 
           << "\",\"appVersion\":\"" << appVersion << "\",\"varName\":\"" << varName << "\"}";
        std::string res = httpPost("/getvariable", ss.str());
        if (findJsonField(res, "success") == "true") {
            return findJsonField(res, "value");
        }
        return "";
    }
};
