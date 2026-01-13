import { StyleSheet } from 'react-native';

export const getQrScannerModalStyles = () => StyleSheet.create({
  container: { 
    flex: 1, 
    backgroundColor: '#000' 
  },
  title: {
    color: '#fff',
    fontSize: 20,
    textAlign: 'center',
    marginTop: 16,
    marginBottom: 8,
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
  reticle: {
    position: 'absolute',
    top: '25%',
    left: '10%',
    right: '10%',
    bottom: '25%',
    borderWidth: 2,
    borderColor: 'rgba(255,255,255,0.8)',
    borderRadius: 12,
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
  closeBtn: { 
    padding: 16, 
    alignItems: 'center' 
  },
  closeText: { 
    color: '#fff', 
    fontSize: 18 
  },
});

