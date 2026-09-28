/*
  ============================================================
                         BELTGUARD
       INTELLIGENT CONVEYOR BELT CONDITION MONITORING
  ============================================================

  CONTROLLER:
      ESP32

  SENSORS:
      MPU6050 -> vibration / mechanical behaviour
      DHT22   -> ambient temperature & humidity

  DISPLAY:
      SSD1306 128x64 OLED

  NETWORK:
      ESP32 SoftAP

      SSID     : YOUR_WIFI_SSID
      PASSWORD : YOUR_WIFI_PASSWORD
      IP       : 192.168.4.1

  I2C:
      SDA -> GPIO 21
      SCL -> GPIO 22

  DHT22:
      DATA -> GPIO 4

  MPU6050:
      ±4G
      ~200 Hz sampling
      100 sample RMS window

  IMPORTANT:
      This is a prototype condition-monitoring system.
      Thresholds must be calibrated against actual conveyor
      baseline measurements before industrial deployment.

      Health Index is a prototype derived indicator.
      It is NOT a certified RUL prediction.
  ============================================================
*/


// ============================================================
// LIBRARIES
// ============================================================

#include <Wire.h>
#include <MPU6050.h>
#include <math.h>

#include <DHT.h>

#include <WiFi.h>
#include <WebServer.h>

#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>


// ============================================================
// PIN CONFIGURATION
// ============================================================

#define SDA_PIN 21
#define SCL_PIN 22

#define DHT_PIN 4
#define DHT_TYPE DHT22


// ============================================================
// OLED
// ============================================================

#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64

#define OLED_RESET -1
#define OLED_ADDR 0x3C


// ============================================================
// OBJECTS
// ============================================================

MPU6050 mpu;

DHT dht(
  DHT_PIN,
  DHT_TYPE
);

WebServer server(80);

Adafruit_SSD1306 display(
  SCREEN_WIDTH,
  SCREEN_HEIGHT,
  &Wire,
  OLED_RESET
);


// ============================================================
// WIFI
// ============================================================

const char* AP_SSID = "YOUR_WIFI_SSID";
const char* AP_PASS = "YOUR_WIFI_PASSWORD";

IPAddress localIP;


// ============================================================
// MPU6050
// ============================================================

const float ACCEL_SCALE = 8192.0;

const float NOISE_FLOOR = 0.03;

const float VIB_ALPHA = 0.20;


// ============================================================
// RMS
// ============================================================

const int RMS_SAMPLES = 100;

float rmsBuffer[RMS_SAMPLES];

int rmsIndex = 0;

bool rmsFilled = false;


// ============================================================
// SENSOR VALUES
// ============================================================

float vibration = 0.0;

float vibrationRMS = 0.0;

float vibrationPeak = 0.0;

float temperature = 0.0;

float humidity = 0.0;


// ============================================================
// CONDITION
// ============================================================

String condition = "NORMAL";

String severityClass = "normal";

int healthIndex = 100;


// ============================================================
// PROTOTYPE THRESHOLDS
// ============================================================

const float WARNING_THRESHOLD = 0.08;

const float CRITICAL_THRESHOLD = 0.16;

const float HEALTH_MAX_RMS = 0.25;


// ============================================================
// TIMING
// ============================================================

unsigned long lastSample = 0;

unsigned long lastDHT = 0;

unsigned long lastOLED = 0;

unsigned long lastSerial = 0;

unsigned long lastPeakReset = 0;

unsigned long bootTime = 0;


const unsigned long SAMPLE_INTERVAL = 5;

const unsigned long DHT_INTERVAL = 2000;

const unsigned long OLED_INTERVAL = 250;

const unsigned long SERIAL_INTERVAL = 1000;

const unsigned long PEAK_INTERVAL = 1000;


// ============================================================
// OLED STATE
// ============================================================

int currentOLEDPage = 0;

unsigned long lastOLEDPageChange = 0;

const unsigned long OLED_PAGE_TIME = 5000;


// ============================================================
// EVENT HISTORY
// ============================================================

String event1 = "Monitoring active";

String event2 = "Sensors online";

String event3 = "System initialized";

unsigned long event1Time = 0;

unsigned long event2Time = 0;

unsigned long event3Time = 0;


// ============================================================
// PREVIOUS CONDITION
// ============================================================

String previousCondition = "NORMAL";


// ============================================================
// PUSH EVENT
// ============================================================

void pushEvent(String message)
{
  event3 = event2;
  event3Time = event2Time;

  event2 = event1;
  event2Time = event1Time;

  event1 = message;
  event1Time = millis();
}


// ============================================================
// UPTIME
// ============================================================

String getUptime()
{
  unsigned long totalSeconds =
    (millis() - bootTime) / 1000;


  unsigned long hours =
    totalSeconds / 3600;


  unsigned long minutes =
    (totalSeconds % 3600) / 60;


  unsigned long seconds =
    totalSeconds % 60;


  char buffer[20];


  sprintf(
    buffer,
    "%02lu:%02lu:%02lu",
    hours,
    minutes,
    seconds
  );


  return String(buffer);
}


// ============================================================
// EVENT TIME
// ============================================================

String getEventTime(
  unsigned long eventMillis
)
{
  if(eventMillis == 0)
  {
    return "--:--:--";
  }


  unsigned long seconds =
    eventMillis / 1000;


  unsigned long hours =
    (seconds / 3600) % 24;


  unsigned long minutes =
    (seconds / 60) % 60;


  unsigned long secs =
    seconds % 60;


  char buffer[15];


  sprintf(
    buffer,
    "%02lu:%02lu:%02lu",
    hours,
    minutes,
    secs
  );


  return String(buffer);
}


// ============================================================
// CONDITION UPDATE
// ============================================================

