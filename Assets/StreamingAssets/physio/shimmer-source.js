// Connects a Shimmer sensor in the browser and hands its PPG and GSR samples to Physio.
// Uses Shimmer's own web SDK (vendor/shimmer-web-sdk.esm.js, see vendor/NOTICE.txt).
//   shimmer3r-ble     Shimmer3R over Bluetooth Low Energy (Web Bluetooth: Chrome or Edge)
//   shimmer3-serial   classic Shimmer3 paired in the computer's Bluetooth settings, opened as a serial
//                     port (Web Serial: Chrome or Edge on a computer). Not tested by us yet.
// Both need a click from the user (a browser rule) and the sensor set up for a PPG + GSR unit
// (Shimmer3 GSR+ or Shimmer3R GSR+). Sensors enabled: GSR (0x04) + internal ADC A13 (0x100) = 260.
const SENSORS_PPG_GSR = 260;
const SAMPLING_HZ = 128;

export async function connectShimmer(kind, handlers) {
  const sdk = await import('./vendor/shimmer-web-sdk.esm.js');
  let client;
  if (kind === 'shimmer3r-ble') {
    client = new sdk.Shimmer3RClient();
  } else if (kind === 'shimmer3-serial') {
    const transport = new sdk.WebSerialTransport({ ...sdk.SHIMMER3_SPP_SERIAL_OPTIONS });
    client = new sdk.Shimmer3Client({ transport });
  } else {
    throw new Error('Unknown Shimmer connection: ' + kind);
  }
  client.onStatus = (msg) => handlers.status(String(msg));
  client.onDisconnect = (reason) => handlers.closed(reason ? String(reason.message || reason) : 'the sensor went away');
  let sampleCount = 0;
  client.onStreamFrame = (oc) => {
    const ppg = pick(oc, ['PPG'], /ppg|a13/i, 'raw');
    const eda = pick(oc, ['GSR'], /^gsr$/i, 'cal');      // calibrated conductance in microsiemens
    sampleCount++;
    handlers.sample({ ppg: ppg, eda: eda });
  };
  handlers.status('Choose the sensor in the browser window that opens...');
  await client.connect();
  handlers.status('Connected. Setting up PPG and GSR...');
  await client.setSamplingRate(SAMPLING_HZ);
  await client.setInternalExpPower(1);
  await client.setGSRRange(4);                 // auto range
  await client.setSensors(SENSORS_PPG_GSR);
  await client.startStreaming();
  handlers.status('Streaming PPG and GSR at ' + SAMPLING_HZ + ' Hz.');
  return {
    name: (client.device && client.device.name) || 'Shimmer',
    stop: async () => {
      try { await client.stopStreaming(); } catch (e) { /* already stopped */ }
      try { await client.disconnect(); } catch (e) { /* already closed */ }
    },
  };
}

// The first field that exists: by exact name, else by pattern on the field names
function pick(oc, names, pattern, kind) {
  for (const n of names) { const f = oc.get(n, kind); if (f && Number.isFinite(f.value)) { return f.value; } }
  for (const f of oc.fields || []) {
    if (f.kind === kind && pattern.test(f.name) && Number.isFinite(f.value)) { return f.value; }
  }
  return undefined;
}
