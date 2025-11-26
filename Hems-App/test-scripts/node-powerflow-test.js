/**
 * PowerFlow SignalR Streaming Test - Node.js
 *
 * Kurulum:
 *   npm install @microsoft/signalr
 *
 * Kullanım:
 *   node node-powerflow-test.js <DEVICE_ID> <JWT_TOKEN> [INTERVAL]
 *
 * Örnek:
 *   node node-powerflow-test.js "a1b2c3d4-e5f6-7890-abcd-ef1234567890" "eyJhbGc..." 5
 */

const signalR = require('@microsoft/signalr');

// Parse arguments
const deviceId = process.argv[2];
const token = process.argv[3];
const intervalSeconds = parseInt(process.argv[4]) || 5;

if (!deviceId || !token) {
  console.error('❌ Missing arguments!');
  console.error('Usage: node node-powerflow-test.js <DEVICE_ID> <JWT_TOKEN> [INTERVAL]');
  process.exit(1);
}

const HUB_URL = 'https://hems-api-dev.oklidis.com.tr/powerFlowHub';

async function testPowerFlow() {
  console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
  console.log('🔬 PowerFlow SignalR Streaming Test');
  console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
  console.log(`📍 Hub URL: ${HUB_URL}`);
  console.log(`📱 Device ID: ${deviceId}`);
  console.log(`⏱️  Interval: ${intervalSeconds}s`);
  console.log(`🔑 Token: ${token.substring(0, 20)}...`);
  console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n');

  // Create connection
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  // Connection events
  connection.onclose((error) => {
    console.log('🔴 Connection closed:', error?.message || 'Unknown error');
    process.exit(0);
  });

  connection.onreconnecting((error) => {
    console.log('🟡 Reconnecting...', error?.message);
  });

  connection.onreconnected((connectionId) => {
    console.log('🟢 Reconnected! Connection ID:', connectionId);
  });

  try {
    // Start connection
    console.log('🔌 Connecting to hub...');
    await connection.start();
    console.log('✅ Connected!\n');

    // Start streaming
    console.log('▶️  Starting stream...');
    const stream = connection.stream('StreamPowerFlow', deviceId, intervalSeconds);

    let count = 0;
    const startTime = Date.now();

    stream.subscribe({
      next: (data) => {
        count++;
        const elapsed = ((Date.now() - startTime) / 1000).toFixed(1);

        console.log(`\n📊 PowerFlow Data #${count} [+${elapsed}s] [${new Date().toLocaleTimeString()}]`);
        console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
        console.log(`Device ID:         ${data.deviceId}`);
        console.log(`Timestamp:         ${data.timestamp}`);
        console.log('─────────────────────────────────────────');
        console.log(`🔌 Grid Power:     ${formatPower(data.gridPower)}`);
        console.log(`☀️  Solar Power:    ${formatPower(data.solarPower)}`);
        console.log(`🔋 Battery Power:  ${formatPower(data.batteryPower)}`);
        console.log(`💡 Load Power:     ${formatPower(data.loadPower)}`);
        console.log('─────────────────────────────────────────');
        console.log(`🔋 Battery SOC:    ${data.batterySOC.toFixed(1)}%`);
        if (data.batteryStatus) {
          console.log(`   Battery Status: ${data.batteryStatus}`);
        }
        if (data.gridStatus) {
          console.log(`   Grid Status:    ${data.gridStatus}`);
        }
        console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
      },
      error: (err) => {
        console.error('\n❌ Stream error:', err.message || err);
        connection.stop();
      },
      complete: () => {
        console.log('\n🏁 Stream completed');
        connection.stop();
      }
    });

    console.log('✅ Stream started! Listening for data...');
    console.log('Press Ctrl+C to stop\n');

    // Handle Ctrl+C
    process.on('SIGINT', async () => {
      console.log('\n\n⏹️  Stopping stream...');
      await connection.stop();
      console.log('✅ Stopped');
      process.exit(0);
    });

  } catch (error) {
    console.error('\n❌ Error:', error.message || error);
    process.exit(1);
  }
}

function formatPower(watts) {
  const absWatts = Math.abs(watts);
  if (absWatts >= 1000) {
    return `${(watts / 1000).toFixed(2)} kW`;
  }
  return `${watts.toFixed(2)} W`;
}

// Run test
testPowerFlow().catch((error) => {
  console.error('Fatal error:', error);
  process.exit(1);
});