void updateCondition()
{
  String newCondition;


  // ----------------------------------------------------------
  // CLASSIFICATION
  // ----------------------------------------------------------

  if(
    vibrationRMS <
    WARNING_THRESHOLD
  )
  {
    newCondition =
      "NORMAL";

    severityClass =
      "normal";
  }

  else if(
    vibrationRMS <
    CRITICAL_THRESHOLD
  )
  {
    newCondition =
      "WARNING";

    severityClass =
      "warning";
  }

  else
  {
    newCondition =
      "CRITICAL";

    severityClass =
      "critical";
  }


  condition =
    newCondition;


  // ----------------------------------------------------------
  // PROTOTYPE HEALTH INDEX
  // ----------------------------------------------------------

  float normalized =
    vibrationRMS /
    HEALTH_MAX_RMS;


  normalized =
    constrain(
      normalized,
      0.0,
      1.0
    );


  healthIndex =
    100 -
    (int)(
      normalized * 100.0
    );


  healthIndex =
    constrain(
      healthIndex,
      0,
      100
    );


  // ----------------------------------------------------------
  // EVENT LOG
  // ----------------------------------------------------------

  if(
    condition !=
    previousCondition
  )
  {
    if(
      condition ==
      "NORMAL"
    )
    {
      pushEvent(
        "Condition returned to NORMAL"
      );
    }

    else if(
      condition ==
      "WARNING"
    )
    {
      pushEvent(
        "WARNING: vibration elevated"
      );
    }

    else if(
      condition ==
      "CRITICAL"
    )
    {
      pushEvent(
        "CRITICAL: high vibration"
      );
    }


    previousCondition =
      condition;
  }
}


// ============================================================
// VIBRATION PROCESSING
// ============================================================

void updateVibration()
{
  int16_t ax;
  int16_t ay;
  int16_t az;

  int16_t gx;
  int16_t gy;
  int16_t gz;


  mpu.getMotion6(
    &ax,
    &ay,
    &az,
    &gx,
    &gy,
    &gz
  );


  // ----------------------------------------------------------
  // CONVERT TO G
  // ----------------------------------------------------------

  float axG =
    ax / ACCEL_SCALE;

  float ayG =
    ay / ACCEL_SCALE;

  float azG =
    az / ACCEL_SCALE;


  // ----------------------------------------------------------
  // ACCELERATION MAGNITUDE
  // ----------------------------------------------------------

  float magnitude =
    sqrt(
      axG * axG +
      ayG * ayG +
      azG * azG
    );


  // ----------------------------------------------------------
  // APPROXIMATE GRAVITY REMOVAL
  // ----------------------------------------------------------

  float rawVibration =
    fabs(
      magnitude - 1.0
    );


  // ----------------------------------------------------------
  // NOISE FLOOR
  // ----------------------------------------------------------

  if(
    rawVibration <
    NOISE_FLOOR
  )
  {
    rawVibration =
      0.0;
  }


  // ----------------------------------------------------------
  // EXPONENTIAL SMOOTHING
  // ----------------------------------------------------------

  vibration =
    VIB_ALPHA *
    rawVibration
    +
    (1.0 - VIB_ALPHA) *
    vibration;


  // ----------------------------------------------------------
  // RMS BUFFER
  // ----------------------------------------------------------

  rmsBuffer[rmsIndex] =
    vibration;


  rmsIndex++;


  if(
    rmsIndex >=
    RMS_SAMPLES
  )
  {
    rmsIndex = 0;

    rmsFilled = true;
  }


  int count =
    rmsFilled
    ?
    RMS_SAMPLES
    :
    rmsIndex;


  // ----------------------------------------------------------
  // RMS
  // ----------------------------------------------------------

  if(count > 0)
  {
    double sumSquares =
      0.0;


    for(
      int i = 0;
      i < count;
      i++
    )
    {
      sumSquares +=
        (
          double
        )
        rmsBuffer[i]
        *
        rmsBuffer[i];
    }


    if(
      sumSquares <
      0
    )
    {
      sumSquares = 0;
    }


    vibrationRMS =
      sqrt(
        sumSquares /
        count
      );
  }


  // ----------------------------------------------------------
  // PEAK
  // ----------------------------------------------------------

  if(
    vibration >
    vibrationPeak
  )
  {
    vibrationPeak =
      vibration;
  }


  // ----------------------------------------------------------
  // UPDATE CONDITION
  // ----------------------------------------------------------

  updateCondition();
}


// ============================================================
// DHT22
// ============================================================

void updateEnvironment()
{
  float newTemperature =
    dht.readTemperature();


  float newHumidity =
    dht.readHumidity();


  if(
    !isnan(
      newTemperature
    )
  )
  {
    temperature =
      newTemperature;
  }


  if(
    !isnan(
      newHumidity
    )
  )
  {
    humidity =
      newHumidity;
  }
}


// ============================================================
// OLED HEADER
// ============================================================

void oledHeader(
  const char* title
)
{
  display.clearDisplay();


  display.setTextColor(
    SSD1306_WHITE
  );


  display.setTextSize(
    1
  );


  display.setCursor(
    0,
    0
  );

  display.print(
    "BELTGUARD"
  );


  display.setCursor(
    88,
    0
  );

  display.print(
    "BG-01"
  );


  display.drawLine(
    0,
    10,
    127,
    10,
    SSD1306_WHITE
  );


  display.setCursor(
    0,
    14
  );

  display.print(
    title
  );
}


// ============================================================
// OLED — MAIN STATUS
// ============================================================

