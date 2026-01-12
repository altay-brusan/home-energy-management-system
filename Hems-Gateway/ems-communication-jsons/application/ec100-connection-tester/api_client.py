"""
ThingsBoard API Client for EC100 Connection Testing
Handles authentication, device lookup, and RPC calls
"""

import requests
from typing import Dict, List, Optional, Any
from datetime import datetime, timedelta


class ThingsBoardClient:
    """Client for ThingsBoard REST API interactions"""

    def __init__(self, base_url: str):
        """
        Initialize ThingsBoard client

        Args:
            base_url: ThingsBoard server URL (e.g., https://thingsboard-stg.oklidis.com.tr)
        """
        self.base_url = base_url.rstrip('/')
        self.token = None
        self.token_expiry = None
        self.session = requests.Session()
        self.session.headers.update({
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        })

    def login(self, username: str, password: str) -> bool:
        """
        Authenticate with ThingsBoard and obtain JWT token

        Args:
            username: ThingsBoard username
            password: ThingsBoard password

        Returns:
            True if login successful, False otherwise
        """
        try:
            url = f"{self.base_url}/api/auth/login"
            payload = {
                "username": username,
                "password": password
            }

            response = self.session.post(url, json=payload, timeout=10)
            response.raise_for_status()

            data = response.json()
            self.token = data.get('token')

            if self.token:
                self.token_expiry = datetime.now() + timedelta(hours=1)
                self.session.headers.update({
                    'X-Authorization': f'Bearer {self.token}'
                })
                return True

            return False

        except requests.RequestException as e:
            raise Exception(f"Login failed: {str(e)}")

    def is_token_valid(self) -> bool:
        """Check if current token is still valid"""
        if not self.token or not self.token_expiry:
            return False
        return datetime.now() < self.token_expiry

    def get_devices(self, page_size: int = 1000, page: int = 0) -> List[Dict[str, Any]]:
        """
        Retrieve list of devices from ThingsBoard

        Args:
            page_size: Number of devices per page
            page: Page number (0-indexed)

        Returns:
            List of device objects with id and name
        """
        if not self.is_token_valid():
            raise Exception("Not authenticated. Please login first.")

        try:
            url = f"{self.base_url}/api/tenant/devices"
            params = {
                'pageSize': page_size,
                'page': page
            }

            response = self.session.get(url, params=params, timeout=10)
            response.raise_for_status()

            data = response.json()
            return data.get('data', [])

        except requests.RequestException as e:
            raise Exception(f"Failed to get devices: {str(e)}")

    def find_device_by_name(self, device_name: str) -> Optional[str]:
        """
        Find device ID by device name (serial number)

        Args:
            device_name: Device name/serial number to search for

        Returns:
            Device ID if found, None otherwise
        """
        devices = self.get_devices()
        for device in devices:
            if device.get('name') == device_name:
                return device['id']['id']
        return None

    def rpc_call(self, device_id: str, method: str, params: Dict[str, Any],
                 timeout: int = 30) -> Dict[str, Any]:
        """
        Make a two-way RPC call to a device

        Args:
            device_id: Target device ID
            method: RPC method name
            params: Method parameters
            timeout: Request timeout in seconds

        Returns:
            RPC response as dictionary
        """
        if not self.is_token_valid():
            raise Exception("Not authenticated. Please login first.")

        try:
            url = f"{self.base_url}/api/rpc/twoway/{device_id}"
            payload = {
                "method": method,
                "params": params,
                "timeout": timeout * 1000
            }

            response = self.session.post(url, json=payload, timeout=timeout + 5)
            response.raise_for_status()

            return response.json()

        except requests.Timeout:
            raise Exception("RPC timeout - device did not respond")
        except requests.RequestException as e:
            raise Exception(f"RPC call failed: {str(e)}")
