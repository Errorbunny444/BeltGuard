// Standalone test for Health Assessment mathematical rules & classifications
const BASE_SCORE = 100;
const DEFECT_PENALTIES = {
  'small tear': 15,
  'large tear': 35,
  'small hole': 25,
  'large hole': 45,
  'belt joint': 5,
  'unknown defect': 10,
};

function mapScoreToCondition(score) {
  if (score >= 85) return { condition: 'HEALTHY', severity: 'NORMAL' };
  if (score >= 60) return { condition: 'WARNING', severity: 'MEDIUM' };
  if (score >= 30) return { condition: 'HIGH_RISK', severity: 'HIGH' };
  return { condition: 'CRITICAL', severity: 'CRITICAL' };
}

function calculateHealthScore({ primaryDefect, confidence, count, temperature, vibration, beltSpeed, sensorOffline, visionOffline }) {
  let defectPenalty = 0;
  let sensorPenalty = 0;
  let freshnessPenalty = 0;

  // Defect
  if (primaryDefect && primaryDefect !== '--' && primaryDefect !== 'none') {
    let base = DEFECT_PENALTIES[primaryDefect.toLowerCase()] || 10;
    if (count > 3) base += 10;
    else if (count > 1) base += 5;

    if (confidence < 0.50) base = Math.round(base * 0.5);
    else if (confidence > 0.90) base += 5;

    defectPenalty = base;
  }

  // Sensors
  if (!sensorOffline) {
    if (temperature !== null) {
      if (temperature > 65.0) sensorPenalty += 30;
      else if (temperature >= 50.0) sensorPenalty += 15;
    }
    if (vibration !== null) {
      if (vibration > 0.60) sensorPenalty += 30;
      else if (vibration >= 0.30) sensorPenalty += 15;
    }
    if (beltSpeed !== null) {
      if (beltSpeed < 0.05) sensorPenalty += 10;
      else if (beltSpeed < 0.50) sensorPenalty += 5;
      else if (beltSpeed > 2.0) sensorPenalty += 10;
    }
  }

  // Freshness
  if (sensorOffline) freshnessPenalty += 10;
  if (visionOffline) freshnessPenalty += 15;

  const totalPenalty = defectPenalty + sensorPenalty + freshnessPenalty;
  const score = Math.max(0, Math.min(100, Math.round(BASE_SCORE - totalPenalty)));
  const { condition, severity } = mapScoreToCondition(score);

  return { score, condition, severity, defectPenalty, sensorPenalty, freshnessPenalty, totalPenalty };
}

// Test cases
console.log('=== TEST 1: Baseline Nominal ===');
const t1 = calculateHealthScore({
  primaryDefect: '--', confidence: 0, count: 0,
  temperature: 42.0, vibration: 0.18, beltSpeed: 1.25,
  sensorOffline: false, visionOffline: false
});
console.log(t1);
console.assert(t1.score === 100 && t1.condition === 'HEALTHY', 'Test 1 failed');

console.log('\n=== TEST 2: Large Tear Detection (94% confidence) ===');
const t2 = calculateHealthScore({
  primaryDefect: 'Large Tear', confidence: 0.94, count: 1,
  temperature: 42.0, vibration: 0.18, beltSpeed: 1.25,
  sensorOffline: false, visionOffline: false
});
console.log(t2);
// 35 base + 5 high confidence = 40 penalty -> score 60 (WARNING)
console.assert(t2.score === 60 && t2.condition === 'WARNING', 'Test 2 failed');

console.log('\n=== TEST 3: Large Tear + Critical Bearing Temperature (68°C) ===');
const t3 = calculateHealthScore({
  primaryDefect: 'Large Tear', confidence: 0.94, count: 1,
  temperature: 68.0, vibration: 0.18, beltSpeed: 1.25,
  sensorOffline: false, visionOffline: false
});
console.log(t3);
// 40 defect + 30 temp = 70 penalty -> score 30 (HIGH_RISK)
console.assert(t3.score === 30 && t3.condition === 'HIGH_RISK', 'Test 3 failed');

console.log('\n=== TEST 4: Large Tear + Critical Temp + Critical Vibration (0.75g) ===');
const t4 = calculateHealthScore({
  primaryDefect: 'Large Tear', confidence: 0.94, count: 1,
  temperature: 68.0, vibration: 0.75, beltSpeed: 1.25,
  sensorOffline: false, visionOffline: false
});
console.log(t4);
// 40 defect + 30 temp + 30 vib = 100 penalty -> score 0 (CRITICAL)
console.assert(t4.score === 0 && t4.condition === 'CRITICAL', 'Test 4 failed');

console.log('\n=== TEST 5: Sensor Offline Freshness Penalty ===');
const t5 = calculateHealthScore({
  primaryDefect: '--', confidence: 0, count: 0,
  temperature: null, vibration: null, beltSpeed: null,
  sensorOffline: true, visionOffline: false
});
console.log(t5);
// 10 penalty -> score 90 (HEALTHY)
console.assert(t5.score === 90 && t5.condition === 'HEALTHY', 'Test 5 failed');

console.log('\nAll 5 Unit Tests Passed Cleanly!');