void oledStatus()
{
  display.clearDisplay();


  display.setTextColor(
    SSD1306_WHITE
  );


  display.setTextSize(1);


  display.setCursor(
    0,
    0
  );

  display.print(
    "BELTGUARD"
  );


  display.setCursor(
    88,
    0
  );

  display.print(
    "BG-01"
  );


  display.drawLine(
    0,
    10,
    127,
    10,
    SSD1306_WHITE
  );


  display.setCursor(
    0,
    14
  );

  display.print(
    "BELT CONDITION"
  );


  // ----------------------------------------------------------
  // LARGE STATUS
  // ----------------------------------------------------------

  display.setTextSize(
    2
  );


  display.setCursor(
    0,
    27
  );


  display.print(
    condition
  );


  display.setTextSize(
    1
  );


  display.setCursor(
    0,
    47
  );


  display.print(
    "HEALTH "
  );


  display.print(
    healthIndex
  );


  display.print(
    "%"
  );


  display.setCursor(
    72,
    47
  );


  display.print(
    "RMS "
  );


  display.print(
    vibrationRMS,
    3
  );


  display.setCursor(
    0,
    58
  );


  display.print(
    "LIVE MONITOR"
  );


  display.display();
}


// ============================================================
// OLED — VIBRATION
// ============================================================

void oledVibration()
{
  oledHeader(
    "VIBRATION"
  );


  display.setCursor(
    0,
    27
  );

  display.print(
    "LIVE   "
  );

  display.print(
    vibration,
    3
  );

  display.print(
    " G"
  );


  display.setCursor(
    0,
    38
  );

  display.print(
    "RMS    "
  );

  display.print(
    vibrationRMS,
    3
  );

  display.print(
    " G"
  );


  display.setCursor(
    0,
    49
  );

  display.print(
    "PEAK   "
  );

  display.print(
    vibrationPeak,
    3
  );

  display.print(
    " G"
  );


  display.setCursor(
    82,
    27
  );

  display.print(
    condition
  );


  display.display();
}


// ============================================================
// OLED — ENVIRONMENT
// ============================================================

void oledEnvironment()
{
  oledHeader(
    "ENVIRONMENT"
  );


  display.setCursor(
    0,
    27
  );

  display.print(
    "TEMP   "
  );

  display.print(
    temperature,
    1
  );

  display.print(
    " C"
  );


  display.setCursor(
    0,
    40
  );

  display.print(
    "HUM    "
  );

  display.print(
    humidity,
    1
  );

  display.print(
    " %"
  );


  display.setCursor(
    0,
    53
  );

  display.print(
    "AMBIENT CONTEXT"
  );


  display.display();
}


// ============================================================
// OLED — NODE INFORMATION
// ============================================================

void oledNode()
{
  oledHeader(
    "EDGE NODE"
  );


  display.setCursor(
    0,
    26
  );

  display.print(
    "WIFI   READY"
  );


  display.setCursor(
    0,
    37
  );

  display.print(
    "IP     192.168.4.1"
  );


  display.setCursor(
    0,
    48
  );

  display.print(
    "RATE   ~200 Hz"
  );


  display.setCursor(
    0,
    59
  );

  display.print(
    "UP     "
  );

  display.print(
    getUptime()
  );


  display.display();
}


// ============================================================
// OLED UPDATE
// ============================================================

void updateOLED()
{
  unsigned long now =
    millis();


  /*
    If abnormal behaviour occurs,
    immediately show status screen.
  */

  if(
    condition !=
    "NORMAL"
  )
  {
    oledStatus();

    return;
  }


  // ----------------------------------------------------------
  // NORMAL ROTATION
  // ----------------------------------------------------------

  if(
    now -
    lastOLEDPageChange
    >=
    OLED_PAGE_TIME
  )
  {
    lastOLEDPageChange =
      now;


    currentOLEDPage++;


    if(
      currentOLEDPage >=
      4
    )
    {
      currentOLEDPage =
        0;
    }
  }


  if(
    currentOLEDPage ==
    0
  )
  {
    oledStatus();
  }

  else if(
    currentOLEDPage ==
    1
  )
  {
    oledVibration();
  }

  else if(
    currentOLEDPage ==
    2
  )
  {
    oledEnvironment();
  }

  else
  {
    oledNode();
  }
}


// ============================================================
// SERIAL DEBUG
// ============================================================

void serialDebug()
{
  Serial.println();

  Serial.println(
    "===================================================="
  );

  Serial.println(
    "                 BELTGUARD BG-01"
  );

  Serial.println(
    "===================================================="
  );


  Serial.print(
    "STATUS        : "
  );

  Serial.println(
    condition
  );


  Serial.print(
    "VIBRATION     : "
  );

  Serial.print(
    vibration,
    3
  );

  Serial.println(
    " G"
  );


  Serial.print(
    "RMS           : "
  );

  Serial.print(
    vibrationRMS,
    3
  );

  Serial.println(
    " G"
  );


  Serial.print(
    "PEAK          : "
  );

  Serial.print(
    vibrationPeak,
    3
  );

  Serial.println(
    " G"
  );


  Serial.print(
    "HEALTH INDEX  : "
  );

  Serial.print(
    healthIndex
  );

  Serial.println(
    "%"
  );


  Serial.print(
    "TEMPERATURE   : "
  );

  Serial.print(
    temperature,
    1
  );

  Serial.println(
    " C"
  );


  Serial.print(
    "HUMIDITY      : "
  );

  Serial.print(
    humidity,
    1
  );

  Serial.println(
    " %"
  );


  Serial.print(
    "UPTIME        : "
  );

  Serial.println(
    getUptime()
  );


  Serial.print(
    "WIFI CLIENTS  : "
  );

  Serial.println(
    WiFi.softAPgetStationNum()
  );


  Serial.println(
    "===================================================="
  );
}


// ============================================================
// WEB DASHBOARD
// ============================================================

const char INDEX_HTML[] PROGMEM = R"rawliteral(

<!DOCTYPE html>

<html>

<head>

<meta charset="UTF-8">

