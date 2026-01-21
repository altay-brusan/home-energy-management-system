import React, { useEffect, useState, useCallback, useRef } from 'react';
import {
  Modal,
  View,
  Text as RNText,
  Pressable,
  Platform,
  ScrollView,
  ActivityIndicator,
} from 'react-native';
import { Camera, useCameraDevice, PhotoFile } from 'react-native-vision-camera';
import TextRecognition, { TextRecognitionScript } from '@react-native-ml-kit/text-recognition';
import { useTranslation } from 'react-i18next';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import { getTextScannerModalStyles } from './TextScannerModal.styles';

interface TextScannerModalProps {
  visible: boolean;
  onClose: () => void;
  onTextSelected: (text: string) => void;
}

// Serial number pattern: 2-3 letter country code + alphanumeric, total 12-16 chars
// Example: NL0RS3P12S0006 (14 chars)
const SERIAL_NUMBER_PATTERN = /^[A-Z]{2,3}[A-Z0-9]{9,14}$/;

// Validate if a string matches the serial number format
const isValidSerialNumber = (text: string): boolean => {
  if (!text || text.length < 12 || text.length > 17) return false;
  return SERIAL_NUMBER_PATTERN.test(text);
};

// Calculate similarity score between detected text and expected format
const getFormatScore = (text: string): number => {
  let score = 0;

  // Check length (optimal: 14 chars)
  const lengthDiff = Math.abs(text.length - 14);
  score += Math.max(0, 10 - lengthDiff * 2);

  // Check if starts with 2-3 letters (country code)
  if (/^[A-Z]{2,3}/.test(text)) score += 5;

  // Check mix of letters and numbers (good serial numbers have both)
  const hasLetters = /[A-Z]/.test(text.slice(3));
  const hasNumbers = /[0-9]/.test(text);
  if (hasLetters && hasNumbers) score += 5;

  return score;
};

// Filter detected texts to find potential serial numbers
const filterSerialNumbers = (recognizedText: string): string[] => {
  if (!recognizedText) return [];

  const potentialSerials: string[] = [];

  // Split by lines and whitespace
  const lines = recognizedText.split(/[\n\r]+/);

  for (const line of lines) {
    const words = line.split(/\s+/);

    for (const word of words) {
      // Clean the word - remove non-alphanumeric
      const cleaned = word.replace(/[^A-Z0-9]/gi, '').toUpperCase();

      if (isValidSerialNumber(cleaned) && !potentialSerials.includes(cleaned)) {
        potentialSerials.push(cleaned);
      }
    }

    // Also check the entire line without spaces (in case OCR adds spaces)
    const cleanedLine = line.replace(/[^A-Z0-9]/gi, '').toUpperCase();
    if (isValidSerialNumber(cleanedLine) && !potentialSerials.includes(cleanedLine)) {
      potentialSerials.push(cleanedLine);
    }
  }

  // Sort by format score (best matches first)
  return [...new Set(potentialSerials)]
    .sort((a, b) => getFormatScore(b) - getFormatScore(a))
    .slice(0, 5);
};

