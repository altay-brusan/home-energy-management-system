import { StyleSheet } from 'react-native';

export const getTextScannerModalStyles = () => StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#000'
  },
  header: {
    paddingTop: 16,
    paddingHorizontal: 16,
    paddingBottom: 8,
  },
  title: {
    color: '#fff',
    fontSize: 20,
    textAlign: 'center',
    fontWeight: '600',
  },
  subtitle: {
    color: 'rgba(255,255,255,0.7)',
    fontSize: 14,
    textAlign: 'center',
    marginTop: 4,
  },
  cameraWrap: {
    flex: 1,
    margin: 16,
    borderRadius: 16,
    overflow: 'hidden'
  },
  camera: {
    flex: 1
  },
  scanArea: {
    position: 'absolute',
    top: '20%',
    left: '5%',
    right: '5%',
    bottom: '35%',
    borderWidth: 2,
    borderColor: 'rgba(255,255,255,0.5)',
    borderRadius: 12,
  },
  scanAreaCorner: {
    position: 'absolute',
    width: 24,
    height: 24,
    borderColor: '#4CAF50',
    borderWidth: 4,
  },
  cornerTopLeft: {
    top: -2,
    left: -2,
    borderRightWidth: 0,
    borderBottomWidth: 0,
    borderTopLeftRadius: 10,
  },
  cornerTopRight: {
    top: -2,
    right: -2,
    borderLeftWidth: 0,
    borderBottomWidth: 0,
    borderTopRightRadius: 10,
  },
  cornerBottomLeft: {
    bottom: -2,
    left: -2,
    borderRightWidth: 0,
    borderTopWidth: 0,
    borderBottomLeftRadius: 10,
  },
  cornerBottomRight: {
    bottom: -2,
    right: -2,
    borderLeftWidth: 0,
    borderTopWidth: 0,
    borderBottomRightRadius: 10,
  },
  processingOverlay: {
    ...StyleSheet.absoluteFillObject,
    backgroundColor: 'rgba(0,0,0,0.7)',
    justifyContent: 'center',
    alignItems: 'center',
    borderRadius: 16,
  },
  processingText: {
    color: '#fff',
    fontSize: 16,
    marginTop: 12,
  },
  stateBox: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 24,
  },
  stateText: {
    color: '#fff',
    fontSize: 16,
    textAlign: 'center',
    opacity: 0.85,
  },
  scanButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#4CAF50',
    marginHorizontal: 16,
    paddingVertical: 16,
    borderRadius: 12,
    gap: 8,
  },
  scanButtonDisabled: {
    backgroundColor: 'rgba(76, 175, 80, 0.5)',
  },
  scanButtonText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: '600',
  },
  resultsPanel: {
    backgroundColor: 'rgba(30,30,30,0.95)',
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    paddingHorizontal: 16,
    paddingTop: 16,
    paddingBottom: 8,
    marginTop: 16,
    maxHeight: 220,
  },
  resultsPanelHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  resultsTitle: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  clearButton: {
    color: '#f44336',
    fontSize: 14,
  },
  resultsList: {
    marginBottom: 8,
  },
  resultItem: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: 'rgba(255,255,255,0.08)',
    borderRadius: 10,
    paddingVertical: 12,
    paddingHorizontal: 16,
    marginBottom: 8,
    borderWidth: 1,
    borderColor: 'transparent',
  },
  resultItemSelected: {
    backgroundColor: 'rgba(76, 175, 80, 0.15)',
    borderColor: '#4CAF50',
  },
  resultTextContainer: {
    flex: 1,
  },
  resultText: {
    color: '#fff',
    fontSize: 16,
    fontFamily: 'monospace',
    fontWeight: '500',
    letterSpacing: 1,
  },
  resultTextSelected: {
    color: '#4CAF50',
  },
  checkIcon: {
    marginLeft: 12,
  },
  noResultsText: {
    color: 'rgba(255,255,255,0.4)',
    fontSize: 14,
    textAlign: 'center',
    paddingVertical: 16,
  },
  buttonContainer: {
    flexDirection: 'row',
    gap: 12,
    paddingHorizontal: 16,
    paddingVertical: 16,
    backgroundColor: 'rgba(30,30,30,0.95)',
  },
  closeBtn: {
    flex: 1,
    padding: 16,
    alignItems: 'center',
    backgroundColor: 'rgba(255,255,255,0.1)',
    borderRadius: 12,
  },
  closeText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '500',
  },
  useBtn: {
    flex: 1,
    padding: 16,
    alignItems: 'center',
    backgroundColor: '#2196F3',
    borderRadius: 12,
  },
  useBtnDisabled: {
    backgroundColor: 'rgba(33, 150, 243, 0.3)',
  },
  useText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  useTextDisabled: {
    opacity: 0.5,
  },
});