<meta
  name="viewport"
  content="width=device-width, initial-scale=1"
>

<title>
BeltGuard | BG-01
</title>


<style>

/* =========================================================
   BASE
========================================================= */

:root
{
  --bg:#070c12;

  --panel:#101821;

  --panel2:#17232f;

  --line:#293847;

  --text:#edf3f7;

  --muted:#8da0b2;

  --good:#31d583;

  --warn:#f3c84b;

  --bad:#ff5965;

  --blue:#4ba4ff;
}


*
{
  box-sizing:border-box;
}


body
{
  margin:0;

  background:
    radial-gradient(
      circle at 80% 0%,
      #152432,
      #070c12 45%
    );

  color:var(--text);

  font-family:
    Arial,
    Helvetica,
    sans-serif;
}


.wrap
{
  width:100%;

  max-width:1250px;

  margin:auto;

  padding:22px;
}


/* =========================================================
   TOP BAR
========================================================= */

.top
{
  display:flex;

  align-items:center;

  justify-content:space-between;

  gap:20px;

  margin-bottom:17px;
}


.brand
{
  display:flex;

  align-items:center;

  gap:13px;
}


.logo
{
  width:50px;

  height:50px;

  border:
    2px solid
    var(--blue);

  border-radius:12px;

  display:grid;

  place-items:center;

  font-weight:900;

  font-size:15px;

  letter-spacing:1px;

  box-shadow:
    0 0 25px
    rgba(75,164,255,.12);
}


.title
{
  font-size:27px;

  font-weight:900;
}


.subtitle
{
  color:var(--muted);

  font-size:12px;

  margin-top:4px;
}


.live
{
  color:var(--good);

  font-weight:800;

  font-size:12px;

  white-space:nowrap;
}


.dot
{
  display:inline-block;

  width:9px;

  height:9px;

  border-radius:50%;

  background:currentColor;

  box-shadow:
    0 0 10px currentColor;

  margin-right:6px;
}


/* =========================================================
   PANELS
========================================================= */

.panel
{
  background:
    rgba(16,24,33,.96);

  border:
    1px solid
    var(--line);

  border-radius:16px;

  padding:18px;

  box-shadow:
    0 12px 30px
    rgba(0,0,0,.10);
}


.label
{
  color:var(--muted);

  font-size:10px;

  font-weight:900;

  letter-spacing:1.5px;
}


/* =========================================================
   HERO
========================================================= */

.hero
{
  display:flex;

  justify-content:space-between;

  align-items:center;

  gap:30px;

  flex-wrap:wrap;

  margin-bottom:14px;
}


.conditionText
{
  font-size:42px;

  font-weight:900;

  margin-top:5px;
}


.normal
{
  color:var(--good);
}


.warning
{
  color:var(--warn);
}


.critical
{
  color:var(--bad);

  text-shadow:
    0 0 18px
    rgba(255,89,101,.25);
}


.health
{
  width:300px;

  max-width:100%;
}


.healthTop
{
  display:flex;

  align-items:center;

  justify-content:space-between;

  margin-bottom:8px;
}


.healthValue
{
  font-size:25px;

  font-weight:900;
}


.healthBar
{
  width:100%;

  height:12px;

  background:#25323e;

  border-radius:20px;

  overflow:hidden;
}


.healthFill
{
  height:100%;

  width:100%;

  background:var(--good);

  transition:
    width .3s ease,
    background .3s ease;
}


/* =========================================================
   METRICS
========================================================= */

.metrics
{
  display:grid;

  grid-template-columns:
    repeat(3,1fr);

  gap:13px;

  margin-bottom:13px;
}


.metric
{
  min-height:110px;
}


.metricValue
{
  font-size:31px;

  font-weight:900;

  margin-top:8px;
}


.unit
{
  font-size:13px;

  color:var(--muted);

  font-weight:normal;
}


.metricSub
{
  color:var(--muted);

  font-size:10px;

  margin-top:5px;
}


/* =========================================================
   CHART
========================================================= */

.chartPanel
{
  margin-bottom:13px;
}


.chartHeader
{
  display:flex;

  align-items:center;

  justify-content:space-between;

  margin-bottom:10px;
}


canvas
{
  display:block;

  width:100%;

  height:280px;

  background:#0b1219;

  border:
    1px solid
    var(--line);

  border-radius:11px;
}


/* =========================================================
   TWO COLUMN
========================================================= */

.two
{
  display:grid;

  grid-template-columns:
    1fr 1fr;

  gap:13px;

  margin-bottom:13px;
}


/* =========================================================
   ENVIRONMENT
========================================================= */

.env
{
  display:grid;

  grid-template-columns:
    1fr 1fr;

  gap:20px;

  margin-top:14px;
}


.big
{
  font-size:28px;

  font-weight:900;

  margin-top:5px;
}


/* =========================================================
   PIPELINE
========================================================= */

.pipeline
{
  display:flex;

  align-items:center;

  flex-wrap:wrap;

  gap:7px;

  margin-top:15px;
}


.node
{
  background:var(--panel2);

  border:
    1px solid
    var(--line);

  border-radius:8px;

  padding:9px 10px;

  font-size:10px;

  font-weight:900;

  white-space:nowrap;
}


.arrow
{
  color:var(--muted);

  font-weight:bold;
}


/* =========================================================
   INFORMATION GRID
========================================================= */

.infoGrid
{
  display:grid;

  grid-template-columns:
    repeat(2,1fr);

  gap:9px;

  margin-top:13px;
}


.info
{
  background:var(--panel2);

  border:
    1px solid
    var(--line);

  border-radius:9px;

  padding:10px;
}


.infoLabel
{
  color:var(--muted);

  font-size:9px;

  letter-spacing:1px;

  font-weight:900;
}