export default function TextScannerModal({
  visible,
  onClose,
  onTextSelected,
}: TextScannerModalProps) {
  const { t } = useTranslation();
  const styles = getTextScannerModalStyles();

  const device = useCameraDevice('back');
  const cameraRef = useRef<Camera>(null);

  const [hasPermission, setHasPermission] = useState<boolean>(false);
  const [detectedTexts, setDetectedTexts] = useState<string[]>([]);
  const [selectedText, setSelectedText] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);

  useEffect(() => {
    (async () => {
      const status = await Camera.requestCameraPermission();
      setHasPermission(status === 'granted');
    })();
  }, []);

  // Reset state when modal opens
  useEffect(() => {
    if (visible) {
      setDetectedTexts([]);
      setSelectedText(null);
      setIsProcessing(false);
    }
  }, [visible]);

  // Take photo and perform OCR
  const handleScan = useCallback(async () => {
    if (!cameraRef.current || isProcessing) return;

    try {
      setIsProcessing(true);

      // Take photo
      const photo: PhotoFile = await cameraRef.current.takePhoto({
        qualityPrioritization: 'quality',
        flash: 'off',
      });

      // Perform OCR on the photo
      const imagePath = Platform.OS === 'ios'
        ? photo.path
        : `file://${photo.path}`;

      const result = await TextRecognition.recognize(imagePath, TextRecognitionScript.LATIN);

      // Filter for serial numbers
      const serials = filterSerialNumbers(result.text);

      if (serials.length > 0) {
        setDetectedTexts(prev => {
          // Merge with existing results
          const merged = [...prev];
          for (const serial of serials) {
            if (!merged.includes(serial)) {
              merged.push(serial);
            }
          }
          return merged.slice(0, 10); // Keep max 10 results
        });
      }
    } catch (error) {
      console.error('OCR Error:', error);
    } finally {
      setIsProcessing(false);
    }
  }, [isProcessing]);

  const handleSelectText = useCallback((text: string) => {
    setSelectedText(prev => prev === text ? null : text);
  }, []);

  const handleUseSelected = useCallback(() => {
    if (selectedText) {
      onTextSelected(selectedText);
      onClose();
    }
  }, [selectedText, onTextSelected, onClose]);

  const handleClearResults = useCallback(() => {
    setDetectedTexts([]);
    setSelectedText(null);
  }, []);

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
        {/* Header */}
        <View style={styles.header}>
          <RNText style={styles.title}>{t('textScanner.title')}</RNText>
          <RNText style={styles.subtitle}>{t('textScanner.subtitle')}</RNText>
        </View>

        {/* Camera View */}
        {canShowCamera ? (
          <View style={styles.cameraWrap}>
            <Camera
              ref={cameraRef}
              style={styles.camera}
              device={device!}
              isActive={visible}
              photo={true}
            />
            {/* Scan area overlay */}
            <View pointerEvents="none" style={styles.scanArea}>
              <View style={[styles.scanAreaCorner, styles.cornerTopLeft]} />
              <View style={[styles.scanAreaCorner, styles.cornerTopRight]} />
              <View style={[styles.scanAreaCorner, styles.cornerBottomLeft]} />
              <View style={[styles.scanAreaCorner, styles.cornerBottomRight]} />
            </View>

            {/* Processing overlay */}
            {isProcessing && (
              <View style={styles.processingOverlay}>
                <ActivityIndicator size="large" color="#4CAF50" />
                <RNText style={styles.processingText}>
                  {t('textScanner.processing')}
                </RNText>
              </View>
            )}
          </View>
        ) : (
          <View style={styles.stateBox}>
            {!hasPermission ? (
              <RNText style={styles.stateText}>
                {t('textScanner.permissionRequired')}
              </RNText>
            ) : !device ? (
              <RNText style={styles.stateText}>{t('textScanner.cameraLoading')}</RNText>
            ) : null}
          </View>
        )}

        {/* Scan Button */}
        <Pressable
          style={[styles.scanButton, isProcessing && styles.scanButtonDisabled]}
          onPress={handleScan}
          disabled={isProcessing || !canShowCamera}
        >
          <MaterialIcons name="document-scanner" size={24} color="#fff" />
          <RNText style={styles.scanButtonText}>
            {isProcessing ? t('textScanner.scanning') : t('textScanner.scanButton')}
          </RNText>
        </Pressable>

        {/* Results Panel */}
        <View style={styles.resultsPanel}>
          <View style={styles.resultsPanelHeader}>
            <RNText style={styles.resultsTitle}>{t('textScanner.detectedText')}</RNText>
            {detectedTexts.length > 0 && (
              <Pressable onPress={handleClearResults}>
                <RNText style={styles.clearButton}>{t('textScanner.clear')}</RNText>
              </Pressable>
            )}
          </View>

          {detectedTexts.length > 0 ? (
            <ScrollView style={styles.resultsList} showsVerticalScrollIndicator={false}>
              {detectedTexts.map((text, index) => {
                const isSelected = selectedText === text;
                return (
                  <Pressable
                    key={`${text}-${index}`}
                    style={[
                      styles.resultItem,
                      isSelected && styles.resultItemSelected
                    ]}
                    onPress={() => handleSelectText(text)}
                  >
                    <View style={styles.resultTextContainer}>
                      <RNText
                        style={[
                          styles.resultText,
                          isSelected && styles.resultTextSelected
                        ]}
                        numberOfLines={1}
                      >
                        {text}
                      </RNText>
                    </View>
                    {isSelected && (
                      <MaterialIcons
                        name="check-circle"
                        size={24}
                        color="#4CAF50"
                        style={styles.checkIcon}
                      />
                    )}
                  </Pressable>
                );
              })}
            </ScrollView>
          ) : (
            <RNText style={styles.noResultsText}>
              {t('textScanner.noTextDetected')}
            </RNText>
          )}
        </View>

        {/* Action Buttons */}
        <View style={styles.buttonContainer}>
          <Pressable style={styles.closeBtn} onPress={onClose}>
            <RNText style={styles.closeText}>{t('textScanner.close')}</RNText>
          </Pressable>
          <Pressable
            style={[styles.useBtn, !selectedText && styles.useBtnDisabled]}
            onPress={handleUseSelected}
            disabled={!selectedText}
          >
            <RNText style={[styles.useText, !selectedText && styles.useTextDisabled]}>
              {t('textScanner.useThis')}
            </RNText>
          </Pressable>
        </View>
      </View>
    </Modal>
  );
}
