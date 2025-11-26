/**
 * PowerFlow SignalR Streaming Test - Browser Console
 *
 * Kullanım:
 * 1. Chrome/Firefox DevTools Console'u aç (F12)
 * 2. Bu dosyayı kopyala-yapıştır ve Enter'a bas
 * 3. Canlı verileri console'da gör
 */

// SignalR CDN'den yükle (eğer yoksa)
if (typeof signalR === 'undefined') {
  const script = document.createElement('script');
  script.src = 'https://cdn.jsdelivr.net/npm/@microsoft/signalr@7.0.14/dist/browser/signalr.min.js';
  script.onload = () => {
    console.log('✅ SignalR loaded!');
    console.log('Now run: testPowerFlow("YOUR-DEVICE-ID", "YOUR-JWT-TOKEN")');
  };
  document.head.appendChild(script);
} else {
  console.log('✅ SignalR already loaded!');
}

/**
 * Test PowerFlow streaming
 * @param {string} deviceId - Device GUID
 * @param {string} token - JWT access token
 * @param {number} intervalSeconds - Update interval (default: 5)
 */
async function testPowerFlow(deviceId, token, intervalSeconds = 5) {
  console.log('🧪 Starting PowerFlow stream test...');
  console.log(`📍 Device ID: ${deviceId}`);
  console.log(`⏱️ Interval: ${intervalSeconds}s`);

  const hubUrl = 'https://hems-api-dev.oklidis.com.tr/powerFlowHub';

  // Create connection
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  // Connection events
  connection.onclose((error) => {
    console.log('🔴 Connection closed', error);
  });

  connection.onreconnecting((error) => {
    console.log('🟡 Reconnecting...', error);
  });

  connection.onreconnected((connectionId) => {
    console.log('🟢 Reconnected!', connectionId);
  });

  try {
    // Start connection
    console.log('🔌 Connecting to hub...');
    await connection.start();
    console.log('✅ Connected!');

    // Start streaming
    console.log('▶️ Starting stream...');
    const stream = connection.stream('StreamPowerFlow', deviceId, intervalSeconds);

    let count = 0;
    const subscription = stream.subscribe({
      next: (data) => {
        count++;
        console.log(`\n📊 PowerFlow Data #${count} [${new Date().toLocaleTimeString()}]`);
        console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
        console.table({
          'Device ID': data.deviceId,
          'Timestamp': data.timestamp,
          '🔌 Grid Power (W)': data.gridPower,
          '☀️ Solar Power (W)': data.solarPower,
          '🔋 Battery Power (W)': data.batteryPower,
          '💡 Load Power (W)': data.loadPower,
          '🔋 Battery SOC (%)': data.batterySOC,
          'Battery Status': data.batteryStatus || 'N/A',
          'Grid Status': data.gridStatus || 'N/A',
        });
        console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
      },
      error: (err) => {
        console.error('❌ Stream error:', err);
      },
      complete: () => {
        console.log('🏁 Stream completed');
      }
    });

    // Store globally to stop later
    window.powerFlowSubscription = subscription;
    window.powerFlowConnection = connection;

    console.log('✅ Stream started! Listening for data...');
    console.log('To stop: stopPowerFlow()');

  } catch (error) {
    console.error('❌ Error:', error);
  }
}

/**
 * Stop PowerFlow streaming
 */
async function stopPowerFlow() {
  console.log('⏹️ Stopping stream...');

  if (window.powerFlowSubscription) {
    window.powerFlowSubscription.dispose();
    console.log('✅ Subscription disposed');
  }

  if (window.powerFlowConnection) {
    await window.powerFlowConnection.stop();
    console.log('✅ Connection closed');
  }

  console.log('🏁 Test completed');
}

// Instructions
console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
console.log('🔬 PowerFlow SignalR Test Tool');
console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
console.log('');
console.log('📝 Usage:');
console.log('1. Get your JWT token from app (copy from AsyncStorage or network tab)');
console.log('2. Get a device ID (GUID format)');
console.log('3. Run:');
console.log('   testPowerFlow("YOUR-DEVICE-ID", "YOUR-JWT-TOKEN", 5)');
console.log('');
console.log('4. To stop:');
console.log('   stopPowerFlow()');
console.log('');
console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