.infoValue
{
  margin-top:5px;

  font-size:13px;

  font-weight:800;
}


/* =========================================================
   EVENTS
========================================================= */

.events
{
  margin-top:4px;
}


.event
{
  display:flex;

  align-items:center;

  padding:11px 0;

  border-bottom:
    1px solid
    var(--line);

  font-size:12px;
}


.event:last-child
{
  border-bottom:0;
}


.eventTime
{
  color:var(--muted);

  font-size:10px;

  min-width:70px;
}


.eventText
{
  font-weight:600;
}


/* =========================================================
   FOOTER
========================================================= */

footer
{
  text-align:center;

  color:var(--muted);

  font-size:10px;

  margin-top:17px;

  padding-bottom:5px;
}


/* =========================================================
   RESPONSIVE
========================================================= */

@media(max-width:820px)
{
  .metrics
  {
    grid-template-columns:
      1fr;
  }


  .two
  {
    grid-template-columns:
      1fr;
  }


  .conditionText
  {
    font-size:34px;
  }


  .wrap
  {
    padding:13px;
  }


  .top
  {
    align-items:flex-start;
  }


  .live
  {
    font-size:10px;
  }
}

</style>

</head>


<body>

<div class="wrap">


<!-- =======================================================
     HEADER
======================================================== -->

<div class="top">

  <div class="brand">

    <div class="logo">
      BG
    </div>


    <div>

      <div class="title">
        BeltGuard
      </div>


      <div class="subtitle">
        Intelligent Conveyor Belt Condition Monitoring
      </div>

    </div>

  </div>


  <div
    class="live"
    id="live"
  >

    <span class="dot"></span>

    LIVE • EDGE NODE BG-01

  </div>

</div>



<!-- =======================================================
     CONDITION HERO
======================================================== -->

<div class="panel hero">


  <div>

    <div class="label">
      CURRENT BELT CONDITION
    </div>


    <div
      id="condition"
      class="conditionText normal"
    >
      NORMAL
    </div>


    <div class="subtitle">
      Real-time mechanical condition assessment
    </div>

  </div>


  <div class="health">

    <div class="healthTop">

      <div class="label">
        PROTOTYPE HEALTH INDEX
      </div>


      <div
        id="health"
        class="healthValue"
      >
        100%
      </div>

    </div>


    <div class="healthBar">

      <div
        id="healthFill"
        class="healthFill"
      ></div>

    </div>

  </div>


</div>



<!-- =======================================================
     METRICS
======================================================== -->

<div class="metrics">


  <div class="panel metric">

    <div class="label">
      LIVE VIBRATION
    </div>


    <div class="metricValue">

      <span id="vib">
        0.000
      </span>

      <span class="unit">
        G
      </span>

    </div>


    <div class="metricSub">
      Smoothed acceleration deviation
    </div>

  </div>



  <div class="panel metric">

    <div class="label">
      RMS VIBRATION
    </div>


    <div class="metricValue">

      <span id="rms">
        0.000
      </span>

      <span class="unit">
        G
      </span>

    </div>


    <div class="metricSub">
      100-sample condition window
    </div>

  </div>



  <div class="panel metric">

    <div class="label">
      PEAK — LAST 1 SECOND
    </div>


    <div class="metricValue">

      <span id="peak">
        0.000
      </span>

      <span class="unit">
        G
      </span>

    </div>


    <div class="metricSub">
      Highest observed vibration
    </div>

  </div>


</div>



<!-- =======================================================
     GRAPH
======================================================== -->

<div class="panel chartPanel">


  <div class="chartHeader">

    <div class="label">
      LIVE VIBRATION TREND
    </div>


    <div
      class="subtitle"
      id="chartStatus"
    >
      Rolling monitoring window
    </div>

  </div>


  <canvas id="chart"></canvas>


</div>



<!-- =======================================================
     ENVIRONMENT + PIPELINE
======================================================== -->

<div class="two">


  <div class="panel">

    <div class="label">
      ENVIRONMENT CONTEXT
    </div>


    <div class="env">


      <div>

        <div class="subtitle">
          TEMPERATURE
        </div>


        <div class="big">

          <span id="temp">
            0.0
          </span>

          <span class="unit">
            °C
          </span>

        </div>

      </div>


      <div>

        <div class="subtitle">
          HUMIDITY
        </div>


        <div class="big">

          <span id="hum">
            0.0
          </span>

          <span class="unit">
            %
          </span>

        </div>

      </div>


    </div>

  </div>



  <div class="panel">

    <div class="label">
      BELTGUARD EDGE PIPELINE
    </div>


    <div class="pipeline">


      <div class="node">
        MPU6050
      </div>


      <div class="arrow">
        →
      </div>


      <div class="node">
        ESP32
      </div>


      <div class="arrow">
        →
      </div>


      <div class="node">
        EDGE PROCESSING
      </div>


      <div class="arrow">
        →
      </div>


      <div class="node">
        WI-FI
      </div>


      <div class="arrow">
        →
      </div>


      <div class="node">
        DASHBOARD
      </div>


    </div>

  </div>


</div>



<!-- =======================================================
     SYSTEM INFORMATION
======================================================== -->

