import urllib.request
import urllib.error
import json
import platform
import subprocess

class MxaAuth:
    def __init__(self, secret: str, app_name: str, app_version: str):
        self.secret = secret
        self.app_name = app_name
        self.app_version = app_version
        self.base_url = "https://misanxauth.qzz.io"
        self.username = None
        self.subscription = None
        self.expiry = None

    def _get_hwid(self) -> str:
        try:
            if platform.system() == "Windows":
                # Get Windows UUID
                cmd = "wmic csproduct get uuid"
                uuid = subprocess.check_output(cmd, shell=True).decode().split('\n')[1].strip()
                return uuid
            elif platform.system() == "Linux":
                # Get machine-id
                with open("/etc/machine-id", "r") as f:
                    return f.read().strip()
            elif platform.system() == "Darwin":
                # Get macOS UUID
                cmd = "ioreg -rd1 -c IOPlatformExpertDevice | grep -i UUID"
                uuid = subprocess.check_output(cmd, shell=True).decode().split('"')[-2]
                return uuid
        except Exception:
            pass
        return "UNKNOWN_HWID_" + platform.node()

    def _post(self, path: str, data: dict) -> dict:
        url = f"{self.base_url}{path}"
        headers = {"Content-Type": "application/json"}
        req_data = json.dumps(data).encode("utf-8")
        req = urllib.request.Request(url, data=req_data, headers=headers, method="POST")
        try:
            with urllib.request.urlopen(req) as response:
                res_body = response.read().decode("utf-8")
                return json.loads(res_body)
        except urllib.error.HTTPError as e:
            try:
                res_body = e.read().decode("utf-8")
                return json.loads(res_body)
            except Exception:
                return {"success": False, "message": f"HTTP_ERROR_{e.code}"}
        except Exception as e:
            return {"success": False, "message": f"CONNECTION_FAILED: {str(e)}"}

    def check_version(self) -> bool:
        resp = self._post("/versioncheck", {
            "secret": self.secret,
            "appName": self.app_name,
            "appVersion": self.app_version
        })
        return resp.get("success", False) and resp.get("message") == "VERSION_OK"

    def login(self, username: str, password: str) -> dict:
        resp = self._post("/login", {
            "username": username,
            "password": password,
            "secret": self.secret,
            "appName": self.app_name,
            "appVersion": self.app_version,
            "hwid": self._get_hwid()
        })
        if resp.get("success"):
            self.username = resp.get("username")
            self.subscription = resp.get("subscription")
            self.expiry = resp.get("expiry")
        return resp

    def register(self, username: str, password: str, license_key: str) -> dict:
        return self._post("/register", {
            "username": username,
            "password": password,
            "licenseKey": license_key,
            "secret": self.secret,
            "appName": self.app_name,
            "appVersion": self.app_version,
            "hwid": self._get_hwid()
        })

    def get_variable(self, var_name: str) -> str:
        resp = self._post("/getvariable", {
            "secret": self.secret,
            "appName": self.app_name,
            "appVersion": self.app_version,
            "varName": var_name
        })
        if resp.get("success"):
            return resp.get("value")
        return None
