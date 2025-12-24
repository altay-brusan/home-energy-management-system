"""
ThingsBoard API Client for Node-RED Flow Management
Handles authentication, device management, and RPC calls
"""

import requests
import json
from typing import Dict, List, Optional, Any
from datetime import datetime, timedelta


class ThingsBoardClient:
    """Client for ThingsBoard REST API interactions"""

    def __init__(self, base_url: str):
        """
        Initialize ThingsBoard client

        Args:
            base_url: ThingsBoard server URL (e.g., https://thingsboard-dev.oklidis.com.tr)
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

        Raises:
            requests.RequestException: On network or API errors
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
                # Set token expiry (JWT tokens typically valid for 1 hour)
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

    def get_devices(self, page_size: int = 100, page: int = 0) -> List[Dict[str, Any]]:
        """
        Retrieve list of devices from ThingsBoard

        Args:
            page_size: Number of devices per page
            page: Page number (0-indexed)

        Returns:
            List of device objects with id and name

        Raises:
            Exception: If not authenticated or request fails
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

        Raises:
            Exception: If not authenticated or RPC call fails
        """
        if not self.is_token_valid():
            raise Exception("Not authenticated. Please login first.")

        try:
            url = f"{self.base_url}/api/rpc/twoway/{device_id}"
            payload = {
                "method": method,
                "params": params,
                "timeout": timeout * 1000  # Convert to milliseconds
            }

            response = self.session.post(url, json=payload, timeout=timeout + 5)
            response.raise_for_status()

            return response.json()

        except requests.RequestException as e:
            raise Exception(f"RPC call failed: {str(e)}")

    def get_flows(self, device_id: str) -> Dict[str, Any]:
        """
        Get all Node-RED flows from device

        Args:
            device_id: Target device ID

        Returns:
            Dictionary containing success status, flow count, and flows list
        """
        try:
            response = self.rpc_call(device_id, 'getFlows', {})
            return response
        except Exception as e:
            raise Exception(f"Failed to get flows: {str(e)}")

    def add_flow(self, device_id: str, flow: Dict[str, Any]) -> Dict[str, Any]:
        """
        Add a new Node-RED flow to device

        Args:
            device_id: Target device ID
            flow: Flow object containing label, nodes, and configs
                  Format: {
                      "id": "flow_id",
                      "label": "Flow Label",
                      "disabled": false,
                      "info": "",
                      "nodes": [...]
                  }

        Returns:
            Dictionary containing success status, message, and flowId
        """
        try:
            params = {"flow": flow}
            response = self.rpc_call(device_id, 'addFlow', params)
            return response
        except Exception as e:
            raise Exception(f"Failed to add flow: {str(e)}")

    def remove_flow(self, device_id: str, flow_id: str) -> Dict[str, Any]:
        """
        Remove a Node-RED flow from device

        Args:
            device_id: Target device ID
            flow_id: ID of the flow tab to remove

        Returns:
            Dictionary containing success status and message
        """
        try:
            params = {"id": flow_id}
            response = self.rpc_call(device_id, 'removeFlow', params)
            return response
        except Exception as e:
            raise Exception(f"Failed to remove flow: {str(e)}")

    def parse_flow_file(self, file_path: str) -> Dict[str, Any]:
        """
        Parse a Node-RED flow JSON file and format it for the API

        Args:
            file_path: Path to the flow JSON file

        Returns:
            Formatted flow object ready for add_flow API

        Raises:
            Exception: If file is invalid or missing required fields
        """
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                flow_json = json.load(f)

            # Find tab node
            tab_nodes = [n for n in flow_json if n.get('type') == 'tab']
            if not tab_nodes:
                raise Exception("No tab node found in flow file")

            tab_node = tab_nodes[0]

            # Filter out tab and broker/client nodes
            other_nodes = [
                n for n in flow_json
                if n.get('type') != 'tab'
                and not n.get('type', '').endswith('-broker')
                and not n.get('type', '').endswith('-client')
            ]

            # Format for API
            formatted_flow = {
                "id": tab_node.get("id"),
                "label": tab_node.get("label", "New Flow"),
                "disabled": tab_node.get("disabled", False),
                "info": tab_node.get("info", ""),
                "nodes": other_nodes
            }

            return formatted_flow

        except json.JSONDecodeError:
            raise Exception("Invalid JSON file")
        except FileNotFoundError:
            raise Exception("Flow file not found")
        except Exception as e:
            raise Exception(f"Failed to parse flow file: {str(e)}")