<div class="panel">


  <div class="label">
    EDGE NODE STATUS
  </div>


  <div class="infoGrid">


    <div class="info">

      <div class="infoLabel">
        NODE ID
      </div>

      <div class="infoValue">
        BG-01
      </div>

    </div>


    <div class="info">

      <div class="infoLabel">
        SAMPLING
      </div>

      <div class="infoValue">
        ~200 Hz
      </div>

    </div>


    <div class="info">

      <div class="infoLabel">
        RMS WINDOW
      </div>

      <div class="infoValue">
        100 Samples
      </div>

    </div>


    <div class="info">

      <div class="infoLabel">
        NETWORK
      </div>

      <div class="infoValue">
        BeltGuard AP
      </div>

    </div>


    <div class="info">

      <div class="infoLabel">
        IP ADDRESS
      </div>

      <div class="infoValue">
        192.168.4.1
      </div>

    </div>


    <div class="info">

      <div class="infoLabel">
        CONNECTED DEVICES
      </div>

      <div
        class="infoValue"
        id="clients"
      >
        0
      </div>

    </div>


    <div class="info">

      <div class="infoLabel">
        UPTIME
      </div>

      <div
        class="infoValue"
        id="uptime"
      >
        00:00:00
      </div>

    </div>


    <div class="info">

      <div class="infoLabel">
        MONITORING MODE
      </div>

      <div class="infoValue">
        REAL-TIME
      </div>

    </div>


  </div>

</div>



<!-- =======================================================
     EVENTS
======================================================== -->

<div
  class="panel"
  style="margin-top:13px"
>


  <div class="label">
    RECENT SYSTEM EVENTS
  </div>


  <div class="events">


    <div class="event">

      <span
        class="eventTime"
        id="time1"
      >
        --:--:--
      </span>


      <span
        class="eventText"
        id="event1"
      >
        Monitoring active
      </span>

    </div>



    <div class="event">

      <span
        class="eventTime"
        id="time2"
      >
        --:--:--
      </span>


      <span
        class="eventText"
        id="event2"
      >
        Sensors online
      </span>

    </div>



    <div class="event">

      <span
        class="eventTime"
        id="time3"
      >
        --:--:--
      </span>


      <span
        class="eventText"
        id="event3"
      >
        System initialized
      </span>

    </div>


  </div>

</div>



<footer>

  BELTGUARD • BG-01 •
  LOCAL EDGE CONDITION MONITORING •
  PROTOTYPE

</footer>


</div>



<script>


// ==========================================================
// CHART
// ==========================================================

const canvas =
  document.getElementById(
    "chart"
  );


const ctx =
  canvas.getContext(
    "2d"
  );


const MAX_SAMPLES = 120;


let samples = [];


// ==========================================================
// CANVAS RESIZE
// ==========================================================

function resizeCanvas()
{
  const ratio =
    window.devicePixelRatio || 1;


  canvas.width =
    canvas.clientWidth *
    ratio;


  canvas.height =
    canvas.clientHeight *
    ratio;


  ctx.setTransform(
    ratio,
    0,
    0,
    ratio,
    0,
    0
  );


  drawChart();
}


window.addEventListener(
  "resize",
  resizeCanvas
);


resizeCanvas();


// ==========================================================
// THRESHOLD
// ==========================================================

function drawThreshold(
  value,
  label,
  max,
  width,
  height,
  pad
)
{

  if(value >= max)
  {
    return;
  }


  const plotHeight =
    height -
    2 * pad;


  const y =
    pad +
    plotHeight *
    (
      1 -
      value / max
    );


  ctx.save();


  ctx.setLineDash(
    [6,5]
  );


  ctx.strokeStyle =
    "#465566";


  ctx.lineWidth = 1;


  ctx.beginPath();


  ctx.moveTo(
    pad,
    y
  );


  ctx.lineTo(
    width - pad,
    y
  );


  ctx.stroke();


  ctx.restore();


  ctx.fillStyle =
    "#8da0b2";


  ctx.font =
    "10px Arial";


  ctx.fillText(
    label,
    pad + 6,
    y - 5
  );
}


// ==========================================================
// DRAW CHART
// ==========================================================

function drawChart()
{
  const width =
    canvas.clientWidth;


  const height =
    canvas.clientHeight;


  const pad = 27;


  let max =
    0.20;


  for(
    let i = 0;
    i < samples.length;
    i++
  )
  {
    if(
      samples[i] >
      max
    )
    {
      max =
        samples[i];
    }
  }


  max *= 1.15;


  ctx.clearRect(
    0,
    0,
    width,
    height
  );


  // --------------------------------------------------------
  // GRID
  // --------------------------------------------------------

  ctx.strokeStyle =
    "#293847";


  ctx.lineWidth = 1;


  for(
    let i = 0;
    i <= 4;
    i++
  )
  {

    const y =
      pad +
      (
        height -
        2 * pad
      ) *
      i / 4;


    ctx.beginPath();


    ctx.moveTo(
      pad,
      y
    );


    ctx.lineTo(
      width - pad,
      y
    );


    ctx.stroke();


    ctx.fillStyle =
      "#6e8295";


    ctx.font =
      "10px Arial";


    ctx.fillText(
      (
        max *
        (1 - i / 4)
      ).toFixed(2),
      4,
      y + 3
    );
  }


  // --------------------------------------------------------
  // THRESHOLDS
  // --------------------------------------------------------

  drawThreshold(
    0.08,
    "WARNING",
    max,
    width,
    height,
    pad
  );


  drawThreshold(
    0.16,
    "CRITICAL",
    max,
    width,
    height,
    pad
  );


  // --------------------------------------------------------
  // LINE
  // --------------------------------------------------------

  if(
    samples.length < 2
  )
  {
    return;
  }


  ctx.strokeStyle =
    "#4ba4ff";


  ctx.lineWidth = 2;


  ctx.beginPath();


  for(
    let i = 0;
    i < samples.length;
    i++
  )
  {

    const x =
      pad +
      (
        i /
        (MAX_SAMPLES - 1)
      ) *
      (
        width -
        2 * pad
      );


    const y =
      pad +
      (
        height -
        2 * pad
      ) *
      (
        1 -
        samples[i] /
        max
      );


    if(i === 0)
    {
      ctx.moveTo(
        x,
        y
      );
    }

    else
    {
      ctx.lineTo(
        x,
        y
      );
    }
  }


  ctx.stroke();


  // --------------------------------------------------------
  // CURRENT POINT
  // --------------------------------------------------------

  const lastIndex =
    samples.length - 1;


  const lastValue =
    samples[lastIndex];


  const lx =
    pad +
    (
      lastIndex /
      (MAX_SAMPLES - 1)
    ) *
    (
      width -
      2 * pad
    );


  const ly =
    pad +
    (
      height -
      2 * pad
    ) *
    (
      1 -
      lastValue /
      max
    );


  ctx.fillStyle =
    "#4ba4ff";


  ctx.beginPath();


  ctx.arc(
    lx,
    ly,
    3.5,
    0,
    Math.PI * 2
  );


  ctx.fill();
}


