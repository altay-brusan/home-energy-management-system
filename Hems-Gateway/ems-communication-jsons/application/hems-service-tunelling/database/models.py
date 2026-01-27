"""
Data models for EC100 Provisioning Tool
"""
from dataclasses import dataclass
from datetime import datetime
from typing import Optional


@dataclass
class Device:
    """Device model representing an EC100 device"""
    id: Optional[int] = None
    device_name: str = ""
    serial_number: Optional[str] = None
    mac_address: Optional[str] = None
    tunnel_port: int = 0
    location: Optional[str] = None
    notes: Optional[str] = None
    public_key: Optional[str] = None
    status: str = "offline"  # online, offline, error
    last_seen: Optional[datetime] = None
    created_at: Optional[datetime] = None
    updated_at: Optional[datetime] = None

    def to_dict(self) -> dict:
        return {
            'id': self.id,
            'device_name': self.device_name,
            'serial_number': self.serial_number,
            'mac_address': self.mac_address,
            'tunnel_port': self.tunnel_port,
            'location': self.location,
            'notes': self.notes,
            'public_key': self.public_key,
            'status': self.status,
            'last_seen': self.last_seen.isoformat() if self.last_seen else None,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'updated_at': self.updated_at.isoformat() if self.updated_at else None,
        }

    @classmethod
    def from_dict(cls, data: dict) -> 'Device':
        return cls(
            id=data.get('id'),
            device_name=data.get('device_name', ''),
            serial_number=data.get('serial_number'),
            mac_address=data.get('mac_address'),
            tunnel_port=data.get('tunnel_port', 0),
            location=data.get('location'),
            notes=data.get('notes'),
            public_key=data.get('public_key'),
            status=data.get('status', 'offline'),
            last_seen=datetime.fromisoformat(data['last_seen']) if data.get('last_seen') else None,
            created_at=datetime.fromisoformat(data['created_at']) if data.get('created_at') else None,
            updated_at=datetime.fromisoformat(data['updated_at']) if data.get('updated_at') else None,
        )


@dataclass
class ProvisionLog:
    """Log entry for provisioning actions"""
    id: Optional[int] = None
    device_id: Optional[int] = None
    action: str = ""
    status: str = ""  # success, failed
    message: Optional[str] = None
    created_at: Optional[datetime] = None

    def to_dict(self) -> dict:
        return {
            'id': self.id,
            'device_id': self.device_id,
            'action': self.action,
            'status': self.status,
            'message': self.message,
            'created_at': self.created_at.isoformat() if self.created_at else None,
        }
