import React, { useEffect, useRef, useState } from 'react';
import {
  Modal,
  View,
  Text,
  Pressable,
  Platform,
} from 'react-native';
import { Camera, useCameraDevice, useCodeScanner } from 'react-native-vision-camera';
import { useTranslation } from 'react-i18next';
import { getQrScannerModalStyles } from './QrScannerModal.styles';

interface QrScannerModalProps {
  visible: boolean;
  onClose: () => void;
  onScan: (code: string) => void;
}

export default function QrScannerModal ({
  visible,
  onClose,
  onScan,
}: QrScannerModalProps) {
  const { t } = useTranslation();
  const styles = getQrScannerModalStyles();

  const device = useCameraDevice('back');

  const [hasPermission, setHasPermission] = useState<boolean>(false);
  useEffect(() => {
    (async () => {
      const status = await Camera.requestCameraPermission();
      setHasPermission(status === 'granted');
    })();
  }, []);

  const codeScanner = useCodeScanner({
    codeTypes: ['qr', 'ean-13'],
    onCodeScanned: (codes) => {
      if (codes.length > 0 && !lockedRef.current) {
        const code = codes[0];
        if (code.value) {
          lockedRef.current = true;
          try {
            onScan(code.value);
          } finally {
            setTimeout(onClose, 50);
          }
        }
      }
    },
  });

  const lockedRef = useRef(false);

  useEffect(() => {
    if (visible) lockedRef.current = false;
  }, [visible]);

  const canShowCamera = visible && hasPermission && !!device;

  return (
    <Modal
      visible={visible}
      animationType="slide"
      onRequestClose={onClose}
      presentationStyle={Platform.OS === 'ios' ? 'fullScreen' : undefined}
      hardwareAccelerated
    >
      <View style={styles.container}>
        <Text style={styles.title}>{t('qrScanner.title')}</Text>

        {canShowCamera ? (
          <View style={styles.cameraWrap}>
            <Camera
              style={styles.camera}
              device={device!}
              isActive={visible}
              codeScanner={codeScanner}
            />
            <View pointerEvents="none" style={styles.reticle} />
          </View>
        ) : (
          <View style={styles.stateBox}>
            {!hasPermission ? (
              <Text style={styles.stateText}>
                {t('qrScanner.permissionRequired')}
              </Text>
            ) : !device ? (
              <Text style={styles.stateText}>{t('qrScanner.cameraLoading')}</Text>
            ) : null}
          </View>
        )}

        <Pressable style={styles.closeBtn} onPress={onClose}>
          <Text style={styles.closeText}>{t('qrScanner.close')}</Text>
        </Pressable>
      </View>
    </Modal>
  );
}