// ==========================================================
// UPDATE DASHBOARD
// ==========================================================

async function updateDashboard()
{

  try
  {

    const response =
      await fetch(
        "/data?" +
        Date.now()
      );


    const d =
      await response.json();


    // ------------------------------------------------------
    // SENSOR VALUES
    // ------------------------------------------------------

    document.getElementById(
      "vib"
    ).textContent =
      d.vibration.toFixed(3);


    document.getElementById(
      "rms"
    ).textContent =
      d.rms.toFixed(3);


    document.getElementById(
      "peak"
    ).textContent =
      d.peak.toFixed(3);


    document.getElementById(
      "temp"
    ).textContent =
      d.temperature.toFixed(1);


    document.getElementById(
      "hum"
    ).textContent =
      d.humidity.toFixed(1);


    // ------------------------------------------------------
    // CONDITION
    // ------------------------------------------------------

    const condition =
      document.getElementById(
        "condition"
      );


    condition.textContent =
      d.condition;


    condition.className =
      "conditionText " +
      d.severity;


    // ------------------------------------------------------
    // HEALTH
    // ------------------------------------------------------

    document.getElementById(
      "health"
    ).textContent =
      d.health + "%";


    const healthFill =
      document.getElementById(
        "healthFill"
      );


    healthFill.style.width =
      d.health + "%";


    if(
      d.severity ===
      "critical"
    )
    {
      healthFill.style.background =
        "#ff5965";
    }

    else if(
      d.severity ===
      "warning"
    )
    {
      healthFill.style.background =
        "#f3c84b";
    }

    else
    {
      healthFill.style.background =
        "#31d583";
    }


    // ------------------------------------------------------
    // CHART
    // ------------------------------------------------------

    samples.push(
      d.vibration
    );


    if(
      samples.length >
      MAX_SAMPLES
    )
    {
      samples.shift();
    }


    drawChart();


    // ------------------------------------------------------
    // EVENTS
    // ------------------------------------------------------

    document.getElementById(
      "event1"
    ).textContent =
      d.event1;


    document.getElementById(
      "event2"
    ).textContent =
      d.event2;


    document.getElementById(
      "event3"
    ).textContent =
      d.event3;


    document.getElementById(
      "time1"
    ).textContent =
      d.event1Time;


    document.getElementById(
      "time2"
    ).textContent =
      d.event2Time;


    document.getElementById(
      "time3"
    ).textContent =
      d.event3Time;


    // ------------------------------------------------------
    // NODE INFORMATION
    // ------------------------------------------------------

    document.getElementById(
      "clients"
    ).textContent =
      d.clients;


    document.getElementById(
      "uptime"
    ).textContent =
      d.uptime;


    // ------------------------------------------------------
    // LIVE INDICATOR
    // ------------------------------------------------------

    document.getElementById(
      "live"
    ).innerHTML =
      '<span class="dot"></span>' +
      'LIVE • EDGE NODE BG-01';

  }


  catch(error)
  {

    document.getElementById(
      "live"
    ).innerHTML =
      '<span class="dot" ' +
      'style="color:#ff5965"></span>' +
      'CONNECTION LOST';

  }

}


// ==========================================================
// START DASHBOARD
// ==========================================================

setInterval(
  updateDashboard,
  250
);


updateDashboard();


</script>

</body>

</html>

)rawliteral";


// ============================================================
// WEB ROOT
// ============================================================

void handleRoot()
{
  server.send_P(
    200,
    "text/html",
    INDEX_HTML
  );
}


// ============================================================
// WEB DATA
// ============================================================

void handleData()
{
  String json = "{";


  json +=
    "\"vibration\":" +
    String(vibration,4) +
    ",";


  json +=
    "\"rms\":" +
    String(vibrationRMS,4) +
    ",";


  json +=
    "\"peak\":" +
    String(vibrationPeak,4) +
    ",";


  json +=
    "\"temperature\":" +
    String(temperature,1) +
    ",";


  json +=
    "\"humidity\":" +
    String(humidity,1) +
    ",";


  json +=
    "\"condition\":\"" +
    condition +
    "\",";


  json +=
    "\"severity\":\"" +
    severityClass +
    "\",";


  json +=
    "\"health\":" +
    String(healthIndex) +
    ",";


  json +=
    "\"event1\":\"" +
    event1 +
    "\",";


  json +=
    "\"event2\":\"" +
    event2 +
    "\",";


  json +=
    "\"event3\":\"" +
    event3 +
    "\",";


  json +=
    "\"event1Time\":\"" +
    getEventTime(event1Time) +
    "\",";


  json +=
    "\"event2Time\":\"" +
    getEventTime(event2Time) +
    "\",";


  json +=
    "\"event3Time\":\"" +
    getEventTime(event3Time) +
    "\",";


  json +=
    "\"uptime\":\"" +
    getUptime() +
    "\",";


  json +=
    "\"clients\":" +
    String(
      WiFi.softAPgetStationNum()
    );


  json += "}";


  server.send(
    200,
    "application/json",
    json
  );
}


