namespace WireViewMonitor.Service.WireView;

// ── SensorStruct byte offsets ────────────────────────────────────────────
// short[4] Ts           offset  0  size  8
// ushort   Vdd          offset  8  size  2
// byte     FanDuty      offset 10  size  1
// (padding)             offset 11  size  1  — align PowerSensor[] to 4
// PowerSensor[6]        offset 12  size 72  — 12 bytes each
// uint     TotalPower   offset 84  size  4
// uint     TotalCurrent offset 88  size  4
// ushort   AvgVoltage   offset 92  size  2
// byte     HpwrCap      offset 94  size  1
// (padding)             offset 95  size  1  — align ushort to 2
// ushort   FaultStatus  offset 96  size  2
// ushort   FaultLog     offset 98  size  2
//                       total     100 bytes

// ── PowerSensor offsets (relative, 12 bytes each) ────────────────────────
// short Voltage  offset 0  size 2
// (padding)      offset 2  size 2  — align uint to 4
// uint  Current  offset 4  size 4
// uint  Power    offset 8  size 4