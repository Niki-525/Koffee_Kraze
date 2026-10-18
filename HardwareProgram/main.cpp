#include <Arduino.h>
#include "WiFi.h"
#include "WebSocketsServer.h"
#include "Keypad.h"
#include "ArduinoJson.h"

// put function declarations here:

const char* ssid="esp32-game-access-point";
const char* password="123456789";

// Component pin setup
// Pins (joystick on ADC1 pins, since ADC2 doesn't work while WiFi is on)
// const int JOY_X = 34, JOY_Y = 35, JOY_BTN = 23;
const int LED_PINS[2] = {18, 19};
const int BUZZER_PIN = 21; 

// key pad setup
const byte ROWS = 4;
const byte COLS = 4;
char hexaKeys[ROWS][COLS] = {
  {'1', '2', '3', 'A'},
  {'4', '5', '6', 'B'},
  {'7', '8', '9', 'C'},
  {'C', '0', 'E', 'D'}
};
byte rowPins[ROWS] = {32, 33, 25, 26};
byte colPins[COLS] = {27, 14, 12, 13};

Keypad numPad = Keypad(makeKeymap(hexaKeys), rowPins, colPins, ROWS, COLS);

// Component variables
// float lastX = 0, lastY = 0;
// bool lastBtn = false;
// unsigned long lastJoySend = 0;
unsigned long buzzUntil = 0;

WebSocketsServer ws(81);

//Function declarations
void sendJson(JsonDocument& doc);
void onEvent(uint8_t num, WStype_t type, uint8_t* payload, size_t len);


// Good pins for buttons (digital, with INPUT_PULLUP): 13, 14, 25, 26, 27, 32, 33, and also 16, 17, 18, 19, 21, 22, 23.
// Good pins for the joystick’s analog axes: 32-39 (ADC1), as in the earlier code with 34 and 35.

void setup() {
  Serial.begin(115200);

  // Setup pins
  for (int p: LED_PINS) pinMode(p, OUTPUT);
  pinMode(BUZZER_PIN, OUTPUT);
  // pinMode(JOY_BTN, INPUT_PULLUP);
  Serial.println("Keypad initialized.");

  WiFi.softAP(ssid, password);
  Serial.println("Access point has been set up:");
  Serial.println(WiFi.softAPIP());

  ws.begin();
  ws.onEvent(onEvent);
}

void loop() {
  ws.loop();

  // Keypad
  char key = numPad.getKey();
  if (key) {
    Serial.print("Key pressed: ");
    Serial.println(key);

    JsonDocument doc;
    doc["type"] = "key";
    doc["key"] = String(key);
    sendJson(doc);
  }

  // // Joystick 
  // if (millis() - lastJoySend >= 50){
  //   float x = readAxis(JOY_X), y = readAxis(JOY_Y);
  //   if (fabs(x - lastX) > 0.05f || fabs(y - lastY) > 0.05f){
  //     JsonDocument doc;
  //     doc["type"] = "joy";
  //     doc["x"] = round(x*100) / 100.0;
  //     doc["y"] = round(y*100) / 100.0;
  //     sendJson(doc);
  //     lastX = x; lastY = y;
  //   }
  //   lastJoySend = millis();
  // }

  // // Joystick button press
  // bool btn = digitalRead(JOY_BTN) == LOW;
  // if (btn != lastBtn){
  //   JsonDocument doc; 
  //   doc["type"] = "btn";
  //   doc["pressed"] = btn;
  //   sendJson(doc);
  //   lastBtn = btn;
  // }

  // Buzzer
  if (buzzUntil && millis() > buzzUntil) {
    digitalWrite(BUZZER_PIN, LOW);
    noTone(BUZZER_PIN);
    buzzUntil = 0;
  }
}

void sendJson(JsonDocument& doc){
  String out;
  serializeJson(doc, out);
  uint32_t t = millis();
  ws.broadcastTXT(out);
  Serial.printf("broadcast took %lu ms, clients=%u\n", millis() - t, ws.connectedClients());
}

// float readAxis(int pin){
//   float v = (analogRead(pin) - 2048) /2048.0f; // -1..1
//   return fabs(v) < 0.1f ? 0 : v;               // accidental bump check
// }

void onEvent(uint8_t num, WStype_t type, uint8_t* payload, size_t len) {
  if (type == WStype_CONNECTED) {
    Serial.printf("Client %u connected\n", num);
  }
  if (type == WStype_DISCONNECTED) {
    Serial.printf("Client %u disconnected\n", num);
  }

  // Handle incoming requests from Unity for output
  if (type == WStype_TEXT){
    JsonDocument doc;
    if (deserializeJson(doc, payload, len)) return; 

    const char* t = doc["type"];
    if (!t) return;

    if (strcmp(t, "leds") == 0){
      JsonArray arr = doc["leds"];
      for (int i = 0; i < 2 && i < (int)arr.size(); i++){
        digitalWrite(LED_PINS[i], arr[i].as<int>() ? HIGH: LOW);
      }
    }
    else if (strcmp(t, "buzz") == 0){
      tone(BUZZER_PIN, 1000);
      buzzUntil = millis() + doc["ms"].as<int>();
    }
  }
}