// ============================================================
// SETUP
// ============================================================

void setup()
{
  // ----------------------------------------------------------
  // SERIAL
  // ----------------------------------------------------------

  Serial.begin(
    115200
  );


  delay(500);


  bootTime =
    millis();


  // ----------------------------------------------------------
  // I2C
  // ----------------------------------------------------------

  Wire.begin(
    SDA_PIN,
    SCL_PIN
  );


  // ----------------------------------------------------------
  // MPU6050
  // ----------------------------------------------------------

  mpu.initialize();


  mpu.setFullScaleAccelRange(
    MPU6050_ACCEL_FS_4
  );


  if(
    mpu.testConnection()
  )
  {
    Serial.println(
      "MPU6050: CONNECTED"
    );
  }

  else
  {
    Serial.println(
      "MPU6050: CONNECTION FAILED"
    );
  }


  // ----------------------------------------------------------
  // DHT22
  // ----------------------------------------------------------

  dht.begin();


  // ----------------------------------------------------------
  // OLED
  // ----------------------------------------------------------

  if(
    display.begin(
      SSD1306_SWITCHCAPVCC,
      OLED_ADDR
    )
  )
  {

    display.clearDisplay();


    display.setTextColor(
      SSD1306_WHITE
    );


    display.setTextSize(
      1
    );


    display.setCursor(
      0,
      12
    );


    display.println(
      "BELTGUARD"
    );


    display.setCursor(
      0,
      25
    );


    display.println(
      "EDGE NODE BG-01"
    );


    display.setCursor(
      0,
      39
    );


    display.println(
      "INITIALIZING..."
    );


    display.setCursor(
      0,
      52
    );


    display.println(
      "PLEASE WAIT"
    );


    display.display();


    delay(1000);
  }

  else
  {
    Serial.println(
      "OLED: INITIALIZATION FAILED"
    );
  }


  // ----------------------------------------------------------
  // RMS BUFFER
  // ----------------------------------------------------------

  for(
    int i = 0;
    i < RMS_SAMPLES;
    i++
  )
  {
    rmsBuffer[i] =
      0.0;
  }


  // ----------------------------------------------------------
  // WIFI ACCESS POINT
  // ----------------------------------------------------------

  WiFi.mode(
    WIFI_AP
  );


  WiFi.softAP(
    AP_SSID,
    AP_PASS
  );


  localIP =
    WiFi.softAPIP();


  // ----------------------------------------------------------
  // WEB SERVER
  // ----------------------------------------------------------

  server.on(
    "/",
    handleRoot
  );


  server.on(
    "/data",
    handleData
  );


  server.begin();


  // ----------------------------------------------------------
  // INITIAL EVENTS
  // ----------------------------------------------------------

  event1Time =
    millis();


  event2Time =
    millis();


  event3Time =
    millis();


  // ----------------------------------------------------------
  // SERIAL STARTUP INFO
  // ----------------------------------------------------------

  Serial.println();

  Serial.println(
    "===================================================="
  );

  Serial.println(
    "                 BELTGUARD READY"
  );

  Serial.println(
    "===================================================="
  );


  Serial.print(
    "Wi-Fi SSID : "
  );

  Serial.println(
    AP_SSID
  );


  Serial.print(
    "Password   : "
  );

  Serial.println(
    AP_PASS
  );


  Serial.print(
    "IP Address : "
  );

  Serial.println(
    localIP
  );


  Serial.println();


  Serial.println(
    "Dashboard:"
  );


  Serial.println(
    "http://192.168.4.1/"
  );


  Serial.println();


  Serial.println(
    "Architecture:"
  );


  Serial.println(
    "MPU6050 -> ESP32 -> Edge Processing"
  );


  Serial.println(
    "DHT22   -> ESP32"
  );


  Serial.println(
    "ESP32   -> Wi-Fi -> Dashboard"
  );


  Serial.println(
    "ESP32   -> OLED"
  );


  Serial.println(
    "===================================================="
  );


  // ----------------------------------------------------------
  // TIMERS
  // ----------------------------------------------------------

  lastSample =
    millis();


  lastDHT =
    millis();


  lastOLED =
    millis();


  lastSerial =
    millis();


  lastPeakReset =
    millis();


  lastOLEDPageChange =
    millis();
}


// ============================================================
// LOOP
// ============================================================

void loop()
{

  // ----------------------------------------------------------
  // WEB SERVER
  // ----------------------------------------------------------

  server.handleClient();


  unsigned long now =
    millis();


  // ----------------------------------------------------------
  // MPU6050
  // ~200 Hz
  // ----------------------------------------------------------

  if(
    now -
    lastSample
    >=
    SAMPLE_INTERVAL
  )
  {

    lastSample =
      now;


    updateVibration();
  }


  // ----------------------------------------------------------
  // DHT22
  // EVERY 2 SEC
  // ----------------------------------------------------------

  if(
    now -
    lastDHT
    >=
    DHT_INTERVAL
  )
  {

    lastDHT =
      now;


    updateEnvironment();
  }


  // ----------------------------------------------------------
  // OLED
  // ----------------------------------------------------------

  if(
    now -
    lastOLED
    >=
    OLED_INTERVAL
  )
  {

    lastOLED =
      now;


    updateOLED();
  }


  // ----------------------------------------------------------
  // SERIAL
  // ----------------------------------------------------------

  if(
    now -
    lastSerial
    >=
    SERIAL_INTERVAL
  )
  {

    lastSerial =
      now;


    serialDebug();
  }


  // ----------------------------------------------------------
  // PEAK RESET
  // ----------------------------------------------------------

  if(
    now -
    lastPeakReset
    >=
    PEAK_INTERVAL
  )
  {

    lastPeakReset =
      now;


    vibrationPeak =
      0.0;
  }

}